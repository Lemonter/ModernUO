using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Systems.MahaonWorld;

namespace Server.Systems.Bots;

/// <summary>
/// Building a house: a guild's first house, paid for by its richest member, or a private house
/// for a bot that has grown rich. The bot walks out to a spot that passes the placement check and
/// builds there.
/// </summary>
public sealed class BuildHouseGoal : BotGoal
{
    // A guild house goes up once the guild is big enough to need one.
    private const int MinGuildMembers = 4;

    // What stays in the bank after building, for the furnishing and the fence.
    private const long Reserve = 20_000;

    public override string Name => "Строительство";

    public override string[] News => ["Дом себе поставил, заходи как-нибудь.", "Строился, все деньги ушли."];

    private static bool BuildsForGuild(Mobile bot, out Guild guild)
    {
        guild = bot.Guild as Guild;
        if (guild == null || guild.Members.Count < MinGuildMembers || BotHousing.GuildHouse(guild) != null)
        {
            return false;
        }

        // The richest member at hand pays.
        var funds = BotShopping.Funds(bot);
        foreach (var member in guild.Members)
        {
            if (member != bot && member is BotMobile { Deleted: false } other && BotShopping.Funds(other) > funds)
            {
                return false;
            }
        }

        return true;
    }

    private static bool Wants(BotBrain brain, out bool forGuild, out HousePlacementEntry entry)
    {
        var bot = brain.Bot;
        forGuild = false;
        entry = null;

        if (bot is not BotMobile || BotHousing.OwnHouse(bot) != null || BotSocialRules.IsOutlaw(bot) || brain.HomeMap != bot.Map)
        {
            return false;
        }

        var budget = BotShopping.Funds(bot) - Reserve;

        if (BuildsForGuild(bot, out _))
        {
            forGuild = true;
        }
        else if (BotShopping.Funds(bot) < BotHousing.RichThreshold || BotHousing.HomeOf(bot) != null ||
                 BotHousing.PrivateHouseCount() >= BotHousing.MaxPrivateHouses)
        {
            return false;
        }

        entry = BotHousing.ChooseEntry(budget, forGuild);
        return entry != null;
    }

    public override double Score(BotBrain brain) => Wants(brain, out var forGuild, out _) ? forGuild ? 0.7 : 0.55 : 0;

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        if (!Wants(brain, out var forGuild, out var entry) ||
            BotHousing.FindSpot(bot, entry, brain.Home) is not { } spot)
        {
            return null;
        }

        return [new GoToAction(bot.Map, spot, 6, "на место для дома"), new PlaceHouseAction(entry, spot, forGuild)];
    }
}

public sealed class PlaceHouseAction : BotAction
{
    private readonly HousePlacementEntry _entry;
    private readonly Point3D _spot;
    private readonly bool _forGuild;

    public PlaceHouseAction(HousePlacementEntry entry, Point3D spot, bool forGuild)
    {
        _entry = entry;
        _spot = spot;
        _forGuild = forGuild;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var house = BotHousing.Place(bot, _entry, _spot, _forGuild);
        if (house == null)
        {
            return BotActionResult.Failed();
        }

        brain.Home = BotHousing.Inside(house);
        brain.HomeMap = house.Map;
        BotSpeech.SayText(bot, _forGuild ? "Вот и наш гильдейский дом!" : "Наконец-то свой дом!");
        return BotActionResult.Done(2000);
    }

    public override string Describe(BotBrain brain) => "Строит дом";
}

/// <summary>What a bot puts in its house.</summary>
public enum HouseFitting
{
    Chest,
    Forge,
    Anvil,
    Loom,
    SpinningWheel,
    Fence
}

/// <summary>
/// Furnishing the house the bot owns: a chest for its things, a forge and an anvil for a smith, a
/// loom and a spinning wheel for a weaver — a guild house gets everything — and a fence round the
/// yard, whose land then yields more. Furniture that a carpenter crafts is commissioned from the
/// town carpenter; it arrives as the usual deed and goes in through the deed's own fit rules.
/// </summary>
public sealed class FurnishGoal : BotGoal
{
    private const int FenceRadius = 3;

    public override string Name => "Обустройство дома";

    public override bool IsUpkeep => true;

    public override string[] News => ["Обставил дом, теперь уютно.", "Поставил в доме кузню, сам себе мастер."];

