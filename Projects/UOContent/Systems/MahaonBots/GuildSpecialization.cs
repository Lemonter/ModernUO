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
public static class GuildSpecialization
{
    public const int DefaultPvpPercent = 50;

    private static readonly Dictionary<string, int> PvpPercentByGuild = new();
    private static readonly Dictionary<string, bool> AllowsCraftsByGuild = new();

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

    /// <summary>Rolls a BotArchetype biased by this guild's specialization — called from
    /// wherever a beacon assigned to a guild picks what to spawn next, instead of the
    /// beacon's own flat per-archetype target counts alone.</summary>
    public static BotArchetype RollArchetype(string guildName)
    {
        var pvpPercent = GetPvpPercent(guildName);
        var allowsCrafts = GetAllowsCrafts(guildName);

        if (Utility.Random(100) < pvpPercent)
        {
            // PvP-leaning roll — Warrior/Mage/Archer only, evenly split.
            return Utility.Random(3) switch
            {
                0 => BotArchetype.Warrior,
                1 => BotArchetype.Mage,
                _ => BotArchetype.Archer
            };
        }

        // PvE-leaning roll — crafts included only if this guild allows them.
        return allowsCrafts
            ? Utility.Random(5) switch
            {
                0 => BotArchetype.Warrior,
                1 => BotArchetype.Mage,
                2 => BotArchetype.Archer,
                3 => BotArchetype.Crafter,
                _ => BotArchetype.Trader
            }
            : Utility.Random(3) switch
            {
                0 => BotArchetype.Warrior,
                1 => BotArchetype.Mage,
                _ => BotArchetype.Archer
            };
    }
}
