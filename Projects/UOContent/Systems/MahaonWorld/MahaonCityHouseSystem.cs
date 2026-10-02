using System.Collections.Generic;
using Server.Accounting;
using Server.Mobiles;
using Server.Items;

namespace Server.Systems.MahaonWorld;

public static class MahaonCityHouseSystem
{
    private static readonly List<MahaonCityHouse> Houses = new();

    // Every marked floor column, for the movement hot path and for finding a house by spot.
    private static readonly Dictionary<(Map, int, int), List<MahaonCityHouse>> ByColumn = new();
    private static bool _scanned;

    /// <summary>Price of an apartment per tile of floor, unless the house has its own.</summary>
    public static int PricePerTile { get; private set; } = 2500;

    public static void Configure()
    {
        PricePerTile = ServerConfiguration.GetOrUpdateSetting("cityHouses.pricePerTile", 2500);

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
                Index(house);
            }
        }
    }

    public static void Register(MahaonCityHouse house)
    {
        EnsureScanned();
        if (!Houses.Contains(house))
        {
            Houses.Add(house);
        }

        Index(house);
        house.UpdateRegion();
    }

    public static void Unregister(MahaonCityHouse house)
    {
        Houses.Remove(house);
        Unindex(house);
    }

    /// <summary>Re-reads a house's tiles after they changed: index and region.</summary>
    public static void Reindex(MahaonCityHouse house)
    {
        Unindex(house);
        Index(house);
        house.UpdateRegion();
    }

    private static void Index(MahaonCityHouse house)
    {
        if (house.AreaMap == null)
        {
            return;
        }

        foreach (var t in house.Tiles)
        {
            var key = (house.AreaMap, t.X, t.Y);
            if (!ByColumn.TryGetValue(key, out var list))
            {
                ByColumn[key] = list = [];
            }

            if (!list.Contains(house))
            {
                list.Add(house);
            }
        }
    }

    private static void Unindex(MahaonCityHouse house)
    {
        var empty = new List<(Map, int, int)>();
        foreach (var (key, list) in ByColumn)
        {
            if (list.Remove(house) && list.Count == 0)
            {
                empty.Add(key);
            }
        }

        foreach (var key in empty)
        {
            ByColumn.Remove(key);
        }
    }

    public static IReadOnlyList<MahaonCityHouse> All()
    {
        EnsureScanned();
        return Houses;
    }

    public static MahaonCityHouse Find(Point3D loc, Map map)
    {
        EnsureScanned();

        if (map == null || !ByColumn.TryGetValue((map, loc.X, loc.Y), out var list))
        {
            return null;
        }

        foreach (var house in list)
        {
            if (!house.Deleted && house.Contains(loc, map))
            {
                return house;
            }
        }

        return null;
    }

    /// <summary>Cheap check for the movement hot path (Engines/Pathing/Movement.cs): one
    /// dictionary lookup. All furniture inside a marked house's floor becomes walkable — matches
    /// "вся статика внутри отмеченных плиток автоматически" from the original request; there's
    /// no per-graphic allowlist to maintain.</summary>
    public static bool IsInsideAnyHouse(Map map, int x, int y)
    {
        EnsureScanned(); // cheap no-op after the first call ever made (bool-guarded)
        return ByColumn.Count > 0 && ByColumn.ContainsKey((map, x, y));
    }

    // How far a door may stand above or below the floor it leads onto.
    private const int DoorZRange = 20;

    /// <summary>Owned city houses whose floor this door opens onto: the door stands on one of
    /// their tiles or beside one.</summary>
    public static void HousesAtDoor(Item door, List<MahaonCityHouse> result)
    {
        result.Clear();
        EnsureScanned();

        if (door.Map == null || ByColumn.Count == 0)
        {
            return;
        }

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (!ByColumn.TryGetValue((door.Map, door.X + dx, door.Y + dy), out var list))
                {
                    continue;
                }

                foreach (var house in list)
                {
                    if (!house.Deleted && !result.Contains(house) && house.HasTileNear(door.X + dx, door.Y + dy, door.Z, DoorZRange))
                    {
                        result.Add(house);
                    }
                }
            }
        }
    }

    private static readonly List<MahaonCityHouse> _doorHouses = [];

    /// <summary>
    /// Whether a city apartment keeps <paramref name="m"/> from opening this door: the door leads
    /// into an owned apartment, the mobile is no friend of it, and isn't inside already (anyone
    /// may let themselves out). A door shared by two apartments opens for the friends of either.
    /// </summary>
    public static bool DoorBlocks(Item door, Mobile m)
    {
        if (m.AccessLevel > AccessLevel.Player || ByColumn.Count == 0)
        {
            return false;
        }

        HousesAtDoor(door, _doorHouses);
        if (_doorHouses.Count == 0)
        {
            return false;
        }

        var owned = false;
        foreach (var house in _doorHouses)
        {
            if (house.Owner == null || house.IsFriend(m) || house.Contains(m.Location, m.Map))
            {
                return false;
            }

            owned = true;
        }

        return owned;
    }

    /// <summary>The city house <paramref name="m"/> owns, or any owned by a character on the same
    /// account — one per account.</summary>
    public static MahaonCityHouse OwnedBy(Mobile m)
    {
        EnsureScanned();

        foreach (var house in Houses)
        {
            if (house.Deleted || house.Owner == null)
            {
                continue;
            }

            if (house.Owner == m || m.Account is Account a && house.Owner.Account == a)
            {
                return house;
            }
        }

        return null;
    }

    /// <summary>Buys a free house for <paramref name="buyer"/> from the bank, as the sign's gump
    /// does. Returns a message either way.</summary>
    public static string TryBuy(Mobile buyer, MahaonCityHouse house, MahaonCityHouseSign sign)
    {
        if (house.Deleted || house.Owner != null)
        {
            return "Этот дом уже кому-то принадлежит.";
        }

        // A second home is sold only as an extension of the first: the neighbour on the same floor.
        var own = OwnedBy(buyer);
        if (buyer.AccessLevel == AccessLevel.Player && own != null && !own.Adjoins(house))
        {
            return "У тебя уже есть городской дом — второй продадут, только если он примыкает к твоему.";
        }

        var price = house.SalePrice;
        if (buyer.AccessLevel == AccessLevel.Player && !Banker.Withdraw(buyer, price))
        {
            return $"Не хватает золота в банке — дом стоит {price}.";
        }

        if (own != null && own.Adjoins(house))
        {
            Merge(own, house);
            return $"Соседний дом присоединён к «{own.Label}». Списано {price} золота.";
        }

        house.Owner = buyer;
        sign?.RefreshName();
        return $"Дом «{house.Label}» твой. Списано {price} золота.";
    }

    /// <summary>Gives the house back to the city for half its price: everything fixed in it is let
    /// go, friends and bans cleared, and it goes back on sale.</summary>
    public static string SellBack(MahaonCityHouse house, MahaonCityHouseSign sign)
    {
        var owner = house.Owner;
        if (owner == null)
        {
            return "Дом и так ничей.";
        }

        var refund = house.SalePrice / 2;
        house.ReleaseAll();
        house.ReturnAddons(owner);
        house.Owner = null;
        house.Friends.Clear();
        house.Bans.Clear();
        sign?.RefreshName();

        if (owner.AccessLevel == AccessLevel.Player)
        {
            Banker.Deposit(owner, refund);
        }

        return $"Дом «{house.Label}» продан городу, {refund} золота в банке.";
    }

    /// <summary>Whether furniture may go on this cell: inside one city house, owned by the placer,
    /// and the same house as the rest of the piece.</summary>
    public static bool CanPlaceAddon(Mobile from, Point3D p, Map map, ref MahaonCityHouse house)
    {
        var here = Find(p, map);
        if (here == null || from != null && !here.IsOwner(from) || house != null && house != here)
        {
            return false;
        }

        house = here;
        return true;
    }

    /// <summary>Joins a free neighbour into an owned house: one home, one sign, the joined floor's
    /// price added to the house's own.</summary>
    public static void Merge(MahaonCityHouse into, MahaonCityHouse joined)
    {
        var price = into.SalePrice + joined.SalePrice;
        var tiles = new List<Point3D>(joined.Tiles);

        into.AdoptBasement(joined);
        DeleteSigns(joined);
        joined.Delete();

        into.AddTiles(tiles);
        into.Price = price;
    }

    private static void DeleteSigns(MahaonCityHouse house)
    {
        foreach (var tile in house.Tiles)
        {
            foreach (var item in house.AreaMap.GetItemsInRange<MahaonCityHouseSign>(tile, 8))
            {
                if (item.House == house)
                {
                    item.Delete();
                }
            }
        }
    }

    /// <summary>Digs a cellar under the owner's house for half the ground floor's price.</summary>
    public static string TryBuyBasement(Mobile buyer, MahaonCityHouse house)
    {
        if (!house.IsOwner(buyer))
        {
            return "Подвал может заказать только хозяин дома.";
        }

        if (house.HasBasement)
        {
            return "Подвал у этого дома уже есть.";
        }

        var price = house.BasementPrice;
        if (buyer.AccessLevel == AccessLevel.Player && !Banker.Withdraw(buyer, price))
        {
            return $"Не хватает золота в банке — подвал стоит {price}.";
        }

        if (!house.BuildBasement())
        {
            if (buyer.AccessLevel == AccessLevel.Player)
            {
                Banker.Deposit(buyer, price);
            }

            return "Подвал здесь не выкопать — деньги вернулись в банк.";
        }

        return $"Подвал выкопан: люк в полу первого этажа. Списано {price} золота.";
    }
}
