using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Systems.MahaonSeasons;

namespace Server.Systems.MahaonMapEdits;

/// <summary>
///     Что делает со следами копки само время.
///
///     ---- Зачем ----
///
///     Яма, вырытая лопатой, до сих пор оставалась ямой навсегда и выглядела одинаково в
///     первый день и на сотый. Между тем всё, что с ней происходит, — это обычное
///     зарастание: вскопанное обнажается до песка, засыпанное снова затягивается дёрном, а
///     дерево, под которым увели землю, остаётся пнём.
///
///     ---- Почему одна система, а не три ----
///
///     Земля, пни и ростки связаны: росток появляется ровно там, где дёрн вернулся И когда-
///     то стояло дерево. Разнеси это по трём системам — и «когда-то стояло дерево» придётся
///     хранить дважды.
///
///     ---- Чего тут НЕТ ----
///
///     Своего понятия времени. Сроки считаются в сезонах (SeasonSystem.SeasonLength), и
///     когда сезон у шарда станет длиннее или короче, зарастание поедет вместе с ним.
///     Ровно так же устроен рост саженца (MahaonPlantedSapling), и это не совпадение:
///     наш росток ему же и передаётся.
/// </summary>
public static class MahaonSuccession
{
    // ---- Графика -----------------------------------------------------------------------

    /// <summary>Свежевскопанное. Его же ставит лопата в момент удара.</summary>
    public static readonly int[] Furrows = { 0x0009, 0x000A, 0x000B, 0x000C };

    /// <summary>Голый песок: чем вскопанное становится, когда дёрн сходит.</summary>
    public static readonly int[] Bare = { 0x0016, 0x0017, 0x0018, 0x0019 };

    /// <summary>Пни. Непроходимы, как и деревья, — обойти их по-прежнему надо.</summary>
    public static readonly int[] Stumps = { 0x0E56, 0x0E57, 0x0E58, 0x0E59 };

    public static bool IsFurrow(int graphic) => Array.IndexOf(Furrows, graphic) >= 0;

    public static bool IsBare(int graphic) => Array.IndexOf(Bare, graphic) >= 0;

    public static bool IsStump(int graphic) => Array.IndexOf(Stumps, graphic) >= 0;

    public static int RandomFurrow() => Furrows[Utility.Random(Furrows.Length)];

    // ---- Сроки, в сезонах ----------------------------------------------------------------

    /// <summary>Вскопанное → голый песок.</summary>
    private const int SeasonsToBare = 1;

    /// <summary>Засыпанное до целины → как было.</summary>
    private const int SeasonsToGreen = 2;

    /// <summary>Затопленный пень → ничего.</summary>
    private const int SeasonsToRot = 2;

    private enum Stage : byte
    {
        /// <summary>Ждём, когда сойдёт дёрн.</summary>
        Bare,

        /// <summary>Ждём, когда вернётся дёрн.</summary>
        Green,

        /// <summary>Пень под водой ждёт, когда размокнет.</summary>
        Rot
    }

    private sealed class Spot
    {
        public DateTime Due;
        public Stage Stage;

        /// <summary>Тут стояло дерево. Вернётся дёрн — появится росток.</summary>
        public bool HadTree;
    }

    private static readonly Dictionary<(int Map, int X, int Y), Spot> Waiting = new();

    private static TimeSpan Seasons(int n) => TimeSpan.FromTicks(SeasonSystem.SeasonLength.Ticks * n);

    private static readonly TimeSpan Step = TimeSpan.FromMinutes(1.0);

    /// <summary>Сколько тайлов разбираем за такт. Зарастание никуда не спешит.</summary>
    private const int Budget = 64;

    private static Timer _timer;
    private static Persistence _persistence;

    public static int Pending => Waiting.Count;

    public static void Configure()
    {
        _persistence = new Persistence();
        CommandSystem.Register("Succession", AccessLevel.GameMaster, Succession_OnCommand);
    }

    [CallPriority(90)]
    public static void Initialize()
    {
        _timer ??= Timer.DelayCall(Step, Step, Tick);
    }

    // ---- Кого ждём ------------------------------------------------------------------------

    /// <summary>Тайл только что вскопали: со временем обнажится до песка.</summary>
    public static void Dug(Map map, int x, int y) => Queue(map, x, y, Stage.Bare, SeasonsToBare);

