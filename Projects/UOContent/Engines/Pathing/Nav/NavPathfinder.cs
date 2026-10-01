using System.Collections.Generic;

namespace Server.Engines.Pathing.Nav;

/// <summary>
/// A* over the nav graphs of every map at once, walking edges and links (teleporters) alike. The
/// result is a <see cref="NavRoute"/> of portal cells; <see cref="NavFollower"/> turns it into
/// steps.
///
/// Runs on the game loop. Collections are reused across searches, and the expansion budget bounds
/// the cost of an unreachable goal.
/// </summary>
public static class NavPathfinder
{
    private struct Via
    {
        public long Prev;
        public int Edge;     // walking edge index on the previous node's map, or -1
        public NavLink Link; // set when the step was a link
    }

    public static int MaxExpansions { get; set; } = 200_000;

    private static readonly PriorityQueue<long, int> _open = new();
    private static readonly Dictionary<long, int> _g = new();
    private static readonly Dictionary<long, Via> _via = new();
    private static readonly HashSet<long> _closed = [];

    // Per-search: each link's source tile with a lower bound on the remaining cost once taken.
    private static readonly List<(Map map, Point3D source, int bound)> _exits = [];

    private static int MapIdOf(long node) => (int)(node >> 32);
    private static int RegionOf(long node) => (int)(node & 0xFFFFFFFF);

    public static NavRoute Find(Map startMap, Point3D start, Map goalMap, Point3D goal)
    {
        var startGraph = NavSystem.GetGraph(startMap);
        var goalGraph = NavSystem.GetGraph(goalMap);

        if (startGraph == null || goalGraph == null)
        {
            return null;
        }

        var startRegion = startGraph.Locate(start.X, start.Y, start.Z);
        var goalRegion = goalGraph.Locate(goal.X, goal.Y, goal.Z);

        if (startRegion < 0 || goalRegion < 0)
        {
            return null;
        }

        var startNode = NavLinks.NodeKey(startMap.MapID, startRegion);
        var goalNode = NavLinks.NodeKey(goalMap.MapID, goalRegion);

        if (startNode == goalNode)
        {
            return new NavRoute([new NavWaypoint(goalMap, goal, NavWaypointKind.Goal)], NavMath.Octile(start.X, start.Y, goal.X, goal.Y), 0);
        }

        // Links only shape the heuristic when the goal can't be walked to: otherwise the plain
        // distance stays the bound and teleporters are just more edges.
        var useLinkBounds = startMap != goalMap ||
                            NavSystem.GetIsland(startGraph, startRegion) != NavSystem.GetIsland(goalGraph, goalRegion);

        PrepareLinkBounds(useLinkBounds, goalMap, goal);

        _open.Clear();
        _g.Clear();
        _via.Clear();
        _closed.Clear();

        _g[startNode] = 0;
        _via[startNode] = new Via { Prev = -1, Edge = -1 };
        _open.Enqueue(startNode, Heuristic(startGraph, startRegion, startMap, goalMap, goal));

        var expanded = 0;
        var found = false;

        while (_open.TryDequeue(out var node, out _))
        {
            if (!_closed.Add(node))
            {
                continue;
            }

            if (node == goalNode)
            {
                found = true;
                break;
            }

            if (++expanded > MaxExpansions)
            {
                break;
            }

            var mapId = MapIdOf(node);
            var region = RegionOf(node);
            var map = Map.Maps[mapId];
            var graph = NavSystem.GetGraph(map);
            var g = _g[node];

            for (int e = graph.EdgeStart[region], end = graph.EdgeStart[region + 1]; e < end; e++)
            {
                var next = NavLinks.NodeKey(mapId, graph.EdgeTarget[e]);
                Relax(next, g + graph.EdgeCost[e], new Via { Prev = node, Edge = e }, goalMap, goal);
            }

            var links = NavLinks.GetFrom(node);
            if (links != null)
            {
                foreach (var link in links)
                {
                    // Getting from the region centre to the teleporter tile, then the jump.
                    var cost = NavMath.Octile(graph.RegionX[region], graph.RegionY[region], link.Source.X, link.Source.Y)
                               + link.Cost;
                    var next = NavLinks.NodeKey(link.DestMap.MapID, link.DestRegion);
                    Relax(next, g + cost, new Via { Prev = node, Edge = -1, Link = link }, goalMap, goal);
                }
            }
        }

        if (!found)
        {
            return null;
        }

        return BuildRoute(goalNode, goalMap, goal, expanded);
    }

