using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonBots;

/// <summary>
///     PvP/craft leaning per named bot guild — same "keyed by name, works even before a
///     real Guild object exists" pattern as BotGuilds.Relations. PvpPercent biases which
///     BotArchetype gets rolled when a beacon assigned to this guild spawns a bot (higher
///     = more Warrior/Mage/Archer, less Crafter/Trader); AllowsCrafts, when false, removes
///     Crafter/Trader from the roll entirely regardless of percent — a PK guild doesn't
///     want tradesmen at all, not just fewer of them.
/// </summary>
public sealed class GuildSpecialization : GenericPersistence
{
    private static GuildSpecialization _instance;

    public const int DefaultPvpPercent = 50;

    private static readonly Dictionary<string, int> PvpPercentByGuild = new();
    private static readonly Dictionary<string, bool> AllowsCraftsByGuild = new();

    public GuildSpecialization() : base("MahaonGuildSpecialization", 1)
    {
    }

    public static void Configure() => _instance = new GuildSpecialization();

    public static int GetPvpPercent(string guildName) =>
        guildName != null && PvpPercentByGuild.TryGetValue(guildName, out var pct) ? pct : DefaultPvpPercent;

    public static void SetPvpPercent(string guildName, int percent)
    {
        if (guildName == null)
        {
            return;
        }

        PvpPercentByGuild[guildName] = System.Math.Clamp(percent, 0, 100);
    }

    public static bool GetAllowsCrafts(string guildName) =>
        guildName == null || !AllowsCraftsByGuild.TryGetValue(guildName, out var allows) || allows;

    public static void SetAllowsCrafts(string guildName, bool allows)
    {
        if (guildName == null)
        {
            return;
        }

        AllowsCraftsByGuild[guildName] = allows;
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(PvpPercentByGuild.Count);
        foreach (var (guildName, pct) in PvpPercentByGuild)
        {
            writer.Write(guildName);
            writer.WriteEncodedInt(pct);
        }

        writer.WriteEncodedInt(AllowsCraftsByGuild.Count);
        foreach (var (guildName, allows) in AllowsCraftsByGuild)
        {
            writer.Write(guildName);
            writer.Write(allows);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var pvpCount = reader.ReadEncodedInt();
        for (var i = 0; i < pvpCount; i++)
        {
            var guildName = reader.ReadString();
            var pct = reader.ReadEncodedInt();
            PvpPercentByGuild[guildName] = pct;
        }

        var craftsCount = reader.ReadEncodedInt();
        for (var i = 0; i < craftsCount; i++)
        {
            var guildName = reader.ReadString();
            var allows = reader.ReadBool();
            AllowsCraftsByGuild[guildName] = allows;
        }
    }
}
