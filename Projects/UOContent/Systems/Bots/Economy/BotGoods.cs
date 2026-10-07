using Server.Items;
using Server.Systems.MahaonAuction;
using Server.Systems.MahaonMetals;

namespace Server.Systems.Bots;

/// <summary>What bots produce and trade, and what it is worth. Prices start from a base table
/// and follow the town's auction: a bot undercuts the going rate a little, the way sellers do.</summary>
public static class BotGoods
{
    public static bool IsRawGood(Item item) => item is MahaonIngot or MahaonOre or Log or Board or Fish or MahaonCoal;

    /// <summary>What fields and orchards yield.</summary>
    public static bool IsFarmGood(Item item) =>
        item is Cotton or Flax or SpoolOfThread or BoltOfCloth or WheatSheaf or Apple or Peach or Pear or Grapes or Cabbage or Carrot or EarOfCorn or
            Lettuce or Onion or Pumpkin or Turnip or Watermelon or HoneydewMelon or Cantaloupe or Squash or YellowGourd or
            GreenGourd;

    // Food the bot keeps for its animals rather than selling.
    private const int AnimalFoodReserve = 25;

    private static bool KeepsAsFood(Mobile bot, Item item) =>
        bot is Mobiles.PlayerMobile pm && pm.AllFollowers is { Count: > 0 } followers && item.Parent is Container pack &&
        IsEatenBy(followers, item) && pack.GetAmount(item.GetType()) <= AnimalFoodReserve;

    private static bool IsEatenBy(System.Collections.Generic.HashSet<Mobile> followers, Item item)
    {
        foreach (var m in followers)
        {
            if (m is Mobiles.BaseCreature pet && pet.ControlMaster != null && pet.CheckFoodPreference(item))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Something this bot made and doesn't wear or work with.</summary>
    public static bool IsProduct(Mobile bot, Item item) =>
        item.PlayerConstructed && IsCarried(bot, item) && item is not BaseTool && item is not Container &&
        BotCrafting.ProductValue(item) > 0;

    /// <summary>Loose in the bot's own pack or in its pack animal's — not in a bag, not worn.</summary>
    public static bool IsCarried(Mobile bot, Item item) =>
        item.Parent is Container c && (c == bot.Backpack ||
                                       c.Parent is Mobiles.BaseCreature owner && BotStable.IsPackAnimal(owner) && owner.ControlMaster == bot);

    /// <summary>How much more a bot hauls before a market trip when an animal carries for it.</summary>
    public static double TripCapacity(Mobile bot) => bot is Mobiles.PlayerMobile pm && BotStable.PackAnimal(pm) != null ? 2.5 : 1.0;

    /// <summary>What a bot puts on the market: raw goods it doesn't need for its own craft, and
    /// what it crafted.</summary>
    // A map the bot will dig itself, or one it has read (then only it can dig there), stays.
    public static bool IsForSale(Mobile bot, Item item) =>
        item is not TreasureMap { Decoder: not null } && !(item is TreasureMap map && BotTreasure.Keeps(bot, map)) && (
        IsRawGood(item) && !BotCrafting.KeepsForCraft(bot, item) ||
        IsProduct(bot, item) && !BotCrafting.KeepsForOwnUse(bot, item) ||
        IsFarmGood(item) && IsCarried(bot, item) && !KeepsAsFood(bot, item) && !WeaveGoal.KeepsForWeaving(bot, item) &&
        !BotCrafting.KeepsForCraft(bot, item) ||
        bot.GetBrain() is { } brain && brain.IsLoot(item) && IsCarried(bot, item));

    public static int BaseUnitPrice(Item item) => item switch
    {
        _ when item.PlayerConstructed && BotCrafting.ProductValue(item) > 0 => BotCrafting.ProductValue(item),
        MahaonIngot ingot => TierPrice(ingot.Metal),
        MahaonOre ore     => TierPrice(ore.Metal) * 3 / 2,
        Board             => 6,
        Log               => 3,
        Fish              => 5,
        MahaonCoal        => 4,
        Cotton or Flax    => 40,
        SpoolOfThread     => 8,
        BaseHides         => 2,
        BaseLeather       => 3,
        Cloth or UncutCloth => 1,
        BoltOfCloth       => 45,
        WheatSheaf        => 2,
        Food              => 2,
        BaseWeapon or BaseArmor or BaseJewel => 30,
        BaseReagent       => 3,
        SpellScroll       => 10,
        // Near what Guido asks for them: a scroll nobody here could read still has a buyer.
        PowerScroll { Value: <= 105 } => 4000,
        PowerScroll { Value: <= 110 } => 10000,
        PowerScroll { Value: <= 115 } => 20000,
        PowerScroll       => 40000,
        TreasureMap { Completed: false } map => 150 * System.Math.Max(1, map.Level),
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
        var value = ValueIn(bot, bot.Backpack);

        if (bot is Mobiles.PlayerMobile pm)
        {
            value += ValueIn(bot, BotStable.ReachablePack(pm, 12));
        }

        return value;
    }

    private static long ValueIn(Mobile bot, Container pack)
    {
        if (pack == null)
        {
            return 0;
        }

        long value = 0;
        foreach (var item in pack.Items)
        {
            if (IsForSale(bot, item))
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
