using System.Collections.Generic;
using Server.Guilds;

namespace Server.Systems.MahaonBots;

/// <summary>
///     What GuildBank's gems are actually FOR — until this existed, gems dropped into a
///     guild's bank on every member death (see BotPets.DropGuildDeathGem) and just sat
///     there forever, read-only, shown as a number in MahaonBotGuildGump with nothing to
///     spend it on. This adds the one sink: "боевая подготовка" — spending gems permanently
///     buffs Str/Dex/Int, but only on ONE member per purchase — picked at random from
///     whoever's alive and in the guild *at the moment of purchase*, not a fixed,
///     player-chosen "champion" that keeps stacking bonuses forever. Losing that one bot
///     later means the investment is gone with it; the guild has to keep earning and
///     spending gems to keep any of its members strong. LevelByGuild below only tracks how
///     many times the guild has bought a boost (for the rising cost curve) — it's not a
///     "guild-wide tier" anymore, since the bonus never applies to more than one bot.
/// </summary>
public sealed class GuildUpgrades : GenericPersistence
{
    private static GuildUpgrades _instance;

    public const int MaxLevel = 5;
    public const int StatBonusPerPurchase = 5; // applied to Str, Dex, and Int on the one bot picked

    // Cost of the guild's 1st through 5th boost purchase ever — index 0 is the price of
    // the 1st. Rising cost means the death-gem trickle (1 gem per member death, weighted
    // toward common gems) takes meaningfully longer to afford each successive purchase.
    private static readonly int[] CostForPurchase = { 10, 20, 35, 55, 80 };

    private static readonly Dictionary<string, int> PurchaseCountByGuild = new();

    public GuildUpgrades() : base("MahaonGuildUpgrades", 1)
    {
    }

    public static void Configure() => _instance = new GuildUpgrades();

    public static int GetPurchaseCount(string guildName) =>
        guildName != null && PurchaseCountByGuild.TryGetValue(guildName, out var count) ? count : 0;

    /// <summary>Gem cost of the next purchase, or -1 if the guild's already bought all
    /// MaxLevel boosts available.</summary>
    public static int GetNextPurchaseCost(string guildName)
    {
        var count = GetPurchaseCount(guildName);
        return count >= MaxLevel ? -1 : CostForPurchase[count];
    }

    /// <summary>Tries to buy one boost for this guild, spending gems straight out of
    /// GuildBank and applying it to a random living member of the guild. Returns false
    /// (spending nothing) if already maxed, the bank doesn't have enough gems, or the
    /// guild currently has no living member to receive it — never partially spends.</summary>
    public static bool TryBoostRandomMember(string guildName)
    {
        if (string.IsNullOrEmpty(guildName))
        {
            return false;
        }

        var cost = GetNextPurchaseCost(guildName);
        if (cost < 0)
        {
            return false;
        }

        var guild = BotGuilds.TryGet(guildName);
        var recipient = PickRandomLivingMember(guild);
        if (recipient == null)
        {
            return false;
        }

        if (!GuildBank.TrySpendGems(guildName, cost))
        {
            return false;
        }

        recipient.RawStr += StatBonusPerPurchase;
        recipient.RawDex += StatBonusPerPurchase;
        recipient.RawInt += StatBonusPerPurchase;

        PurchaseCountByGuild[guildName] = GetPurchaseCount(guildName) + 1;
        return true;
    }

    private static Mobile PickRandomLivingMember(Guild guild)
    {
        if (guild == null)
        {
            return null;
        }

        var candidates = new List<Mobile>();

        foreach (var member in guild.Members)
        {
            if (member?.Deleted == false && member.Alive)
            {
                candidates.Add(member);
            }
        }

        return candidates.Count == 0 ? null : candidates[Utility.Random(candidates.Count)];
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(PurchaseCountByGuild.Count);

        foreach (var (guildName, count) in PurchaseCountByGuild)
        {
            writer.Write(guildName);
            writer.WriteEncodedInt(count);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var entryCount = reader.ReadEncodedInt();
        for (var i = 0; i < entryCount; i++)
        {
            var guildName = reader.ReadString();
            var count = reader.ReadEncodedInt();
            PurchaseCountByGuild[guildName] = count;
        }
    }
}
