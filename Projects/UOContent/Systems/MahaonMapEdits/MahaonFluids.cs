using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Logging;
using Server.Targeting;

namespace Server.Systems.MahaonMapEdits;

public enum FluidKind : byte
{
    Water,
    Lava
}

/// <summary>
///     Текучая жидкость: разливается по рельефу во времени, а не мгновенно.
///
///     ---- Чем отличается от MahaonWaterFlow ----
///
///     Тот считает, где вода СТОИТ: уровень один, тайлы ниже уровня залиты, выше — нет. Это
///     верно и мгновенно, и для прудов этого довольно: канава наполняется ровно с той
///     скоростью, с какой её копают, то есть по взмаху лопаты за тайл.
///
///     Здесь другое — жидкость, которой никто не копал русла. Она идёт сама, по такту, и
///     растекается там, где её не ждали. Воде это нужно редко (прорвало запруду), лаве —
///     всегда.
///
///     ---- Почему лава не вода ----
///
///     Воду в ультиме делает водой флаг Wet. Лаву делает лавой НЕ течение, а урон: она жжёт
///     того, кто в ней стоит. И второе: лава остывает. Постояв на месте, она превращается в
///     камень — и тем самым сама себе строит русло, потому что остывший край больше не
///     течёт, а поток идёт дальше по незастывшему. Это единственная причина, по которой
///     здесь вообще нужен такт; всё остальное в системе правок карты обходится событиями.
///
///     ---- Что считается, а что нет ----
///
///     Объёма у жидкости нет: на тайл одна высота земли и одна статика жидкости сверху.
///     Поэтому «давления», «уровня в колонне» и водопадов тут не будет — будет растекание по
///     высотам с убывающей силой. Для вида этого достаточно, а честная симуляция объёма
///     требует воксельного мира НА СЕРВЕРЕ, то есть другого сервера.
/// </summary>
public static class MahaonFluids
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(MahaonFluids));

    private const string Setting = "fluids.enabled";

    /// <summary>Выключатель. Погаснет — жидкости замрут там, где стоят, и перестанут течь.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Длина такта.</summary>
    public static TimeSpan Step { get; set; } = TimeSpan.FromSeconds(1.0);

    /// <summary>Сколько клеток обрабатываем за такт. Предохранитель от разлива на пол карты.</summary>
    private const int Budget = 96;

    /// <summary>Сколько тактов лава стоит неподвижно, прежде чем остынет в камень.</summary>
    public const int CoolSteps = 25;

    /// <summary>Сколько всего клеток жидкости согласны держать.</summary>
    private const int MaxCells = 4000;

    public const string LavaTag = "лава";
    public const string SpillTag = "пролив";

    /// <summary>Лава: анимированная, светит сама, жжёт стоящего. Высота ноль.</summary>
    private static readonly int[] LavaStatics =
    {
        0x12EE, 0x12EF, 0x12F0, 0x12F1, 0x12F2, 0x12F4, 0x12F5, 0x12F6, 0x12F7, 0x12F8,
        0x12F9, 0x12FA, 0x12FB, 0x12FC, 0x12FD, 0x12FE, 0x1300, 0x1301, 0x1302, 0x1303
    };

    /// <summary>Остывшая лава — проходимый каменный пол.</summary>
    private static readonly int[] StoneLand = { 0x0436, 0x0437, 0x0438, 0x0439, 0x043A, 0x043B };

    private sealed class Cell
    {
        public FluidKind Kind;
        public int Z;
        public int Strength;
        public int Still;
    }

    private static readonly Dictionary<(int MapId, int X, int Y), Cell> Cells = new();
    private static readonly Queue<(int MapId, int X, int Y)> Active = new();

    private static Persistence _persistence;
    private static Timer _timer;

    public static int Count => Cells.Count;

    public static void Configure()
    {
        _persistence = new Persistence();
        Enabled = ServerConfiguration.GetOrUpdateSetting(Setting, true);

        CommandSystem.Register("Lava", AccessLevel.GameMaster, Lava_OnCommand);
        CommandSystem.Register("Spill", AccessLevel.GameMaster, Spill_OnCommand);
        CommandSystem.Register("FluidStats", AccessLevel.GameMaster, FluidStats_OnCommand);
    }

    [CallPriority(90)]
    public static void Initialize()
    {
        // Всё, что лежало в сохранении, снова ставим в очередь: пусть дотечёт и остынет.
        foreach (var key in Cells.Keys)
        {
            Active.Enqueue(key);
        }

        _timer ??= Timer.DelayCall(Step, Step, Tick);
    }

    /// <summary>
    ///     Выливает жидкость на тайл. Сила — на сколько тайлов по ровному её хватит; вниз по
    ///     склону она течёт, силы не тратя.
    /// </summary>
    public static bool Spill(Map map, int x, int y, FluidKind kind, int strength)
    {
        if (map == null || map == Map.Internal || Cells.Count >= MaxCells)
        {
            return false;
        }

        return Place(map, x, y, kind, Math.Clamp(strength, 1, 64));
    }

    private static bool Place(Map map, int x, int y, FluidKind kind, int strength)
    {
        var key = (map.MapID, x, y);

        if (Cells.ContainsKey(key) || !MahaonExcavation.CanDigHere(map, x, y, out _))
        {
            return false;
        }

        var z = MahaonMapEdits.LandAt(map, x, y).Z;

        Cells[key] = new Cell { Kind = kind, Z = z, Strength = strength };
        Active.Enqueue(key);

        MahaonMapEdits.AddStatic(map, x, y, z, Graphic(kind), 0, Tag(kind));

        return true;
    }

    private static int Graphic(FluidKind kind) =>
        kind == FluidKind.Lava
            ? LavaStatics[Utility.Random(LavaStatics.Length)]
            : MahaonWaterFlow.RandomWaterStatic();

    private static string Tag(FluidKind kind) => kind == FluidKind.Lava ? LavaTag : SpillTag;

    private static void Tick()
    {
        if (!Enabled || Active.Count == 0)
        {
            return;
        }

        MahaonMapEdits.BeginBatch();

        try
        {
            var budget = Math.Min(Budget, Active.Count);

            for (var i = 0; i < budget; i++)
            {
                var key = Active.Dequeue();

                if (!Cells.TryGetValue(key, out var cell))
                {
                    continue;
                }

                var map = Map.Maps[key.MapId];

                if (map == null)
                {
                    Cells.Remove(key);
                    continue;
                }

                if (cell.Kind == FluidKind.Lava)
                {
                    Burn(map, key.X, key.Y);
                }

                if (Flow(map, key.X, key.Y, cell))
                {
                    cell.Still = 0;
                    Active.Enqueue(key); // ещё может потечь дальше
                    continue;
                }

                cell.Still++;

                if (cell.Kind == FluidKind.Lava && cell.Still >= CoolSteps)
                {
                    Cool(map, key, cell);
                    continue;
                }

                // Стоячая вода нас больше не занимает: пересчётом уровня занимается
                // MahaonWaterFlow, а тут ей делать нечего. Лава ждёт остывания.
                if (cell.Kind == FluidKind.Lava)
                {
                    Active.Enqueue(key);
                }
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Fluid tick failed; fluids disabled to keep the loop alive");
            Enabled = false;
        }
        finally
        {
            MahaonMapEdits.EndBatch();
        }
    }

    /// <summary>
    ///     Куда потечёт — решает рельеф, и решает ОДНОЗНАЧНО: поток идёт по самому крутому
    ///     спуску.
    ///
    ///     Сперва смотрим, какая из четырёх сторон ниже всего, и льёмся только туда. Если
    ///     все вокруг на одной высоте с нами — растекаемся по всем сразу, но за единицу
    ///     силы, то есть лужей с конечным радиусом.
    ///
    ///     Без этого правила направления не было вовсе: лава одинаково охотно шла во все
    ///     стороны и расползалась кляксой вместо ручья. Вверх не течёт никогда.
    /// </summary>
    private static bool Flow(Map map, int x, int y, Cell cell)
    {
        if (Cells.Count >= MaxCells)
        {
            return false;
        }

        Span<(int X, int Y)> around = [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)];

        var lowest = int.MaxValue;

        foreach (var (nx, ny) in around)
        {
            if (Open(map, nx, ny, out var groundZ) && groundZ <= cell.Z && groundZ < lowest)
            {
                lowest = groundZ;
            }
        }

        if (lowest == int.MaxValue)
        {
            return false; // некуда: кругом выше или уже занято
        }

        // Вниз по склону — бесплатно, по ровному — за единицу силы.
        var strength = lowest < cell.Z ? cell.Strength : cell.Strength - 1;

        if (strength <= 0)
        {
            return false;
        }

        var moved = false;

        foreach (var (nx, ny) in around)
        {
            if (!Open(map, nx, ny, out var groundZ) || groundZ != lowest)
            {
                continue;
            }

            if (Place(map, nx, ny, cell.Kind, strength))
            {
                moved = true;
            }
        }

        return moved;
    }

    /// <summary>Свободен ли сосед и какая под ним земля.</summary>
    private static bool Open(Map map, int x, int y, out int groundZ)
    {
        groundZ = 0;

        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height || Cells.ContainsKey((map.MapID, x, y)))
        {
            return false;
        }

        groundZ = MahaonMapEdits.LandAt(map, x, y).Z;

        return true;
    }

    /// <summary>Жжёт тех, кто стоит в лаве.</summary>
    private static void Burn(Map map, int x, int y)
    {
        foreach (var m in map.GetMobilesAt(new Point2D(x, y)))
        {
            if (!m.Alive || m.AccessLevel > AccessLevel.Player || m.Blessed || m.FireResistance >= 100)
            {
                continue;
            }

            AOS.Damage(m, Utility.RandomMinMax(6, 14), 0, 100, 0, 0, 0);
            m.PlaySound(0x208);
        }
    }

    /// <summary>
    ///     Остывание: лава уходит, земля становится камнем. Именно из этого лава сама себе
    ///     строит русло — остывший край держит поток, а он идёт дальше по незастывшему.
    /// </summary>
    private static void Cool(Map map, (int MapId, int X, int Y) key, Cell cell)
    {
        Cells.Remove(key);

        foreach (var s in MahaonMapEdits.StaticsAt(map, key.X, key.Y))
        {
            if (Array.IndexOf(LavaStatics, s.Graphic) >= 0)
            {
                MahaonMapEdits.RemoveStatics(map, key.X, key.Y, s.Graphic, LavaTag);
            }
        }

        MahaonMapEdits.SetLand(
            map, key.X, key.Y,
            StoneLand[Utility.Random(StoneLand.Length)],
            cell.Z,
            LavaTag
        );
    }

    // ---- Команды ---------------------------------------------------------------------------

    [Usage("Lava [сила]")]
    [Description("Выливает лаву: течёт по склону вниз, жжёт стоящих, остывает в камень.")]
    private static void Lava_OnCommand(CommandEventArgs e)
    {
        var strength = e.Length >= 1 ? e.GetInt32(0) : 8;

        e.Mobile.SendMessage(0x3B2, $"Куда вылить лаву (сила {strength})?");
        e.Mobile.Target = new SpillTarget(FluidKind.Lava, strength);
    }

    [Usage("Spill [сила]")]
    [Description("Выливает воду: течёт по склону вниз и остаётся.")]
    private static void Spill_OnCommand(CommandEventArgs e)
    {
        var strength = e.Length >= 1 ? e.GetInt32(0) : 8;

        e.Mobile.SendMessage(0x3B2, $"Куда вылить воду (сила {strength})?");
        e.Mobile.Target = new SpillTarget(FluidKind.Water, strength);
    }

    [Usage("FluidStats")]
    [Description("Сколько клеток жидкости живо и сколько из них в очереди.")]
    private static void FluidStats_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage(
            0x59,
            $"Жидкости: {(Enabled ? "включены" : "выключены")}, клеток {Cells.Count}, в очереди {Active.Count}."
        );
    }

    private class SpillTarget : Target
    {
        private readonly FluidKind _kind;
        private readonly int _strength;

        public SpillTarget(FluidKind kind, int strength) : base(16, true, TargetFlags.None)
        {
            _kind = kind;
            _strength = strength;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D p || from.Map == null)
            {
                return;
            }

            from.SendMessage(
                0x59,
                Spill(from.Map, p.X, p.Y, _kind, _strength)
                    ? "Полилось."
                    : "Сюда не льётся."
            );
        }
    }

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonFluids", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt(Cells.Count);

            foreach (var ((mapId, x, y), cell) in Cells)
            {
                writer.WriteEncodedInt(mapId);
                writer.WriteEncodedInt(x);
                writer.WriteEncodedInt(y);
                writer.Write((byte)cell.Kind);
                writer.WriteEncodedInt(cell.Z);
                writer.WriteEncodedInt(cell.Strength);
                writer.WriteEncodedInt(cell.Still);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version

            var count = reader.ReadEncodedInt();

            for (var i = 0; i < count; i++)
            {
                var mapId = reader.ReadEncodedInt();
                var x = reader.ReadEncodedInt();
                var y = reader.ReadEncodedInt();
                var kind = (FluidKind)reader.ReadByte();
                var z = reader.ReadEncodedInt();
                var strength = reader.ReadEncodedInt();
                var still = reader.ReadEncodedInt();

                Cells[(mapId, x, y)] = new Cell { Kind = kind, Z = z, Strength = strength, Still = still };
            }
        }
    }
}
