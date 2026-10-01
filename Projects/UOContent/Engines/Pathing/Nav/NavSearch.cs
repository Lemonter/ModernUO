using System.Collections.Generic;

namespace Server.Engines.Pathing.Nav;

/// <summary>A route as the search produces it: map ids, not maps, so it can be built off the loop.</summary>
public readonly record struct NavRawWaypoint(int MapId, Point3D Location, NavWaypointKind Kind);

public sealed class NavSearchResult
{
    public NavSearchResult(List<NavRawWaypoint> waypoints, int cost, int expanded)
    {
        Waypoints = waypoints;
        Cost = cost;
        Expanded = expanded;
    }

    public List<NavRawWaypoint> Waypoints { get; }
    public int Cost { get; }
    public int Expanded { get; }
}

/// <summary>
/// A* over the region graphs of every map at once, edges and links alike, reading nothing but a
/// <see cref="NavSnapshot"/>. Each instance owns its collections, so the loop and the route worker
/// each keep one and never share. Start and goal regions are resolved by the caller on the loop.
/// </summary>
public sealed class NavSearch
{
    private struct Via
    {
        public long Prev;
        public int Edge;     // walking edge index on the previous node's map, or -1
        public NavLink Link; // set when the step was a link
    }

    private readonly PriorityQueue<long, int> _open = new();
    private readonly Dictionary<long, int> _g = new();
    private readonly Dictionary<long, Via> _via = new();
    private readonly HashSet<long> _closed = [];

    // Per search: each link's source tile with a lower bound on the remaining cost once taken.
    private readonly List<(int mapId, Point3D source, int bound)> _exits = [];

    private int[] _dist = new int[16];
    private bool[] _done = new bool[16];

    public int MaxExpansions { get; set; } = 200_000;

    private static int MapIdOf(long node) => (int)(node >> 32);
    private static int RegionOf(long node) => (int)(node & 0xFFFFFFFF);

    public NavSearchResult Run(NavSnapshot snap, long startNode, long goalNode, Point3D goal)
    {
        var goalMapId = MapIdOf(goalNode);
        var startGraph = snap.Graphs[MapIdOf(startNode)];
        var goalGraph = snap.Graphs[goalMapId];

        if (startGraph == null || goalGraph == null)
        {
            return null;
        }

        // Links only shape the heuristic when the goal can't be walked to: otherwise the plain
        // distance stays the bound and teleporters are just more edges.
        var useLinkBounds = MapIdOf(startNode) != goalMapId ||
                            Island(snap, startNode) != Island(snap, goalNode);

        PrepareLinkBounds(snap, useLinkBounds, goalMapId, goal);

        _open.Clear();
        _g.Clear();
        _via.Clear();
        _closed.Clear();

        _g[startNode] = 0;
        _via[startNode] = new Via { Prev = -1, Edge = -1 };
        _open.Enqueue(startNode, Heuristic(snap, startNode, goalMapId, goal));

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
            var graph = snap.Graphs[mapId];
            var g = _g[node];

            for (int e = graph.EdgeStart[region], end = graph.EdgeStart[region + 1]; e < end; e++)
            {
                Relax(snap, NavLinks.NodeKey(mapId, graph.EdgeTarget[e]), g + graph.EdgeCost[e], new Via { Prev = node, Edge = e }, goalMapId, goal);
            }

            if (snap.LinksFrom.TryGetValue(node, out var links))
            {
                foreach (var link in links)
                {
                    // Getting from the region centre to the teleporter tile, then the jump.
                    var cost = NavMath.Octile(graph.RegionX[region], graph.RegionY[region], link.Source.X, link.Source.Y) + link.Cost;
                    Relax(snap, NavLinks.NodeKey(link.DestMapId, link.DestRegion), g + cost, new Via { Prev = node, Edge = -1, Link = link }, goalMapId, goal);
                }
            }
        }

        return found ? Build(snap, goalNode, goal, expanded) : new NavSearchResult(null, 0, expanded);
    }

    private static int Island(NavSnapshot snap, long node) => snap.Islands[MapIdOf(node)]?[RegionOf(node)] ?? -1;

    private void Relax(NavSnapshot snap, long next, int g, Via via, int goalMapId, Point3D goal)
    {
        if (_closed.Contains(next) || _g.TryGetValue(next, out var old) && old <= g)
        {
            return;
        }

        _g[next] = g;
        _via[next] = via;
        _open.Enqueue(next, g + Heuristic(snap, next, goalMapId, goal));
    }

    private int Heuristic(NavSnapshot snap, long node, int goalMapId, Point3D goal)
    {
        var mapId = MapIdOf(node);
        var graph = snap.Graphs[mapId];
        var region = RegionOf(node);
        int x = graph.RegionX[region];
        int y = graph.RegionY[region];

        var h = mapId == goalMapId ? NavMath.Octile(x, y, goal.X, goal.Y) : int.MaxValue;

        foreach (var (exitMap, source, bound) in _exits)
        {
            if (exitMap == mapId)
            {
                var viaExit = NavMath.Octile(x, y, source.X, source.Y) + bound;
                if (viaExit < h)
                {
                    h = viaExit;
                }
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
    private void PrepareLinkBounds(NavSnapshot snap, bool enabled, int goalMapId, Point3D goal)
    {
        _exits.Clear();

        var links = snap.Links;
        var count = links.Length;
        if (!enabled || count == 0)
        {
            return;
        }

        if (_dist.Length < count)
        {
            _dist = new int[count];
            _done = new bool[count];
        }

        var dist = _dist;
        var done = _done;

        for (var i = 0; i < count; i++)
        {
            var link = links[i];
            done[i] = false;
            dist[i] = link.DestMapId == goalMapId
                ? link.Cost + NavMath.Octile(link.Destination.X, link.Destination.Y, goal.X, goal.Y)
                : int.MaxValue;
        }

        // Dense Dijkstra: links number in the hundreds, so O(n^2) without a heap is fine.
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

            for (var i = 0; i < count; i++)
            {
                var before = links[i];
                if (done[i] || before.DestMapId != after.SourceMapId)
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
                _exits.Add((links[i].SourceMapId, links[i].Source, dist[i]));
            }
        }
    }

    private NavSearchResult Build(NavSnapshot snap, long goalNode, Point3D goal, int expanded)
    {
        var waypoints = new List<NavRawWaypoint> { new(MapIdOf(goalNode), goal, NavWaypointKind.Goal) };

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
                waypoints.Add(new NavRawWaypoint(via.Link.SourceMapId, via.Link.Source, NavWaypointKind.Teleport));
            }
            else
            {
                var mapId = MapIdOf(via.Prev);
                var graph = snap.Graphs[mapId];
                var e = via.Edge;
                waypoints.Add(
                    new NavRawWaypoint(mapId, new Point3D(graph.PortalToX[e], graph.PortalToY[e], graph.PortalToZ[e]), NavWaypointKind.Portal)
                );
            }

            node = via.Prev;
        }

        waypoints.Reverse();
        return new NavSearchResult(waypoints, _g[goalNode], expanded);
    }
}
