using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.Engines.Pathing.Nav;

public enum NavLinkKind : byte
{
    Teleporter
}

/// <summary>A non-walking edge: stepping on <see cref="Source"/> puts the walker at
/// <see cref="Destination"/>, possibly on another map.</summary>
public sealed class NavLink
{
    public NavLink(NavLinkKind kind, Map sourceMap, Point3D source, int sourceRegion, Map destMap, Point3D dest, int destRegion)
    {
        Kind = kind;
        SourceMap = sourceMap;
        Source = source;
        SourceRegion = sourceRegion;
        DestMap = destMap;
        Destination = dest;
        DestRegion = destRegion;
    }

    public NavLinkKind Kind { get; }
    public Map SourceMap { get; }
    public Point3D Source { get; }
    public int SourceRegion { get; }
    public Map DestMap { get; }
    public Point3D Destination { get; }
    public int DestRegion { get; }

    /// <summary>Tenths of a tile. A teleport is instant; a token cost keeps routes from
    /// bouncing through teleporters for no reason.</summary>
    public int Cost => 20;
}

/// <summary>
/// The links layered on top of the static graphs. Teleporters are world items — placed by
/// decoration, moved and deleted by staff — so they are gathered from the live world instead of
/// being baked, and rebuilt on demand with [NavLinks.
/// </summary>
public static class NavLinks
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(NavLinks));

    private static readonly Dictionary<long, List<NavLink>> _bySourceNode = new();
    private static readonly List<NavLink> _all = [];

    public static IReadOnlyList<NavLink> All => _all;

    public static long NodeKey(int mapId, int region) => ((long)mapId << 32) | (uint)region;

    public static List<NavLink> GetFrom(long node) => _bySourceNode.GetValueOrDefault(node);

    public static void Rebuild()
    {
        _bySourceNode.Clear();
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

                if (!TryAdd(NavLinkKind.Teleporter, map, tele.Location, destMap, dest))
                {
                    skipped++;
                }
            }
        }

        logger.Information(
            "Nav links: {Count} teleporters linked, {Skipped} skipped (no walkable ground at an end)",
            _all.Count,
            skipped
        );
    }

    public static bool TryAdd(NavLinkKind kind, Map sourceMap, Point3D source, Map destMap, Point3D dest)
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

        var link = new NavLink(kind, sourceMap, source, sourceRegion, destMap, dest, destRegion);
        var key = NodeKey(sourceMap.MapID, sourceRegion);

        if (!_bySourceNode.TryGetValue(key, out var list))
        {
            _bySourceNode[key] = list = [];
        }

        list.Add(link);
        _all.Add(link);
        return true;
    }

    public static void Clear()
    {
        _bySourceNode.Clear();
        _all.Clear();
    }
}
