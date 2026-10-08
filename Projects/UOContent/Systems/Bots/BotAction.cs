namespace Server.Systems.Bots;

public enum BotActionStatus : byte
{
    Running,
    Done,
    Failed
}

public readonly record struct BotActionResult(BotActionStatus Status, int DelayMs)
{
    public static BotActionResult Running(int delayMs) => new(BotActionStatus.Running, delayMs);
    public static BotActionResult Done(int delayMs = 250) => new(BotActionStatus.Done, delayMs);
    public static BotActionResult Failed(int delayMs = 1000) => new(BotActionStatus.Failed, delayMs);
}

/// <summary>
/// One step of a plan: walk somewhere, deposit gold, rest, talk. Ticked by the brain until it is
/// done or fails; a failure makes the brain replan the goal from wherever the bot now is, so an
/// action never has to recover on its own beyond its own retries.
/// </summary>
public abstract class BotAction
{
    public virtual void Start(BotBrain brain)
    {
    }

    public abstract BotActionResult Tick(BotBrain brain);

    /// <summary>Called when the action ends for any reason, including being replaced.</summary>
    public virtual void Stop(BotBrain brain)
    {
    }

    /// <summary>Short Russian description for [BotInspect.</summary>
    public abstract string Describe(BotBrain brain);
}
