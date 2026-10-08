namespace Server.Systems.Bots;

/// <summary>
/// A group member's standing order: stay near the leader, join whatever fight the group is in,
/// patch up wounded members between blows. Runs until the group breaks up.
/// </summary>
public sealed class FollowLeaderAction : BotAction
{
    private const int KeepWithin = 3;

    private readonly BotGroup _group;
    private GoToAction _follow;

    public FollowLeaderAction(BotGroup group) => _group = group;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var leader = _group.Leader;

        if (_group.Disbanded || brain.Group != _group || leader.Deleted || !leader.Alive || leader.Map != bot.Map)
        {
            return BotActionResult.Done();
        }

        if (_group.LedByPlayer && bot is Mobiles.BotMobile member && !BotParty.StillServes(member, _group))
        {
            return BotActionResult.Done();
        }

        if (_group.CurrentFoe() is { } foe && foe.Map == bot.Map && bot.InRange(foe, 14))
        {
            BotCombat.Engage(brain, foe);
            return BotActionResult.Running(300);
        }

        if (BotHealing.TryHealAlly(brain, _group))
        {
            return BotActionResult.Running(1000);
        }

        if (bot.InRange(leader, KeepWithin))
        {
            _follow = null;
            return BotActionResult.Running(500);
        }

        _follow ??= new GoToAction(leader, 2, "за лидером");
        var result = _follow.Tick(brain);
        if (result.Status != BotActionStatus.Running)
        {
            _follow = null;
            return BotActionResult.Running(500);
        }

        return result;
    }

    public override void Stop(BotBrain brain) => _follow?.Stop(brain);

    public override string Describe(BotBrain brain) => $"В отряде {_group.Leader.Name}";
}
