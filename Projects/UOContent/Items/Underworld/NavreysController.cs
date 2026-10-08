using System;
using ModernUO.Serialization;
using Server.Commands;
using Server.Mobiles;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/Navrey's
// Lair/NavreysController.cs). Simplifications: `Map.GetSpawnPosition` doesn't exist in this
// codebase (see Engines/Shadowguard for the same fix elsewhere) — replaced with a manual
// nearby-point search. `Diagnostics.ExceptionLogging` doesn't exist — the TypeRestart
// setter's try/catch around a DateTime add can't overflow in practice, so it's dropped.
// `HuedEffect`/`Effects.SendPacket` (RunUO packet-level effect API) replaced with the
// existing `Effects.SendMovingEffect` helper. `IPooledEnumerable` (RunUO-era enumerable)
// replaced with `GetMobilesInRange`.
[SerializationGenerator(0, false)]
public partial class NavreysController : Item
{
    public static void Initialize() =>
        CommandSystem.Register("GenNavrey", AccessLevel.Developer, GenNavrey_OnCommand);

    [Usage("GenNavrey")]
    [Description("Creates the Navrey Night-Eyes lair (controller + 3 pillars + boss) if one doesn't already exist.")]
    private static void GenNavrey_OnCommand(CommandEventArgs e)
    {
        if (Exists())
        {
            e.Mobile.SendMessage("Navrey spawner is already present.");
            return;
        }

        e.Mobile.SendMessage("Creating Navrey Night-Eyes Lair...");
        _ = new NavreysController();
        e.Mobile.SendMessage("Generation completed!");
    }

    private static bool Exists()
    {
        foreach (var item in World.Items.Values)
        {
            if (item is NavreysController { Deleted: false })
            {
                return true;
            }
        }

        return false;
    }

    [SerializableField(0)]
    private NavreysPillar[] _pillars;

    [SerializableField(1)]
    private Navrey _navrey;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _typeRestartAt;

    [CommandProperty(AccessLevel.GameMaster)]
    public TimeSpan TypeRestart
    {
        get
        {
            var ts = _typeRestartAt - Core.Now;

            if (ts < TimeSpan.Zero)
            {
                foreach (var pillar in _pillars)
                {
                    pillar.Type = pillar.Type != PillarType.Nine ? pillar.Type + 1 : PillarType.Three;
                }

                // The timers rotate among the three stone ruins randomly once a day.
                TypeRestart = TimeSpan.FromHours(24.0);
                return TimeSpan.Zero;
            }

            return ts;
        }
        set => _typeRestartAt = Core.Now + value;
    }

    public bool AllPillarsHot
    {
        get
        {
            foreach (var pillar in _pillars)
            {
                if (pillar == null || pillar.Deleted || pillar.State != NavreysPillarState.Hot)
                {
                    return false;
                }
            }

            return true;
        }
    }

    [Constructible]
    public NavreysController() : base(0x1F13)
    {
        Name = "Navrey Spawner - Do not remove !!";
        Visible = false;
        Movable = false;

        MoveToWorld(new Point3D(1054, 861, -31), Map.TerMur);

        _pillars = new NavreysPillar[3];

        _pillars[0] = new NavreysPillar(this, PillarType.Three);
        _pillars[0].MoveToWorld(new Point3D(1071, 847, 0), Map.TerMur);
        _pillars[1] = new NavreysPillar(this, PillarType.Six);
        _pillars[1].MoveToWorld(new Point3D(1039, 879, 0), Map.TerMur);
        _pillars[2] = new NavreysPillar(this, PillarType.Nine);
        _pillars[2].MoveToWorld(new Point3D(1039, 850, 0), Map.TerMur);

        Respawn();
    }

    public void OnNavreyKilled()
    {
        SetAllPillars(NavreysPillarState.Off);
        Timer.DelayCall(TimeSpan.FromMinutes(10.0), Respawn);
    }

    public void Respawn()
    {
        var navrey = new Navrey(this) { RangeHome = 20 };
        navrey.MoveToWorld(FindNearbySpawnPoint(), Map);

        SetAllPillars(NavreysPillarState.On);

        _navrey = navrey;
    }

    private Point3D FindNearbySpawnPoint()
    {
        for (var i = 0; i < 10; i++)
        {
            var x = Location.X + Utility.RandomMinMax(-10, 10);
            var y = Location.Y + Utility.RandomMinMax(-10, 10);
            var z = Map.GetAverageZ(x, y);

            if (Map.CanSpawnMobile(x, y, z))
            {
                return new Point3D(x, y, z);
            }
        }

        return Location;
    }

    public void ResetPillars()
    {
        if (_navrey is { Deleted: false, Alive: true })
        {
            SetAllPillars(NavreysPillarState.On);
        }
    }

    public override void OnDelete()
    {
        base.OnDelete();

        foreach (var pillar in _pillars)
        {
            pillar?.Delete();
        }

        _navrey?.Delete();
    }

    public void CheckPillars()
    {
        if (!AllPillarsHot)
        {
            return;
        }

        _navrey.UsedPillars = true;

        new RockRainTimer(_navrey).Start();

        SetAllPillars(NavreysPillarState.Off);
        Timer.DelayCall(TimeSpan.FromMinutes(5.0), ResetPillars);
    }

    private void SetAllPillars(NavreysPillarState state)
    {
        foreach (var pillar in _pillars)
        {
            pillar.State = state;
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_navrey == null)
        {
            Timer.DelayCall(TimeSpan.Zero, Respawn);
        }
        else
        {
            SetAllPillars(NavreysPillarState.On);
        }
    }

    private class RockRainTimer : Timer
    {
        private readonly Navrey _navrey;
        private int _ticks;

        public RockRainTimer(Navrey navrey) : base(TimeSpan.Zero, TimeSpan.FromSeconds(0.25))
        {
            _navrey = navrey;
            _ticks = 120;

            _navrey.CantWalk = true;
        }

        protected override void OnTick()
        {
            _ticks--;

            var dest = _navrey.Location;
            var orig = new Point3D(
                dest.X - Utility.RandomMinMax(3, 4),
                dest.Y - Utility.RandomMinMax(8, 9),
                dest.Z + Utility.RandomMinMax(41, 43)
            );
            var itemId = Utility.RandomMinMax(0x1362, 0x136D);
            var speed = Utility.RandomMinMax(5, 10);
            var hue = Utility.RandomBool() ? 0 : Utility.RandomMinMax(0x456, 0x45F);

            Effects.SendMovingEffect(_navrey.Map, itemId, orig, dest, speed, 0, false, false, hue);
            Effects.SendMovingEffect(_navrey.Map, itemId, orig, dest, speed, 0, false, false, hue);

            Effects.PlaySound(_navrey.Location, _navrey.Map, 0x15E + Utility.Random(3));
            Effects.PlaySound(_navrey.Location, _navrey.Map, Utility.RandomList(0x305, 0x306, 0x307, 0x309));

            var amount = Utility.RandomMinMax(100, 150);

            foreach (var m in _navrey.Map.GetMobilesInRange(_navrey.Location, 1))
            {
                if (!m.Alive)
                {
                    continue;
                }

                if (m is Navrey navrey)
                {
                    navrey.Damage(amount);
                }
                else
                {
                    m.RevealingAction();
                    AOS.Damage(m, amount, 100, 0, 0, 0, 0);
                }
            }

            if (_ticks == 0 || !_navrey.Alive)
            {
                _navrey.CantWalk = false;
                Stop();
            }
        }
    }
}
