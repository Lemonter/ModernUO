using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonWorld;

public static class MahaonCityHouseSystem
{
    private static readonly List<MahaonCityHouse> Houses = new();
    private static bool _scanned;

    public static void Configure()
    {
        // StaticOverrideManager itself is memory-only and resets on restart (by design —
        // it's cosmetic-only elsewhere), so anything relying on it for something permanent
        // (unlike a temporary chopped-tree stump) has to actively resend once the world
        // — and therefore every city house's furniture-hiding list — is actually loaded.
        EventSink.WorldLoad += ReapplyAllFurnitureHiding;
    }

    private static void ReapplyAllFurnitureHiding()
    {
        foreach (var house in All())
        {
            if (!house.Deleted)
            {
                house.ApplyFurnitureHiding();
            }
        }
    }

    private static void EnsureScanned()
    {
        if (_scanned)
        {
            return;
        }

        _scanned = true;

        foreach (var item in World.Items.Values)
        {
            if (item is MahaonCityHouse { Deleted: false } house)
            {
                Houses.Add(house);
            }
        }
    }

    public static void Register(MahaonCityHouse house)
    {
        EnsureScanned();
        Houses.Add(house);
    }

    public static IReadOnlyList<MahaonCityHouse> All()
    {
        EnsureScanned();
        return Houses;
    }

    public static MahaonCityHouse Find(Point3D loc, Map map)
    {
        EnsureScanned();

        foreach (var house in Houses)
        {
            if (!house.Deleted && house.Contains(loc, map))
            {
                return house;
            }
        }

        return null;
    }

    /// <summary>Cheap check for the movement hot path (Engines/Pathing/Movement.cs) — bails
    /// immediately if no city houses are marked at all (near-zero cost for every mobile
    /// movement everywhere else in the game, which is the overwhelming majority of calls),
    /// only doing the real bounds scan once there's at least one house to check against.
    /// All furniture inside a marked house's floor becomes walkable — matches "вся статика
    /// внутри отмеченных плиток автоматически" from the original request; there's no
    /// per-graphic allowlist to maintain.</summary>
    public static bool IsInsideAnyHouse(Map map, int x, int y)
    {
        EnsureScanned(); // cheap no-op after the first call ever made (bool-guarded)

        if (Houses.Count == 0)
        {
            return false;
        }

        foreach (var house in Houses)
        {
            if (!house.Deleted && house.AreaMap == map && house.ContainsColumn(x, y))
            {
                return true;
            }
        }

        return false;
    }
}
