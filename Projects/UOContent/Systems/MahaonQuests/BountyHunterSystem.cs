using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonQuests;

/// <summary>
///     Second quest-giver on top of the same QuestMarkerRegistry foundation as
///     MayorQuestSystem — a bounty board that names ONE current target and a reward.
///     Deliberately scoped to PK-flagged bots (BotMobile.IsPk) rather than real players
///     for this first version — a real player-vs-player bounty system needs real
///     anti-grief design (combat log verification, cooldowns, opt-out, etc) that's its
///     own separate piece of work, not something to bolt on carelessly. Killing an
///     actual PK bot for a real reward is a genuinely fitting use of the label either
///     way, not just a placeholder.
///
///     No per-player "accept" step in this version — the board just names a target
///     publicly, and whoever actually lands the kill gets paid. Simpler than
///     MayorQuestSystem's per-player tracking, and matches "a bounty poster" better
///     than "a personal quest log" conceptually.
/// </summary>
public static class BountyHunterSystem
{
    private static readonly TimeSpan RerollInterval = TimeSpan.FromMinutes(20);
    private const int MinReward = 200;
    private const int MaxReward = 600;

    private static BotMobile _currentTarget;
    private static int _currentReward;

    public static void Initialize()
    {
        Timer.DelayCall(RerollInterval, RerollInterval, EnsureValidTarget);
    }

    public static BotMobile CurrentTarget
    {
        get
        {
            EnsureValidTarget();
            return _currentTarget;
        }
    }

    public static int CurrentReward => _currentReward;

    public static string CurrentBountyText()
    {
        EnsureValidTarget();

        return _currentTarget == null
            ? "Сейчас нет известных преступников — доска пуста."
            : $"Разыскивается: {_currentTarget.Name} (последний раз видели в {_currentTarget.Map?.Name ?? "неизвестных землях"}). Награда: {_currentReward} золота.";
    }

    private static void EnsureValidTarget()
    {
        if (_currentTarget?.Deleted == false && _currentTarget.Alive)
        {
            return; // still a valid, live target — nothing to do
        }

        _currentTarget = null;

        // Only bots can be bounty targets, so the bot roster is the whole search.
        foreach (var mobile in Bots.BotSystem.Bots)
        {
            if (mobile is BotMobile { IsPk: true, Deleted: false } bot && bot.Alive)
            {
                _currentTarget = bot;
                break;
            }
        }

        _currentReward = Utility.RandomMinMax(MinReward, MaxReward);
    }

    /// <summary>Call from PlayerMobile.OnDeath's BotMobile-specific block — bounty
    /// targets are BotMobile (a PlayerMobile), so this hooks the SAME place
    /// RegearAfterDeath/ScheduleResurrection do, not BaseCreature.OnDeath (a BotMobile
    /// dying never fires that one at all — it's a PlayerMobile, not a BaseCreature).</summary>
    public static void OnBotKilled(PlayerMobile killer, BotMobile bot)
    {
        if (_currentTarget == null || bot != _currentTarget)
        {
            return;
        }

        if (killer.Backpack != null)
        {
            killer.Backpack.DropItem(new Gold(_currentReward));
        }

        killer.SendMessage(0x59, $"Награда за голову {bot.Name} выплачена: {_currentReward} золота.");
        World.Broadcast(0x59, true, $"{killer.Name} получил награду за {bot.Name}!");

        _currentTarget = null; // board goes empty until the next reroll picks someone new
    }
}
