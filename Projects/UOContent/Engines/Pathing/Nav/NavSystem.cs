using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Server.Commands;
using Server.Engines.Pathing.Cache;
using Server.Items;
using Server.Logging;
using Server.Targeting;

namespace Server.Engines.Pathing.Nav;

/// <summary>
/// Owns the nav graph of every map: loads the baked files at startup, bakes the missing or stale
/// ones, and keeps the teleporter links current.
///
///   [NavBake [mapId|all] — rebuild and save a map's graph.
///   [NavLinks            — regather teleporter links from the world.
///   [NavRoute            — find a route from you to a target and show its waypoints.
///   [NavGo               — walk a targeted client-less mobile (a bot) to a target.
/// </summary>
public static class NavSystem
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(NavSystem));

    private const string BakeOnStartupSetting = "pathfinding.navBakeOnStartup";

    private static readonly NavMapGraph[] _graphs = new NavMapGraph[0x100];
    private static readonly int[][] _islands = new int[0x100][];

    private static string PathFor(int mapId) => Path.Combine(Core.BaseDirectory, "Data", "Pathfinding", $"{mapId}.nav");

    private static volatile NavSnapshot _snapshot = NavSnapshot.Empty;

    /// <summary>The frozen graphs and links route searches read; swapped whole on every change.</summary>
    public static NavSnapshot Snapshot => _snapshot;

    /// <summary>Rebuilds and publishes the snapshot. Loop only.</summary>
    public static void PublishSnapshot() => _snapshot = NavSnapshot.Build(_graphs, _islands, NavLinks.All);

    public static NavMapGraph GetGraph(Map map) => map == null || map == Map.Internal ? null : _graphs[map.MapID];

    /// <summary>
    /// Connected component of the graph treated as undirected. Two regions on different islands
    /// can't be walked between; the pathfinder then routes through links.
    /// </summary>
    public static int GetIsland(NavMapGraph graph, int region) => _islands[graph.MapId]?[region] ?? -1;

    public static void Configure()
    {
        CommandSystem.Register("NavBake", AccessLevel.Administrator, OnNavBake);
        CommandSystem.Register("NavLinks", AccessLevel.Administrator, OnNavLinks);
        CommandSystem.Register("NavRoute", AccessLevel.GameMaster, OnNavRoute);
        CommandSystem.Register("NavGo", AccessLevel.GameMaster, OnNavGo);
        CommandSystem.Register("NavStats", AccessLevel.GameMaster, OnNavStats);
    }

    /// <summary>
    /// Runs after the step cache has opened (or baked) its .swb files — they make the nav bake a
    /// file read — and, being Initialize, after the world loads, so the teleporters exist.
    /// </summary>
    [CallPriority(60)]
    public static void Initialize()
    {
        var bakeMissing = ServerConfiguration.GetOrUpdateSetting(BakeOnStartupSetting, true);

        foreach (var map in Map.AllMaps)
        {
            if (map == null || map == Map.Internal)
            {
                continue;
            }

            if (TryLoad(map))
            {
                continue;
            }

            if (bakeMissing)
            {
                Bake(map);
            }
        }

        NavLinks.Rebuild();

        // Map edits applied at world load: a graph read from its file predates them.
        _initialized = true;
        ApplyPatches();
    }

    // Clusters whose terrain changed since their graph was built, per map, waiting for the next
    // patch. Edits come in bursts (a dig, a demolition), so patches are batched.
    private static readonly Dictionary<int, HashSet<int>> _changedClusters = new();
    private static readonly TimeSpan PatchDelay = TimeSpan.FromSeconds(5);
    private static bool _initialized;
    private static bool _patchScheduled;

    /// <summary>
    /// The statics or land of an area changed at runtime (map edits): the step cache forgets it at
    /// once, and the nav graph is patched around it shortly after. Safe to call before startup is
    /// done — the patch then waits for the graphs.
    /// </summary>
    public static void InvalidateArea(Map map, int x, int y, int width, int height)
    {
        if (map == null || map == Map.Internal)
        {
            return;
        }

        StepCache.Instance.InvalidateArea(map, x, y, width, height);

        if (!_changedClusters.TryGetValue(map.MapID, out var clusters))
        {
            _changedClusters[map.MapID] = clusters = [];
        }

        // Same one-tile margin as the step cache: border cells of the neighbours changed too.
        var cols = (map.Width + NavClusterCells.Size - 1) >> NavClusterCells.Shift;
        var rows = (map.Height + NavClusterCells.Size - 1) >> NavClusterCells.Shift;
        var x0 = Math.Max(0, (x - 1) >> NavClusterCells.Shift);
        var y0 = Math.Max(0, (y - 1) >> NavClusterCells.Shift);
        var x1 = Math.Min(cols - 1, (x + width) >> NavClusterCells.Shift);
        var y1 = Math.Min(rows - 1, (y + height) >> NavClusterCells.Shift);

        for (var cy = y0; cy <= y1; cy++)
        {
            for (var cx = x0; cx <= x1; cx++)
            {
                clusters.Add(cy * cols + cx);
            }
        }

        if (_initialized && !_patchScheduled)
        {
            _patchScheduled = true;
            Timer.StartTimer(PatchDelay, ApplyPatches);
        }
    }

    private static void ApplyPatches()
    {
        _patchScheduled = false;

        foreach (var (mapId, clusters) in _changedClusters)
        {
            var map = Map.Maps[mapId];
            var old = _graphs[mapId];
            if (map == null || old == null || clusters.Count == 0)
            {
                continue;
            }

            var watch = Stopwatch.StartNew();
            var graph = NavGraphBuilder.Patch(old, new StepCacheNavCellSource(map), clusters);
            Install(map, graph);

            logger.Information(
                "Nav graph for map {MapId} patched around {Clusters} changed clusters in {Elapsed:F1} ms",
                mapId,
                clusters.Count,
                watch.Elapsed.TotalMilliseconds
            );
        }

        _changedClusters.Clear();
    }

    public static bool TryLoad(Map map)
    {
        var path = PathFor(map.MapID);
        var graph = NavGraphFile.Read(path, map.MapID, StepCacheFile.ComputeFingerprint(map.MapID));

        if (graph == null)
        {
            return false;
        }

        Install(map, graph);
        logger.Information(
            "Nav graph for map {MapId} loaded: {Regions} regions, {Edges} edges",
            map.MapID,
            graph.RegionCount,
            graph.EdgeCount
        );
        return true;
    }

    /// <summary>
    /// Builds a map's graph and saves it. Synchronous on the game loop, like [PathBake: a full-size
    /// facet takes a while, so this belongs to startup or maintenance.
    /// </summary>
    public static NavMapGraph Bake(Map map)
    {
        logger.Information("Nav bake: map {MapId} ({Width}x{Height}) — this can take a while...", map.MapID, map.Width, map.Height);

        var watch = Stopwatch.StartNew();
        var lastLog = 0L;

        var graph = NavGraphBuilder.Build(
            map.MapID,
            new StepCacheNavCellSource(map),
            (row, rows) =>
            {
                if (watch.ElapsedMilliseconds - lastLog >= 5000 || row == rows)
                {
                    lastLog = watch.ElapsedMilliseconds;
                    logger.Information("Nav bake: map {MapId} row {Row}/{Rows} ({Elapsed:F1}s)", map.MapID, row, rows, watch.Elapsed.TotalSeconds);
                }
            }
        );

        var path = PathFor(map.MapID);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        NavGraphFile.Write(path, graph, StepCacheFile.ComputeFingerprint(map.MapID));

        Install(map, graph);

        logger.Information(
            "Nav bake: map {MapId} done in {Elapsed:F1}s — {Regions} regions, {Edges} edges",
            map.MapID,
            watch.Elapsed.TotalSeconds,
            graph.RegionCount,
            graph.EdgeCount
        );

        return graph;
    }

    /// <summary>Installs a graph built elsewhere — tests use this with synthetic sources.</summary>
    public static void Install(Map map, NavMapGraph graph, INavCellSource source = null)
    {
        graph.AttachSource(source ?? new StepCacheNavCellSource(map));
        _graphs[map.MapID] = graph;
        _islands[map.MapID] = ComputeIslands(graph);
        PublishSnapshot();
    }

    public static void Uninstall(Map map)
    {
        _graphs[map.MapID] = null;
        _islands[map.MapID] = null;
        PublishSnapshot();
    }

    private static int[] ComputeIslands(NavMapGraph graph)
    {
        var count = graph.RegionCount;
        var parent = new int[count];
        for (var i = 0; i < count; i++)
        {
            parent[i] = i;
        }

        for (var region = 0; region < count; region++)
        {
            for (int e = graph.EdgeStart[region], end = graph.EdgeStart[region + 1]; e < end; e++)
            {
                var a = Find(parent, region);
                var b = Find(parent, graph.EdgeTarget[e]);
                if (a != b)
                {
                    parent[Math.Max(a, b)] = Math.Min(a, b);
                }
            }
        }

        for (var i = 0; i < count; i++)
        {
            parent[i] = Find(parent, i);
        }

        return parent;

        static int Find(int[] parent, int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }

            return i;
        }
    }

    [Usage("NavBake [mapId|all]")]
    [Description("Rebuilds and saves the nav graph of your map, a given map, or all maps.")]
    private static void OnNavBake(CommandEventArgs e)
    {
        var arg = e.Length > 0 ? e.GetString(0) : null;

        if (arg?.Equals("all", StringComparison.OrdinalIgnoreCase) == true)
        {
            foreach (var map in Map.AllMaps)
            {
                if (map != null && map != Map.Internal)
                {
                    Bake(map);
                }
            }
        }
        else
        {
            var map = arg == null ? e.Mobile.Map : Map.Maps[Math.Clamp(Utility.ToInt32(arg), 0, 0xFF)];
            if (map == null || map == Map.Internal)
            {
                e.Mobile.SendMessage("No such map.");
                return;
            }

            Bake(map);
        }

        NavLinks.Rebuild();
        e.Mobile.SendMessage("Nav bake complete.");
    }

    [Usage("NavLinks")]
    [Description("Regathers teleporter links for the nav graphs.")]
    private static void OnNavLinks(CommandEventArgs e)
    {
        NavLinks.Rebuild();
        e.Mobile.SendMessage($"{NavLinks.All.Count} nav links.");
    }

    [Usage("NavStats")]
    [Description("On-loop path planning cost since the last call: graph routes and local A* legs.")]
    private static void OnNavStats(CommandEventArgs e)
    {
        var m = e.Mobile;
        var routes = NavStats.Routes;
        var legs = NavStats.Legs;

        m.SendMessage(
            $"Routes: {routes} ({NavStats.RoutesFailed} failed), avg {(routes == 0 ? 0 : NavStats.RouteMsTotal / routes):F3} ms, max {NavStats.RouteMsMax:F2} ms, total {NavStats.RouteMsTotal:F0} ms, avg expanded {(routes == 0 ? 0 : NavStats.RouteExpanded / routes)}."
        );
        m.SendMessage(
            $"Legs: {legs} ({NavStats.LegsFailed} failed), avg {(legs == 0 ? 0 : NavStats.LegMsTotal / legs):F3} ms, max {NavStats.LegMsMax:F2} ms, total {NavStats.LegMsTotal:F0} ms."
        );
        var worker = NavStats.WorkerRoutes;
        m.SendMessage(
            $"Worker: {(NavRouteWorker.Enabled ? "on" : "off")}, {worker} routes, avg {(worker == 0 ? 0 : NavStats.WorkerMsTotal / worker):F3} ms off-loop, max {NavStats.WorkerMsMax:F2} ms, on-loop dispatch total {NavStats.DispatchMsTotal:F1} ms, pending {NavRouteWorker.Pending}."
        );
        m.SendMessage($"Cores: {Environment.ProcessorCount}. Stats reset.");
        NavStats.Reset();
    }

    [Usage("NavRoute")]
    [Description("Finds a nav route from you to a targeted location and marks its waypoints.")]
    private static void OnNavRoute(CommandEventArgs e)
    {
        e.Mobile.SendMessage("Target the destination.");
        e.Mobile.BeginTarget(
            -1,
            true,
            TargetFlags.None,
            (from, targeted) =>
            {
                if (targeted is not IPoint3D p)
                {
                    return;
                }

                var goal = new Point3D(p);
                var watch = Stopwatch.StartNew();
                var route = NavPathfinder.Find(from.Map, from.Location, from.Map, goal, NavPathfinder.AccessOf(from));
                watch.Stop();

                if (route == null)
                {
                    from.SendMessage($"No route ({watch.Elapsed.TotalMilliseconds:F2} ms).");
                    return;
                }

                from.SendMessage(
                    $"Route: {route.Waypoints.Count} waypoints, ~{route.Cost / 10} tiles, {route.Expanded} expanded, {watch.Elapsed.TotalMilliseconds:F2} ms."
                );

                foreach (var wp in route.Waypoints)
                {
                    if (wp.Map != from.Map)
                    {
                        continue;
                    }

                    var hue = wp.Kind switch
                    {
                        NavWaypointKind.Teleport => 0x22,
                        NavWaypointKind.Goal     => 0x44,
                        _                        => 0
                    };

                    Effects.SendLocationParticles(
                        EffectItem.Create(wp.Location, wp.Map, TimeSpan.FromSeconds(30)),
                        0x376A,
                        9,
                        600,
                        hue,
                        0,
                        5024,
                        0
                    );
                }
            }
        );
    }

    [Usage("NavGo")]
    [Description("Walks a targeted client-less mobile (e.g. a bot) to a targeted location along a nav route.")]
    private static void OnNavGo(CommandEventArgs e)
    {
        e.Mobile.SendMessage("Target the mobile to walk.");
        e.Mobile.BeginTarget(
            -1,
            false,
            TargetFlags.None,
            (from, targeted) =>
            {
                if (targeted is not Mobile walker || walker.NetState != null)
                {
                    from.SendMessage("Target a mobile without a client.");
                    return;
                }

                from.SendMessage("Target the destination.");
                from.BeginTarget(
                    -1,
                    true,
                    TargetFlags.None,
                    (_, dest) =>
                    {
                        if (dest is IPoint3D p)
                        {
                            NavWalkTimer.Start(from, walker, from.Map, new Point3D(p));
                        }
                    }
                );
            }
        );
    }

    /// <summary>Test harness behind [NavGo: runs a follower at running pace and replans on failure.</summary>
    private sealed class NavWalkTimer : Timer
    {
        private const int MaxReplans = 5;

        private readonly Mobile _reporter;
        private readonly Mobile _walker;
        private readonly Map _goalMap;
        private readonly Point3D _goal;
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private NavFollower _follower;
        private int _replans;
        private int _steps;

        private NavWalkTimer(Mobile reporter, Mobile walker, Map goalMap, Point3D goal)
            : base(TimeSpan.Zero, TimeSpan.FromMilliseconds(200))
        {
            _reporter = reporter;
            _walker = walker;
            _goalMap = goalMap;
            _goal = goal;
        }

        public static void Start(Mobile reporter, Mobile walker, Map goalMap, Point3D goal)
        {
            var timer = new NavWalkTimer(reporter, walker, goalMap, goal);
            if (!timer.Replan())
            {
                reporter.SendMessage("No route.");
                return;
            }

            timer.Start();
        }

        private bool Replan()
        {
            var route = NavPathfinder.Find(_walker.Map, _walker.Location, _goalMap, _goal, NavPathfinder.AccessOf(_walker));
            if (route == null)
            {
                return false;
            }

            _follower = new NavFollower(_walker, route);
            return true;
        }

        protected override void OnTick()
        {
            if (_walker.Deleted)
            {
                Stop();
                return;
            }

            switch (_follower.Step(true))
            {
                case NavStepResult.Moved:
                    {
                        _steps++;
                        break;
                    }
                case NavStepResult.Arrived:
                    {
                        _reporter.SendMessage($"{_walker.Name} arrived: {_steps} steps, {_replans} replans, {_watch.Elapsed.TotalSeconds:F0}s.");
                        Stop();
                        break;
                    }
                case NavStepResult.Failed:
                    {
                        if (++_replans > MaxReplans || !Replan())
                        {
                            _reporter.SendMessage($"{_walker.Name} gave up at {_walker.Location} after {_steps} steps.");
                            Stop();
                        }

                        break;
                    }
            }
        }
    }
}
