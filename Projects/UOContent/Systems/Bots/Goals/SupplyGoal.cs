using System;
using System.Collections.Generic;
using Server.Items;
using Server.Engines.Craft;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>Keep the kit stocked: a broken working tool first, then consumables — bandages,
/// reagents, ammunition, potions — drawing on the bank when the purse is short.</summary>
public sealed class SupplyGoal : BotGoal
{
    public override string Name => "Закупка";

    public override bool IsUpkeep => true;


    // Ниже этого навыка добыча — не ремесло бота, и инструмент для неё не покупается. Без порога
    // любой бот, да и одержимый персонаж без кирки, получал «купить кирку» с оценкой 0,9.
    private const double GatherTradeSkill = 20;

    private static (ResourceKind kind, Type tool) MainTrade(BotBrain brain)
    {
        var skills = brain.Bot.Skills;
        var mining = skills.Mining.Value;
        var lumber = skills.Lumberjacking.Value;
        var fishing = skills.Fishing.Value;

        if (Math.Max(mining, Math.Max(lumber, fishing)) < GatherTradeSkill)
        {
            return (ResourceKind.Ore, null);
        }

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
        if (gatherTool != null && GatherAction.FindTool(brain.Bot, kind) == null)
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

    /// <summary>Every consumable the bot is short of and how many to buy, most pressing first:
    /// bandages for a healer, reagents for a caster, ammunition for an archer, a few heal potions
    /// for everyone. All of them, not just the first: one the town doesn't sell must not keep the
    /// rest from ever being bought.</summary>
    private static List<(Type type, int amount)> Shortages(BotBrain brain)
    {
        var bot = brain.Bot;
        var pack = bot.Backpack;
        var needs = new List<(Type type, int amount)>();

        if (pack == null)
        {
            return needs;
        }

        if (bot.Skills.Healing.Value >= 20 && pack.GetAmount(typeof(Bandage)) < 20)
        {
            needs.Add((typeof(Bandage), 60));
        }

        // A scribe writes only what its own book holds.
        if (BotScribe.IsScribe(bot) && Spellbook.FindRegular(bot) == null)
        {
            needs.Add((typeof(Spellbook), 1));
        }

        if (bot.Skills.Magery.Value >= 30 || BotCrafting.IsAlchemist(bot) || BotScribe.IsScribe(bot))
        {
            foreach (var reagent in Reagents)
            {
                if (pack.GetAmount(reagent) < 15)
                {
                    needs.Add((reagent, 40));
                }
            }
        }

        if (bot.Skills.Necromancy.Value >= 30)
        {
            AddShort(needs, pack, NecroReagents, 15, 40);
        }

        if (bot.Skills.Mysticism.Value >= 30)
        {
            AddShort(needs, pack, MysticReagents, 15, 40);
        }

        if (bot.Skills.Musicianship.Value >= 30 && pack.FindItemByType<BaseInstrument>() == null)
        {
            needs.Add((typeof(BaseInstrument), 1));
        }

        if (bot.Weapon is BaseRanged { AmmoType: not null } ranged && pack.GetAmount(ranged.AmmoType) < 50)
        {
            needs.Add((ranged.AmmoType, 150));
        }

        if (BotStable.TryNeededFood(bot, out var food, out var foodAmount))
        {
            needs.Add((food, foodAmount));
        }

        if (BotCrafting.IsAlchemist(bot) && pack.GetAmount(typeof(Bottle)) < 20)
        {
            needs.Add((typeof(Bottle), 50));
        }

        // Травнику из лавки нужно только то, чего не соберёшь в поле: чеснок для
        // противоядия и бинты для шин. Травы он собирает сам.
        if (BotCrafting.IsHerbalist(bot))
        {
            if (pack.GetAmount(typeof(Garlic)) < 5)
            {
                needs.Add((typeof(Garlic), 20));
            }

            if (pack.GetAmount(typeof(Bandage)) < 5)
            {
                needs.Add((typeof(Bandage), 20));
            }
        }

        if (BotScribe.IsScribe(bot))
        {
            if (pack.GetAmount(typeof(BlankScroll)) < 20)
            {
                needs.Add((typeof(BlankScroll), 50));
            }

            if (BotScribe.NextScrollToLearn(bot) is { } scroll)
            {
                needs.Add((scroll, 1));
            }
        }

        // Treasure is dug with a shovel; a pickaxe is a weapon to the dig.
        if (BotTreasure.Usable(bot) != null && !TreasureMap.HasDiggingTool(bot))
        {
            needs.Add((typeof(Shovel), 1));
        }

        if (BotTreasure.WantsLockpicks(bot))
        {
            needs.Add((typeof(Lockpick), 5));
        }

        // A cook short of meat buys ribs from the butcher; the rest comes from its hunting.
        if (BotCrafting.IsCook(bot) && pack.GetAmount(typeof(RawRibs)) + pack.GetAmount(typeof(RawBird)) +
            pack.GetAmount(typeof(RawFishSteak)) < 10)
        {
            needs.Add((typeof(RawRibs), 20));
        }

        // A tailor with nothing to sew buys cloth; leather comes from its own hunting.
        if (BotCrafting.IsTailor(bot) && pack.GetAmount(typeof(Cloth)) + pack.GetAmount(typeof(BoltOfCloth)) * 50 +
            pack.GetAmount(typeof(BaseLeather)) < 30)
        {
            needs.Add((typeof(Cloth), 60));
        }

        // Ремесленник без материала и без сырья, чтобы его добыть, смотрит рынок своего города:
        // аукцион, где боты-добытчики выставляют слитки и доски, или лавку.
        if ((IsCrafterOf(bot, DefBlacksmithy.CraftSystem) || IsCrafterOf(bot, DefTinkering.CraftSystem)) &&
            pack.GetAmount(typeof(MahaonIngot)) < 20 && pack.GetAmount(typeof(MahaonOre)) == 0)
        {
            needs.Add((typeof(MahaonIngot), 50));
        }

        if ((IsCrafterOf(bot, DefCarpentry.CraftSystem) || IsCrafterOf(bot, DefBowFletching.CraftSystem)) &&
            pack.GetAmount(typeof(Board)) + pack.GetAmount(typeof(Log)) < 20)
        {
            needs.Add((typeof(Board), 50));
        }

        if (pack.GetAmount(typeof(BaseHealPotion)) < 2 && Banker.GetBalance(bot) + pack.GetAmount(typeof(Gold)) > 2000)
        {
            needs.Add((typeof(GreaterHealPotion), 4));
        }

        return needs;
    }

    private static bool IsCrafterOf(Mobile bot, Engines.Craft.CraftSystem system) =>
        system != null && BotCrafting.IsCrafter(bot, system) && BotCrafting.FindTool(bot, system) != null;

    private static void AddShort(List<(Type type, int amount)> needs, Container pack, Type[] types, int min, int buy)
    {
        foreach (var t in types)
        {
            if (pack.GetAmount(t) < min)
            {
                needs.Add((t, buy));
            }
        }
    }

    public override double Score(BotBrain brain)
    {
        if (NeededTool(brain) != null)
        {
            return 0.9;
        }

        return Shortages(brain).Count > 0 ? 0.65 : 0;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var city = BotSocialRules.TownFor(bot);
        if (city == null)
        {
            return null;
        }

        var wants = Shortages(brain);
        if (NeededTool(brain) is { } tool)
        {
            wants.Insert(0, (tool, 1));
        }

        // Первое, что в этом городе можно купить — в лавке или на аукционе. Чего нет нигде,
        // пропускаем, а не бросаем закупку целиком.
        foreach (var (type, amount) in wants)
        {
            if (BotShopping.PlanPurchase(bot, city, type, amount) is not { } steps)
            {
                continue;
            }

            if (typeof(SpellScroll).IsAssignableFrom(type))
            {
                steps.Add(new FillSpellbookAction());
            }

            return steps;
        }

        return null;
    }
}
