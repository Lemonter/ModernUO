using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>Replace the working tool that broke: a miner without a pickaxe buys one before
/// anything else, drawing on the bank when the purse is short.</summary>
public sealed class SupplyGoal : BotGoal
{
    public override string Name => "Закупка";

    private static (ResourceKind kind, Type tool) MainTrade(BotBrain brain)
    {
        var skills = brain.Bot.Skills;
        var mining = skills.Mining.Value;
        var lumber = skills.Lumberjacking.Value;
        var fishing = skills.Fishing.Value;

        if (mining >= lumber && mining >= fishing)
        {
            return (ResourceKind.Ore, typeof(Pickaxe));
        }

        return lumber >= fishing ? (ResourceKind.Wood, typeof(Hatchet)) : (ResourceKind.Fish, typeof(FishingPole));
    }

    public override double Score(BotBrain brain)
    {
        var (kind, _) = MainTrade(brain);
        return GatherAction.FindTool(brain.Bot, kind) == null ? 0.9 : 0;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var city = WorldCatalog.FindNearest(bot.Map, bot.Location);
        if (city == null)
        {
            return null;
        }

        var (_, toolType) = MainTrade(brain);

        BaseVendor seller = null;
        GenericBuyInfo stock = null;
        foreach (var vendor in WorldCatalog.GetVendors(city))
        {
            stock = BuyFromVendorAction.FindStock(vendor, toolType);
            if (stock != null)
            {
                seller = vendor;
                break;
            }
        }

        if (seller == null)
        {
            return null;
        }

        var steps = new List<BotAction>();
        var purse = bot.Backpack?.GetAmount(typeof(Gold)) ?? 0;

        if (purse < stock.Price)
        {
            var banker = WorldCatalog.GetBanker(city);
            if (banker == null || Banker.GetBalance(bot) < stock.Price)
            {
                return null;
            }

            steps.Add(new GoToAction(banker, 3, "в банк"));
            steps.Add(new WithdrawGoldAction(banker, stock.Price * 2));
        }

        steps.Add(new GoToAction(seller, 2, $"к торговцу {seller.Name}"));
        steps.Add(new BuyFromVendorAction(seller, toolType, 1));
        return steps;
    }
}
