using System;
using System.IO;
using Server.Engines.Pathing.Nav;
using Xunit;

namespace Server.Tests.Pathfinding.Nav;

[Collection("Sequential Pathfinding Tests")]
public class NavGraphTests : IDisposable
{
    // Leg targets in NavFollower stay within this many tiles; a route whose consecutive waypoints
    // sit further apart would force the follower into a leg the local A* can't solve.
    private const int LegRange = 24;

    private static Map MapA => Map.Maps[0];
    private static Map MapB => Map.Maps[1];

    public void Dispose()
    {
        NavSystem.Uninstall(MapA);
        NavSystem.Uninstall(MapB);
        NavLinks.Clear();
    }

    private static NavMapGraph Install(Map map, GridNavCellSource source)
    {
        var graph = NavGraphBuilder.Build(map.MapID, source);
        NavSystem.Install(map, graph, source);
        return graph;
    }

    private static void AssertLegsFit(NavRoute route, Point3D start)
    {
        var prev = start;
        var prevMap = route.Waypoints[0].Map;
        var afterTeleport = false;

        foreach (var wp in route.Waypoints)
        {
            // The waypoint after a teleporter is measured from the teleporter's destination, which
            // the route doesn't carry.
            if (wp.Map == prevMap && !afterTeleport)
            {
                Assert.True(
                    Server.Utility.InRange(prev, wp.Location, LegRange),
                    $"Waypoint {wp.Location} is more than {LegRange} tiles from {prev}"
                );
            }

            prev = wp.Location;
            prevMap = wp.Map;
            afterTeleport = wp.Kind == NavWaypointKind.Teleport;
        }
    }

    [Fact]
    public void OpenField_RoutesWithShortLegs()
    {
        Install(MapA, new GridNavCellSource(96, 96).Fill(0, 0, 0, 96, 96, '.'));

        var start = new Point3D(2, 2, 0);
        var goal = new Point3D(92, 90, 0);
        var route = NavPathfinder.Find(MapA, start, MapA, goal);

        Assert.NotNull(route);
        Assert.Equal(NavWaypointKind.Goal, route.Goal.Kind);
        Assert.Equal(goal, route.Goal.Location);
        AssertLegsFit(route, start);
    }

    [Fact]
    public void WallWithFarOpening_RouteGoesThroughTheOpening()
    {
        // A wall down the middle with the only gap at the bottom: exactly what greedy waypoint
        // picking toward the goal could never get around.
        var source = new GridNavCellSource(128, 64)
            .Fill(0, 0, 0, 128, 64, '.')
            .Fill(0, 64, 0, 1, 60, '#');
        Install(MapA, source);

        var start = new Point3D(10, 5, 0);
        var goal = new Point3D(118, 5, 0);
        var route = NavPathfinder.Find(MapA, start, MapA, goal);

        Assert.NotNull(route);
        Assert.Contains(route.Waypoints, wp => wp.Location.Y >= 56);
        AssertLegsFit(route, start);
    }

    [Fact]
    public void FullWall_IsUnreachable()
    {
        var source = new GridNavCellSource(64, 32)
            .Fill(0, 0, 0, 64, 32, '.')
            .Fill(0, 32, 0, 1, 32, '#');
        Install(MapA, source);

        Assert.Null(NavPathfinder.Find(MapA, new Point3D(5, 5, 0), MapA, new Point3D(60, 5, 0)));
    }

    [Fact]
    public void WallInsideACluster_SplitsItIntoTwoRegions()
    {
        var source = new GridNavCellSource(16, 16)
            .Fill(0, 0, 0, 16, 16, '.')
            .Fill(0, 8, 0, 1, 16, '#');
        var graph = Install(MapA, source);

        Assert.Equal(2, graph.RegionCount);
        Assert.NotEqual(graph.Locate(2, 2, 0), graph.Locate(12, 2, 0));
    }

