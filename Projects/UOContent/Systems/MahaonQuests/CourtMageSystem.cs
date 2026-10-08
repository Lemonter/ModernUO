using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonQuests;

/// <summary>
///     Mahaon "Court Mage" mechanic — same shape as GuardSystem/RangerSystem (points earned
///     from a quest raise your rank, which grants a flat bonus, here to ManaMax instead of
///     HitsMax/StamMax). Придворный маг is the quest-giver (see MahaonCourtMage.cs).
/// </summary>
public static class CourtMageSystem
{
    // Same curve as Guard/RangerSystem's — tune freely, this is a first pass.
    private static readonly int[] RankThresholds =
    [
        0, // Подмастерье
        50, // Заклинатель
        150, // Чародей
        350, // Волшебник
        700, // Архимаг
        1200, // Магистр
        2000 // Придворный чародей
    ];

    public static readonly string[] RankNames =
    [
        "Подмастерье",
        "Заклинатель",
        "Чародей",
        "Волшебник",
        "Архимаг",
        "Магистр",
        "Придворный чародей"
    ];

    // Flat ManaMax bonus per rank level (rank 0 = +0, rank 6 = +60).
    private const int ManaBonusPerRank = 10;

    private static Persistence _persistence;

    private static readonly Dictionary<PlayerMobile, int> MagePoints = new();

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static int GetPoints(PlayerMobile player) => MagePoints.GetValueOrDefault(player, 0);

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

    public static int? GetPointsToNextRank(PlayerMobile player)
    {
        var rank = GetRank(player);

        if (rank + 1 >= RankThresholds.Length)
        {
            return null;
        }

        return RankThresholds[rank + 1] - GetPoints(player);
    }

    /// <summary>
    ///     Flat ManaMax bonus for the player's current rank, scaled by how much this player
    ///     is actually a mage-type profession — same GetCategoryBonusScale pattern as
    ///     GuardSystem.GetHitsBonus/RangerSystem.GetStamBonus. Hooked into PlayerMobile.ManaMax.
    /// </summary>
    public static int GetManaBonus(PlayerMobile player)
    {
        var scale = Systems.MahaonProfessions.ProfessionSystem.GetCategoryBonusScale(
            player, Systems.MahaonProfessions.ProfessionCategory.Magic
        );

        return (int)(GetRank(player) * ManaBonusPerRank * scale);
    }

    public static void AddPoints(PlayerMobile player, int amount)
    {
        if (player == null || amount == 0)
        {
            return;
        }

        var oldRank = GetRank(player);
        MagePoints[player] = GetPoints(player) + amount;
        var newRank = GetRank(player);

        player.SendMessage(0x59, $"Ты заработал {amount} очков магии. (всего: {GetPoints(player)})");

        if (newRank > oldRank)
        {
            player.SendMessage(0x59, $"Тебя повысили до звания «{RankNames[newRank]}»!");
            player.Delta(MobileDelta.Mana); // recompute ManaMax
        }
    }

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonCourtMage", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt(MagePoints.Count);
            foreach (var (player, points) in MagePoints)
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
                    MagePoints[player] = points;
                }
            }
        }
    }
}