    internal static HouseFitting? NextFitting(Mobile bot, BaseHouse house)
    {
        var guildHouse = BotHousing.IsGuildHouse(house);

        if (!HasChest(house))
        {
            return HouseFitting.Chest;
        }

        if ((guildHouse || bot.Skills.Blacksmith.Value >= 40) && !HasAddon<SmallForgeAddon>(house))
        {
            return HouseFitting.Forge;
        }

        if ((guildHouse || bot.Skills.Blacksmith.Value >= 40) && !HasAddon<AnvilEastAddon>(house))
        {
            return HouseFitting.Anvil;
        }

        var weaver = bot.GetBrain() is { Diligence: >= 50 } || bot.Skills.Tailoring.Value >= 40;
        if ((guildHouse || weaver) && !HasAddon<LoomEastAddon>(house))
        {
            return HouseFitting.Loom;
        }

        if ((guildHouse || weaver) && !HasAddon<SpinningWheelEastAddon>(house))
        {
            return HouseFitting.SpinningWheel;
        }

        if (!MahaonHouseFenceSystem.HasFence(house))
        {
            return HouseFitting.Fence;
        }

        return null;
    }

    private static bool HasChest(BaseHouse house)
    {
        foreach (var info in house.Secures ?? [])
        {
            if (info.Item is BaseContainer { Deleted: false })
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAddon<T>(BaseHouse house) where T : BaseAddon
    {
        foreach (var addon in house.Addons)
        {
            if (addon is T { Deleted: false })
            {
                return true;
            }
        }

        return false;
    }

    internal static int CommissionPrice(HouseFitting fitting) =>
        fitting switch
        {
            HouseFitting.Chest         => 150,
            HouseFitting.Forge         => 3000,
            HouseFitting.Anvil         => 2000,
            HouseFitting.Loom          => 2500,
            HouseFitting.SpinningWheel => 1500,
            _                          => MahaonHouseFenceSystem.GetCost(FenceRadius)
        };

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (BotHousing.OwnHouse(bot) is not { } house || NextFitting(bot, house) is not { } fitting ||
            BotShopping.Funds(bot) < CommissionPrice(fitting) + 2000)
        {
            return 0;
        }

        return 0.5 + BotBrain.Trait(brain.Diligence) * 0.3;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        if (BotHousing.OwnHouse(bot) is not { } house || NextFitting(bot, house) is not { } fitting)
        {
            return null;
        }

        if (fitting == HouseFitting.Fence)
        {
            return [new GoToAction(house.Map, house.BanLocation, 3, "к дому"), new BuildFenceAction(house, FenceRadius)];
        }

        var steps = PlanCommission(bot, WorldCatalog.FindNearest(house.Map, house.Location), fitting);
        if (steps == null)
        {
            return null;
        }

        steps.Add(new GoToAction(house.Map, BotHousing.Inside(house), 1, "домой"));
        steps.Add(new FitAction(house, fitting));
        return steps;
    }

    /// <summary>The walk to the town carpenter, money fetched from the bank if needed, and the
    /// order itself — the piece then waits in the bot's pack.</summary>
    internal static List<BotAction> PlanCommission(Mobile bot, BotCity city, HouseFitting fitting)
    {
        Carpenter carpenter = null;
        if (city != null)
        {
            foreach (var vendor in WorldCatalog.GetVendors(city))
            {
                if (vendor is Carpenter c)
                {
                    carpenter = c;
                    break;
                }
            }
        }

        if (carpenter == null)
        {
            return null;
        }

        var steps = new List<BotAction>();
        var price = CommissionPrice(fitting);
        var purse = bot.Backpack?.GetAmount(typeof(Gold)) ?? 0;

        if (purse < price)
        {
            var banker = WorldCatalog.GetBanker(city);
            if (banker == null || Banker.GetBalance(bot) < price - purse)
            {
                return null;
            }

            steps.Add(new GoToAction(banker, 3, "в банк"));
            steps.Add(new WithdrawGoldAction(banker, price - purse));
        }

        steps.Add(new GoToAction(carpenter, 2, $"к плотнику {carpenter.Name}"));
        steps.Add(new CommissionAction(carpenter, fitting));
        return steps;
    }
}

/// <summary>Pays the carpenter for a piece of furniture and takes it away — a deed for anything
/// built in place, the chest itself otherwise.</summary>
public sealed class CommissionAction : BotAction
{
    private readonly Carpenter _carpenter;
    private readonly HouseFitting _fitting;

