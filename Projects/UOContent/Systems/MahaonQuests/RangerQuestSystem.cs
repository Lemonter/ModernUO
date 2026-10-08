using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonQuests;

/// <summary>
///     Forest ranger quest-giver built on the same "kill N of this creature type" shape as
///     MayorQuestSystem, but the targets are wildlife (culling/thinning game animals) rather
///     than raid threats, and the payout is MahaonRanger rank points instead of gold.
///
///     Unlike MayorQuestSystem/GuardQuestSystem (silent kill-counter, no item involved),
///     progress here is a REAL physical trophy: killing a tracked wildlife type drops a
///     MahaonAnimalHead into its corpse (see DropHeadIfWildlife, hooked from
///     BaseCreature.OnDeath same place as the others) — the player has to actually loot it
///     and hand it to Питэр (checked/consumed from their backpack in Talk below), matching
///     the shard owner's explicit "нужен реальный предмет «голова»" ask. Heads drop for
///     ANY kill of these species, not just while a matching quest is active, so a player
///     can stockpile them ahead of time.
///
///     Fully independent from MayorQuestSystem (and from GuardQuestSystem) — separate
///     PlayerQuests dictionary and ActiveQuest type, so a player can have one quest active from
///     each giver at the same time.
///
///     In-memory only, same convention as the rest of the Mahaon systems — active quests reset
///     on restart (a stockpiled head item itself is real and persists fine, only the
///     "quest accepted" bookkeeping is in-memory), cheap to re-roll through normal play.
/// </summary>
public static class RangerQuestSystem
{
    private const int MinKills = 3;
    private const int MaxKills = 8;

    // Reward is ranger rank points instead of gold. RangerSystem's own rank thresholds run
    // 0/50/150/350/700/1200/2000, same shape as GuardSystem's — same pacing rationale as
    // GuardQuestSystem's PointsPerKill.
    private const int PointsPerKill = 20;

    // Wildlife, not raid enemies — class names verified against Projects/UOContent/Mobiles/Animals.
    private static readonly string[] TargetPool =
    {
        "GreatHart", "GrizzlyBear", "BlackBear", "Cougar", "Boar", "JackRabbit", "Eagle"
    };

    public class ActiveQuest
    {
        public string TargetTypeName;
        public int RequiredKills;
        public int CurrentKills;
        public Mobile Ranger;

        public bool IsComplete => CurrentKills >= RequiredKills;
    }

    private static readonly Dictionary<Mobile, ActiveQuest> PlayerQuests = new();

    public static ActiveQuest QuestOf(Mobile player) => PlayerQuests.GetValueOrDefault(player);

    /// <summary>Double-click handler entry point — see the forest-ranger NPC's OnDoubleClick.
    /// Also the turn-in point: checks the player's backpack for matching MahaonAnimalHead
    /// trophies and consumes as many as still needed every time they talk to Питэр, so
    /// carrying heads in and double-clicking him is the whole "turn in" gesture.</summary>
    public static void Talk(Mobile player, Mobile giver)
    {
        if (PlayerQuests.TryGetValue(player, out var existing) && existing.Ranger == giver)
        {
            var turnedIn = ConsumeHeads(player, existing);

            if (turnedIn > 0)
            {
                giver.PublicOverheadMessage(
                    MessageType.Regular, 0x3B2, false,
                    $"Принимаю {turnedIn} голов(ы) ({RuName(existing.TargetTypeName)})."
                );
            }

            if (existing.IsComplete)
            {
                TurnIn(player, giver, existing);
            }
            else
            {
                giver.PublicOverheadMessage(
                    MessageType.Regular, 0x3B2, false,
                    $"Сдано {existing.CurrentKills} из {existing.RequiredKills} голов ({RuName(existing.TargetTypeName)})." +
                    (turnedIn == 0 ? " Принеси ещё, когда добудешь." : "")
                );
            }

            return;
        }

        // Already has a DIFFERENT ranger's quest active — one at a time, keeps this
        // simple rather than juggling parallel trackers.
        if (existing != null)
        {
            giver.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "У тебя уже есть незавершённое задание от другого лесника.");
            return;
        }

