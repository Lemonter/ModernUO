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

    private static bool IsJump(NavWaypointKind kind) => kind is NavWaypointKind.Teleport or NavWaypointKind.Moongate;

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
            var planStart = Systems.Bots.BotProfile.Start();
            var planned = PlanLeg();
            Systems.Bots.BotProfile.Stop("путь: локальный поиск", planStart);
            if (!planned)
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

        var moveStart = Systems.Bots.BotProfile.Start();
        var moved = m.Move(run ? dir | Direction.Running : dir);
        Systems.Bots.BotProfile.Stop(moved ? "путь: шаг" : "путь: шаг не удался", moveStart);
        if (!moved)
        {
            Systems.Bots.BotProfile.Stop("отказ шага", WhyRefused(m, dir, next), Systems.Bots.BotProfile.Start());
        }
        if (!moved || m.Location == oldLocation && m.Map == oldMap)
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
                if (IsJump(waypoints[j].Kind))
                {
                    _index = j + 1;
                    break;
                }
            }

            _teleportWait = 0;
            _leg = null;
            return NavStepResult.Moved;
        }

        // Reached a teleporter tile or a moongate on purpose: wait for it to fire, or use it.
        if (_legTarget >= 0 && _legTarget < waypoints.Count && ReachedJump(m, waypoints[_legTarget]))
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

        if (_index >= waypoints.Count || !IsJump(waypoints[_index].Kind))
        {
            return true;
        }

        var tele = waypoints[_index];
        var m = _mobile;

        // A moongate is used, not stepped on: within a tile of it, pick the destination the
        // route goes on from. A refusal (combat, a criminal flag) is waited out like a delay.
        if (tele.Kind == NavWaypointKind.Moongate && m.Map == tele.Map && Utility.InRange(m.Location, tele.Location, 1) &&
            _index + 1 < waypoints.Count)
        {
            var after = waypoints[_index + 1];
            if (FindMoongate(tele) is { } gate && PublicMoongate.TryTravel(m, gate, after.Map, after.Location))
            {
                _index++;
                _teleportWait = 0;
                _leg = null;
                return true;
            }

            waiting = true;
            return ++_teleportWait < MaxTeleportWaitTicks;
        }

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
            if (!IsJump(waypoints[_legTarget].Kind))
            {
                _index = _legTarget + 1;
            }

            _leg = null;
        }

        for (var j = _index; j < waypoints.Count && j < _index + 8; j++)
        {
            var wp = waypoints[j];
            if (IsJump(wp.Kind))
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

            if (IsJump(wp.Kind))
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

    private static bool ReachedJump(Mobile m, NavWaypoint wp) =>
        wp.Kind switch
        {
            NavWaypointKind.Teleport => m.Location == wp.Location,
            NavWaypointKind.Moongate => m.Map == wp.Map && Utility.InRange(m.Location, wp.Location, 1),
            _                        => false
        };

    private static PublicMoongate FindMoongate(NavWaypoint wp)
    {
        foreach (var gate in wp.Map.GetItemsAt<PublicMoongate>(wp.Location))
        {
            return gate;
        }

        return null;
    }

    private static Direction[] FindLeg(Mobile m, Point3D target)
    {
        // Большинство отрезков — прямая по открытому месту: проверить её по шагам куда дешевле
        // поиска, а поиск нужен, только если прямая во что-то упёрлась.
        if (StraightLeg(m, target) is { } line)
        {
            NavStats.RecordStraightLeg();
            return line;
        }

        var alg = BitmapAStarAlgorithm.Instance;
        if (!alg.CheckCondition(m, m.Map, m.Location, target))
        {
            return null;
        }

        var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        var path = alg.Find(m, m.Map, m.Location, target);
        var found = path?.Length > 0;
        NavStats.RecordLeg(NavStats.ElapsedMs(t0), found);
        return found ? path : null;
    }

    // Диагностика: почему движок не дал шагнуть.
    private static string WhyRefused(Mobile m, Direction dir, Point3D next)
    {
        var who = m is Server.Mobiles.BotMobile ? "бот" : "не бот";
        if (m.Spell != null)
        {
            return who + ", колдует";
        }

        if (m.Paralyzed || m.Frozen)
        {
            return who + ", скован";
        }

        if (!Movement.Movement.CheckMovement(m, m.Map, m.Location, dir, out _))
        {
            return who + ", " + TerrainWhy(m, dir, next);
        }

        foreach (var other in m.Map.GetMobilesAt(next))
        {
            if (other != m && (other.Z - m.Z).Abs() < 16)
            {
                var kind = other switch
                {
                    Server.Mobiles.BotMobile => "бот",
                    Server.Mobiles.PlayerMobile => "игрок",
                    Server.Mobiles.BaseVendor => "торговец",
                    Server.Mobiles.BaseCreature { Controlled: true } => "питомец",
                    Server.Mobiles.BaseCreature bc when Systems.Bots.BotPassage.IsPeaceful(bc) => "мирное существо",
                    Server.Mobiles.BaseCreature bc => $"существо {bc.GetType().Name}",
                    _ => other.GetType().Name
                };
                return $"{who}, на клетке {kind}";
            }
        }

        return who + ", прочее (регион, предмет на клетке)";
    }

    private static string TerrainWhy(Mobile m, Direction dir, Point3D next)
    {
        var map = m.Map;
        string why;

        if (map.GetItemsAt<BaseDoor>(next) is var doors && HasAny(doors))
        {
            why = "дверь";
        }
        else if (((int)dir & 1) == 1 && DiagonalCorner(m, dir))
        {
            why = "срезание угла по диагонали";
        }
        else if (IsWet(map, next))
        {
            why = "вода";
        }
        else
        {
            why = "стена/высота";
        }

        return "рельеф: " + why;
    }

    private static bool HasAny(Map.ItemAtEnumerable<BaseDoor> doors)
    {
        foreach (var _ in doors)
        {
            return true;
        }

        return false;
    }

    // Диагональ: в одну из боковых клеток шагнуть можно, а наискосок — нет.
    private static bool DiagonalCorner(Mobile m, Direction dir)
    {
        var left = (Direction)(((int)dir - 1) & 7);
        var right = (Direction)(((int)dir + 1) & 7);
        return Movement.Movement.CheckMovement(m, m.Map, m.Location, left, out _) ||
               Movement.Movement.CheckMovement(m, m.Map, m.Location, right, out _);
    }

    private static bool IsWet(Map map, Point3D p)
    {
        var land = map.Tiles.GetLandTile(p.X, p.Y);
        if ((TileData.LandTable[land.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0)
        {
            return true;
        }

        foreach (var tile in map.Tiles.GetStaticTiles(p.X, p.Y))
        {
            if ((TileData.ItemTable[tile.ID & TileData.MaxItemValue].Flags & TileFlag.Wet) != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Шаги по прямой до точки, если каждый шаг проходим; иначе null.</summary>
    private static Direction[] StraightLeg(Mobile m, Point3D target)
    {
        var map = m.Map;
        var loc = m.Location;
        var steps = new Direction[LegRange * 2];
        var count = 0;

        while (loc.X != target.X || loc.Y != target.Y)
        {
            if (count >= steps.Length)
            {
                return null;
            }

            var dir = Utility.GetDirection(loc, target) & Direction.Mask;
            if (!Movement.Movement.CheckMovement(m, map, loc, dir, out var z))
            {
                return null;
            }

            int x = loc.X, y = loc.Y;
            Movement.Movement.Offset(dir, ref x, ref y);
            loc = new Point3D(x, y, z);
            steps[count++] = dir;
        }

        if (count == 0)
        {
            return null;
        }

        System.Array.Resize(ref steps, count);
        return steps;
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
