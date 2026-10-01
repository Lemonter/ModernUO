using System.Collections.Generic;
using Server.Mobiles;
using Server.Regions;

namespace Server.Systems.Bots;

public sealed class BotCity
{
    public BotCity(string name, Map map, Point3D center, Region region)
    {
        Name = name;
        Map = map;
        Center = center;
        Region = region;
    }

    public string Name { get; }
    public Map Map { get; }
    public Point3D Center { get; }
    public Region Region { get; }

    internal Banker Banker;
    internal BaseHealer Healer;
}

/// <summary>
/// The places bots know about: towns, and the banker and healer of each. Towns come from the town
/// regions; vendors are found with a spatial query near the town centre and re-found when the one
/// remembered is gone (killed, deleted, respawned elsewhere).
/// </summary>
public static class WorldCatalog
{
    private const int VendorSearchRange = 64;

    private static readonly List<BotCity> _cities = [];

    public static IReadOnlyList<BotCity> Cities => _cities;

    public static void Rebuild()
    {
        _cities.Clear();

        foreach (var region in Region.Regions)
        {
            if (region is not TownRegion || region.Map == null || region.Map == Map.Internal || region.Area.Length == 0)
            {
                continue;
            }

            var center = region.GoLocation;
            if (center == Point3D.Zero)
            {
                var rect = region.Area[0];
                center = new Point3D((rect.Start.X + rect.End.X) / 2, (rect.Start.Y + rect.End.Y) / 2, rect.Start.Z);
            }

            _cities.Add(new BotCity(region.Name, region.Map, center, region));
        }
    }

    public static BotCity FindNearest(Map map, Point3D p)
    {
        BotCity best = null;
        var bestDist = int.MaxValue;

        foreach (var city in _cities)
        {
            if (city.Map != map)
            {
                continue;
            }

            var dist = (int)city.Center.GetDistanceToSqrt(p);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = city;
            }
        }

        return best;
    }

    public static BotCity FindByName(string name, Map map)
    {
        foreach (var city in _cities)
        {
            if (city.Map == map && city.Name == name)
            {
                return city;
            }
        }

        return null;
    }

    public static Banker GetBanker(BotCity city)
    {
        if (city.Banker is { Deleted: false, Alive: true } banker && banker.Map == city.Map)
        {
            return banker;
        }

        city.Banker = FindNearestVendor<Banker>(city);
        return city.Banker;
    }

    public static BaseHealer GetHealer(BotCity city)
    {
        if (city.Healer is { Deleted: false, Alive: true } healer && healer.Map == city.Map)
        {
            return healer;
        }

        city.Healer = FindNearestVendor<BaseHealer>(city);
        return city.Healer;
    }

    private static T FindNearestVendor<T>(BotCity city) where T : Mobile
    {
        T best = null;
        var bestDist = double.MaxValue;

        foreach (var m in city.Map.GetMobilesInRange<T>(city.Center, VendorSearchRange))
        {
            if (!m.Alive || m.Deleted)
            {
                continue;
            }

            var dist = m.GetDistanceToSqrt(city.Center);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = m;
            }
        }

        return best;
    }
}
