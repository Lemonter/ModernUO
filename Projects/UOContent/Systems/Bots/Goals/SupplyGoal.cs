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

    public override bool IsUpkeep => true;


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

        // A tailor cuts its bolts and hides before sewing.
        if (BotCrafting.IsTailor(brain.Bot) && brain.Bot.Backpack?.FindItemByType<Scissors>() == null)
        {
            return typeof(Scissors);
        }

        return null;
    }

    private static readonly Type[] NecroReagents =
    [
        typeof(BatWing), typeof(GraveDust), typeof(DaemonBlood), typeof(NoxCrystal), typeof(PigIron)
    ];

    private static readonly Type[] MysticReagents =
    [
        typeof(Bone), typeof(DragonsBlood), typeof(DaemonBone), typeof(FertileDirt)
    ];

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

        // A scribe writes only what its own book holds.
        if (BotScribe.IsScribe(bot) && Spellbook.FindRegular(bot) == null)
        {
            (type, amount) = (typeof(Spellbook), 1);
            return true;
        }

        if (bot.Skills.Magery.Value >= 30 || BotCrafting.IsAlchemist(bot) || BotScribe.IsScribe(bot))
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

        if (bot.Skills.Necromancy.Value >= 30 && TryShort(pack, NecroReagents, 15, out type))
        {
            amount = 40;
            return true;
        }

        if (bot.Skills.Mysticism.Value >= 30 && TryShort(pack, MysticReagents, 15, out type))
        {
            amount = 40;
            return true;
        }

        if (bot.Skills.Musicianship.Value >= 30 && pack.FindItemByType<BaseInstrument>() == null)
        {
            (type, amount) = (typeof(BaseInstrument), 1);
            return true;
        }

        if (bot.Weapon is BaseRanged { AmmoType: not null } ranged && pack.GetAmount(ranged.AmmoType) < 50)
        {
            (type, amount) = (ranged.AmmoType, 150);
            return true;
        }

        if (BotStable.TryNeededFood(bot, out type, out amount))
        {
            return true;
        }

        if (BotCrafting.IsAlchemist(bot) && pack.GetAmount(typeof(Bottle)) < 20)
        {
            (type, amount) = (typeof(Bottle), 50);
            return true;
        }

        if (BotScribe.IsScribe(bot))
        {
            if (pack.GetAmount(typeof(BlankScroll)) < 20)
            {
                (type, amount) = (typeof(BlankScroll), 50);
                return true;
            }

            if (BotScribe.NextScrollToLearn(bot) is { } scroll)
            {
                (type, amount) = (scroll, 1);
                return true;
            }
        }

        // Treasure is dug with a shovel; a pickaxe is a weapon to the dig.
        if (BotTreasure.Usable(bot) != null && !TreasureMap.HasDiggingTool(bot))
        {
            (type, amount) = (typeof(Shovel), 1);
            return true;
        }

        if (BotTreasure.WantsLockpicks(bot))
        {
            (type, amount) = (typeof(Lockpick), 5);
            return true;
        }

        // A cook short of meat buys ribs from the butcher; the rest comes from its hunting.
        if (BotCrafting.IsCook(bot) && pack.GetAmount(typeof(RawRibs)) + pack.GetAmount(typeof(RawBird)) +
            pack.GetAmount(typeof(RawFishSteak)) < 10)
        {
            (type, amount) = (typeof(RawRibs), 20);
            return true;
        }

        // A tailor with nothing to sew buys cloth; leather comes from its own hunting.
        if (BotCrafting.IsTailor(bot) && pack.GetAmount(typeof(Cloth)) + pack.GetAmount(typeof(BoltOfCloth)) * 50 +
            pack.GetAmount(typeof(BaseLeather)) < 30)
        {
            (type, amount) = (typeof(Cloth), 60);
            return true;
        }

        if (pack.GetAmount(typeof(BaseHealPotion)) < 2 && Banker.GetBalance(bot) + pack.GetAmount(typeof(Gold)) > 2000)
        {
            (type, amount) = (typeof(GreaterHealPotion), 4);
            return true;
        }

        return false;
    }

    private static bool TryShort(Container pack, Type[] types, int min, out Type shortOf)
    {
        foreach (var t in types)
        {
            if (pack.GetAmount(t) < min)
            {
                shortOf = t;
                return true;
            }
        }

        shortOf = null;
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
        var city = BotSocialRules.TownFor(bot);
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

        if (typeof(SpellScroll).IsAssignableFrom(toolType))
        {
            steps.Add(new FillSpellbookAction());
        }

        return steps;
    }
}
