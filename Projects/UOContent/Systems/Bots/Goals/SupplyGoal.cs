using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>Keep the kit stocked: a broken working tool first, then consumables — bandages,
/// reagents, ammunition, potions — drawing on the bank when the purse is short.</summary>
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

    /// <summary>The tool to buy, or null when nothing the bot works with is missing: the gathering
    /// tool of its main trade first, then the tool of any craft it practises.</summary>
    private static Type NeededTool(BotBrain brain)
    {
        var (kind, gatherTool) = MainTrade(brain);
        if (GatherAction.FindTool(brain.Bot, kind) == null)
        {
            return gatherTool;
        }

        foreach (var system in BotCrafting.Systems)
        {
            if (system != null && BotCrafting.IsCrafter(brain.Bot, system) && BotCrafting.FindTool(brain.Bot, system) == null)
            {
                return BotCrafting.ToolTypeFor(system);
            }
        }

        return null;
    }

    private static readonly Type[] Reagents =
    [
        typeof(BlackPearl), typeof(Bloodmoss), typeof(Garlic), typeof(Ginseng), typeof(MandrakeRoot),
        typeof(Nightshade), typeof(SpidersSilk), typeof(SulfurousAsh)
    ];

    /// <summary>The next consumable the bot is short of and how many to buy: bandages for a
    /// healer, reagents for a caster, ammunition for an archer, a few heal potions for everyone.</summary>
    private static bool TryNextConsumable(BotBrain brain, out Type type, out int amount)
    {
        var bot = brain.Bot;
        var pack = bot.Backpack;
        type = null;
        amount = 0;

        if (pack == null)
        {
            return false;
        }

        if (bot.Skills.Healing.Value >= 20 && pack.GetAmount(typeof(Bandage)) < 20)
        {
            (type, amount) = (typeof(Bandage), 60);
            return true;
        }

        if (bot.Skills.Magery.Value >= 30)
        {
            foreach (var reagent in Reagents)
            {
                if (pack.GetAmount(reagent) < 15)
                {
                    (type, amount) = (reagent, 40);
                    return true;
                }
            }
        }

        if (bot.Weapon is BaseRanged ranged && pack.GetAmount(ranged.AmmoType) < 50)
        {
            (type, amount) = (ranged.AmmoType, 150);
            return true;
        }

        if (pack.GetAmount(typeof(BaseHealPotion)) < 2 && Banker.GetBalance(bot) + pack.GetAmount(typeof(Gold)) > 2000)
        {
            (type, amount) = (typeof(GreaterHealPotion), 4);
            return true;
        }

        return false;
    }

    public override double Score(BotBrain brain)
    {
        if (NeededTool(brain) != null)
        {
            return 0.9;
        }

        return TryNextConsumable(brain, out _, out _) ? 0.65 : 0;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var city = WorldCatalog.FindNearest(bot.Map, bot.Location);
        if (city == null)
        {
            return null;
        }

        var toolType = NeededTool(brain);
        var amount = 1;
        if (toolType == null && !TryNextConsumable(brain, out toolType, out amount))
        {
            return null;
        }

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

        var cost = stock.Price * Math.Min(amount, stock.Amount);
        if (purse < cost)
        {
            var banker = WorldCatalog.GetBanker(city);
            if (banker == null || Banker.GetBalance(bot) < cost - purse)
            {
                return null;
            }

            steps.Add(new GoToAction(banker, 3, "в банк"));
            steps.Add(new WithdrawGoldAction(banker, cost - purse + stock.Price));
        }

        steps.Add(new GoToAction(seller, 2, $"к торговцу {seller.Name}"));
        steps.Add(new BuyFromVendorAction(seller, toolType, amount));
        return steps;
    }
}
