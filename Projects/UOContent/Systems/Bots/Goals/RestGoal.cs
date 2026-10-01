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
        return [new RestAction((long)(minutes * 60_000))];
    }
}
