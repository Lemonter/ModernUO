using Server.Items;
using Server.Systems.MahaonAuction;
using Server.Systems.MahaonMetals;

namespace Server.Systems.Bots;

/// <summary>What bots produce and trade, and what it is worth. Prices start from a base table
/// and follow the town's auction: a bot undercuts the going rate a little, the way sellers do.</summary>
public static class BotGoods
{
    public static bool IsForSale(Item item) => item is MahaonIngot or MahaonOre or Log or Board or Fish or MahaonCoal;

    public static int BaseUnitPrice(Item item) => item switch
    {
        MahaonIngot ingot => TierPrice(ingot.Metal),
        MahaonOre ore     => TierPrice(ore.Metal) * 3 / 2,
        Board             => 6,
        Log               => 3,
        Fish              => 5,
        MahaonCoal        => 4,
        _                 => 0
    };

    private static int TierPrice(MahaonMetal metal) => MahaonMetalTable.Get(metal).Tier switch
    {
        MahaonMetalTier.Обычный    => 10,
        MahaonMetalTier.Уникальный => 40,
        MahaonMetalTier.Раритетный => 150,
        _                          => 500
    };

    /// <summary>Total base value of the goods in a pack — how much a trip to market is worth.</summary>
    public static long ValueCarried(Mobile bot)
    {
        var pack = bot.Backpack;
        if (pack == null)
        {
            return 0;
        }

        long value = 0;
        foreach (var item in pack.Items)
        {
            if (IsForSale(item))
            {
                value += (long)BaseUnitPrice(item) * item.Amount;
            }
        }

        return value;
    }

    /// <summary>Asking price per unit at this town's auction: a little under the cheapest
    /// comparable listing, never below most of the base price.</summary>
    public static long AskingUnitPrice(Item item, string city)
    {
        var basePrice = BaseUnitPrice(item);
        long cheapest = long.MaxValue;

        foreach (var listing in AuctionHouseSystem.ActiveListings(city))
        {
            var other = listing.Item;
            if (other == null || other.GetType() != item.GetType() || other.Amount <= 0)
            {
                continue;
            }

            if (other is MahaonIngot oi && item is MahaonIngot ii && oi.Metal != ii.Metal ||
                other is MahaonOre oo && item is MahaonOre io && oo.Metal != io.Metal)
            {
                continue;
            }

            var unit = listing.Price / other.Amount;
            if (unit < cheapest)
            {
                cheapest = unit;
            }
        }

        var floor = basePrice * 8 / 10;
        return cheapest == long.MaxValue ? basePrice + basePrice / 4 : System.Math.Max(floor, cheapest * 95 / 100);
    }
}
