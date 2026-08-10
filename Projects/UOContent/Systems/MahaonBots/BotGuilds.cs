using System.Collections.Generic;
using Server.Guilds;

namespace Server.Systems.MahaonBots;

public enum BotGuildRelation
{
    Neutral,
    Ally,
    War
}

/// <summary>
///     Real Guild objects (Server.Guilds.Guild) for bot factions — bots actually join them
///     (guild tag over the head, name in the paperdoll/props, the usual), but war/ally/
///     neutral between two bot guilds is tracked here instead of through the vanilla
///     WarDeclaration/Alliance machinery.
///
///     Relations are keyed by NAME (string), not by Guild reference — deliberately, so the
///     GM gump can display and set relations for a guild that doesn't have a single bot in
///     it yet. A real Guild.Guild object can't exist without a living Mobile leader (its
///     constructor unconditionally calls AddMember(leader), which throws instantly on a
///     null leader — this crashed the server the first time the guild gump opened for a
///     guild nobody had joined yet). So the real Guild object only gets created the first
///     time an actual bot joins one (see Join below); until then GetOrCreate(name) with no
///     founder just isn't a thing anymore, only TryGet (safe, returns null).
///
///     Why not just use guild.IsWar()/IsAlly() directly: that machinery hard-branches on
///     Guild.NewGuildSystem (== Core.SE, i.e. depends on which era this shard runs), and
///     either path (old Allies/Enemies lists vs. new WarDeclaration+AllianceInfo) expects
///     to get there through an interactive accept/reject gump flow between two *players*.
///     Not something a GM configuring NPC bot factions from a beacon gump should have to
///     fight with, and not something that should silently do nothing if the shard happens
///     to run the "wrong" era. This is the same "call the real system for the parts that
///     matter (real Guild, real membership), skip the interactive bits" pattern the rest
///     of Mahaon already uses (blueprint buying, gathering, etc).
/// </summary>
public static class BotGuilds
{
    private static readonly Dictionary<string, Guild> Registry = new();
    private static readonly Dictionary<(string, string), BotGuildRelation> Relations = new();

    // 15 flavor names, no particular lore attached — pick freely, rename/replace any of
    // these directly in this array, nothing else references them by name.
    public static readonly string[] NamePool =
    {
        "Клинок Рассвета", "Пепел Луны", "Стальной Завет", "Дети Бурь",
        "Орден Пепла", "Багровый Договор", "Северный Волк", "Тихий Клинок",
        "Железная Гвардия", "Странники Пустоши", "Ночная Стража", "Огненный Круг",
        "Костяной Легион", "Сумеречный Союз", "Вольные Мечи"
    };

    public static IReadOnlyCollection<Guild> All => Registry.Values;

    /// <summary>The real Guild object for this name, if one's actually been created (i.e.
    /// at least one bot has joined it via Join). Safe to call any time — returns null
    /// rather than creating anything, so it's fine to use for read-only UI display even
    /// for a guild nobody's in yet.
    ///
    /// Falls back to BaseGuild.FindByName when the in-memory Registry doesn't have it —
    /// Registry itself doesn't survive a server restart, but the real Guild objects it
    /// points to DO (the engine saves/loads guilds normally). Without this fallback,
    /// Join() below would think no guild by this name exists yet and create a duplicate,
    /// orphaning the original with all its real members and history.</summary>
    public static Guild TryGet(string name)
    {
        if (name == null)
        {
            return null;
        }

        if (Registry.TryGetValue(name, out var g))
        {
            return g;
        }

        if (BaseGuild.FindByName(name) is Guild found)
        {
            Registry[name] = found;
            return found;
        }

        return null;
    }

    /// <summary>The one entry point bots should use to join a named guild — creates the
    /// real Guild the first time anyone actually joins it (using that bot as founder/
    /// leader, since the constructor requires one), and keeps a real Leader assigned after
    /// that too (re-picked if the founder's ever deleted) so Guild.Disbanded stays false.</summary>
    public static void Join(string guildName, Mobile bot)
    {
        if (string.IsNullOrEmpty(guildName) || bot == null)
        {
            return;
        }

        var guild = TryGet(guildName); // checks Registry, falls back to a real persisted guild

        if (guild == null)
        {
            guild = new Guild(bot, guildName, MakeAbbreviation(guildName)) { Type = GuildType.Regular };
            Registry[guildName] = guild;
        }

        guild.AddMember(bot);

        if (guild.Leader?.Deleted != false)
        {
            guild.Leader = bot;
        }
    }

    private static string MakeAbbreviation(string name)
    {
        var letters = new List<char>();

        foreach (var word in name.Split(' '))
        {
            if (word.Length > 0)
            {
                letters.Add(char.ToUpperInvariant(word[0]));
            }
        }

        return new string(letters.ToArray());
    }

    public static BotGuildRelation GetRelation(string a, string b)
    {
        if (a == null || b == null || a == b)
        {
            return BotGuildRelation.Neutral; // no guild (or same guild) is never hostile
        }

        if (Relations.TryGetValue((a, b), out var relation))
        {
            return relation;
        }

        return Relations.TryGetValue((b, a), out relation) ? relation : BotGuildRelation.Neutral;
    }

    /// <summary>Sets how guild A and guild B feel about each other — symmetric, one call
    /// covers both directions. Overwrites whatever relation was there before. Works even
    /// if neither guild has a real Guild object yet (see class remarks).</summary>
    public static void SetRelation(string a, string b, BotGuildRelation relation)
    {
        if (a == null || b == null || a == b)
        {
            return;
        }

        Relations[(a, b)] = relation;
        Relations.Remove((b, a)); // keep exactly one direction stored, no stale duplicate
    }

    public static bool IsAtWar(Mobile a, Mobile b) =>
        GetRelation(a?.Guild?.Name, b?.Guild?.Name) == BotGuildRelation.War;

    public static bool IsAllied(Mobile a, Mobile b) =>
        a?.Guild != null && a.Guild == b?.Guild ||
        GetRelation(a?.Guild?.Name, b?.Guild?.Name) == BotGuildRelation.Ally;
}
