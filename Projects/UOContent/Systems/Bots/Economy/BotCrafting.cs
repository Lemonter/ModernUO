using System;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>A craft the bot can start right now: what, with which material row.</summary>
public readonly record struct BotCraftChoice(CraftItem Item, Type ResourceType, int SubResIndex, double Chance);

/// <summary>
/// What bots craft and how they decide. Only what a player in the same shoes could craft: a
/// learned blueprint (every item is gated on one in this shard), the skill, a working tool and the
/// materials in the pack. The craft itself goes through <see cref="CraftItem.Craft"/>.
/// </summary>
public static class BotCrafting
{
    /// <summary>Crafts whose inputs bots produce or buy: metal for the smith and the tinker, wood
    /// for the carpenter and the bowyer, cloth and leather for the tailor.</summary>
    public static CraftSystem[] Systems =>
    [
        DefBlacksmithy.CraftSystem, DefTinkering.CraftSystem, DefCarpentry.CraftSystem, DefBowFletching.CraftSystem,
        DefTailoring.CraftSystem
    ];

    public static bool IsTailor(Mobile bot) => DefTailoring.CraftSystem is { } system && IsCrafter(bot, system);

    /// <summary>A tailor's material, cut or still to be cut.</summary>
    public static bool IsTailoringMaterial(Item item) => item is Cloth or UncutCloth or BoltOfCloth or BaseLeather or BaseHides;

    // Below this a bot doesn't think of itself as a crafter of that kind.
    public const double CrafterSkill = 20.0;

    // Chance floor for picking a craft: below it the materials mostly go to waste.
    private const double MinChance = 0.4;

    // Materials a crafter holds back from the market, per resource kind.
    private const int CraftReserve = 300;

    public static bool IsCrafter(Mobile bot, CraftSystem system) => bot.Skills[system.MainSkill].Value >= CrafterSkill;

