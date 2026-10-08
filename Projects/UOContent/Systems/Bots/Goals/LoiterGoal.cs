using System.Collections.Generic;

namespace Server.Systems.Bots;

/// <summary>The fallback: stroll around town between other things. Always plannable, always
/// weakly wanted, so a bot is never left standing with nothing to do.</summary>
public sealed class LoiterGoal : BotGoal
{
    private const int Radius = 14;

    public override string Name => "Прогулка";

    public override double Score(BotBrain brain) => 0.15;

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var center = bot.Map == brain.HomeMap && brain.HomeMap != null ? brain.Home : bot.Location;

        var steps = new List<BotAction>();
        var stops = Utility.RandomMinMax(1, 3);

        for (var i = 0; i < stops; i++)
        {
            if (BotMovement.TryRandomSpot(bot.Map, center, Radius, out var spot))
            {
                steps.Add(new GoToAction(bot.Map, spot, 1, "прогулка"));
                steps.Add(new RestAction(Utility.RandomMinMax(5_000, 25_000)));
            }
        }

        return steps.Count > 0 ? steps : null;
    }
}
