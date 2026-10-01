using Server.Mobiles;
using Server.Regions;

namespace Server.Systems.Bots;

/// <summary>Who is on whose side, and where an outlaw may go.</summary>
public static class BotSocialRules
{
    /// <summary>Guild mates, allied guilds, and members of the same hunting group.</summary>
    public static bool IsFriend(Mobile a, Mobile b) =>
        a != null && b != null && a != b &&
        (MahaonBots.BotGuilds.IsAllied(a, b) ||
         a is BotMobile { Brain.Group: { } ga } && b is BotMobile { Brain.Group: { } gb } && ga == gb);

    public static bool IsAtWar(Mobile a, Mobile b) => MahaonBots.BotGuilds.IsAtWar(a, b);

    /// <summary>Murderers and criminals: town guards attack them on sight.</summary>
    public static bool IsOutlaw(Mobile m) => m.Murderer || m.Criminal;

    /// <summary>
    /// Whether outlaws are hunted here. Vanilla guards may be switched off on this shard, but the
    /// town defenders that replace them still attack murderers inside guarded regions, so any
    /// guarded region counts — except the pirates' den, the outlaws' own town.
    /// </summary>
    public static bool IsGuarded(Map map, Point3D p) => IsGuarded(Region.Find(p, map));

    public static bool IsGuarded(Region region) =>
        region.GetRegion<GuardedRegion>() is { } guarded && guarded.Name?.Contains("Buccaneer") != true;

    /// <summary>The nearest town this bot can safely visit: any town, unless it is an outlaw, in
    /// which case only one without guards.</summary>
    public static BotCity TownFor(Mobile bot) => WorldCatalog.FindNearest(bot.Map, bot.Location, !IsOutlaw(bot));
}
