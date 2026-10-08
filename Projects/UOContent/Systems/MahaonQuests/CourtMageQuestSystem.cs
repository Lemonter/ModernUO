using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonQuests;

/// <summary>
///     Court-mage quest-giver built on the same "kill N of this creature type" shape as
///     GuardQuestSystem — silent kill-counter, no item involved, pays out MahaonCourtMage
///     points instead of gold. Targets are magic-flavored creatures rather than raid threats
///     or wildlife.
///
///     Fully independent from GuardQuestSystem/RangerQuestSystem — separate PlayerQuests
///     dictionary and ActiveQuest type, so a player can have one quest active from each giver
///     at the same time. In-memory only, same convention as the rest of the Mahaon systems.
/// </summary>
public static class CourtMageQuestSystem
{
    private const int MinKills = 3;
    private const int MaxKills = 8;

    // Same pacing rationale as GuardQuestSystem's/RangerQuestSystem's PointsPerKill.
    private const int PointsPerKill = 20;

    // Magic-flavored creatures — class names verified against Projects/UOContent/Mobiles.
    private static readonly string[] TargetPool =
    {
        "Wisp", "EnergyVortex", "Imp", "GazerLarva", "Gargoyle", "EvilMage"
    };

    public class ActiveQuest
    {
        public string TargetTypeName;
        public int RequiredKills;
        public int CurrentKills;
        public Mobile Mage;

        public bool IsComplete => CurrentKills >= RequiredKills;
    }

    private static readonly Dictionary<Mobile, ActiveQuest> PlayerQuests = new();

    public static ActiveQuest QuestOf(Mobile player) => PlayerQuests.GetValueOrDefault(player);

    /// <summary>Double-click handler entry point — see the court-mage NPC's OnDoubleClick.</summary>
    public static void Talk(Mobile player, Mobile giver)
    {
        if (PlayerQuests.TryGetValue(player, out var existing) && existing.Mage == giver)
        {
            if (existing.IsComplete)
            {
                TurnIn(player, giver, existing);
            }
            else
            {
                giver.PublicOverheadMessage(
                    MessageType.Regular, 0x3B2, false,
                    $"Повержено {existing.CurrentKills} из {existing.RequiredKills} ({RuName(existing.TargetTypeName)}). Возвращайся, когда закончишь."
                );
            }

            return;
        }

        if (existing != null)
        {
            giver.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "У тебя уже есть незавершённое задание от другого мага.");
            return;
        }

        var target = TargetPool[Utility.Random(TargetPool.Length)];
        var required = Utility.RandomMinMax(MinKills, MaxKills);

        var quest = new ActiveQuest
        {
            TargetTypeName = target,
            RequiredKills = required,
            CurrentKills = 0,
            Mage = giver
        };

        PlayerQuests[player] = quest;
        QuestMarkerRegistry.SetState(player, giver, QuestMarkerState.InProgress);

        giver.PublicOverheadMessage(
            MessageType.Regular, 0x3B2, false,
            $"Магическая нечисть тревожит покой двора — {RuName(target)}. Одолей {required} — получишь {required * PointsPerKill} очков магии."
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
            player.SendMessage(0x59, $"Задание придворного мага выполнено ({quest.CurrentKills}/{quest.RequiredKills}) — вернись за наградой.");
            QuestMarkerRegistry.SetState(player, quest.Mage, QuestMarkerState.Complete);
        }
    }

    private static void TurnIn(Mobile player, Mobile giver, ActiveQuest quest)
    {
        var reward = quest.RequiredKills * PointsPerKill;

        if (player is PlayerMobile playerMobile)
        {
            CourtMageSystem.AddPoints(playerMobile, reward);
        }

        giver.PublicOverheadMessage(
            MessageType.Regular, 0x3B2, false,
            $"Достойная работа! Вот {reward} очков магии — твои познания растут."
        );

        PlayerQuests.Remove(player);
        QuestMarkerRegistry.SetState(player, giver, QuestMarkerState.None);
    }

    public static string GetStatusLine(Mobile player, Mobile mage)
    {
        if (!PlayerQuests.TryGetValue(player, out var quest) || quest.Mage != mage)
        {
            return "Задания нет — поговори со мной, чтобы получить.";
        }

        return quest.IsComplete
            ? "Задание выполнено — возьми награду!"
            : $"Задание: повержено {quest.CurrentKills} из {quest.RequiredKills} ({RuName(quest.TargetTypeName)}).";
    }

    private static string RuName(string typeName) => typeName switch
    {
        "Wisp"         => "духов",
        "EnergyVortex" => "энергетических вихрей",
        "Imp"          => "бесов",
        "GazerLarva"   => "личинок глаза",
        "Gargoyle"     => "гаргулий",
        "EvilMage"     => "тёмных магов",
        _              => typeName
    };
}
