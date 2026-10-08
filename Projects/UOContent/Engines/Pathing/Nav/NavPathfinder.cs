using System;
using System.Diagnostics;

namespace Server.Engines.Pathing.Nav;

/// <summary>
/// Route finding over the nav graphs, from the loop's side. Start and goal are located on the loop
/// (that reads the step cache); the graph search itself runs on <see cref="NavRouteWorker"/> when
/// the host has the cores for it, and inline otherwise.
/// </summary>
public static class NavPathfinder
{
    private static readonly NavSearch _loopSearch = new();

    public static int MaxExpansions
    {
        get => _loopSearch.MaxExpansions;
        set
        {
            _loopSearch.MaxExpansions = value;
            NavRouteWorker.MaxExpansions = value;
        }
    }

    /// <summary>The link restrictions that apply to <paramref name="m"/> right now.</summary>
    public static NavAccess AccessOf(Mobile m) =>
        (m.Criminal ? NavAccess.Criminal : NavAccess.None) | (m.Player && m.Murderer ? NavAccess.Murderer : NavAccess.None);

    private enum Resolve
    {
        Failed,
        SameRegion,
        Search
    }

    private static Resolve TryResolve(Map startMap, Point3D start, Map goalMap, Point3D goal, out long startNode, out long goalNode)
    {
        startNode = goalNode = 0;

        var startGraph = NavSystem.GetGraph(startMap);
        var goalGraph = NavSystem.GetGraph(goalMap);
        if (startGraph == null || goalGraph == null)
        {
            return Resolve.Failed;
        }

        var startRegion = startGraph.Locate(start.X, start.Y, start.Z);
        var goalRegion = goalGraph.Locate(goal.X, goal.Y, goal.Z);
        if (startRegion < 0 || goalRegion < 0)
        {
            return Resolve.Failed;
        }

        startNode = NavLinks.NodeKey(startMap.MapID, startRegion);
        goalNode = NavLinks.NodeKey(goalMap.MapID, goalRegion);
        return startNode == goalNode ? Resolve.SameRegion : Resolve.Search;
    }

    private static NavRoute Direct(Map goalMap, Point3D start, Point3D goal) =>
        new([new NavWaypoint(goalMap, goal, NavWaypointKind.Goal)], NavMath.Octile(start.X, start.Y, goal.X, goal.Y), 0);

    /// <summary>Finds a route inline on the calling (loop) thread.</summary>
    public static NavRoute Find(Map startMap, Point3D start, Map goalMap, Point3D goal, NavAccess access = NavAccess.None)
    {
        var t0 = Stopwatch.GetTimestamp();

        switch (TryResolve(startMap, start, goalMap, goal, out var startNode, out var goalNode))
        {
            case Resolve.Failed:
                {
                    NavStats.RecordRoute(NavStats.ElapsedMs(t0), 0, false);
                    return null;
                }
            case Resolve.SameRegion:
                {
                    NavStats.RecordRoute(NavStats.ElapsedMs(t0), 0, true);
                    return Direct(goalMap, start, goal);
                }
        }

        var raw = _loopSearch.Run(NavSystem.Snapshot, startNode, goalNode, goal, access);
        var route = ToRoute(raw);
        NavStats.RecordRoute(NavStats.ElapsedMs(t0), raw?.Expanded ?? 0, route != null);
        return route;
    }

    /// <summary>
    /// Finds a route and hands it to <paramref name="onDone"/> on the loop — later, from the route
    /// worker, or right away when no search is needed or the worker is off or full. Null means no
    /// route. The caller must re-validate in the callback: time may have passed.
    /// </summary>
    public static void FindAsync(
        Map startMap, Point3D start, Map goalMap, Point3D goal, Action<NavRoute> onDone, NavAccess access = NavAccess.None
    )
    {
        var t0 = Stopwatch.GetTimestamp();
        var resolved = TryResolve(startMap, start, goalMap, goal, out var startNode, out var goalNode);

        if (resolved != Resolve.Search)
        {
            NavStats.RecordRoute(NavStats.ElapsedMs(t0), 0, resolved == Resolve.SameRegion);
            onDone(resolved == Resolve.SameRegion ? Direct(goalMap, start, goal) : null);
            return;
        }

        if (NavRouteWorker.TryEnqueue(NavSystem.Snapshot, startNode, goalNode, goal, access, onDone))
        {
            NavStats.RecordDispatch(NavStats.ElapsedMs(t0));
            return;
        }

        var raw = _loopSearch.Run(NavSystem.Snapshot, startNode, goalNode, goal, access);
        var route = ToRoute(raw);
        NavStats.RecordRoute(NavStats.ElapsedMs(t0), raw?.Expanded ?? 0, route != null);
        onDone(route);
    }

    /// <summary>Turns a search result into a route of maps. Loop only.</summary>
    internal static NavRoute ToRoute(NavSearchResult raw)
    {
        if (raw?.Waypoints == null)
        {
            return null;
        }

        var waypoints = new System.Collections.Generic.List<NavWaypoint>(raw.Waypoints.Count);
        foreach (var wp in raw.Waypoints)
        {
            var map = Map.Maps[wp.MapId];
            if (map == null)
            {
                return null;
            }

            waypoints.Add(new NavWaypoint(map, wp.Location, wp.Kind));
        }

        return new NavRoute(waypoints, raw.Cost, raw.Expanded);
    }
}
