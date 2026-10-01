using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonQuests;

/// <summary>
///     Guard-sergeant quest-giver built on the same "kill N of this creature type" shape as
///     MayorQuestSystem, but pays out MahaonGuard rank points instead of gold — a way to earn
///     guard rank through directed bounty work, not just incidental raid kills. Tracks progress
///     via BaseCreature.OnDeath (see the hook there), same as MayorQuestSystem.
///
///     Fully independent from MayorQuestSystem (and from RangerQuestSystem) — separate
///     PlayerQuests dictionary and ActiveQuest type, so a player can have one quest active from
///     each giver at the same time.
///
///     In-memory only, same convention as the rest of the Mahaon systems — active quests reset
///     on restart, cheap to re-roll through normal play.
/// </summary>
public static class GuardQuestSystem
{
    private const int MinKills = 3;
    private const int MaxKills = 8;

    // Reward is guard rank points instead of gold. GuardSystem's own rank thresholds run
    // 0/50/150/350/700/1200/2000 — at ~5 kills/quest and 20 points/kill (~100 points/quest),
    // that's roughly 1-2 quests per early rank-up, tapering off at higher ranks like the
    // thresholds themselves already do.
    private const int PointsPerKill = 20;

    // Same target pool as MayorQuestSystem — raid/city-threat enemies fit guard duty just as
    // well as mayoral concern, no need to invent a different pool.
    private static readonly string[] TargetPool =
    {
        "Skeleton", "Zombie", "GreyWolf", "DireWolf", "Ratman", "Orc", "Brigand"
    };

    public class ActiveQuest
    {
        public string TargetTypeName;
        public int RequiredKills;
        public int CurrentKills;
        public Mobile Sergeant;

        public bool IsComplete => CurrentKills >= RequiredKills;
    }

    private static readonly Dictionary<Mobile, ActiveQuest> PlayerQuests = new();

    // Mahaon: gump-visible status (MahaonGuardSergeantGump) — the quest-complete/in-progress
    // feedback previously only existed as a one-off chat line at the moment a kill finished
    // it (OnCreatureKilled below); re-opening the gump later showed nothing about it.
    public static string GetStatusLine(Mobile player, Mobile sergeant)
    {
        if (!PlayerQuests.TryGetValue(player, out var quest) || quest.Sergeant != sergeant)
        {
            return "Задания нет — поговори со мной, чтобы получить.";
        }

        return quest.IsComplete
            ? "Задание выполнено — возьми награду!"
            : $"Задание: убито {quest.CurrentKills} из {quest.RequiredKills} ({RuName(quest.TargetTypeName)}).";
    }

    /// <summary>Double-click handler entry point — see the guard-sergeant NPC's OnDoubleClick.</summary>
    public static void Talk(Mobile player, Mobile giver)
    {
        if (PlayerQuests.TryGetValue(player, out var existing) && existing.Sergeant == giver)
        {
            if (existing.IsComplete)
            {
                TurnIn(player, giver, existing);
            }
            else
            {
                giver.PublicOverheadMessage(
                    MessageType.Regular, 0x3B2, false,
                    $"Убито {existing.CurrentKills} из {existing.RequiredKills} ({RuName(existing.TargetTypeName)}). Возвращайся, когда закончишь."
                );
            }

            return;
        }

        // Already has a DIFFERENT sergeant's quest active — one at a time, keeps this
        // simple rather than juggling parallel trackers.
        if (existing != null)
        {
            giver.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "У тебя уже есть незавершённое задание от другого сержанта.");
            return;
        }

        var target = TargetPool[Utility.Random(TargetPool.Length)];
        var required = Utility.RandomMinMax(MinKills, MaxKills);

        var quest = new ActiveQuest
        {
            TargetTypeName = target,
            RequiredKills = required,
            CurrentKills = 0,
            Sergeant = giver
        };

        PlayerQuests[player] = quest;
        QuestMarkerRegistry.SetState(player, giver, QuestMarkerState.InProgress);

        giver.PublicOverheadMessage(
            MessageType.Regular, 0x3B2, false,
            $"Страже докучают {RuName(target)}. Принеси мне {required} — получишь {required * PointsPerKill} очков стражи."
        );
    }

    public static void OnCreatureKilled(PlayerMobile player, BaseCreature creature)
    {
        if (!PlayerQuests.TryGetValue(player, out var quest) || quest.IsComplete)
        {
            return;
        }

        if (!string.Equals(creature.GetType().Name, quest.TargetTypeName, StringComparison.Ordinal))
        {
            return;
        }

        quest.CurrentKills++;

        if (quest.IsComplete)
        {
            player.SendMessage(0x59, $"Задание стражи выполнено ({quest.CurrentKills}/{quest.RequiredKills}) — вернись за наградой.");
            QuestMarkerRegistry.SetState(player, quest.Sergeant, QuestMarkerState.Complete);
        }
    }

    private static void TurnIn(Mobile player, Mobile giver, ActiveQuest quest)
    {
        var reward = quest.RequiredKills * PointsPerKill;

        if (player is PlayerMobile playerMobile)
        {
            MahaonGuard.GuardSystem.AddPoints(playerMobile, reward);
        }

        giver.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Благодарю за службу! Вот {reward} очков стражи.");

        PlayerQuests.Remove(player);
        QuestMarkerRegistry.SetState(player, giver, QuestMarkerState.None);
    }

    private static string RuName(string typeName) => typeName switch
    {
        "Skeleton"  => "скелетов",
        "Zombie"    => "зомби",
        "GreyWolf"  => "серых волков",
        "DireWolf"  => "лютых волков",
        "Ratman"    => "крысолюдов",
        "Orc"       => "орков",
        "Brigand"   => "разбойников",
        _           => typeName
    };
}
