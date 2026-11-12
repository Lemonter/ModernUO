namespace Server.Systems.Bots;

/// <summary>
/// A group member's standing order: stay near the leader, join whatever fight the group is in,
/// patch up wounded members between blows. Runs until the group breaks up.
/// </summary>
public sealed class FollowLeaderAction : BotAction
{
    private const int KeepWithin = 3;
    private const long LeaderDeadPatienceMs = 10 * 60_000;

    private readonly BotGroup _group;
    private GoToAction _follow;
    private long? _leaderDeadSince;

    public FollowLeaderAction(BotGroup group) => _group = group;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var leader = _group.Leader;

        if (_group.Disbanded || brain.Group != _group || leader.Deleted)
        {
            return BotActionResult.Done();
        }

        // Мёртвый лидер — не повод разойтись: игрок воскреснет и пойдёт дальше. Раньше действие
        // кончалось сразу, «В отряде» тут же выигрывало снова, и бот четыре раза в секунду
        // пересматривал все цели. Ждём, а если лидер так и не встал — уходим из отряда.
        if (!leader.Alive)
        {
            _leaderDeadSince ??= Core.TickCount;
            if (Core.TickCount - _leaderDeadSince.Value < LeaderDeadPatienceMs)
            {
                return BotActionResult.Running(2000);
            }

            if (_group.LedByPlayer && bot is Mobiles.BotMobile hired)
            {
                BotParty.Leave(hired, "Не дождался я тебя. Пойду своей дорогой.");
            }
            else
            {
                _group.Remove(bot);
            }

            return BotActionResult.Done();
        }

        _leaderDeadSince = null;

        if (_group.LedByPlayer && bot is Mobiles.BotMobile member && !BotParty.StillServes(member, _group))
        {
            return BotActionResult.Done();
        }

        // Свои вещи на трупе и силы важнее отряда бота: «В отряде» весит 2.0 и иначе
        // перебивала бы и возврат к трупу, и отдых.
        if (!_group.LedByPlayer &&
            (brain.Fatigue > 0.95 || brain.OwnCorpse is { Deleted: false } corpse && corpse.Items.Count > 0))
        {
            _group.Remove(bot);
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

        if (bot.Map == leader.Map && bot.InRange(leader, KeepWithin))
        {
            _follow = null;
            return BotActionResult.Running(500);
        }

        // На другую карту тоже: GoToAction по мобилю ходит через лунные врата.
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
