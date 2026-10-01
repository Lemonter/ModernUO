using System.Collections.Generic;
using Server.Commands;
using Server.Mobiles;

namespace Server.Systems.MahaonGuard;

/// <summary>
///     Mahaon "City Guard" mechanic: joining the guard and earning Guard Points (mostly from
///     killing raid mobs, see MahaonRaids) raises your rank, which grants a flat bonus to
///     HitsMax. Only warriors were said to be able to reach high tactics naturally, but the
///     guard rank bonus itself is open to anyone who participates.
/// </summary>
public class GuardSystem : GenericPersistence
{
    // Points required to reach each rank. Index 0 = Recruit (0 pts), last = max rank.
    // Tune freely — this is a first pass. 4 more added above Marshal per the shard
    // owner's own ask ("накинь ещё званий") — same growing-gap curve continued
    // (+1000/+1500/+2000/+2500) rather than flattening out at the old ceiling.
    private static readonly int[] RankThresholds =
    [
        0, // Recruit
        50, // Guardsman
        150, // Sergeant
        350, // Lieutenant
        700, // Captain
        1200, // Commander
        2000, // Marshal
        3000, // Generalissimo
        4500, // Legend of the Guard
        6500, // Guardian of the City
        9000 // Living Legend
    ];

    public static readonly string[] RankNames =
    [
        "Новобранец",
        "Стражник",
        "Сержант",
        "Лейтенант",
        "Капитан",
        "Командир",
        "Маршал",
        "Генералиссимус",
        "Легенда стражи",
        "Хранитель города",
        "Живая легенда"
    ];

    // Flat HitsMax bonus per rank level (rank 0 = +0, rank 10 = +100).
    private const int HitsBonusPerRank = 10;

    private static GuardSystem _instance;

    private static readonly Dictionary<PlayerMobile, int> GuardPoints = new();

    public GuardSystem() : base("MahaonGuard", 1)
    {
    }

    public static void Configure()
    {
        _instance = new GuardSystem();

        CommandSystem.Register("GuardRank", AccessLevel.Player, GuardRank_OnCommand);
        CommandSystem.Register("SetGuardPoints", AccessLevel.GameMaster, SetGuardPoints_OnCommand);
    }

    public static int GetPoints(PlayerMobile player) => GuardPoints.GetValueOrDefault(player, 0);

    /// <summary>Sets a random starting rank directly (not earned through points) — for
    /// warrior bots spawning in with an existing rank instead of everyone starting at
    /// Recruit.</summary>
    public static void SetRandomRank(PlayerMobile player)
    {
        var rank = Utility.Random(RankThresholds.Length);
        GuardPoints[player] = RankThresholds[rank];
    }

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

    /// <summary>Points still needed to reach the next rank, or null if already at the
    /// highest rank — public wrapper around RankThresholds for gumps/NPCs (see
    /// MahaonGuardSergeant) that shouldn't need to reimplement GuardRank_OnCommand's own
    /// math.</summary>
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
    ///     Flat HitsMax bonus for the player's current guard rank, scaled by how much this
    ///     player is actually a warrior: full bonus if Warrior is their PRIMARY profession
    ///     category, half if only secondary, none otherwise (or no profession chosen) — per
    ///     the shard owner's own rule, hooked via ProfessionSystem.GetCategoryBonusScale.
    ///     Hook this into PlayerMobile.HitsMax (see dev-docs note in this file's PR).
    /// </summary>
    /// <summary>Скидка в лавке наград за каждое звание.</summary>
    private const double ShopDiscountPerRank = 0.05;

    /// <summary>
    ///     Во сколько раз дешевле обходится награда за выслугу.
    ///
    ///     Очки стражи — это счётчик выслуги, а не кошелёк: звание считается по ОБЩЕМУ их
    ///     числу (см. GetRank), поэтому «потратить» их нельзя, не разжаловав человека. При
    ///     переводе лавки на золото это оставило накопленные очки вовсе ни при чём —
    ///     звание есть, очков гора, а в лавке они не значат ничего.
    ///
    ///     Скидка возвращает им смысл, ничего не отнимая: пять процентов за звание, до
    ///     половины цены у «Живой легенды». Выслуга не покупает награду, но делает её
    ///     доступнее — что для стражи и логично.
    /// </summary>
    public static double GetShopPriceScalar(PlayerMobile player) =>
        System.Math.Clamp(1.0 - GetRank(player) * ShopDiscountPerRank, 0.1, 1.0);

    public static int GetHitsBonus(PlayerMobile player)
    {
        var scale = Systems.MahaonProfessions.ProfessionSystem.GetCategoryBonusScale(
            player, Systems.MahaonProfessions.ProfessionCategory.Warrior
        );

        return (int)(GetRank(player) * HitsBonusPerRank * scale);
    }

    /// <summary>Spends guard points — used by the rewards shop (MahaonRewardShop). Returns
    /// false (and spends nothing) if the player doesn't have enough.</summary>
    public static bool SpendPoints(PlayerMobile player, int amount)
    {
        if (player == null || amount <= 0 || GetPoints(player) < amount)
        {
            return false;
        }

        GuardPoints[player] = GetPoints(player) - amount;
        return true;
    }

    public static void AddPoints(PlayerMobile player, int amount)
    {
        if (player == null || amount == 0)
        {
            return;
        }

        var oldRank = GetRank(player);
        GuardPoints[player] = GetPoints(player) + amount;
        var newRank = GetRank(player);

        player.SendMessage(0x59, $"Ты заработал {amount} очков стражи. (всего: {GetPoints(player)})");

        if (newRank > oldRank)
        {
            player.SendMessage(0x59, $"Тебя повысили до звания «{RankNames[newRank]}»!");
            player.Delta(MobileDelta.Hits); // recompute HitsMax
        }
    }

    [Usage("GuardRank")]
    [Description("Shows your current guard rank and points.")]
    private static void GuardRank_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile player)
        {
            return;
        }

        var rank = GetRank(player);
        var points = GetPoints(player);
        var nextThreshold = rank + 1 < RankThresholds.Length ? RankThresholds[rank + 1] : -1;

        player.SendMessage(0x59, $"Звание стражи: {RankNames[rank]} ({points} очков)");

        if (nextThreshold >= 0)
        {
            player.SendMessage(0x59, $"Следующее звание на {nextThreshold} очках (осталось: {nextThreshold - points}).");
        }
        else
        {
            player.SendMessage(0x59, "Ты достиг максимального звания стражи.");
        }
    }

    [Usage("SetGuardPoints <player> <amount>")]
    [Description("Sets a player's guard points directly (GM tool).")]
    private static void SetGuardPoints_OnCommand(CommandEventArgs e)
    {
        if (e.Length < 2 || e.Mobile is not PlayerMobile gm)
        {
            e.Mobile.SendMessage("Использование: [SetGuardPoints <игрок> <количество>");
            return;
        }

        var playerName = e.GetString(0);
        var amount = e.GetInt32(1);

        PlayerMobile target = null;
        foreach (var m in World.Mobiles.Values)
        {
            if (m is PlayerMobile pm && m.RawName.InsensitiveEquals(playerName))
            {
                target = pm;
                break;
            }
        }

        if (target == null)
        {
            gm.SendMessage($"Игрок '{playerName}' не найден.");
            return;
        }

        GuardPoints[target] = amount;
        gm.SendMessage($"Очки стражи {target.Name} установлены в {amount} (звание: {GetRankName(target)}).");
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(GuardPoints.Count);
        foreach (var (player, points) in GuardPoints)
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
                GuardPoints[player] = points;
            }
        }
    }
}