    public CommissionAction(Carpenter carpenter, HouseFitting fitting)
    {
        _carpenter = carpenter;
        _fitting = fitting;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var price = FurnishGoal.CommissionPrice(_fitting);

        if (_carpenter.Deleted || !bot.InRange(_carpenter, 4) || bot.Backpack?.ConsumeTotal(typeof(Gold), price) != true)
        {
            return BotActionResult.Failed();
        }

        Item piece = _fitting switch
        {
            HouseFitting.Chest         => new WoodenChest(),
            HouseFitting.Forge         => new SmallForgeDeed(),
            HouseFitting.Anvil         => new AnvilEastDeed(),
            HouseFitting.Loom          => new LoomEastDeed(),
            _                          => new SpinningWheelEastDeed()
        };

        bot.Backpack.DropItem(piece);
        bot.Direction = bot.GetDirectionTo(_carpenter);
        _carpenter.SayTo(bot, true, "Готово, забирай. Хорошая работа, сам делал.");
        return BotActionResult.Done(2000);
    }

    public override string Describe(BotBrain brain) => $"Заказывает у плотника: {_fitting}";
}

/// <summary>Puts the commissioned piece in the house: an addon through its deed's fit rules on a
/// free floor cell, a chest set down and secured.</summary>
public sealed class FitAction : BotAction
{
    private readonly BaseHouse _house;
    private readonly HouseFitting _fitting;

    public FitAction(BaseHouse house, HouseFitting fitting)
    {
        _house = house;
        _fitting = fitting;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var pack = bot.Backpack;
        if (_house.Deleted || pack == null)
        {
            return BotActionResult.Failed();
        }

        if (_fitting == HouseFitting.Chest)
        {
            return pack.FindItemByType<WoodenChest>() is { } chest && PlaceChest(bot, chest)
                ? BotActionResult.Done(1500)
                : BotActionResult.Failed();
        }

        BaseAddonDeed deed = _fitting switch
        {
            HouseFitting.Forge => pack.FindItemByType<SmallForgeDeed>(),
            HouseFitting.Anvil => pack.FindItemByType<AnvilEastDeed>(),
            HouseFitting.Loom  => pack.FindItemByType<LoomEastDeed>(),
            _                  => pack.FindItemByType<SpinningWheelEastDeed>()
        };

        return deed != null && PlaceAddon(bot, deed) ? BotActionResult.Done(1500) : BotActionResult.Failed();
    }

    private bool PlaceAddon(Mobile bot, BaseAddonDeed deed)
    {
        foreach (var p in BotHousing.FloorPoints(_house))
        {
            var addon = deed.Addon;
            BaseHouse house = null;

            if (addon.CouldFit(p, _house.Map, bot, ref house) == AddonFitResult.Valid && house == _house)
            {
                addon.MoveToWorld(p, _house.Map);
                _house.Addons.Add(addon);
                deed.Delete();
                return true;
            }

            addon.Delete();
        }

        return false;
    }

    private bool PlaceChest(Mobile bot, Item chest)
    {
        foreach (var p in BotHousing.FloorPoints(_house))
        {
            if (!_house.Map.CanFit(p, 16, false, false))
            {
                continue;
            }

            chest.MoveToWorld(p, _house.Map);
            _house.AddSecure(bot, chest);

            if (chest.IsSecure)
            {
                return true;
            }

            pack(bot).DropItem(chest);
            return false;
        }

        return false;

        static Container pack(Mobile m) => m.Backpack;
    }

    public override string Describe(BotBrain brain) => $"Обставляет дом: {_fitting}";
}

/// <summary>Has the house's yard fenced, paying as the fence gump does.</summary>
public sealed class BuildFenceAction : BotAction
{
    private readonly BaseHouse _house;
    private readonly int _radius;

    public BuildFenceAction(BaseHouse house, int radius)
    {
        _house = house;
        _radius = radius;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        if (_house.Deleted)
        {
            return BotActionResult.Failed();
        }

        MahaonHouseFenceSystem.Build(brain.Bot, _house, _radius);
        return MahaonHouseFenceSystem.HasFence(_house) ? BotActionResult.Done(1500) : BotActionResult.Failed();
    }

    public override string Describe(BotBrain brain) => "Ставит ограду";
}
