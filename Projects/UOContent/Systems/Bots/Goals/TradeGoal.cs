using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.Bots;

/// <summary>Turn gathered goods into gold: smelt ore at the town forge, then make a market round.</summary>
public sealed class TradeGoal : BotGoal
{
    private const long MinWorthATrip = 150;

    public override string Name => "Торговля";

    public override double Score(BotBrain brain)
    {
        var value = BotGoods.ValueCarried(brain.Bot);
        if (value < MinWorthATrip)
        {
            return 0;
        }

        // A greedy bot hauls more before bothering with the market.
        var trip = 800 + BotBrain.Trait(brain.Greed) * 1200;
        return System.Math.Min(1.0, value / trip) * 0.85;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var city = BotSocialRules.TownFor(bot);
        if (city == null)
        {
            return null;
        }

        var steps = new List<BotAction>();

        if (bot.Backpack?.FindItemByType<MahaonOre>() != null && WorldCatalog.TryGetForge(city, out var forge, out var forgeLocation))
        {
            steps.Add(new GoToAction(city.Map, forgeLocation, 2, "к печи"));
            steps.Add(new SmeltAction(forge, forgeLocation));
        }

        steps.Add(new SellGoodsAction(city));
        return steps;
    }
}