        var target = TargetPool[Utility.Random(TargetPool.Length)];
        var required = Utility.RandomMinMax(MinKills, MaxKills);

        var quest = new ActiveQuest
        {
            TargetTypeName = target,
            RequiredKills = required,
            CurrentKills = 0,
            Ranger = giver
        };

        PlayerQuests[player] = quest;
        QuestMarkerRegistry.SetState(player, giver, QuestMarkerState.InProgress);

        giver.PublicOverheadMessage(
            MessageType.Regular, 0x3B2, false,
            $"В округе развелось слишком много {RuName(target)}. Принеси мне {required} голов — получишь {required * PointsPerKill} очков следопыта."
        );
    }

    /// <summary>Hooked from BaseCreature.OnDeath alongside Mayor/GuardQuestSystem's own kill
    /// hooks — drops a real MahaonAnimalHead into the corpse if this creature is one of the
    /// tracked wildlife types, regardless of whether the killer has a matching quest active
    /// (a standing trophy, not a quest-gated spawn — see class doc comment).</summary>
    public static void DropHeadIfWildlife(BaseCreature creature, Container corpse)
    {
        var typeName = creature.GetType().Name;

        if (Array.IndexOf(TargetPool, typeName) < 0)
        {
            return;
        }

        corpse?.DropItem(new MahaonAnimalHead(typeName, RuName(typeName)));
    }

    /// <summary>Removes up to as many matching-type MahaonAnimalHead items as the quest
    /// still needs from the player's backpack, crediting CurrentKills for each one consumed.
    /// Returns how many were actually turned in this call.</summary>
    private static int ConsumeHeads(Mobile player, ActiveQuest quest)
    {
        var backpack = player.Backpack;
        var stillNeeded = quest.RequiredKills - quest.CurrentKills;

        if (backpack == null || stillNeeded <= 0)
        {
            return 0;
        }

        var consumed = 0;

        // Mahaon: FindItemsByType iterates the backpack's live item list, so deleting a
        // head mid-loop threw "Item was modified after enumerator was instantiated" —
        // EnumerateItemsByType snapshots into a pooled queue first, which is safe to
        // delete from while iterating.
        using var heads = backpack.EnumerateItemsByType<MahaonAnimalHead>();

        foreach (var head in heads)
        {
            if (consumed >= stillNeeded)
            {
                break;
            }

            if (string.Equals(head.AnimalType, quest.TargetTypeName, StringComparison.Ordinal))
            {
                head.Delete();
                consumed++;
            }
        }

        quest.CurrentKills += consumed;

        return consumed;
    }

    private static void TurnIn(Mobile player, Mobile giver, ActiveQuest quest)
    {
        var reward = quest.RequiredKills * PointsPerKill;

        if (player is PlayerMobile playerMobile)
        {
            RangerSystem.AddPoints(playerMobile, reward);
        }

        // Mahaon: warmer completion message per the shard owner's ask, now that the
        // no-NPC MonsterHead->stamina shortcut is gone and this is the one real way to earn
        // the bonus — make finishing it feel like an actual accomplishment.
        giver.PublicOverheadMessage(
            MessageType.Regular, 0x3B2, false,
            $"Молодец, отлично потрудился! Вот {reward} очков следопыта — заслужил."
        );

        PlayerQuests.Remove(player);
        QuestMarkerRegistry.SetState(player, giver, QuestMarkerState.None);
    }

    private static string RuName(string typeName) => typeName switch
    {
        "GreatHart"  => "оленей",
        "GrizzlyBear" => "гризли",
        "BlackBear"  => "чёрных медведей",
        "Cougar"     => "пум",
        "Boar"       => "кабанов",
        "JackRabbit" => "зайцев",
        "Eagle"      => "орлов",
        _            => typeName
    };
}
