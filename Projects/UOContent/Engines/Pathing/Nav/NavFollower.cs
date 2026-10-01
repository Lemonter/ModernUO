using Server.Items;
using Server.PathAlgorithms;

namespace Server.Engines.Pathing.Nav;

public enum NavStepResult : byte
{
    /// <summary>Took a step.</summary>
    Moved,

    /// <summary>Did something other than step this tick — opened a door, waited on a teleporter
    /// or a blocked cell. Call again.</summary>
    Waiting,

    /// <summary>At the goal.</summary>
    Arrived,

    /// <summary>The route can't be followed from here. Find a new one.</summary>
    Failed
}

/// <summary>
/// Walks a mobile along a <see cref="NavRoute"/>, one step per <see cref="Step"/> call; the caller
/// owns the pacing. Each leg aims at the furthest waypoint still within easy reach of the local
/// A* and lets it solve the ground in between, so the follower never walks blind: when no leg can
/// be found it stops and reports, rather than stepping straight at the goal.
///
/// Doors, mobiles and other live obstacles are the local A*'s business; the follower opens closed
/// doors in its way and replans a leg when a step is refused.
/// </summary>
public sealed class NavFollower
{
    // Leg targets stay well inside the A* window (38 tiles, centred between the ends).
    private const int LegRange = 24;

    // A refused step or a failed leg search is retried this many times in a row before the route
    // is given up — enough to ride out a mobile standing in a corridor.
    private const int MaxConsecutiveFailures = 6;

    // A teleporter with a delay keeps the walker standing on it for a while.
    private const int MaxTeleportWaitTicks = 40;

    private readonly Mobile _mobile;
    private readonly int _arriveRange;

    private int _index;
    private Direction[] _leg;
    private int _legPos;
    private int _legTarget = -1;
    private Point3D _expected;
    private int _failures;
    private int _teleportWait;

    public NavFollower(Mobile mobile, NavRoute route, int arriveRange = 1)
    {
        _mobile = mobile;
        Route = route;
        _arriveRange = arriveRange;
    }

    public NavRoute Route { get; }

    /// <summary>Index of the next waypoint to reach.</summary>
    public int Index => _index;

    public NavStepResult Step(bool run)
    {
        var m = _mobile;
        var waypoints = Route.Waypoints;

        if (m.Deleted || m.Map == null || m.Map == Map.Internal)
        {
            return NavStepResult.Failed;
        }

        var goal = Route.Goal;
        if (m.Map == goal.Map && Utility.InRange(m.Location, goal.Location, _arriveRange) &&
            (m.Z - goal.Location.Z).Abs() < 16)
        {
            return NavStepResult.Arrived;
        }

        if (!AdvanceTeleport(out var waiting))
        {
            return NavStepResult.Failed;
        }

        if (waiting)
        {
            return NavStepResult.Waiting;
        }

        MarkPassedWaypoints();

        if (_index >= waypoints.Count)
        {
            return NavStepResult.Arrived;
        }

        if (_leg == null || _legPos >= _leg.Length || m.Location != _expected)
        {
            if (!PlanLeg())
            {
                return ++_failures >= MaxConsecutiveFailures ? NavStepResult.Failed : NavStepResult.Waiting;
            }
        }

        var dir = _leg[_legPos];
        var next = m.Location;
        Movement.Movement.Offset(dir, ref next);

        if (TryOpenDoor(m, next))
        {
            return NavStepResult.Waiting;
        }

        m.Direction = dir;
        var oldLocation = m.Location;
        var oldMap = m.Map;

        if (!m.Move(run ? dir | Direction.Running : dir) || m.Location == oldLocation && m.Map == oldMap)
        {
            _leg = null;
            return ++_failures >= MaxConsecutiveFailures ? NavStepResult.Failed : NavStepResult.Waiting;
        }

        _failures = 0;
        _legPos++;
        _expected = m.Location;

        // An instant teleporter fires inside Move: the walker is already at the far end. Skip past
        // the teleport waypoint so the next leg starts from there.
        if (m.Map != oldMap || !Utility.InRange(m.Location, oldLocation, 2))
        {
            for (var j = _index; j < waypoints.Count; j++)
            {
                if (waypoints[j].Kind == NavWaypointKind.Teleport)
                {
                    _index = j + 1;
                    break;
                }
            }

            _teleportWait = 0;
            _leg = null;
            return NavStepResult.Moved;
        }

        // Stepped onto a teleporter tile on purpose: wait for it to fire.
        if (_legTarget >= 0 && _legTarget < waypoints.Count && waypoints[_legTarget].Kind == NavWaypointKind.Teleport &&
            m.Location == waypoints[_legTarget].Location)
        {
            _index = _legTarget;
            _leg = null;
        }

        return NavStepResult.Moved;
    }

