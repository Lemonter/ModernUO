using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// Tames a creature the way a player does: stand close, use Animal Taming, aim the cursor, and let
/// the skill's own timer run its course — the creature may turn on the tamer, wander off, or come
/// to heel. Retries a few times while the creature is still wild and near.
/// </summary>
public sealed class TameAction : BotAction
{
    private const int MaxAttempts = 4;
    private const int Reach = 3;

    private readonly BaseCreature _creature;
    private int _attempts;
    private long _nextTry;

    public TameAction(BaseCreature creature) => _creature = creature;

    public override void Start(BotBrain brain) => _nextTry = Core.TickCount;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var c = _creature;

        if (c.Controlled && c.ControlMaster == bot)
        {
            c.ControlTarget = bot;
            c.ControlOrder = OrderType.Follow;
            return BotActionResult.Done(1000);
        }

        if (c.Deleted || !c.Alive || c.Controlled || c.Map != bot.Map || !bot.InRange(c, 12))
        {
            return BotActionResult.Failed();
        }

        if (!bot.InRange(c, Reach))
        {
            return BotActionResult.Failed(); // the plan walks back to it on replan
        }

        // The skill's own timer is running between attempts.
        if (Core.TickCount - _nextTry < 0)
        {
            return BotActionResult.Running(1000);
        }

        if (++_attempts > MaxAttempts)
        {
            return BotActionResult.Failed();
        }

        _nextTry = Core.TickCount + 12_000;

        if (bot.UseSkill(SkillName.AnimalTaming) && bot.Target != null)
        {
            bot.Target.Invoke(bot, c);
        }

        return BotActionResult.Running(1500);
    }

    public override string Describe(BotBrain brain) => $"Приручает: {_creature.Name} ({_attempts}/{MaxAttempts})";
}
