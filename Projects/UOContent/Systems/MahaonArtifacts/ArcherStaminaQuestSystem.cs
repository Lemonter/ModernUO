using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonArtifacts;

/// <summary>
///     Mahaon archer quest: bring heads from different kinds of monsters to raise your max
///     stamina permanently. Only distinct monster names count — turning in five heads from
///     the same creature type only counts once.
/// </summary>
public class ArcherStaminaQuestSystem : GenericPersistence
{
    private static ArcherStaminaQuestSystem _instance;

    // Reward tiers by the head's captured difficulty (source creature's Fame). Harder
    // monsters give more stamina — tune the breakpoints/amounts freely.
    private static int StamForDifficulty(int fame) => fame switch
    {
        >= 15000 => 25, // boss-tier
        >= 5000  => 12, // strong
        >= 1000  => 6,  // medium
        _        => 3   // weak
    };

    private const int MaxUniqueHeads = 15; // caps how many distinct heads count at all

    private static readonly Dictionary<Mobile, HashSet<string>> TurnedIn = new();
    private static readonly Dictionary<Mobile, int> TotalBonus = new();

    public ArcherStaminaQuestSystem() : base("MahaonArcherStaminaQuest", 1)
    {
    }

    public static void Configure()
    {
        _instance = new ArcherStaminaQuestSystem();
    }

    public static int GetStamBonus(Mobile m) => TotalBonus.GetValueOrDefault(m, 0);

    /// <summary>
    ///     Attempts to turn in a monster head. Returns true and grants the bonus if this
    ///     monster type hadn't been turned in before (and the cap isn't reached); false
    ///     (and the head is NOT consumed) if it was a duplicate or the cap is maxed.
    /// </summary>
    public static bool TryTurnIn(PlayerMobile player, MonsterHead head)
    {
        if (!TurnedIn.TryGetValue(player, out var set))
        {
            TurnedIn[player] = set = new HashSet<string>();
        }

        if (set.Count >= MaxUniqueHeads)
        {
            player.SendMessage("Ты уже выбрал максимум по этому квесту на стамину.");
            return false;
        }

        if (!set.Add(head.MonsterName))
        {
            player.SendMessage($"Ты уже сдавал голову от «{head.MonsterName}».");
            return false;
        }

        var reward = StamForDifficulty(head.Difficulty);
        TotalBonus[player] = TotalBonus.GetValueOrDefault(player, 0) + reward;

        player.Delta(MobileDelta.Stam);
        player.SendMessage(0x59, $"Твоя выносливость растёт. (+{reward} к макс. стамине, сдано видов: {set.Count}/{MaxUniqueHeads})");
        return true;
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(TurnedIn.Count);

        foreach (var (mobile, set) in TurnedIn)
        {
            writer.Write(mobile);
            writer.WriteEncodedInt(set.Count);

            foreach (var name in set)
            {
                writer.Write(name);
            }

            writer.WriteEncodedInt(TotalBonus.GetValueOrDefault(mobile, 0));
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var mobileCount = reader.ReadEncodedInt();
        for (var i = 0; i < mobileCount; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var nameCount = reader.ReadEncodedInt();

            var set = new HashSet<string>();
            for (var j = 0; j < nameCount; j++)
            {
                set.Add(reader.ReadString());
            }

            var totalBonus = reader.ReadEncodedInt();

            if (mobile != null)
            {
                TurnedIn[mobile] = set;
                TotalBonus[mobile] = totalBonus;
            }
        }
    }
}