    /// <summary>
    /// Handles the walker standing at (or having just used) a teleporter waypoint. Returns false
    /// when it has waited too long; <paramref name="waiting"/> is true while the teleporter is
    /// still due to fire.
    /// </summary>
    private bool AdvanceTeleport(out bool waiting)
    {
        waiting = false;
        var waypoints = Route.Waypoints;

        if (_index >= waypoints.Count || waypoints[_index].Kind != NavWaypointKind.Teleport)
        {
            return true;
        }

        var tele = waypoints[_index];
        var m = _mobile;

        if (m.Map == tele.Map && m.Location == tele.Location)
        {
            waiting = true;
            return ++_teleportWait < MaxTeleportWaitTicks;
        }

        // Somewhere else than the tile: either the teleport fired, or we haven't reached the tile
        // yet. Having arrived near the next waypoint's map and region tells them apart.
        if (_index + 1 < waypoints.Count)
        {
            var after = waypoints[_index + 1];
            if (m.Map == after.Map && (m.Map != tele.Map || !Utility.InRange(m.Location, tele.Location, 2)))
            {
                _index++;
                _teleportWait = 0;
                _leg = null;
            }
        }

        return true;
    }

    /// <summary>Moves the index past every waypoint the walker is standing on or has stepped past
    /// on the current leg.</summary>
    private void MarkPassedWaypoints()
    {
        var waypoints = Route.Waypoints;
        var m = _mobile;

        // A finished leg means its target was reached.
        if (_leg != null && _legPos >= _leg.Length && _legTarget >= _index)
        {
            if (waypoints[_legTarget].Kind != NavWaypointKind.Teleport)
            {
                _index = _legTarget + 1;
            }

            _leg = null;
        }

        for (var j = _index; j < waypoints.Count && j < _index + 8; j++)
        {
            var wp = waypoints[j];
            if (wp.Kind == NavWaypointKind.Teleport)
            {
                break;
            }

            if (wp.Map == m.Map && wp.Location.X == m.X && wp.Location.Y == m.Y && (wp.Location.Z - m.Z).Abs() < 16)
            {
                _index = j + 1;
            }
        }
    }

    /// <summary>Finds a leg to the furthest reachable waypoint; false when none of them can be
    /// reached from here.</summary>
    private bool PlanLeg()
    {
        var waypoints = Route.Waypoints;
        var m = _mobile;
        var furthest = _index;

        for (var j = _index; j < waypoints.Count; j++)
        {
            var wp = waypoints[j];
            if (wp.Map != m.Map || !Utility.InRange(m.Location, wp.Location, LegRange))
            {
                break;
            }

            furthest = j;

            if (wp.Kind == NavWaypointKind.Teleport)
            {
                break; // the tile is the leg's end; what lies beyond is somewhere else
            }
        }

        for (var j = furthest; j >= _index; j--)
        {
            var target = waypoints[j];
            if (target.Map != m.Map)
            {
                continue;
            }

            var path = FindLeg(m, target.Location);
            if (path != null)
            {
                _leg = path;
                _legPos = 0;
                _legTarget = j;
                _expected = m.Location;
                return true;
            }
        }

        _leg = null;
        _legTarget = -1;
        return false;
    }

    private static Direction[] FindLeg(Mobile m, Point3D target)
    {
        var alg = BitmapAStarAlgorithm.Instance;
        if (!alg.CheckCondition(m, m.Map, m.Location, target))
        {
            return null;
        }

        var path = alg.Find(m, m.Map, m.Location, target);
        return path?.Length > 0 ? path : null;
    }

    /// <summary>Opens a closed door on the cell about to be entered. True when the tick went on
    /// opening it; a door that stays shut (locked) is left for the refused step to report.</summary>
    private static bool TryOpenDoor(Mobile m, Point3D cell)
    {
        foreach (var door in m.Map.GetItemsAt<BaseDoor>(cell))
        {
            if (!door.Open && (door.Z - m.Z).Abs() < 16)
            {
                door.Use(m);
                return door.Open;
            }
        }

        return false;
    }
}
