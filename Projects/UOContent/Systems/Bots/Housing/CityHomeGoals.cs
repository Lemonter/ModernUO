using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonWorld;

namespace Server.Systems.Bots;

/// <summary>
/// City apartments for bots: a bot with money enough buys a free apartment in its town — the
/// cheapest that fits its purse — through the same sign purchase as a player, then furnishes it
/// as a house: a chest under lock, a forge and an anvil for a smith, a loom and a spinning wheel
/// for a weaver.
/// </summary>
public static class BotCityHomes
{
    // What stays in the bank after buying, for the furniture.
    public const long Reserve = 15_000;

    public static MahaonCityHouse CityHome(Mobile bot) => MahaonCityHouseSystem.OwnedBy(bot);

    /// <summary>A free cell of the apartment's floor to stand on.</summary>
    public static Point3D Inside(MahaonCityHouse house)
    {
        foreach (var t in house.Tiles)
        {
            if (house.AreaMap.CanFit(t, 16, false, false))
            {
                return t;
            }
        }

        return house.Tiles[0];
    }

    public static MahaonCityHouseSign FindSign(MahaonCityHouse house)
    {
        foreach (var tile in house.Tiles)
        {
            foreach (var sign in house.AreaMap.GetItemsInRange<MahaonCityHouseSign>(tile, 8))
            {
                if (sign.House == house)
                {
                    return sign;
                }
            }
        }

        return null;
    }

    /// <summary>The cheapest free apartment of the town nearest <paramref name="near"/> that a
    /// budget covers.</summary>
    public static MahaonCityHouse CheapestFree(Map map, Point3D near, long budget)
    {
        var city = WorldCatalog.FindNearest(map, near);
        if (city == null)
        {
            return null;
        }

        MahaonCityHouse best = null;
        foreach (var house in MahaonCityHouseSystem.All())
        {
            if (house.Deleted || house.Owner != null || house.AreaMap != map || house.SalePrice > budget ||
                house.Region?.Parent is not { } parent || !parent.IsPartOf(city.Region))
            {
                continue;
            }

            if (best == null || house.SalePrice < best.SalePrice)
            {
                best = house;
            }
        }

        return best;
    }

