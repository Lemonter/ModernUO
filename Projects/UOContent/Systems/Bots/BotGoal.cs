using System.Collections.Generic;

namespace Server.Systems.Bots;

/// <summary>
/// Something a bot can want. Goals are stateless singletons: <see cref="Score"/> weighs how much
/// this bot wants it right now, <see cref="Plan"/> turns it into actions from where the bot
/// stands. A goal that can't be planned (no bank in reach, nobody to talk to) returns null and the
/// brain picks something else.
/// </summary>
public abstract class BotGoal
{
    /// <summary>Russian name for [BotInspect.</summary>
    public abstract string Name { get; }

    /// <summary>0 = no interest; around 1 = pressing.</summary>
    public abstract double Score(BotBrain brain);

    public abstract List<BotAction> Plan(BotBrain brain);

    /// <summary>Keeps other work going rather than being work itself; still weighed while a
    /// possessed character is focused on one goal.</summary>
    public virtual bool IsUpkeep => false;

    /// <summary>What a bot that just finished this goal may tell the next person it talks to.</summary>
    public virtual string[] News => null;

    public virtual void OnCompleted(BotBrain brain)
    {
    }
}
