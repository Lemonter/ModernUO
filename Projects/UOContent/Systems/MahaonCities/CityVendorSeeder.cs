using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonCities;

/// <summary>
///     Populates each town with a basic set of working vendors on first server start.
///     Uses the stock ModernUO vendor classes (they already exist and work out of the
///     box) rather than inventing new ones — the gap was never "no vendor code", it was
///     "nobody placed any". Runs once, ever, guarded by a persisted flag.
/// </summary>
public class CityVendorSeeder : GenericPersistence
{
    private static CityVendorSeeder _instance;
    private static bool _seeded;

    public CityVendorSeeder() : base("MahaonCityVendorSeeder", 1)
    {
    }

    public static void Configure()
    {
        _instance = new CityVendorSeeder();
        CommandSystem.Register("SeedCities", AccessLevel.GameMaster, SeedCities_OnCommand);
    }

    // Must run after CityControlSystem exists (same assembly, no ordering dependency
    // issue) but this itself just needs the World loaded, so default Initialize timing
    // is fine.
    public static void Initialize()
    {
        if (_seeded)
        {
            return;
        }

        SeedAllCities();
        _seeded = true;
    }

    private static void SeedAllCities()
    {
        foreach (var (cityName, info) in CityControlSystem.Cities)
        {
            SeedCity(cityName, info.spawn, info.map);
        }
    }

    [Usage("SeedCities")]
    [Description("Force-reseeds vendors/stones in every city, even if this has already run before.")]
    private static void SeedCities_OnCommand(CommandEventArgs e)
    {
        SeedAllCities();
        _seeded = true;
        e.Mobile.SendMessage(0x59, "Торговцы и камни повторно расставлены по всем городам.");
    }

    private static void SeedCity(string cityName, Point3D center, Map map)
    {
        SpawnNear(cityName, "banker", new Banker(), center, map);
        SpawnNear(cityName, "healer", new Healer(), center, map);
        SpawnNear(cityName, "provisioner", new Provisioner(), center, map);
        SpawnNear(cityName, "blacksmith", new Blacksmith(), center, map);
        SpawnNear(cityName, "tavernkeeper", new TavernKeeper(), center, map);
        SpawnNear(cityName, "tinker", new Tinker(), center, map);
        SpawnNear(cityName, "alchemist", new Alchemist(), center, map);
        SpawnNear(cityName, "librarian", new MahaonLibrarian(), center, map);
        SpawnNear(cityName, "mystic", new MahaonMystic(), center, map);
        SpawnItemNear(cityName, "travelstone", new CityTravelStone(), center, map);
        SpawnItemNear(cityName, "auctionstone", new AuctionStone(cityName), center, map);
    }

    private static void SpawnNear(string cityName, string label, Mobile vendor, Point3D center, Map map)
    {
        if (CityMarkers.TryGetMarker(cityName, label, out var markedLoc, out var markedMap))
        {
            vendor.MoveToWorld(markedLoc, markedMap);
            return;
        }

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var x = center.X + Utility.RandomMinMax(-15, 15);
            var y = center.Y + Utility.RandomMinMax(-15, 15);
            var z = map.GetAverageZ(x, y);
            var candidate = new Point3D(x, y, z);

            if (map.CanSpawnMobile(candidate))
            {
                vendor.MoveToWorld(candidate, map);
                return;
            }
        }

        // Random sampling kept missing — expanding-ring search instead of just dumping
        // every vendor on the exact same center tile, which is what made cities look
        // "empty" (all the vendors invisibly stacked in one spot instead of spread out).
        for (var ring = 2; ring <= 25; ring++)
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
                        vendor.MoveToWorld(candidate, map);
                        return;
                    }
                }
            }
        }

        vendor.MoveToWorld(center, map);
    }

    private static void SpawnItemNear(string cityName, string label, Item item, Point3D center, Map map)
    {
        if (CityMarkers.TryGetMarker(cityName, label, out var markedLoc, out var markedMap))
        {
            item.MoveToWorld(markedLoc, markedMap);
            return;
        }

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var x = center.X + Utility.RandomMinMax(-6, 6);
            var y = center.Y + Utility.RandomMinMax(-6, 6);
            var z = map.GetAverageZ(x, y);
            var candidate = new Point3D(x, y, z);

            if (map.CanFit(candidate.X, candidate.Y, candidate.Z, 16))
            {
                item.MoveToWorld(candidate, map);
                return;
            }
        }

        item.MoveToWorld(center, map);
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
