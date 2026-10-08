using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonQuests;

/// <summary>
///     First concrete quest-giver built on top of QuestMarkerRegistry — a mayor per city
///     hands out a simple "kill N of this creature type" job, tracks progress via
///     BaseCreature.OnDeath (see the hook there), pays gold on turn-in. Deliberately the
///     simplest possible quest shape to prove the marker/registry plumbing end to end
///     before the bounty board and crafting guild orders (which will want richer quest
///     types) get built on the same foundation.
///
///     In-memory only, same convention as the rest of the Mahaon systems — active quests
///     reset on restart, cheap to re-roll through normal play.
/// </summary>
public static class MayorQuestSystem
{
    private const int MinKills = 3;
    private const int MaxKills = 8;
    private const int GoldPerKill = 40;

    // Reasonable, commonly-found targets — kept short and generic rather than trying to
    // guess what's actually nearby a given city; a wrong pick just means the player picks
    // a different mayor's quest instead of one that's hard to find targets for.
    private static readonly string[] TargetPool =
    {
        "Skeleton", "Zombie", "GreyWolf", "DireWolf", "Ratman", "Orc", "Brigand"
    };

    public class ActiveQuest
    {
        public string TargetTypeName;
        public int RequiredKills;
        public int CurrentKills;
        public Mobile Mayor;

        public bool IsComplete => CurrentKills >= RequiredKills;
    }

    private static readonly Dictionary<Mobile, ActiveQuest> PlayerQuests = new();

    public static ActiveQuest QuestOf(Mobile player) => PlayerQuests.GetValueOrDefault(player);

    /// <summary>Double-click handler entry point — see MahaonMayor.OnDoubleClick.</summary>
    public static void Talk(Mobile player, Mobile mayor)
    {
        if (PlayerQuests.TryGetValue(player, out var existing) && existing.Mayor == mayor)
        {
            if (existing.IsComplete)
            {
                TurnIn(player, mayor, existing);
            }
            else
            {
                mayor.PublicOverheadMessage(
                    MessageType.Regular, 0x3B2, false,
                    $"Убито {existing.CurrentKills} из {existing.RequiredKills} ({RuName(existing.TargetTypeName)}). Возвращайся, когда закончишь."
                );
            }

            return;
        }

        // Already has a DIFFERENT mayor's quest active — one at a time, keeps this
        // simple rather than juggling parallel trackers.
        if (existing != null)
        {
            mayor.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "У тебя уже есть незавершённое задание от другого мэра.");
            return;
        }

        var target = TargetPool[Utility.Random(TargetPool.Length)];
        var required = Utility.RandomMinMax(MinKills, MaxKills);

        var quest = new ActiveQuest
        {
            TargetTypeName = target,
            RequiredKills = required,
            CurrentKills = 0,
            Mayor = mayor
        };

        PlayerQuests[player] = quest;
        QuestMarkerRegistry.SetState(player, mayor, QuestMarkerState.InProgress);

        mayor.PublicOverheadMessage(
            MessageType.Regular, 0x3B2, false,
            $"Городу докучают {RuName(target)}. Принеси мне {required} — и получишь {required * GoldPerKill} золота."
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
            player.SendMessage(0x59, $"Задание мэра выполнено ({quest.CurrentKills}/{quest.RequiredKills}) — вернись за наградой.");
            QuestMarkerRegistry.SetState(player, quest.Mayor, QuestMarkerState.Complete);
        }
    }

    private static void TurnIn(Mobile player, Mobile mayor, ActiveQuest quest)
    {
        var reward = quest.RequiredKills * GoldPerKill;

        if (player.Backpack != null)
        {
            player.Backpack.DropItem(new Items.Gold(reward));
        }

        mayor.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Благодарю! Вот твои {reward} золота.");

        PlayerQuests.Remove(player);
        QuestMarkerRegistry.SetState(player, mayor, QuestMarkerState.None);
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
