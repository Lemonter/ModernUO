using System.Collections.Generic;

namespace Server.Systems.Bots;

/// <summary>Moves to another town on the same facet. The bot's home moves with it: wherever it
/// ends up is where it lives now, until the road calls again.</summary>
public sealed class TravelGoal : BotGoal
{
    public override string Name => "Путешествие";

    public override string[] News => ["Только с дороги.", "Долгий был путь.", "В пути разбойников видел, обошлось."];

    public override double Score(BotBrain brain) => brain.Restlessness * (0.3 + BotBrain.Trait(brain.Wanderlust) * 0.8);

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var current = WorldCatalog.FindNearest(bot.Map, bot.Location);

        BotCity destination = null;
        var candidates = 0;

        foreach (var city in WorldCatalog.Cities)
        {
            if (city.Map != bot.Map || city == current || BotSocialRules.IsOutlaw(bot) && BotSocialRules.IsGuarded(city.Region))
            {
                continue;
            }

            // Reservoir pick: uniform over the other towns without building a list.
            if (Utility.Random(++candidates) == 0)
            {
                destination = city;
            }
        }

        if (destination == null)
        {
            return null;
        }

        BotSpeech.Say(bot, BotTopic.Traveling, chance: 0.6);
        return [new GoToAction(destination.Map, destination.Center, 4, destination.Name), new ArriveInCityAction(destination)];
    }
}

/// <summary>Settles the bot in the town it just reached.</summary>
public sealed class ArriveInCityAction : BotAction
{
    private readonly BotCity _city;

    public ArriveInCityAction(BotCity city) => _city = city;

    public override BotActionResult Tick(BotBrain brain)
    {
        brain.HomeCity = _city.Name;
        brain.HomeMap = _city.Map;
        brain.Home = _city.Center;
        brain.OnTraveled();
        BotSpeech.Say(brain.Bot, BotTopic.Arrived, chance: 0.5);
        return BotActionResult.Done();
    }

    public override string Describe(BotBrain brain) => $"Обустраивается в {_city.Name}";
}