    public static bool HasAddon<T>(MahaonCityHouse house) where T : BaseAddon
    {
        foreach (var item in house.Addons)
        {
            if (item is T { Deleted: false })
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryGetSmithy(MahaonCityHouse house, out Point3D stand)
    {
        stand = default;
        Item forge = null, anvil = null;

        foreach (var item in house.Addons)
        {
            if (item is SmallForgeAddon { Deleted: false })
            {
                forge = item;
            }
            else if (item is AnvilEastAddon { Deleted: false })
            {
                anvil = item;
            }
        }

        if (forge == null || anvil == null)
        {
            return false;
        }

        foreach (var t in house.Tiles)
        {
            if (Utility.InRange(t, forge.Location, 2) && Utility.InRange(t, anvil.Location, 2) && house.AreaMap.CanFit(t, 16, false, false))
            {
                stand = t;
                return true;
            }
        }

        return false;
    }

    public static bool TryGetTextiles(MahaonCityHouse house, out Item wheel, out Item loom)
    {
        wheel = null;
        loom = null;

        foreach (var item in house.Addons)
        {
            if (item is ISpinningWheel && !item.Deleted)
            {
                wheel = item;
            }
            else if (item is ILoom && !item.Deleted)
            {
                loom = item;
            }
        }

        return wheel != null && loom != null;
    }
}

public sealed class BuyCityHomeGoal : BotGoal
{
    public override string Name => "Покупка квартиры";

    public override string[] News => ["Купил квартиру в городе, теперь свой угол.", "Наконец-то не под открытым небом."];

    private static MahaonCityHouse Candidate(BotBrain brain)
    {
        var bot = brain.Bot;
        if (bot is not BotMobile || BotSocialRules.IsOutlaw(bot) || BotCityHomes.CityHome(bot) != null ||
            BotHousing.OwnHouse(bot) != null)
        {
            return null;
        }

        var budget = BotShopping.Funds(bot) - BotCityHomes.Reserve;
        return budget > 0 ? BotCityHomes.CheapestFree(bot.Map, bot.Location, budget) : null;
    }

    public override double Score(BotBrain brain) => Candidate(brain) != null ? 0.5 + BotBrain.Trait(brain.Caution) * 0.2 : 0;

    public override List<BotAction> Plan(BotBrain brain)
    {
        if (Candidate(brain) is not { } house || BotCityHomes.FindSign(house) is not { } sign)
        {
            return null;
        }

        return [new GoToAction(sign.Map, sign.Location, 2, "к дому на продажу"), new BuyCityHomeAction(house, sign)];
    }
}

public sealed class BuyCityHomeAction : BotAction
{
    private readonly MahaonCityHouse _house;
    private readonly MahaonCityHouseSign _sign;

    public BuyCityHomeAction(MahaonCityHouse house, MahaonCityHouseSign sign)
    {
        _house = house;
        _sign = sign;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        MahaonCityHouseSystem.TryBuy(bot, _house, _sign);

        if (_house.Owner != bot)
        {
            return BotActionResult.Failed();
        }

        brain.Home = BotCityHomes.Inside(_house);
        brain.HomeMap = _house.AreaMap;
        BotSpeech.SayText(bot, "Своя квартира в городе — красота!");
        return BotActionResult.Done(2000);
    }

    public override string Describe(BotBrain brain) => "Покупает квартиру";
}

/// <summary>Furnishing a city apartment, as <see cref="FurnishGoal"/> does a house — without the
/// fence, which a town room has no yard for.</summary>
public sealed class FurnishCityHomeGoal : BotGoal
{
    public override string Name => "Обустройство квартиры";

    public override bool IsUpkeep => true;

    internal static HouseFitting? NextFitting(Mobile bot, MahaonCityHouse house)
    {
        if (house.Secures.Count == 0)
        {
            return HouseFitting.Chest;
        }

        if (bot.Skills.Blacksmith.Value >= 40 && !BotCityHomes.HasAddon<SmallForgeAddon>(house))
        {
            return HouseFitting.Forge;
        }

        if (bot.Skills.Blacksmith.Value >= 40 && !BotCityHomes.HasAddon<AnvilEastAddon>(house))
        {
            return HouseFitting.Anvil;
        }

        var weaver = bot.GetBrain() is { Diligence: >= 50 } || bot.Skills.Tailoring.Value >= 40;
        if (weaver && !BotCityHomes.HasAddon<LoomEastAddon>(house))
        {
            return HouseFitting.Loom;
        }

        if (weaver && !BotCityHomes.HasAddon<SpinningWheelEastAddon>(house))
        {
            return HouseFitting.SpinningWheel;
        }

        return null;
    }

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (BotCityHomes.CityHome(bot) is not { } house || NextFitting(bot, house) is not { } fitting ||
            BotShopping.Funds(bot) < FurnishGoal.CommissionPrice(fitting) + 2000)
        {
            return 0;
        }

        return 0.45 + BotBrain.Trait(brain.Diligence) * 0.3;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        if (BotCityHomes.CityHome(bot) is not { } house || NextFitting(bot, house) is not { } fitting)
        {
            return null;
        }

        var steps = FurnishGoal.PlanCommission(bot, WorldCatalog.FindNearest(house.AreaMap, house.Tiles[0]), fitting);
        if (steps == null)
        {
            return null;
        }

        steps.Add(new GoToAction(house.AreaMap, BotCityHomes.Inside(house), 1, "домой"));
        steps.Add(new FitCityHomeAction(house, fitting));
        return steps;
    }
}

public sealed class FitCityHomeAction : BotAction
{
    private readonly MahaonCityHouse _house;
    private readonly HouseFitting _fitting;

    public FitCityHomeAction(MahaonCityHouse house, HouseFitting fitting)
    {
        _house = house;
        _fitting = fitting;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var pack = bot.Backpack;
        if (_house.Deleted || _house.Owner != bot || pack == null)
        {
            return BotActionResult.Failed();
        }

        if (_fitting == HouseFitting.Chest)
        {
            if (pack.FindItemByType<WoodenChest>() is not { } chest)
            {
                return BotActionResult.Failed();
            }

            foreach (var t in _house.Tiles)
            {
                if (_house.AreaMap.CanFit(t, 16, false, false))
                {
                    chest.MoveToWorld(t, _house.AreaMap);
                    _house.LockDown(bot, chest, true);
                    return _house.IsLockedDown(chest) ? BotActionResult.Done(1500) : BotActionResult.Failed();
                }
            }

            return BotActionResult.Failed();
        }

        BaseAddonDeed deed = _fitting switch
        {
            HouseFitting.Forge => pack.FindItemByType<SmallForgeDeed>(),
            HouseFitting.Anvil => pack.FindItemByType<AnvilEastDeed>(),
            HouseFitting.Loom  => pack.FindItemByType<LoomEastDeed>(),
            _                  => pack.FindItemByType<SpinningWheelEastDeed>()
        };

        if (deed == null)
        {
            return BotActionResult.Failed();
        }

        foreach (var t in _house.Tiles)
        {
            var addon = deed.Addon;
            Multis.BaseHouse none = null;

            if (addon.CouldFit(t, _house.AreaMap, bot, ref none) == AddonFitResult.Valid && none == null)
            {
                addon.MoveToWorld(t, _house.AreaMap);
                _house.Addons.Add(addon);
                deed.Delete();
                return BotActionResult.Done(1500);
            }

            addon.Delete();
        }

        return BotActionResult.Failed();
    }

    public override string Describe(BotBrain brain) => $"Обставляет квартиру: {_fitting}";
}
