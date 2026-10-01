using System.Collections.Generic;
using Server.Engines.Craft;

namespace Server.Systems.Bots;

/// <summary>Practise a craft: go where it can be done (the town smithy for metalwork), then make
/// a batch from what the bot has gathered.</summary>
public sealed class CraftGoal : BotGoal
{
    private readonly int _systemIndex;

    public CraftGoal(int systemIndex) => _systemIndex = systemIndex;

    private CraftSystem System => BotCrafting.Systems[_systemIndex];

    public override string Name => $"Ремесло: {System?.MainSkill}";

    public override string[] News =>
        ["Наделал товара на продажу.", "Руки в мозолях от работы.", "Заказов бы побольше.", "Сделал вещь — сам бы носил."];

    private bool NeedsSmithy => System == DefBlacksmithy.CraftSystem;

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        var system = System;

        if (system == null || !BotCrafting.IsCrafter(bot, system) || BotCrafting.FindTool(bot, system) == null ||
            !BotCrafting.TryPick(bot, system, out _))
        {
            return 0;
        }

        var skill = bot.Skills[system.MainSkill].Value / 100.0;
        return 0.2 + skill * 0.4 + BotBrain.Trait(brain.Diligence) * 0.3 - brain.Fatigue * 0.5;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var steps = new List<BotAction>();

        if (NeedsSmithy)
        {
            var city = BotSocialRules.TownFor(bot);
            if (city == null || !WorldCatalog.TryGetSmithy(city, out var stand))
            {
                return null;
            }

            steps.Add(new GoToAction(city.Map, stand, 0, "в кузницу"));
        }

        steps.Add(new CraftAction(System, 5 + brain.Diligence / 10));
        return steps;
    }
}

/// <summary>Learn more of one's craft: buy the cheapest blueprint not yet known that the bot's
/// skill allows, when it can spare the gold.</summary>
public sealed class LearnGoal : BotGoal
{
    public override string Name => "Учёба";

    private static (CraftSystem system, Recipe recipe) Next(BotBrain brain)
    {
        var bot = brain.Bot;
        foreach (var system in BotCrafting.Systems)
        {
            if (system != null && BotCrafting.IsCrafter(bot, system) && BotCrafting.CheapestUnknownRecipe(bot, system) is { } recipe)
            {
                return (system, recipe);
            }
        }

        return (null, null);
    }

    public override double Score(BotBrain brain)
    {
        var (system, recipe) = Next(brain);
        if (recipe == null)
        {
            return 0;
        }

        var price = Items.MahaonBlueprint.GetPrice(recipe);
        var wealth = (brain.Bot.Backpack?.GetAmount(typeof(Items.Gold)) ?? 0) + Mobiles.Banker.GetBalance(brain.Bot);

        // Spare gold only: keep a cushion for supplies.
        if (wealth < price + 500)
        {
            return 0;
        }

        // A crafter who knows little is eager to learn; one who knows plenty, less so.
        var known = BotCrafting.KnownRecipes(brain.Bot, system);
        return known < 3 ? 0.8 : 0.5 / (1 + known / 10.0);
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var (_, recipe) = Next(brain);
        if (recipe == null)
        {
            return null;
        }

        var bot = brain.Bot;
        var price = Items.MahaonBlueprint.GetPrice(recipe);
        var steps = new List<BotAction>();

        if ((bot.Backpack?.GetAmount(typeof(Items.Gold)) ?? 0) < price)
        {
            var city = BotSocialRules.TownFor(bot);
            var banker = city == null ? null : WorldCatalog.GetBanker(city);
            if (banker == null)
            {
                return null;
            }

            steps.Add(new GoToAction(banker, 3, "в банк"));
            steps.Add(new WithdrawGoldAction(banker, price));
        }

        steps.Add(new BuyBlueprintAction(recipe));
        return steps;
    }
}