    [Fact]
    public void Bridge_StaysSeparateFromTheRoadUnderIt()
    {
        // Road east-west at Z 0, bridge north-south at Z 20 crossing over it.
        var source = new GridNavCellSource(48, 48)
            .Fill(0, 0, 24, 48, 1, '.')
            .Fill(20, 24, 0, 1, 48, '.');
        var graph = Install(MapA, source);

        Assert.NotEqual(graph.Locate(24, 24, 0), graph.Locate(24, 24, 20));

        // Along the road, under the bridge.
        Assert.NotNull(NavPathfinder.Find(MapA, new Point3D(0, 24, 0), MapA, new Point3D(47, 24, 0)));

        // From the road onto the bridge: no ramp, no way up.
        Assert.Null(NavPathfinder.Find(MapA, new Point3D(0, 24, 0), MapA, new Point3D(24, 2, 20)));
    }

    [Fact]
    public void Ramp_ConnectsLevels()
    {
        // Ground at Z 0 up to x 15, a ramp 2 Z per tile, an upper deck at Z 20 from x 26 on.
        var source = new GridNavCellSource(48, 16)
            .Fill(0, 0, 0, 16, 16, '.')
            .Layer(0, "                0123456789")
            .Fill(20, 26, 0, 22, 16, '.');
        Install(MapA, source);

        var route = NavPathfinder.Find(MapA, new Point3D(2, 8, 0), MapA, new Point3D(40, 8, 20));

        Assert.NotNull(route);
    }

    [Fact]
    public void Teleporter_LinksTwoIslands()
    {
        var source = new GridNavCellSource(64, 32)
            .Fill(0, 0, 0, 64, 32, '.')
            .Fill(0, 32, 0, 1, 32, '#');
        Install(MapA, source);

        Assert.True(NavLinks.TryAdd(NavLinkKind.Teleporter, MapA, new Point3D(10, 10, 0), MapA, new Point3D(50, 10, 0)));

        var start = new Point3D(2, 2, 0);
        var route = NavPathfinder.Find(MapA, start, MapA, new Point3D(60, 28, 0));

        Assert.NotNull(route);
        Assert.Contains(route.Waypoints, wp => wp.Kind == NavWaypointKind.Teleport && wp.Location == new Point3D(10, 10, 0));
        AssertLegsFit(route, start);
    }

    [Fact]
    public void Teleporter_CrossesMaps()
    {
        Install(MapA, new GridNavCellSource(32, 32).Fill(0, 0, 0, 32, 32, '.'));
        Install(MapB, new GridNavCellSource(32, 32).Fill(0, 0, 0, 32, 32, '.'));

        Assert.True(NavLinks.TryAdd(NavLinkKind.Teleporter, MapA, new Point3D(20, 20, 0), MapB, new Point3D(3, 3, 0)));

        var route = NavPathfinder.Find(MapA, new Point3D(1, 1, 0), MapB, new Point3D(28, 28, 0));

        Assert.NotNull(route);
        Assert.Equal(MapB, route.Goal.Map);
        Assert.Contains(route.Waypoints, wp => wp.Kind == NavWaypointKind.Teleport && wp.Map == MapA);
    }

    [Fact]
    public void Locate_FallsBackToNearbyGround()
    {
        var source = new GridNavCellSource(32, 32)
            .Fill(0, 0, 0, 32, 32, '.')
            .Fill(0, 10, 10, 3, 3, '#');
        var graph = Install(MapA, source);

        // (11, 11) is inside a solid block; the ground beside it is the answer.
        Assert.True(graph.Locate(11, 11, 0) >= 0);
        Assert.Equal(-1, graph.Locate(11, 11, 0, searchRadius: 0));
    }

