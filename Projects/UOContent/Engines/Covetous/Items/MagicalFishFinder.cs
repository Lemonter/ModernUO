using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Commands;
using Server.Logging;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Covetous void pool (Items/MagicFishFinder.cs) — a four-hour gadget that
///     points at the nearest school of fish and, if you stand in one, tells you so. Fishing near a live
///     school is what makes it worth carrying; a school disappears a few minutes after someone fishes it
///     and respawns somewhere else on that facet.
///
///     Adapted: schools are seeded through <see cref="SOS" />'s location finder as in the original, but a
///     facet that yields nothing is logged through LogFactory instead of the console, and the lookup
///     loops avoid LINQ on what is a per-use path.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MagicalFishFinder : Item
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(MagicalFishFinder));

    public const int DecayPeriod = 4;

    public const int MessageRange = 100;
    public const int SchoolRange = 20;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _expires;

    private TimerExecutionToken _decayToken;

    public override int LabelNumber => 1152602; // Magical Fish Finder

    [Constructible]
    public MagicalFishFinder() : base(5366)
    {
        Hue = 2500;

        _expires = Core.Now + TimeSpan.FromHours(DecayPeriod);
        ScheduleDecay();
    }

    private void ScheduleDecay()
    {
        _decayToken.Cancel();

        var remaining = _expires - Core.Now;

        if (remaining <= TimeSpan.Zero)
        {
            Timer.DelayCall(Decay);
            return;
        }

        Timer.StartTimer(remaining, Decay, out _decayToken);
    }

    public void Decay()
    {
        if (RootParent is Mobile parent)
        {
            parent.SendLocalizedMessage(1072515, Name == null ? $"#{LabelNumber}" : Name); // The ~1_name~ expired...
            Effects.SendLocationParticles(
                EffectItem.Create(parent.Location, parent.Map, EffectItem.DefaultDuration),
                0x3728,
                8,
                20,
                5042
            );
            Effects.PlaySound(parent.Location, parent.Map, 0x201);
        }
        else
        {
            Effects.SendLocationParticles(
                EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
                0x3728,
                8,
                20,
                5042
            );
            Effects.PlaySound(Location, Map, 0x201);
        }

        Delete();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _decayToken.Cancel();
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var left = _expires > Core.Now ? (int)(_expires - Core.Now).TotalSeconds : 0;
        list.Add(1072517, left.ToString()); // Lifespan: ~1_val~ seconds
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (IsChildOf(m.Backpack))
        {
            CheckUpdate(m);
        }
    }

    public void CheckUpdate(Mobile m)
    {
        if (!Schools.TryGetValue(m.Map, out var schools))
        {
            m.SendLocalizedMessage(1152637); // The fish finder shows you nothing.
            return;
        }

        foreach (var entry in schools)
        {
            if (m.InRange(entry.Location, SchoolRange))
            {
                m.SendLocalizedMessage(1152647); // Fish are schooling right here!
                return;
            }
        }

        foreach (var entry in schools)
        {
            if (m.InRange(entry.Location, MessageRange))
            {
                var direction = Utility.GetDirection(m.X, m.Y, entry.Location.X, entry.Location.Y);

                // The fish finder pulls you to the ~1_DIRECTION~.
                m.SendLocalizedMessage(1152638, $"#{1152639 + (int)direction}");
                return;
            }
        }

        m.SendLocalizedMessage(1152637); // The fish finder shows you nothing.
    }

    public static bool HasSchool(Mobile m)
    {
        if (m == null || !m.Alive || m.Backpack == null)
        {
            return false;
        }

        var finder = m.Backpack.FindItemByType<MagicalFishFinder>();

        if (finder == null || !Schools.TryGetValue(m.Map, out var schools))
        {
            return false;
        }

        foreach (var entry in schools)
        {
            if (m.InRange(entry.Location, SchoolRange))
            {
                entry.OnFish();
                return true;
            }
        }

        return false;
    }

    public static Dictionary<Map, List<SchoolEntry>> Schools { get; } = new();

    public static void Initialize()
    {
        foreach (var map in new[] { Map.Trammel, Map.Felucca, Map.Ilshenar, Map.Tokuno })
        {
            var list = new List<SchoolEntry>();
            var amount = map == Map.Ilshenar || map == Map.Tokuno ? 50 : 150;

            for (var i = 0; i < amount; i++)
            {
                var point = FindSchoolPoint(map);
                list.Add(new SchoolEntry(map, new Point2D(point.X, point.Y)));
            }

            if (list.Count == 0)
            {
                logger.Warning("{Map} has no fish school entries", map);
            }

            Schools[map] = list;
        }

        CommandSystem.Register("MoveToSchool", AccessLevel.GameMaster, MoveToSchoolOnCommand);
    }

    private static Point3D FindSchoolPoint(Map map)
    {
        var point = Point3D.Zero;

        for (var attempt = 0; attempt < 10 && point == Point3D.Zero; attempt++)
        {
            point = SOS.FindLocation(map);
        }

        return point;
    }

    private static void MoveToSchoolOnCommand(CommandEventArgs e)
    {
        var m = e.Mobile;

        if (!Schools.TryGetValue(m.Map, out var schools) || schools.Count == 0)
        {
            m.SendMessage("Для этой карты нет данных.");
            return;
        }

        var entry = schools[Utility.Random(schools.Count)];
        m.MoveToWorld(
            new Point3D(entry.Location.X, entry.Location.Y, m.Map.GetAverageZ(entry.Location.X, entry.Location.Y)),
            m.Map
        );
    }

    public static void ExpireSchool(Map map, SchoolEntry entry)
    {
        if (!Schools.TryGetValue(map, out var schools) || !schools.Contains(entry))
        {
            return;
        }

        schools.Remove(entry);

        var point = FindSchoolPoint(map);
        schools.Add(new SchoolEntry(map, new Point2D(point.X, point.Y)));
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (_expires <= Core.Now)
        {
            Timer.DelayCall(Decay);
        }
        else
        {
            ScheduleDecay();
        }
    }

    public class SchoolEntry
    {
        public SchoolEntry(Map map, Point2D location)
        {
            Map = map;
            Location = location;
        }

        public Point2D Location { get; set; }
        public bool HasFished { get; set; }
        public Map Map { get; set; }

        public void OnFish()
        {
            if (HasFished)
            {
                return;
            }

            HasFished = true;
            Timer.DelayCall(TimeSpan.FromMinutes(Utility.RandomMinMax(5, 8)), Expire);
        }

        public void Expire() => ExpireSchool(Map, this);
    }
}
