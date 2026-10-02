using System.Collections.Generic;

namespace Server.Systems.Bots;

public sealed class RestGoal : BotGoal
{
    public override string Name => "Отдых";

    // Quadratic: a little tiredness is shrugged off, real exhaustion dominates everything.
    public override double Score(BotBrain brain) => brain.Fatigue * brain.Fatigue * 1.2;

    public override List<BotAction> Plan(BotBrain brain)
    {
        var minutes = 5 + brain.Fatigue * 20;
        var rest = new RestAction((long)(minutes * 60_000));

        // Rest at home when there is one: coming home through the door is also what keeps a
        // house from decaying.
        if (BotHousing.HomeOf(brain.Bot) is { Map: { } map } house && map == brain.Bot.Map)
        {
            if (BotHousing.IsGuildHouse(house) && brain.Bot.Guild is Guilds.Guild guild)
            {
                BotHousing.AdmitGuild(house, guild);
            }

            return [new GoToAction(map, BotHousing.Inside(house), 1, "домой"), rest];
        }

        return [rest];
    }
}
