using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.Engines.Pathing.Nav;

public enum NavLinkKind : byte
{
    Teleporter,

    /// <summary>A public moongate into the moongate hub.</summary>
    MoongateEnter,

    /// <summary>Out of the moongate hub to one of the gates' destinations.</summary>
    MoongateExit
}

/// <summary>What keeps a walker off a link: the moongates refuse criminals, and send murderers
/// only to Felucca.</summary>
[System.Flags]
public enum NavAccess : byte
{
    None = 0,
    Criminal = 1,
    Murderer = 2
}

/// <summary>
/// A non-walking edge: stepping on <see cref="Source"/> puts the walker at
/// <see cref="Destination"/>, possibly on another map. Immutable, and holds map ids rather than
/// maps, so the route worker can read it off the loop.
/// </summary>
public sealed class NavLink
{
    public NavLink(
        NavLinkKind kind, int sourceMapId, Point3D source, int sourceRegion, int destMapId, Point3D dest, int destRegion,
        NavAccess forbidden = NavAccess.None
    )
    {
        Kind = kind;
        Forbidden = forbidden;
        SourceMapId = sourceMapId;
        Source = source;
        SourceRegion = sourceRegion;
        DestMapId = destMapId;
        Destination = dest;
        DestRegion = destRegion;
    }

    public NavLinkKind Kind { get; }
    public int SourceMapId { get; }
    public Point3D Source { get; }
    public int SourceRegion { get; }
    public int DestMapId { get; }
    public Point3D Destination { get; }
    public int DestRegion { get; }

    /// <summary>Walkers with any of these flags can't take the link.</summary>
    public NavAccess Forbidden { get; }

    public bool Allows(NavAccess access) => (Forbidden & access) == 0;

    /// <summary>Tenths of a tile. A teleport is instant; a token cost keeps routes from
    /// bouncing through teleporters for no reason. A moongate trip is two links, half each.</summary>
    public int Cost => Kind == NavLinkKind.Teleporter ? 20 : 10;
}

/// <summary>
/// The links layered on top of the static graphs. Teleporters are world items — placed by
/// decoration, moved and deleted by staff — so they are gathered from the live world instead of
/// being baked, and rebuilt on demand with [NavLinks.
/// </summary>
public static class NavLinks
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(NavLinks));

    private static readonly List<NavLink> _all = [];

    public static IReadOnlyList<NavLink> All => _all;

    public static long NodeKey(int mapId, int region) => ((long)mapId << 32) | (uint)region;

    /// <summary>
    /// The public moongates as one virtual node: every gate leads into it and it leads out to every
    /// destination, so N gates and M destinations cost N + M links rather than N × M. No real map
    /// uses this id.
    /// </summary>
    public const int HubMapId = 0xFE;

    public static readonly long MoongateHub = NodeKey(HubMapId, 0);

    public static void Rebuild()
    {
        _all.Clear();

        var skipped = 0;

        foreach (var map in Map.AllMaps)
        {
            if (map == null || map == Map.Internal || NavSystem.GetGraph(map) == null)
            {
                continue;
            }

            foreach (var tele in map.GetItemsInBounds<Teleporter>(new Rectangle2D(0, 0, map.Width, map.Height)))
            {
                // Only the plain teleporter: its subclasses gate on keywords, skills, quests or
                // timers, and a route through one would strand the walker on the tile. Creature-only
                // or player-blocking checks don't apply to bots, which walk as players.
                if (tele.GetType() != typeof(Teleporter) || !tele.Active || tele.Parent != null)
                {
                    continue;
                }

                var destMap = tele.MapDest ?? map;
                var dest = tele.PointDest;

                if (destMap == Map.Internal || dest == Point3D.Zero)
                {
                    continue;
                }

                if (!AddCore(NavLinkKind.Teleporter, map, tele.Location, destMap, dest))
                {
                    skipped++;
                }
            }
        }

        var teleporters = _all.Count;
        var gates = AddMoongates(PublicMoongate.AllDestinations());

        NavSystem.PublishSnapshot();

        logger.Information(
            "Nav links: {Count} teleporters linked, {Skipped} skipped (no walkable ground at an end), {Gates} moongates",
            teleporters,
            skipped,
            gates
        );
    }

    internal static int AddMoongates(IEnumerable<(Map Map, Point3D Location)> destinations)
    {
        var gates = 0;

        foreach (var map in Map.AllMaps)
        {
            if (map == null || map == Map.Internal || NavSystem.GetGraph(map) is not { } graph)
            {
                continue;
            }

            foreach (var gate in map.GetItemsInBounds<PublicMoongate>(new Rectangle2D(0, 0, map.Width, map.Height)))
            {
                if (gate.Parent != null)
                {
                    continue;
                }

                // The gate is used from beside it; the tile next to it is what has to be walkable.
                var region = graph.Locate(gate.X, gate.Y, gate.Z, 2);
                if (region < 0)
                {
                    continue;
                }

                _all.Add(
                    new NavLink(NavLinkKind.MoongateEnter, map.MapID, gate.Location, region, HubMapId, Point3D.Zero, 0, NavAccess.Criminal)
                );
                gates++;
            }
        }

        if (gates == 0)
        {
            return 0;
        }

        foreach (var (destMap, dest) in destinations)
        {
            if (NavSystem.GetGraph(destMap) is not { } graph)
            {
                continue;
            }

            var region = graph.Locate(dest.X, dest.Y, dest.Z, 2);
            if (region >= 0)
            {
                var forbidden = destMap == Map.Felucca ? NavAccess.None : NavAccess.Murderer;
                _all.Add(new NavLink(NavLinkKind.MoongateExit, HubMapId, Point3D.Zero, 0, destMap.MapID, dest, region, forbidden));
            }
        }

        return gates;
    }

    /// <summary>Whether the public moongates can take a walker with <paramref name="access"/> from
    /// one map to another.</summary>
    public static bool GatesConnect(int fromMapId, int toMapId, NavAccess access)
    {
        bool enter = false, exit = false;

        foreach (var link in _all)
        {
            if (!link.Allows(access))
            {
                continue;
            }

            enter |= link.Kind == NavLinkKind.MoongateEnter && link.SourceMapId == fromMapId;
            exit |= link.Kind == NavLinkKind.MoongateExit && link.DestMapId == toMapId;

            if (enter && exit)
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryAdd(NavLinkKind kind, Map sourceMap, Point3D source, Map destMap, Point3D dest)
    {
        if (!AddCore(kind, sourceMap, source, destMap, dest))
        {
            return false;
        }

        NavSystem.PublishSnapshot();
        return true;
    }

    private static bool AddCore(NavLinkKind kind, Map sourceMap, Point3D source, Map destMap, Point3D dest)
    {
        var sourceGraph = NavSystem.GetGraph(sourceMap);
        var destGraph = NavSystem.GetGraph(destMap);

        if (sourceGraph == null || destGraph == null)
        {
            return false;
        }

        // The teleporter tile itself is ordinary ground in the static data.
        var sourceRegion = sourceGraph.Locate(source.X, source.Y, source.Z, 1);
        var destRegion = destGraph.Locate(dest.X, dest.Y, dest.Z, 2);

        if (sourceRegion < 0 || destRegion < 0)
        {
            return false;
        }

        _all.Add(new NavLink(kind, sourceMap.MapID, source, sourceRegion, destMap.MapID, dest, destRegion));
        return true;
    }

    public static void Clear()
    {
        _all.Clear();
        NavSystem.PublishSnapshot();
    }
}
