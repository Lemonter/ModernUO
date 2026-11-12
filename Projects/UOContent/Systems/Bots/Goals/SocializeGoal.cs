using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.Bots;

public sealed class SocializeGoal : BotGoal
{
    private const int SearchRange = 18;

    public override string Name => "Общение";

    public override double Score(BotBrain brain) => brain.Loneliness * (0.4 + BotBrain.Trait(brain.Sociability) * 0.8);

    public override List<BotAction> Plan(BotBrain brain)
    {
        var partner = FindPartner(brain.Bot);
        if (partner == null)
        {
            return null;
        }

        return [new GoToAction(partner, 2, "к собеседнику"), new TalkAction(partner)];
    }

    private static bool IsAtLeisure(BotBrain brain) =>
        brain.Action is null or RestAction || brain.Goal == BotGoals.Loiter ||
        brain.Goal == BotGoals.Socialize && brain.Action is not TalkAction;

    private static Mobile FindPartner(PlayerMobile bot)
    {
        Mobile best = null;
        var bestDist = double.MaxValue;

        foreach (var m in bot.Map.GetMobilesInRange<PlayerMobile>(bot.Location, SearchRange))
        {
            // Занятого делом бота не догоняют: он не остановится, и инициатор гонялся бы за ним.
            if (m == bot || !m.Alive || m.Hidden || m.GetBrain() is { } other && !IsAtLeisure(other))
            {
                continue;
            }

            var dist = bot.GetDistanceToSqrt(m);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = m;
            }
        }

        return best;
    }
}
