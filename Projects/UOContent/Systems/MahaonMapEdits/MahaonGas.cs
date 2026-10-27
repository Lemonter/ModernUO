using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Commands;
using Server.Items;
using Server.Systems.MahaonWeather;
using Server.Targeting;

namespace Server.Systems.MahaonMapEdits;

public enum GasKind : byte
{
    /// <summary>Дым: только видно, ни на что не влияет.</summary>
    Smoke,

    /// <summary>Яд: травит того, кто стоит внутри.</summary>
    Poison,

    /// <summary>Жар: жжёт того, кто стоит внутри. Это над лавой и над пожаром.</summary>
    Heat
}

/// <summary>
///     Дым, яд и жар — облака, которые несёт ветром.
///
///     ---- Почему предметами, а не пакетом ----
///
///     Облако здесь — обычный предмет в мире. Значит клиент рисует его тем же кодом, которым
///     рисует всё остальное, и нового пакета для дыма не нужно вовсе. Это сознательный выбор
///     против соблазна «послать клиенту облако»: у газа, который влияет на игру, авторитет
///     обязан быть у сервера, а предмет — и есть серверный авторитет, видимый всем одинаково.
///
///     Красивый дым клиент может рисовать поверх, сколько хочет, — но то, что травит, стоит
///     здесь.
///
///     ---- Направление ----
///
///     Его задаёт ВЕТЕР (MahaonWeather.WindManager): он у нас серверный, плавно меняется и уже
///     разослан клиентам, которые по нему качают траву и деревья. Поэтому дым идёт туда же,
///     куда наклонена трава, — без единого нового числа в протоколе.
///
///     Есть ветер — облако НЕСЁТ: оно двигается по ветру, а не расплывается. Затишье —
///     расползается во все стороны. Плюс поднимается, пока есть куда: дым идёт вверх и
///     скапливается под крышей, потому что выше крыши ему не пролезть.
///
///     ---- Чего тут нет ----
///
///     Воксельной решётки. Строить её на каждое облако каждый такт — верный способ сжечь
///     игровой цикл; решётка для расчётов пачкой (свет), а не для тиков по сущностям. Стены
///     газ обходит через Map.CanFit, то есть по тем же правилам, по которым ходят все.
/// </summary>
public static class MahaonGas
{
    private const string Setting = "gas.enabled";

    public static bool Enabled { get; set; } = true;

    public static TimeSpan Step { get; set; } = TimeSpan.FromSeconds(2.0);

    /// <summary>Сколько облаков обрабатываем за такт.</summary>
    private const int Budget = 64;

    /// <summary>Сколько облаков вообще согласны держать в мире.</summary>
    private const int MaxClouds = 400;

    /// <summary>Выше этого над местом рождения дым не поднимается.</summary>
    public const int MaxRise = 10;

    /// <summary>Ветер слабее этого считается затишьем.</summary>
    private const float Calm = 0.25f;

    private static readonly HashSet<MahaonGasCloud> Clouds = new();
    private static readonly Queue<MahaonGasCloud> Order = new();

    private static Timer _timer;

    public static int Count => Clouds.Count;

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting(Setting, true);
        CommandSystem.Register("Gas", AccessLevel.GameMaster, Gas_OnCommand);
    }

    [CallPriority(90)]
    public static void Initialize()
    {
        _timer ??= Timer.DelayCall(Step, Step, Tick);
    }

    public static void Register(MahaonGasCloud cloud)
    {
        if (Clouds.Add(cloud))
        {
            Order.Enqueue(cloud);
        }
    }

    public static void Unregister(MahaonGasCloud cloud) => Clouds.Remove(cloud);

    /// <summary>Выпускает облако. Жизнь — в тактах, расход — сколько раз оно может поделиться.</summary>
    public static MahaonGasCloud Release(Map map, Point3D where, GasKind kind, int life, int spread)
    {
        if (map == null || map == Map.Internal || Clouds.Count >= MaxClouds)
        {
            return null;
        }

        var cloud = new MahaonGasCloud(kind, Math.Clamp(life, 1, 120), Math.Clamp(spread, 0, 12), where.Z);
        cloud.MoveToWorld(where, map);

        return cloud;
    }

    /// <summary>Куда дует: ноль градусов — на восток, девяносто — на север (север это −Y).</summary>
    public static (int X, int Y) WindStep()
    {
        var radians = WindManager.Direction * Math.PI / 180.0;

        return ((int)Math.Round(Math.Cos(radians)), (int)Math.Round(-Math.Sin(radians)));
    }

    private static void Tick()
    {
        if (!Enabled || Order.Count == 0)
        {
            return;
        }

        var budget = Math.Min(Budget, Order.Count);

        for (var i = 0; i < budget; i++)
        {
            var cloud = Order.Dequeue();

            if (cloud?.Deleted != false || !Clouds.Contains(cloud))
            {
                continue;
            }

            cloud.Step();

            if (!cloud.Deleted)
            {
                Order.Enqueue(cloud);
            }
        }
    }

    [Usage("Gas [smoke|poison|heat] [жизнь] [расход]")]
    [Description("Выпускает облако: несёт по ветру, поднимается, расходится в затишье.")]
    private static void Gas_OnCommand(CommandEventArgs e)
    {
        var kind = GasKind.Smoke;

        if (e.Length >= 1)
        {
            kind = e.GetString(0).ToLowerInvariant() switch
            {
                "poison" or "яд"  => GasKind.Poison,
                "heat" or "жар"   => GasKind.Heat,
                _                 => GasKind.Smoke
            };
        }

        var life = e.Length >= 2 ? e.GetInt32(1) : 20;
        var spread = e.Length >= 3 ? e.GetInt32(2) : 4;

        e.Mobile.SendMessage(0x3B2, $"Куда выпустить ({kind}, жизнь {life}, расход {spread})?");
        e.Mobile.Target = new GasTarget(kind, life, spread);
    }

    private class GasTarget : Target
    {
        private readonly GasKind _kind;
        private readonly int _life;
        private readonly int _spread;

        public GasTarget(GasKind kind, int life, int spread) : base(16, true, TargetFlags.None)
        {
            _kind = kind;
            _life = life;
            _spread = spread;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D p || from.Map == null)
            {
                return;
            }

            var where = new Point3D(p.X, p.Y, p.Z);

            from.SendMessage(
                0x59,
                Release(from.Map, where, _kind, _life, _spread) != null
                    ? $"Пошло. Ветер {WindManager.Direction:0}°, сила {WindManager.Strength:0.00}."
                    : "Облаков уже слишком много."
            );
        }
    }
}