    /// <summary>Тайл засыпали до целины: со временем затянется, как было.</summary>
    public static void Filled(Map map, int x, int y) => Queue(map, x, y, Stage.Green, SeasonsToGreen);

    private static void Queue(Map map, int x, int y, Stage stage, int seasons)
    {
        if (map == null || map == Map.Internal)
        {
            return;
        }

        var key = (map.MapID, x, y);

        if (!Waiting.TryGetValue(key, out var spot))
        {
            Waiting[key] = spot = new Spot();
        }

        spot.Stage = stage;
        spot.Due = Core.Now + Seasons(seasons);
    }

    // ---- Деревья ---------------------------------------------------------------------------

    /// <summary>
    ///     Земля под этим тайлом сейчас уедет на newZ. Дерево, которое на ней стоит, уехать
    ///     не может — у статики своя высота, — поэтому оно становится пнём на новой высоте.
    ///
    ///     Пень, который тут уже стоит, просто переставляем: иначе при каждом следующем
    ///     взмахе он оставался бы висеть на прежней высоте.
    /// </summary>
    public static void MoveTrees(Map map, int x, int y, int newZ)
    {
        if (map == null || map == Map.Internal)
        {
            return;
        }

        var felled = false;
        var moved = false;

        foreach (var s in MahaonMapEdits.StaticsAt(map, x, y))
        {
            if (IsStump(s.Graphic))
            {
                MahaonMapEdits.RemoveStatics(map, x, y, s.Graphic);
                moved = true;
                continue;
            }

            if (!IsTree(s.Graphic))
            {
                continue;
            }

            MahaonMapEdits.RemoveStatics(map, x, y, s.Graphic);
            felled = true;
        }

        // Посаженные игроком деревья — предметы, а не статика, и сносятся иначе.
        foreach (var item in map.GetItemsAt(x, y))
        {
            if (item is MahaonTree or MahaonTreeFoliage or MahaonPlantedSapling)
            {
                item.Delete();
                felled = true;
            }
        }

        if (!felled && !moved)
        {
            return;
        }

        MahaonMapEdits.AddStatic(map, x, y, newZ, Stumps[Utility.Random(Stumps.Length)], 0);

        var key = (map.MapID, x, y);

        if (!Waiting.TryGetValue(key, out var spot))
        {
            Waiting[key] = spot = new Spot { Stage = Stage.Bare, Due = Core.Now + Seasons(SeasonsToBare) };
        }

        spot.HadTree = true;

        // Пень, оказавшийся под водой, со временем размокнет. Проверять будем на такте:
        // вода может прийти и много позже самой копки.
        if (spot.Stage != Stage.Green)
        {
            spot.Stage = Stage.Rot;
            spot.Due = Core.Now + Seasons(SeasonsToRot);
        }
    }

    /// <summary>Дерево ли это. Список берём у лесоруба — что он рубит, то и дерево.</summary>
    public static bool IsTree(int graphic)
    {
        foreach (var def in Engines.Harvest.Lumberjacking.System.Definitions)
        {
            if (def.Validate(graphic, false))
            {
                return true;
            }
        }

        return false;
    }

    // ---- Ход времени -------------------------------------------------------------------------

    private static void Tick()
    {
        if (Waiting.Count == 0)
        {
            return;
        }

        List<(int Map, int X, int Y)> done = null;
        var budget = Budget;

        foreach (var (key, spot) in Waiting)
        {
            if (budget-- <= 0)
            {
                break;
            }

            if (spot.Due > Core.Now)
            {
                continue;
            }

            if (Ripen(key, spot))
            {
                (done ??= new List<(int, int, int)>()).Add(key);
            }
        }

        if (done == null)
        {
            return;
        }

        foreach (var key in done)
        {
            Waiting.Remove(key);
        }
    }