    public static bool IsAnyCrafter(Mobile bot)
    {
        foreach (var system in Systems)
        {
            if (system != null && IsCrafter(bot, system))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether this good is the bot's own craft material and should stay in the pack.</summary>
    public static bool KeepsForCraft(Mobile bot, Item item)
    {
        if (IsTailoringMaterial(item))
        {
            return IsTailor(bot) && bot.Backpack.GetAmount(item.GetType()) <= CraftReserve;
        }

        var metal = item is MahaonIngot;
        var wood = item is Log or Board;

        if (!metal && !wood)
        {
            return false;
        }

        foreach (var system in Systems)
        {
            if (system == null || !IsCrafter(bot, system))
            {
                continue;
            }

            var usesMetal = system == DefBlacksmithy.CraftSystem || system == DefTinkering.CraftSystem;
            if (usesMetal == metal)
            {
                return bot.Backpack.GetAmount(item.GetType()) <= CraftReserve;
            }
        }

        return false;
    }

    public static BaseTool FindTool(Mobile bot, CraftSystem system)
    {
        var pack = bot.Backpack;
        if (pack == null)
        {
            return null;
        }

        foreach (var tool in pack.FindItemsByType<BaseTool>())
        {
            if (tool.CraftSystem == system && tool.UsesRemaining > 0)
            {
                return tool;
            }
        }

        return null;
    }

    /// <summary>The tool to buy for a craft when the last one broke.</summary>
    public static Type ToolTypeFor(CraftSystem system)
    {
        if (system == DefBlacksmithy.CraftSystem)
        {
            return typeof(SmithHammer);
        }

        if (system == DefTinkering.CraftSystem)
        {
            return typeof(TinkerTools);
        }

        if (system == DefCarpentry.CraftSystem)
        {
            return typeof(Saw);
        }

        if (system == DefTailoring.CraftSystem)
        {
            return typeof(SewingKit);
        }

        return typeof(FletcherTools);
    }

    /// <summary>
    /// The best craft available now: among learned recipes the bot has the skill and materials
    /// for, the one with the best chance, ties broken toward the harder (more valuable, better for
    /// skill) item. Picks the material row itself — the rarest metal or wood it can work and holds
    /// enough of — and leaves it selected in the craft context, which is where the craft engine
    /// reads it from.
    /// </summary>
    public static bool TryPick(PlayerMobile bot, CraftSystem system, out BotCraftChoice choice)
    {
        choice = default;
        var context = system.GetContext(bot);
        var subRes = system.CraftSubRes;
        var bestScore = double.MinValue;

        foreach (var craftItem in system.CraftItems)
        {
            if (craftItem.RequiredExpansion != Expansion.None || craftItem.Resources.Count == 0 ||
                craftItem.Recipe != null && !bot.HasRecipe(craftItem.Recipe))
            {
                continue;
            }

            var primary = craftItem.Resources[0].ItemType;
            var usesSubRes = subRes.Init && subRes.ResType != null && subRes.ResType.IsAssignableFrom(primary);

            if (!usesSubRes)
            {
                Consider(bot, system, craftItem, primary, -1, context, ref choice, ref bestScore);
                continue;
            }

            for (var i = subRes.Count - 1; i >= 0; i--)
            {
                var row = subRes.GetAt(i);
                if (bot.Skills[system.MainSkill].Value < row.RequiredSkill)
                {
                    continue;
                }

                if (Consider(bot, system, craftItem, row.ItemType, i, context, ref choice, ref bestScore))
                {
                    break; // the rarest workable row with enough material
                }
            }
        }

        // Checking rows moved the selection around; leave it on the chosen one.
        if (choice.Item != null && context != null && choice.SubResIndex >= 0)
        {
            context.LastResourceIndex = choice.SubResIndex;
        }

        return choice.Item != null;
    }

    private static bool Consider(
        PlayerMobile bot, CraftSystem system, CraftItem craftItem, Type typeRes, int subResIndex, CraftContext context,
        ref BotCraftChoice best, ref double bestScore
    )
    {
        if (context != null && subResIndex >= 0)
        {
            // The metal row is read from the context, so it must be selected before checking.
            context.LastResourceIndex = subResIndex;
        }

        var chance = craftItem.GetSuccessChance(bot, typeRes, system, false, out var allSkills);
        if (!allSkills || chance < MinChance)
        {
            return false;
        }

        var hue = 0;
        var max = 0;
        TextDefinition message = null;
        if (!craftItem.ConsumeRes(bot, typeRes, system, ref hue, ref max, ConsumeType.None, ref message))
        {
            return false;
        }

        var difficulty = 0.0;
        foreach (var skill in craftItem.Skills)
        {
            difficulty = Math.Max(difficulty, skill.MinSkill);
        }

        var score = chance + difficulty / 200.0 + (subResIndex >= 0 ? subResIndex * 0.01 : 0);
        if (score > bestScore)
        {
            bestScore = score;
            best = new BotCraftChoice(craftItem, typeRes, subResIndex, chance);
        }

        return true;
    }

    /// <summary>
    /// Unlearned blueprints of this craft within the bot's skill, cheapest first — what a bot
    /// buys to grow its range.
    /// </summary>
    public static Recipe CheapestUnknownRecipe(PlayerMobile bot, CraftSystem system)
    {
        Recipe best = null;
        var bestPrice = int.MaxValue;
        var skill = bot.Skills[system.MainSkill].Value;

        foreach (var craftItem in system.CraftItems)
        {
            var recipe = craftItem.Recipe;
            if (recipe == null || bot.HasRecipe(recipe) || craftItem.RequiredExpansion != Expansion.None)
            {
                continue;
            }

            var required = 0.0;
            foreach (var s in craftItem.Skills)
            {
                required = Math.Max(required, s.MinSkill);
            }

            if (required > skill)
            {
                continue;
            }

            var price = MahaonBlueprint.GetPrice(recipe);
            if (price < bestPrice)
            {
                bestPrice = price;
                best = recipe;
            }
        }

        return best;
    }

    public static int KnownRecipes(PlayerMobile bot, CraftSystem system)
    {
        var known = 0;
        foreach (var craftItem in system.CraftItems)
        {
            if (craftItem.Recipe != null && bot.HasRecipe(craftItem.Recipe))
            {
                known++;
            }
        }

        return known;
    }

    /// <summary>Base value of a crafted item: its materials at market base price, doubled for
    /// the work. Zero when the item isn't something bots craft.</summary>
    public static int ProductValue(Item item)
    {
        foreach (var system in Systems)
        {
            var craftItem = system?.CraftItems.SearchFor(item.GetType());
            if (craftItem == null)
            {
                continue;
            }

            var value = 0;
            foreach (var res in craftItem.Resources)
            {
                value += res.Amount * ResourceUnitPrice(res.ItemType);
            }

            return Math.Max(1, value * 2);
        }

        return 0;
    }

    private static int ResourceUnitPrice(Type type) =>
        typeof(IronIngot).IsAssignableFrom(type) || type == typeof(MahaonIngot) ? 10 :
        typeof(Board).IsAssignableFrom(type) ? 6 :
        typeof(Log).IsAssignableFrom(type) ? 3 :
        typeof(BaseLeather).IsAssignableFrom(type) ? 3 : 2;
}
