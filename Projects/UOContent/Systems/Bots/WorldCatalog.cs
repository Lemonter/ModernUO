using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Engines.Pathing.Nav;
using Server.Mobiles;
using Server.Regions;
using Server.Targeting;

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

    internal bool ForgeSearched;
    internal Point3D ForgeLocation;
    internal object Forge; // a forge Item or a StaticTarget, as a smelt target takes it

    internal Item Ankh;

    internal bool SmithySearched;
    internal Point3D? SmithyStand;

    internal long VendorsRefreshedAt;
    internal bool VendorsSearched;
    internal readonly List<BaseVendor> Vendors = [];
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

    public static BotCity FindNearest(Map map, Point3D p, bool allowGuarded = true)
    {
        BotCity best = null;
        var bestDist = int.MaxValue;

        foreach (var city in _cities)
        {
            if (city.Map != map || !allowGuarded && BotSocialRules.IsGuarded(city.Region))
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

    private const int ForgeSearchRadius = 48;
    private const long VendorRefreshMs = 10 * 60_000;

    private static bool IsForgeId(int id) => id is 4017 or >= 6522 and <= 6569 or 11736;

    /// <summary>
    /// The town forge nearest the centre: a forge item, or a forge drawn in the map statics —
    /// the same two kinds the smelting target accepts. Found once per town and remembered.
    /// </summary>
    public static bool TryGetForge(BotCity city, out object forge, out Point3D location)
    {
        if (city.Forge is Item { Deleted: true })
        {
            city.ForgeSearched = false;
        }

        if (!city.ForgeSearched)
        {
            city.ForgeSearched = true;
            city.Forge = null;
            FindForge(city);
        }

        forge = city.Forge;
        location = city.ForgeLocation;
        return forge != null;
    }

    private static void FindForge(BotCity city)
    {
        var map = city.Map;
        var center = city.Center;
        var bestDist = int.MaxValue;

        foreach (var item in map.GetItemsInRange(center, ForgeSearchRadius))
        {
            if (item.Parent != null || !(item.GetType().IsDefined(typeof(ForgeAttribute), false) || IsForgeId(item.ItemID)))
            {
                continue;
            }

            var dist = (int)item.GetDistanceToSqrt(center);
            if (dist < bestDist)
            {
                bestDist = dist;
                city.Forge = item;
                city.ForgeLocation = item.Location;
            }
        }

        for (var dy = -ForgeSearchRadius; dy <= ForgeSearchRadius; dy++)
        {
            for (var dx = -ForgeSearchRadius; dx <= ForgeSearchRadius; dx++)
            {
                var x = center.X + dx;
                var y = center.Y + dy;
                if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                {
                    continue;
                }

                var dist = (int)System.Math.Sqrt(dx * dx + dy * dy);
                if (dist >= bestDist)
                {
                    continue;
                }

                foreach (var tile in map.Tiles.GetStaticTiles(x, y))
                {
                    if (IsForgeId(tile.ID))
                    {
                        bestDist = dist;
                        city.Forge = new StaticTarget(new Point3D(x, y, tile.Z), tile.ID);
                        city.ForgeLocation = new Point3D(x, y, tile.Z);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>The ankh nearest the town centre, for tithing.</summary>
    public static Item GetAnkh(BotCity city)
    {
        if (city.Ankh is { Deleted: false })
        {
            return city.Ankh;
        }

        Item best = null;
        var bestDist = double.MaxValue;

        foreach (var item in city.Map.GetItemsInRange(city.Center, VendorSearchRange))
        {
            if (item is Items.AnkhWest or Items.AnkhNorth)
            {
                var dist = item.GetDistanceToSqrt(city.Center);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = item;
                }
            }
        }

        city.Ankh = best;
        return best;
    }

    private static bool IsAnvilId(int id) => id is 4015 or 4016 or 11733 or 11734;

    /// <summary>
    /// A standable cell within reach of both the town forge and an anvil — where a smith works
    /// (the craft checks both within 2 tiles). Found once per town.
    /// </summary>
    public static bool TryGetSmithy(BotCity city, out Point3D stand)
    {
        if (!city.SmithySearched)
        {
            city.SmithySearched = true;
            city.SmithyStand = FindSmithy(city);
        }

        stand = city.SmithyStand ?? Point3D.Zero;
        return city.SmithyStand != null;
    }

    private static Point3D? FindSmithy(BotCity city)
    {
        if (!TryGetForge(city, out _, out var forge))
        {
            return null;
        }

        var map = city.Map;
        Point3D? anvil = null;

        foreach (var item in map.GetItemsInRange(forge, 4))
        {
            if (item.Parent == null && (item.GetType().IsDefined(typeof(AnvilAttribute), false) || IsAnvilId(item.ItemID)))
            {
                anvil = item.Location;
                break;
            }
        }

        for (var dy = -4; anvil == null && dy <= 4; dy++)
        {
            for (var dx = -4; anvil == null && dx <= 4; dx++)
            {
                foreach (var tile in map.Tiles.GetStaticTiles(forge.X + dx, forge.Y + dy))
                {
                    if (IsAnvilId(tile.ID))
                    {
                        anvil = new Point3D(forge.X + dx, forge.Y + dy, tile.Z);
                        break;
                    }
                }
            }
        }

        if (anvil == null)
        {
            return null;
        }

        var graph = NavSystem.GetGraph(map);
        if (graph == null)
        {
            return null;
        }

        var a = anvil.Value;
        for (var dy = -2; dy <= 2; dy++)
        {
            for (var dx = -2; dx <= 2; dx++)
            {
                var x = forge.X + dx;
                var y = forge.Y + dy;
                if (!Utility.InRange(new Point3D(x, y, 0), new Point3D(a.X, a.Y, 0), 2))
                {
                    continue;
                }

                var region = graph.Locate(x, y, forge.Z, 0);
                if (region >= 0)
                {
                    return new Point3D(x, y, forge.Z);
                }
            }
        }

        return null;
    }

    /// <summary>The town's vendors, refreshed every few minutes as they die and respawn.</summary>
    public static List<BaseVendor> GetVendors(BotCity city)
    {
        var now = Core.TickCount;
        if (!city.VendorsSearched || now - city.VendorsRefreshedAt >= VendorRefreshMs)
        {
            city.VendorsSearched = true;
            city.VendorsRefreshedAt = now;
            city.Vendors.Clear();

            foreach (var vendor in city.Map.GetMobilesInRange<BaseVendor>(city.Center, VendorSearchRange))
            {
                if (vendor.Alive && !vendor.Deleted)
                {
                    city.Vendors.Add(vendor);
                }
            }
        }

        city.Vendors.RemoveAll(v => v.Deleted || !v.Alive);
        return city.Vendors;
    }

    /// <summary>A town vendor that buys this item, nearest the centre first.</summary>
    public static BaseVendor FindBuyerFor(BotCity city, Item item)
    {
        foreach (var vendor in GetVendors(city))
        {
            foreach (var info in vendor.GetSellInfo())
            {
                if (info.IsSellable(item))
                {
                    return vendor;
                }
            }
        }

        return null;
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
