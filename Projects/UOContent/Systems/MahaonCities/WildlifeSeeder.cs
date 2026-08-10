using Server.Commands;
using Server.Mobiles;

namespace Server.Systems.MahaonCities;

/// <summary>
///     A bare ModernUO install has zero wildlife — no spawner data ships with it (that's
///     historically been a separate scripts/data drop in the RunUO/ServUO family). This is
///     a minimal stand-in: scatter common animals around each city's outskirts once, so
///     the world isn't completely dead. Not a real spawner system (no respawn-on-death,
///     no density tuning) — a proper one (XmlSpawner-for-Modernuo or similar) is the real
///     long-term answer if this needs to scale up.
/// </summary>
public class WildlifeSeeder : GenericPersistence
{
    private static WildlifeSeeder _instance;
    private static bool _seeded;

    private static readonly (System.Func<Mobile> create, int weight)[] Animals =
    [
        (() => new Rabbit(), 4),
        (() => new Bird(), 4),
        (() => new Chicken(), 3),
        (() => new Sheep(), 3),
        (() => new Cow(), 2),
        (() => new Horse(), 2),
        (() => new Boar(), 2),
        (() => new Squirrel(), 3),
        (() => new Eagle(), 1),
        (() => new GreyWolf(), 1),
        (() => new BlackBear(), 1)
    ];

    private const int PerCity = 25;
    private const int MinRadius = 15;
    private const int MaxRadius = 45;

    public WildlifeSeeder() : base("MahaonWildlifeSeeder", 1)
    {
    }

    public static void Configure()
    {
        _instance = new WildlifeSeeder();
        CommandSystem.Register("SeedWildlife", AccessLevel.GameMaster, SeedWildlife_OnCommand);
    }

    public static void Initialize()
    {
        if (_seeded)
        {
            return;
        }

        SeedAll();
        _seeded = true;
    }

    private static void SeedAll()
    {
        foreach (var (_, info) in CityControlSystem.Cities)
        {
            SeedAround(info.spawn, info.map);
        }
    }

    private static void SeedAround(Point3D center, Map map)
    {
        for (var i = 0; i < PerCity; i++)
        {
            var create = PickWeighted();
            var loc = FindSpot(center, map);
            var animal = create();
            animal.MoveToWorld(loc, map);
        }
    }

    private static System.Func<Mobile> PickWeighted()
    {
        var total = 0;
        foreach (var (_, weight) in Animals)
        {
            total += weight;
        }

        var roll = Utility.Random(total);
        foreach (var (create, weight) in Animals)
        {
            if (roll < weight)
            {
                return create;
            }

            roll -= weight;
        }

        return Animals[0].create;
    }

    private static Point3D FindSpot(Point3D center, Map map)
    {
        var targetRadius = Utility.RandomMinMax(MinRadius, MaxRadius);

        for (var attempt = 0; attempt < 12; attempt++)
        {
            var angle = Utility.RandomDouble() * System.Math.PI * 2;

            var x = center.X + (int)(System.Math.Cos(angle) * targetRadius);
            var y = center.Y + (int)(System.Math.Sin(angle) * targetRadius);
            var z = map.GetAverageZ(x, y);
            var candidate = new Point3D(x, y, z);

            if (map.CanSpawnMobile(candidate))
            {
                return candidate;
            }
        }

        // The random ring kept missing (probably a lot of water/wall at that particular
        // radius+angle combo) — fall back to a real expanding-ring search near the city
        // center instead of just dumping the animal exactly on top of it.
        for (var ring = MinRadius; ring <= MaxRadius; ring++)
        {
            for (var dx = -ring; dx <= ring; dx++)
            {
                for (var dy = -ring; dy <= ring; dy++)
                {
                    if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != ring)
                    {
                        continue;
                    }

                    var x = center.X + dx;
                    var y = center.Y + dy;
                    var z = map.GetAverageZ(x, y);
                    var candidate = new Point3D(x, y, z);

                    if (map.CanSpawnMobile(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return center;
    }

    [Usage("SeedWildlife")]
    [Description("Force-scatters basic wildlife around every city, even if this has already run before.")]
    private static void SeedWildlife_OnCommand(CommandEventArgs e)
    {
        SeedAll();
        _seeded = true;
        e.Mobile.SendMessage(0x59, "Дикая живность расставлена вокруг городов.");
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.Write(_seeded);
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version
        _seeded = reader.ReadBool();
    }
}
