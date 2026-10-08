using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Guilds;
using Server.Items;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Items/VvVAltar.cs).
// `IPooledEnumerable`/`eable.Free()` replaced with `Map.GetMobilesInBounds` direct enumeration
// (ModernUO's overload already returns a disposable pooled enumerable used with `foreach`).
[SerializationGenerator(0, false)]
public partial class VvVAltar : BaseAddon
{
    // Not a [SerializableField]: VvVBattle is a [PropertyObject], see VvVSigil.cs for why.
    public VvVBattle Battle { get; set; }

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isActive;

    [SerializableField(1)]
    private List<Item> _braziers;

    [SerializableField(2)]
    private List<Item> _torches;

    public override bool HandlesOnMovement => IsActive;

    public OccupyTimer OccupationTimer { get; set; }
    public Timer CheckTimer { get; set; }

    [Constructible]
    public VvVAltar() : this(null)
    {
    }

    public VvVAltar(VvVBattle battle)
    {
        Battle = battle;

        _braziers = new List<Item>();
        _torches = new List<Item>();

        var virtue = Utility.Random(8);

        AddComponent(new AddonComponent(1822), -2, -2, 0);
        AddComponent(new AddonComponent(1822), -1, -2, 0);
        AddComponent(new AddonComponent(1822), 0, -2, 0);
        AddComponent(new AddonComponent(1822), 1, -2, 0);
        AddComponent(new AddonComponent(1822), 2, -2, 0);

        AddComponent(new AddonComponent(1822), -2, -1, 0);
        AddComponent(new AddonComponent(1822), -1, -1, 0);
        AddComponent(new AddonComponent(1822), 0, -1, 0);
        AddComponent(new AddonComponent(1822), 1, -1, 0);
        AddComponent(new AddonComponent(1822), 2, -1, 0);

        AddComponent(new AddonComponent(1822), -2, 0, 0);
        AddComponent(new AddonComponent(1822), -1, 0, 0);
        AddComponent(new AddonComponent(1822), 0, 0, 0);
        AddComponent(new AddonComponent(1822), 1, 0, 0);
        AddComponent(new AddonComponent(1822), 2, 0, 0);

        AddComponent(new AddonComponent(1822), -2, 1, 0);
        AddComponent(new AddonComponent(1822), -1, 1, 0);
        AddComponent(new AddonComponent(1822), 0, 1, 0);
        AddComponent(new AddonComponent(1822), 1, 1, 0);
        AddComponent(new AddonComponent(1822), 2, 1, 0);

        AddComponent(new AddonComponent(1822), -2, 2, 0);
        AddComponent(new AddonComponent(1822), -1, 2, 0);
        AddComponent(new AddonComponent(1822), 0, 2, 0);
        AddComponent(new AddonComponent(1822), 1, 2, 0);
        AddComponent(new AddonComponent(1822), 2, 2, 0);

        // NorthWest
        AddComponent(new AddonComponent(_tiles[0][virtue]), -2, -2, 5);
        AddComponent(new AddonComponent(_tiles[0][virtue] + 1), -2, -1, 5);
        AddComponent(new AddonComponent(_tiles[0][virtue] + 2), -1, -1, 5);
        AddComponent(new AddonComponent(_tiles[0][virtue] + 3), -1, -2, 5);

        // SouthEast
        AddComponent(new AddonComponent(_tiles[0][virtue]), 1, 1, 5);
        AddComponent(new AddonComponent(_tiles[0][virtue] + 1), 1, 2, 5);
        AddComponent(new AddonComponent(_tiles[0][virtue] + 2), 2, 2, 5);
        AddComponent(new AddonComponent(_tiles[0][virtue] + 3), 2, 1, 5);

        // SouthWest
        AddComponent(new AddonComponent(_tiles[1][virtue]), -2, 1, 5);
        AddComponent(new AddonComponent(_tiles[1][virtue] + 1), -1, 1, 5);
        AddComponent(new AddonComponent(_tiles[1][virtue] + 2), -2, 2, 5);
        AddComponent(new AddonComponent(_tiles[1][virtue] + 3), -1, 2, 5);

        // NorthEast
        AddComponent(new AddonComponent(_tiles[1][virtue]), 1, -2, 5);
        AddComponent(new AddonComponent(_tiles[1][virtue] + 1), 2, -2, 5);
        AddComponent(new AddonComponent(_tiles[1][virtue] + 2), 1, -1, 5);
        AddComponent(new AddonComponent(_tiles[1][virtue] + 3), 2, -1, 5);

        AddComponent(new AddonComponent(1866), -1, -3, 0);
        AddComponent(new AddonComponent(1847), 0, -3, 0);
        AddComponent(new AddonComponent(1868), 1, -3, 0);

        AddComponent(new AddonComponent(1868), 3, -1, 0);
        AddComponent(new AddonComponent(1846), 3, 0, 0);
        AddComponent(new AddonComponent(1867), 3, 1, 0);

        AddComponent(new AddonComponent(1869), -1, 3, 0);
        AddComponent(new AddonComponent(1823), 0, 3, 0);
        AddComponent(new AddonComponent(1867), 1, 3, 0);

        AddComponent(new AddonComponent(1866), -3, -1, 0);
        AddComponent(new AddonComponent(1865), -3, 0, 0);
        AddComponent(new AddonComponent(1869), -3, 1, 0);

        var c = new AddonComponent(6570);
        AddComponent(c, 3, -3, 0);
        _braziers.Add(c);

        c = new AddonComponent(6570);
        AddComponent(c, 3, 3, 0);
        _braziers.Add(c);

        c = new AddonComponent(6570);
        AddComponent(c, -3, 3, 0);
        _braziers.Add(c);

        c = new AddonComponent(6570);
        AddComponent(c, -3, -3, 0);
        _braziers.Add(c);
    }