    private static void Relax(long next, int g, Via via, Map goalMap, Point3D goal)
    {
        if (_closed.Contains(next))
        {
            return;
        }

        if (_g.TryGetValue(next, out var old) && old <= g)
        {
            return;
        }

        _g[next] = g;
        _via[next] = via;

        var map = Map.Maps[MapIdOf(next)];
        var graph = NavSystem.GetGraph(map);
        _open.Enqueue(next, g + Heuristic(graph, RegionOf(next), map, goalMap, goal));
    }

    private static int Heuristic(NavMapGraph graph, int region, Map map, Map goalMap, Point3D goal)
    {
        var x = graph.RegionX[region];
        var y = graph.RegionY[region];

        var h = map == goalMap ? NavMath.Octile(x, y, goal.X, goal.Y) : int.MaxValue;

        foreach (var (exitMap, source, bound) in _exits)
        {
            if (exitMap != map)
            {
                continue;
            }

            var viaExit = NavMath.Octile(x, y, source.X, source.Y) + bound;
            if (viaExit < h)
            {
                h = viaExit;
            }
        }

        // No known way out of a map the goal isn't on: leave the node to the budget.
        return h == int.MaxValue ? 0 : h;
    }

    /// <summary>
    /// Lower bounds on "take this link, then finish" for every link, by Dijkstra over the links
    /// with straight-line distances between them. With these the heuristic stays admissible when
    /// the only way to the goal is through teleporters — a dungeon whose map coordinates are
    /// nowhere near its entrance would otherwise drag the search across the whole surface.
    /// </summary>
    private static void PrepareLinkBounds(bool enabled, Map goalMap, Point3D goal)
    {
        _exits.Clear();

        if (!enabled)
        {
            return;
        }

        var links = NavLinks.All;
        var count = links.Count;
        if (count == 0)
        {
            return;
        }

        var dist = new int[count];
        var done = new bool[count];

        for (var i = 0; i < count; i++)
        {
            var link = links[i];
            dist[i] = link.DestMap == goalMap
                ? link.Cost + NavMath.Octile(link.Destination.X, link.Destination.Y, goal.X, goal.Y)
                : int.MaxValue;
        }

        // Dense Dijkstra: links number in the hundreds, so O(n^2) without a heap is fine and
        // allocation-light.
        for (var iter = 0; iter < count; iter++)
        {
            var best = -1;
            for (var i = 0; i < count; i++)
            {
                if (!done[i] && dist[i] != int.MaxValue && (best < 0 || dist[i] < dist[best]))
                {
                    best = i;
                }
            }

            if (best < 0)
            {
                break;
            }

            done[best] = true;
            var after = links[best];

            // Any link that lands on the map where 'after' starts can chain into it.
            for (var i = 0; i < count; i++)
            {
                if (done[i])
                {
                    continue;
                }

                var before = links[i];
                if (before.DestMap != after.SourceMap)
                {
                    continue;
                }

                var candidate = before.Cost +
                                NavMath.Octile(before.Destination.X, before.Destination.Y, after.Source.X, after.Source.Y) +
                                dist[best];
                if (candidate < dist[i])
                {
                    dist[i] = candidate;
                }
            }
        }

        for (var i = 0; i < count; i++)
        {
            if (dist[i] != int.MaxValue)
            {
                _exits.Add((links[i].SourceMap, links[i].Source, dist[i]));
            }
        }
    }

    private static NavRoute BuildRoute(long goalNode, Map goalMap, Point3D goal, int expanded)
    {
        var waypoints = new List<NavWaypoint>();
        waypoints.Add(new NavWaypoint(goalMap, goal, NavWaypointKind.Goal));

        var node = goalNode;
        while (true)
        {
            var via = _via[node];
            if (via.Prev < 0)
            {
                break;
            }

            if (via.Link != null)
            {
                waypoints.Add(new NavWaypoint(via.Link.SourceMap, via.Link.Source, NavWaypointKind.Teleport));
            }
            else
            {
                var map = Map.Maps[MapIdOf(via.Prev)];
                var graph = NavSystem.GetGraph(map);
                var e = via.Edge;
                waypoints.Add(
                    new NavWaypoint(
                        map,
                        new Point3D(graph.PortalToX[e], graph.PortalToY[e], graph.PortalToZ[e]),
                        NavWaypointKind.Portal
                    )
                );
            }

            node = via.Prev;
        }

        waypoints.Reverse();
        return new NavRoute(waypoints, _g[goalNode], expanded);
    }
}