    [Fact]
    public void File_RoundTrips()
    {
        var source = new GridNavCellSource(64, 48)
            .Fill(0, 0, 0, 64, 48, '.')
            .Fill(0, 20, 0, 1, 40, '#');
        var graph = NavGraphBuilder.Build(MapA.MapID, source);
        var path = Path.Combine(Path.GetTempPath(), $"nav_{Guid.NewGuid():N}.nav");

        try
        {
            NavGraphFile.Write(path, graph, 0x1234UL);

            Assert.Null(NavGraphFile.Read(path, MapA.MapID, 0x9999UL)); // stale fingerprint
            Assert.Null(NavGraphFile.Read(path, MapB.MapID, 0x1234UL)); // wrong map

            var read = NavGraphFile.Read(path, MapA.MapID, 0x1234UL);
            Assert.NotNull(read);
            Assert.Equal(graph.ClusterRegionStart, read.ClusterRegionStart);
            Assert.Equal(graph.RegionX, read.RegionX);
            Assert.Equal(graph.RegionY, read.RegionY);
            Assert.Equal(graph.RegionZ, read.RegionZ);
            Assert.Equal(graph.EdgeStart, read.EdgeStart);
            Assert.Equal(graph.EdgeTarget, read.EdgeTarget);
            Assert.Equal(graph.EdgeCost, read.EdgeCost);
            Assert.Equal(graph.PortalToX, read.PortalToX);
            Assert.Equal(graph.PortalToZ, read.PortalToZ);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FindAsync_DeliversTheSameRoute()
    {
        var source = new GridNavCellSource(128, 64)
            .Fill(0, 0, 0, 128, 64, '.')
            .Fill(0, 64, 0, 1, 60, '#');
        Install(MapA, source);

        var start = new Point3D(10, 5, 0);
        var goal = new Point3D(118, 5, 0);
        var sync = NavPathfinder.Find(MapA, start, MapA, goal);

        NavRoute async = null;
        var called = false;
        NavPathfinder.FindAsync(MapA, start, MapA, goal, r => { async = r; called = true; });

        // With the worker off (test host) the callback runs inline.
        Assert.True(called);
        Assert.Equal(sync.Waypoints, async.Waypoints);
    }

    [Fact]
    public void Search_OnAnotherThread_MatchesTheLoop()
    {
        var source = new GridNavCellSource(128, 64)
            .Fill(0, 0, 0, 128, 64, '.')
            .Fill(0, 64, 0, 1, 60, '#');
        var graph = Install(MapA, source);

        var startNode = NavLinks.NodeKey(MapA.MapID, graph.Locate(10, 5, 0));
        var goalNode = NavLinks.NodeKey(MapA.MapID, graph.Locate(118, 5, 0));
        var goal = new Point3D(118, 5, 0);
        var snapshot = NavSystem.Snapshot;

        var loop = new NavSearch().Run(snapshot, startNode, goalNode, goal);

        NavSearchResult offLoop = null;
        var thread = new System.Threading.Thread(() => offLoop = new NavSearch().Run(snapshot, startNode, goalNode, goal));
        thread.Start();
        thread.Join();

        Assert.NotNull(loop.Waypoints);
        Assert.Equal(loop.Waypoints, offLoop.Waypoints);
        Assert.Equal(loop.Cost, offLoop.Cost);
    }

    [Fact]
    public void Patch_AfterTerrainChange_MatchesAFreshBuild()
    {
        // A full wall; then a gap is dug through it, as a map edit would.
        var source = new GridNavCellSource(96, 64)
            .Fill(0, 0, 0, 96, 64, '.')
            .Fill(0, 40, 0, 1, 64, '#');
        var old = Install(MapA, source);
        Assert.Null(NavPathfinder.Find(MapA, new Point3D(5, 5, 0), MapA, new Point3D(90, 5, 0)));

        source.Fill(0, 40, 20, 1, 3, '.');

        // The clusters NavSystem.InvalidateArea marks for that change: the 3x3 tiles' cover plus margin.
        var cols = old.ClusterCols;
        int[] changed = [1 * cols + 2, 1 * cols + 3, 0 * cols + 2, 0 * cols + 3];
        var patched = NavGraphBuilder.Patch(old, source, changed);
        var fresh = NavGraphBuilder.Build(MapA.MapID, source);

        Assert.Equal(fresh.ClusterRegionStart, patched.ClusterRegionStart);
        Assert.Equal(fresh.RegionX, patched.RegionX);
        Assert.Equal(fresh.RegionY, patched.RegionY);
        Assert.Equal(fresh.EdgeStart, patched.EdgeStart);
        Assert.Equal(fresh.EdgeTarget, patched.EdgeTarget);
        Assert.Equal(fresh.EdgeCost, patched.EdgeCost);
        Assert.Equal(fresh.PortalFromX, patched.PortalFromX);
        Assert.Equal(fresh.PortalToY, patched.PortalToY);

        NavSystem.Install(MapA, patched, source);
        Assert.NotNull(NavPathfinder.Find(MapA, new Point3D(5, 5, 0), MapA, new Point3D(90, 5, 0)));
    }
}