    /// <summary>Доводит один тайл до следующего состояния. true — ждать больше нечего.</summary>
    private static bool Ripen((int Map, int X, int Y) key, Spot spot)
    {
        var map = Map.Maps[key.Map];

        if (map == null || map == Map.Internal)
        {
            return true;
        }

        var (_, x, y) = key;

        switch (spot.Stage)
        {
            case Stage.Bare:
            {
                // Дёрн сходит только с того, что осталось вскопанным. Засыпали раньше
                // срока — ждать нечего.
                if (MahaonExcavation.GroundZ(map, x, y) >= MahaonExcavation.OriginalZ(map, x, y))
                {
                    return true;
                }

                var graphic = MahaonMapEdits.LandAt(map, x, y).Graphic;

                if (!IsFurrow(graphic))
                {
                    return true;
                }

                MahaonMapEdits.SetLand(
                    map, x, y, Bare[Utility.Random(Bare.Length)], MahaonExcavation.GroundZ(map, x, y));

                return !spot.HadTree; // пень оставляем в очереди — он ещё может размокнуть
            }

            case Stage.Green:
            {
                if (MahaonExcavation.GroundZ(map, x, y) < MahaonExcavation.OriginalZ(map, x, y))
                {
                    return true; // снова раскопали — зарастать нечему
                }

                var was = MahaonExcavation.OriginalLand(map, x, y);

                MahaonMapEdits.SetLand(map, x, y, was.Graphic, MahaonExcavation.GroundZ(map, x, y));

                if (spot.HadTree)
                {
                    Sprout(map, x, y);
                }

                return true;
            }

            case Stage.Rot:
            {
                if (!MahaonWaterFlow.IsFlooded(map, x, y))
                {
                    // Сухо — пень стоит. Спросим снова через сезон.
                    spot.Due = Core.Now + Seasons(SeasonsToRot);
                    return false;
                }

                foreach (var s in MahaonMapEdits.StaticsAt(map, x, y))
                {
                    if (IsStump(s.Graphic))
                    {
                        MahaonMapEdits.RemoveStatics(map, x, y, s.Graphic);
                    }
                }

                // Размокший пень не прорастёт.
                spot.HadTree = false;
                spot.Stage = Stage.Bare;
                spot.Due = Core.Now;

                return false;
            }

            default:
                return true;
        }
    }

    /// <summary>
    ///     Дёрн вернулся туда, где стояло дерево, — всходит росток. Дальше им занимается
    ///     MahaonPlantedSapling: он доводит его до дерева за пару сезонов, по тем же часам.
    /// </summary>
    private static void Sprout(Map map, int x, int y)
    {
        foreach (var s in MahaonMapEdits.StaticsAt(map, x, y))
        {
            if (IsStump(s.Graphic))
            {
                MahaonMapEdits.RemoveStatics(map, x, y, s.Graphic);
            }
        }

        // Породу восстановить неоткуда: пень её не помнит, а статика дерева и подавно.
        // Берём любую из таблицы — лес и так смешанный.
        var species = (MahaonTreeSpecies)Utility.Random(MahaonTreeSpeciesTable.Data.Length);
        var sapling = new MahaonPlantedSapling(species);

        sapling.MoveToWorld(new Point3D(x, y, map.GetAverageZ(x, y)), map);
    }

    // ---- Команда ------------------------------------------------------------------------------

    [Usage("Succession")]
    [Description("Сколько тайлов сейчас зарастает и что с ними будет.")]
    private static void Succession_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage(0x59, $"Зарастает тайлов: {Waiting.Count}.");

        var shown = 0;

        foreach (var ((mapId, x, y), spot) in Waiting)
        {
            if (shown++ >= 8)
            {
                break;
            }

            var left = spot.Due - Core.Now;

            e.Mobile.SendMessage(
                0x3B2,
                $"  ({x}, {y}) карта {mapId}: {spot.Stage}, через {(int)left.TotalMinutes} мин" +
                (spot.HadTree ? ", было дерево" : "")
            );
        }
    }

    // ---- Сохранение ---------------------------------------------------------------------------

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonSuccession", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt(Waiting.Count);

            foreach (var ((mapId, x, y), spot) in Waiting)
            {
                writer.WriteEncodedInt(mapId);
                writer.WriteEncodedInt(x);
                writer.WriteEncodedInt(y);
                writer.Write(spot.Due);
                writer.Write((byte)spot.Stage);
                writer.Write(spot.HadTree);
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

                Waiting[(mapId, x, y)] = new Spot
                {
                    Due = reader.ReadDateTime(),
                    Stage = (Stage)reader.ReadByte(),
                    HadTree = reader.ReadBool()
                };
            }
        }
    }
}