    public bool Contains(IPoint3D p)
    {
        if (p is IEntity entity && entity.Map != Map)
        {
            return false;
        }

        return p.X >= X - 2 && p.X <= X + 2 && p.Y >= Y - 2 && p.Y <= Y + 2;
    }

    public void Activate()
    {
        IsActive = true;
        CheckTimer = Timer.DelayCall(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500), CheckOccupy);
    }

    public void Complete(Guild g)
    {
        Timer.DelayCall(TimeSpan.FromSeconds(5), () => Battle?.OccupyAltar(g));
        Timer.DelayCall(TimeSpan.FromSeconds(2), DoFireworks);

        IsActive = false;

        OccupationTimer = null;

        CheckTimer?.Stop();
        CheckTimer = null;

        Timer.DelayCall(
            TimeSpan.FromMinutes(2),
            () =>
            {
                Torches.ForEach(t => t.Delete());
                Torches.Clear();
            }
        );
    }

    public void DoFireworks()
    {
        if (Deleted)
        {
            return;
        }

        for (var i = 2; i <= 8; i += 2)
        {
            Timer.DelayCall(
                TimeSpan.FromMilliseconds((i - 2) * 600),
                radius => Misc.Geometry.Circle2D(Location, Map, radius, LaunchFireworks),
                i
            );
        }
    }

    public static void LaunchFireworks(Point3D p, Map map)
    {
        if (map == null || map == Map.Internal)
        {
            return;
        }

        var startLoc = new Point3D(p.X, p.Y, p.Z + 10);
        var endLoc = new Point3D(p.X + Utility.RandomMinMax(-1, 1), p.Y + Utility.RandomMinMax(-1, 1), p.Z + 32);

        Effects.SendMovingEffect(new Entity(Serial.Zero, startLoc, map), new Entity(Serial.Zero, endLoc, map), 0x36E4, 5, 0, false, false);

        Timer.DelayCall(
            TimeSpan.FromSeconds(1.0),
            () =>
            {
                var hue = Utility.Random(40) switch
                {
                    < 8  => 0x66D,
                    < 10 => 0x482,
                    < 12 => 0x47E,
                    < 16 => 0x480,
                    < 20 => 0x47F,
                    _    => 0
                };

                if (Utility.RandomBool())
                {
                    hue = Utility.RandomList(0x47E, 0x47F, 0x480, 0x482, 0x66D);
                }

                var renderMode = Utility.RandomList(0, 2, 3, 4, 5, 7);

                Effects.PlaySound(endLoc, map, Utility.Random(0x11B, 4));
                Effects.SendLocationEffect(endLoc, map, 0x373A + 0x10 * Utility.Random(4), 16, 10, hue, renderMode);
            }
        );
    }

    public void CheckOccupy()
    {
        if (!IsActive || Map == null || Map == Map.Internal)
        {
            return;
        }

        var count = 0;

        foreach (var m in Map.GetMobilesInBounds(new Rectangle2D(X - 2, Y - 2, 5, 5)))
        {
            if (!m.Alive || !ViceVsVirtueSystem.IsVvV(m, out var entry, false, true))
            {
                continue;
            }

            count++;

            if (OccupationTimer != null)
            {
                var g = OccupationTimer.Occupier;

                if (g == null || (entry.Guild != g && !entry.Guild.IsAlly(g)))
                {
                    Clear();
                    break;
                }
            }
            else
            {
                OccupationTimer = new OccupyTimer(this, entry.Guild);
            }
        }

        if (OccupationTimer != null && !OccupationTimer.Running && count > 0)
        {
            OccupationTimer.Start();
        }
        else if (OccupationTimer != null && count == 0)
        {
            Clear();
        }
    }

    private void Clear()
    {
        OccupationTimer?.Stop();
        OccupationTimer = null;

        Torches.ForEach(t => t.Delete());
        Torches.Clear();
    }

    public class OccupyTimer : Timer
    {
        public Guild Occupier { get; }
        public VvVAltar Altar { get; }

        private int _tick;

        public OccupyTimer(VvVAltar altar, Guild occupier) : base(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2))
        {
            Occupier = occupier;
            Altar = altar;
        }

        protected override void OnTick()
        {
            if (_tick < 0)
            {
                _tick = 0;
            }

            if (_tick >= _locs.Length)
            {
                Altar.Complete(Occupier);
                Stop();
                return;
            }

            var p = _locs[_tick];

            Effects.SendLocationEffect(new Point3D(Altar.X + p.X, Altar.Y + p.Y, Altar.Z + p.Z), Altar.Map, 0x3709, 30, 10);
            Effects.PlaySound(Altar.Location, Altar.Map, 0x208);

            var index = _tick / 4;

            if (_tick > 0 && index < Altar.Braziers.Count && (_tick + 1) % 4 == 0)
            {
                DelayCall(
                    TimeSpan.FromSeconds(1),
                    () =>
                    {
                        var torch = new AddonComponent(6571);
                        Altar.Torches.Add(torch);

                        var to = Altar.Braziers[index].Location;

                        torch.MoveToWorld(new Point3D(to.X, to.Y, to.Z + 17), Altar.Map);
                        Effects.PlaySound(to, Altar.Map, 0x47);
                    }
                );
            }

            _tick++;

            if (_tick >= _locs.Length)
            {
                Altar.Complete(Occupier);
                Stop();
            }
        }

        private readonly Point3D[] _locs =
        {
            new(-1, -2, 7), new(0, -2, 7), new(1, -2, 7), new(2, -2, 7),
            new(2, -1, 7), new(2, 0, 7), new(2, 1, 7), new(2, 2, 7),
            new(1, 2, 7), new(0, 2, 7), new(-1, 2, 7), new(-2, 2, 7),
            new(-2, 1, 7), new(-2, 0, 7), new(-2, -1, 7), new(-2, -2, 7)
        };
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        Torches.ForEach(t => t.Delete());

        OccupationTimer?.Stop();
        OccupationTimer = null;

        CheckTimer?.Stop();
        CheckTimer = null;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Battle = ViceVsVirtueSystem.Instance?.Battle;

        if (IsActive)
        {
            CheckTimer = Timer.DelayCall(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500), CheckOccupy);
        }
    }

    private static readonly int[][] _tiles =
    {
        new[] { 5283, 5291, 5299, 5307, 5315, 5323, 5331, 5390 },
        new[] { 39372, 39380, 39388, 39396, 39404, 39412, 39420, 39428 }
    };
}

// ServUO's AltarArrow (a QuestArrow pointing players at the currently-active altar) isn't
// ported — this codebase's `QuestArrow(PlayerMobile, Mobile)` only targets Mobile entities,
// not arbitrary Items like VvVAltar (a BaseAddon). VvVBattle.CheckArrow/ActivateArrows are
// no-ops here as a result; players must find the lit altar without a compass-arrow hint.
