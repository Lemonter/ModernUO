using System.Collections.Generic;

namespace Server.Engines.Pathing.Nav;

public enum NavWaypointKind : byte
{
    /// <summary>A portal cell: the first cell of the next region along the route.</summary>
    Portal,

    /// <summary>A teleporter tile. Stepping onto it moves the walker to the next waypoint's map
    /// and location.</summary>
    Teleport,

    /// <summary>The destination.</summary>
    Goal,

    /// <summary>A public moongate. Used from within a tile of it, it sends the walker to the next
    /// waypoint's map and location.</summary>
    Moongate
}

public readonly record struct NavWaypoint(Map Map, Point3D Location, NavWaypointKind Kind);

/// <summary>A route found over the nav graph: the cells a walker must pass, in order.</summary>
public sealed class NavRoute
{
    public NavRoute(List<NavWaypoint> waypoints, int cost, int expanded)
    {
        Waypoints = waypoints;
        Cost = cost;
        Expanded = expanded;
    }

    public List<NavWaypoint> Waypoints { get; }

    /// <summary>Estimated length in tenths of a tile.</summary>
    public int Cost { get; }

    /// <summary>Graph nodes the search expanded — a cost measure for [NavRoute and [NavStats.</summary>
    public int Expanded { get; }

    public NavWaypoint Goal => Waypoints[^1];
}
