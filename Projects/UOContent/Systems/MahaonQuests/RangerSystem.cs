using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonQuests;

/// <summary>
///     Mahaon "Ranger" mechanic: joining the rangers and earning Ranger Points (mostly from
///     completing ranger/guard bounty quests, see GuardQuestSystem/RangerQuestSystem) raises
///     your rank, which grants a flat bonus to StamMax. Only rangers were said to move
///     tirelessly through the wilds, but the ranger rank bonus itself is open to anyone who
///     participates.
/// </summary>
public static class RangerSystem
{
    // Points required to reach each rank. Index 0 = novice tracker (0 pts), last = max rank.
    // Same curve as GuardSystem's — tune freely, this is a first pass.
    private static readonly int[] RankThresholds =
    [
        0, // Следопыт
        50, // Охотник
        150, // Егерь
        350, // Лесник
        700, // Старший егерь
        1200, // Хранитель леса
        2000 // Мастер-охотник
    ];

    public static readonly string[] RankNames =
    [
        "Следопыт",
        "Охотник",
        "Егерь",
        "Лесник",
        "Старший егерь",
        "Хранитель леса",
        "Мастер-охотник"
    ];

    // Flat StamMax bonus per rank level (rank 0 = +0, rank 6 = +60).
    private const int StamBonusPerRank = 10;

    private static Persistence _persistence;

    private static readonly Dictionary<PlayerMobile, int> RangerPoints = new();

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static int GetPoints(PlayerMobile player) => RangerPoints.GetValueOrDefault(player, 0);

    public static int GetRank(PlayerMobile player)
    {
        var points = GetPoints(player);
        var rank = 0;

        for (var i = 0; i < RankThresholds.Length; i++)
        {
            if (points >= RankThresholds[i])
            {
                rank = i;
            }
        }

        return rank;
    }

    public static string GetRankName(PlayerMobile player) => RankNames[GetRank(player)];

    /// <summary>
    ///     Flat StamMax bonus for the player's current ranger rank, scaled by how much this
    ///     player is actually a ranger: full bonus if Ranger is their PRIMARY profession
    ///     category, half if only secondary, none otherwise (or no profession chosen) — per
    ///     the shard owner's own rule, hooked via ProfessionSystem.GetCategoryBonusScale.
    ///     Hooked into PlayerMobile.StamMax.
    /// </summary>
    public static int GetStamBonus(PlayerMobile player)
    {
        var scale = Systems.MahaonProfessions.ProfessionSystem.GetCategoryBonusScale(
            player, Systems.MahaonProfessions.ProfessionCategory.Ranger
        );

        return (int)(GetRank(player) * StamBonusPerRank * scale);
    }

    public static void AddPoints(PlayerMobile player, int amount)
    {
        if (player == null || amount == 0)
        {
            return;
        }

        var oldRank = GetRank(player);
        RangerPoints[player] = GetPoints(player) + amount;
        var newRank = GetRank(player);

        player.SendMessage(0x59, $"Ты заработал {amount} очков следопыта. (всего: {GetPoints(player)})");

        if (newRank > oldRank)
        {
            player.SendMessage(0x59, $"Тебя повысили до звания «{RankNames[newRank]}»!");
            player.Delta(MobileDelta.Stam); // recompute StamMax
        }
    }

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonRanger", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt(RangerPoints.Count);
            foreach (var (player, points) in RangerPoints)
            {
                writer.Write(player);
                writer.Write(points);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            var count = reader.ReadEncodedInt();
            for (var i = 0; i < count; i++)
            {
                var player = reader.ReadEntity<PlayerMobile>();
                var points = reader.ReadInt();

                if (player != null)
                {
                    RangerPoints[player] = points;
                }
            }
        }
    }
}