/// <summary>Одно облако. Живёт тактами, которые ему отсчитывает MahaonGas.</summary>
[SerializationGenerator(0, false)]
public partial class MahaonGasCloud : Item
{
    /// <summary>Дым. Анимированная графика, в тайлдате так и называется.</summary>
    private static readonly int[] SmokeGraphics = { 0x3728, 0x3729, 0x372A, 0x372B, 0x372C, 0x372D, 0x372E, 0x372F };

    [SerializableField(0)]
    private GasKind _kind;

    [SerializableField(1)]
    private int _life;

    [SerializableField(2)]
    private int _spread;

    /// <summary>Высота, с которой облако начало подниматься, — чтобы не улетело в небо.</summary>
    [SerializableField(3)]
    private int _bornZ;

    [Constructible]
    public MahaonGasCloud(GasKind kind = GasKind.Smoke, int life = 20, int spread = 4, int bornZ = 0)
        : base(kind == GasKind.Poison ? 0x3915 : SmokeGraphics[Utility.Random(SmokeGraphics.Length)])
    {
        _kind = kind;
        _life = life;
        _spread = spread;
        _bornZ = bornZ;

        Movable = false;
        Name = kind switch
        {
            GasKind.Poison => "облако яда",
            GasKind.Heat   => "обжигающий жар",
            _              => "дым"
        };

        if (kind == GasKind.Poison)
        {
            Hue = 0x4F;
        }

        Systems.MahaonMapEdits.MahaonGas.Register(this);
    }

    public override bool HandlesOnMovement => false;

    /// <summary>Один такт: подействовать, подняться, уйти по ветру или разойтись.</summary>
    public void Step()
    {
        if (--_life <= 0)
        {
            Delete();
            return;
        }

        this.MarkDirty();

        Affect();
        Rise();
        Move();
    }

    /// <summary>Действует на тех, кто стоит внутри. Дым не делает ничего — он только видно.</summary>
    private void Affect()
    {
        if (_kind == GasKind.Smoke || Map == null)
        {
            return;
        }

        foreach (var m in Map.GetMobilesAt(new Point2D(X, Y)))
        {
            if (!m.Alive || m.AccessLevel > AccessLevel.Player || m.Blessed)
            {
                continue;
            }

            // По высоте тоже: облако под ногами или над головой не травит.
            if (Math.Abs(m.Z - Z) > 8)
            {
                continue;
            }

            if (_kind == GasKind.Poison)
            {
                m.ApplyPoison(m, Poison.Regular);
                continue;
            }

            AOS.Damage(m, Utility.RandomMinMax(3, 7), 0, 100, 0, 0, 0);
        }
    }

    /// <summary>Поднимается, пока есть куда. Под крышей остановится сам: выше не пролезет.</summary>
    private void Rise()
    {
        if (Map == null || Z - _bornZ >= MahaonGas.MaxRise)
        {
            return;
        }

        if (Map.CanFit(X, Y, Z + 1, 1, false, false, false))
        {
            Z += 1;
        }
    }

    /// <summary>
    ///     На ветру облако НЕСЁТ — оно двигается, а не расплывается. В затишье расходится во
    ///     все стороны, пока хватает расхода.
    /// </summary>
    private void Move()
    {
        if (Map == null)
        {
            return;
        }

        if (WindManager.Strength >= 0.25f)
        {
            var (dx, dy) = MahaonGas.WindStep();

            if ((dx != 0 || dy != 0) && Map.CanFit(X + dx, Y + dy, Z, 1, false, false, false))
            {
                MoveToWorld(new Point3D(X + dx, Y + dy, Z), Map);
            }

            return;
        }

        if (_spread <= 0)
        {
            return;
        }

        Span<(int X, int Y)> around = [(X + 1, Y), (X - 1, Y), (X, Y + 1), (X, Y - 1)];
        var spot = around[Utility.Random(around.Length)];

        if (!Map.CanFit(spot.X, spot.Y, Z, 1, false, false, false))
        {
            return;
        }

        _spread--;

        MahaonGas.Release(Map, new Point3D(spot.X, spot.Y, Z), _kind, _life, _spread);
    }

    public override void OnDelete()
    {
        Systems.MahaonMapEdits.MahaonGas.Unregister(this);

        base.OnDelete();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Systems.MahaonMapEdits.MahaonGas.Register(this);
    }
}
