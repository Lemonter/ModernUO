namespace Server.Systems.Bots;

/// <summary>Stands and recovers. Fatigue drains while this is the current action (see
/// <see cref="BotBrain"/>), so resting longer simply means resting more.</summary>
public sealed class RestAction : BotAction
{
    private const int TickMs = 5000;

    private readonly long _durationMs;
    private long _until;

    public RestAction(long durationMs) => _durationMs = durationMs;

    public override void Start(BotBrain brain)
    {
        _until = Core.TickCount + _durationMs;
        BotSpeech.Say(brain.Bot, BotTopic.Resting, chance: 0.3);
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        if (Core.TickCount - _until >= 0)
        {
            return BotActionResult.Done();
        }

        if (Utility.RandomDouble() < 0.05)
        {
            BotSpeech.Say(brain.Bot, BotTopic.Idle);
        }

        return BotActionResult.Running(TickMs);
    }

    public override string Describe(BotBrain brain) =>
        $"Отдыхает ещё {System.Math.Max(0, (_until - Core.TickCount) / 1000)} с";
}
