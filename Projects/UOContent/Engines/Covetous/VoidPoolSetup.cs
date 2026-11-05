using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.VoidPool;

/// <summary>
///     Ported from ServUO's Covetous void pool (Generate.cs) — the one-time in-game setup.
///
///     `[SetupCovetous` places the two controllers (Trammel/Felucca), the pool walls around the
///     defence point, the Cora and Vela spawners, and both enemy approach paths. `[DeleteCovetous`
///     takes it all back down through WeakEntityCollection's "covetous" tag.
///
///     The original's ConvertSpawners() is dropped: it rewrote spawn tables in already-existing
///     ServUO spawn files (gazer → StrangeGazer and friends) as a data migration for shards that had
///     the dungeon from an older version. A fresh setup here has no such files to rewrite — the
///     dungeon population comes from the normal spawner system.
///
///     Setup rolls itself back on any failure (the Despise port's lesson: a half-built dungeon plus a
///     populated Instance makes the next run answer "already set up" and finish nothing).
/// </summary>
public static class VoidPoolSetup
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(VoidPoolSetup));

    private static readonly (int X, int Y)[] PathA =
    {
        (5590, 2024), (5578, 2029), (5566, 2027), (5555, 2021), (5545, 2015),
        (5537, 2020), (5527, 2015), (5509, 2005), (5500, 1998)
    };

    private static readonly (int X, int Y)[] PathB =
    {
        (5469, 2016), (5478, 2025), (5484, 2029), (5490, 2027), (5504, 2027),
        (5516, 2020), (5524, 2012), (5513, 2005), (5502, 2004), (5500, 1998)
    };

    public static void Initialize()
    {
        EventSink.ServerStarted += OnServerStarted;
    }

    /// <summary>The pool is part of the world, not a command: after every world load the setup is
    /// verified, and whatever is missing is placed once. Controllers persist, so on a normal boot this
    /// finds both instances and does nothing.</summary>
    private static void OnServerStarted() => Timer.DelayCall(TimeSpan.FromSeconds(5), EnsureSetup);

    private static void EnsureSetup()
    {
        if (VoidPoolController.InstanceTram != null && VoidPoolController.InstanceFel != null)
        {
            return;
        }

        try
        {
            SetupCore();
            logger.Information("Covetous void pool is set up on Trammel and Felucca.");
        }
        catch (Exception ex)
        {
            WeakEntityCollection.Delete("covetous");
            VoidPoolController.InstanceTram = null;
            VoidPoolController.InstanceFel = null;

            logger.Error(ex, "Covetous void pool setup failed and was rolled back");
        }
    }

    private static void SetupCore()
    {
        CreateController(Map.Trammel);
        CreateController(Map.Felucca);

        CreatePoolWalls(Map.Trammel);
        CreatePoolWalls(Map.Felucca);

        // Кора и Вела ставятся напрямую: XmlSpawner("имя") ждёт имя типа, а не таблицы спавна,
        // поэтому прежние строки не создавали никого.
        CreateNpc(() => new CoraTheSorceress(), new Point3D(5457, 1808, 0));
        CreateNpc(() => new VelaTheSorceress(), new Point3D(2254, 1207, 0));

        AddWaypoints();
    }

    private static void CreateController(Map map)
    {
        var controller = new VoidPoolController(map);
        WeakEntityCollection.Add("covetous", controller);
        controller.MoveToWorld(new Point3D(5605, 1998, 10), map);
    }

    private static void CreatePoolWalls(Map map)
    {
        for (var x = 5497; x <= 5503; x++)
        {
            for (var y = 1995; y <= 2001; y++)
            {
                int id;

                if (x == 5497 && y == 1995)
                {
                    id = 1886;
                }
                else if (x == 5497 && y == 2001)
                {
                    id = 1887;
                }
                else if (x == 5503 && y == 1995)
                {
                    id = 1888;
                }
                else if (x == 5503 && y == 2001)
                {
                    id = 1885;
                }
                else if (x == 5497)
                {
                    id = 1874;
                }
                else if (x == 5503)
                {
                    id = 1876;
                }
                else if (y == 1995)
                {
                    id = 1873;
                }
                else if (y == 2001)
                {
                    id = 1875;
                }
                else
                {
                    // The pool floor is a random tile out of a small family of water-ish statics.
                    id = Utility.Random(8511, 6);
                }

                var item = new Static(id)
                {
                    Name = "Бассейн Пустоты",
                    Hue = id >= 8511 ? 0 : 1954
                };

                WeakEntityCollection.Add("covetous", item);
                item.MoveToWorld(new Point3D(x, y, 5), map);
            }
        }
    }

    private static void CreateNpc(Func<BaseCreature> factory, Point3D location)
    {
        foreach (var map in new[] { Map.Trammel, Map.Felucca })
        {
            var creature = factory();
            WeakEntityCollection.Add("covetous", creature);
            creature.MoveToWorld(location, map);
        }
    }

    public static void AddWaypoints()
    {
        var tram = VoidPoolController.InstanceTram;
        var fel = VoidPoolController.InstanceFel;

        if (tram == null || fel == null)
        {
            return;
        }

        ResetPath(tram.WaypointsA, Map.Trammel, PathA);
        ResetPath(tram.WaypointsB, Map.Trammel, PathB);
        ResetPath(fel.WaypointsA, Map.Felucca, PathA);
        ResetPath(fel.WaypointsB, Map.Felucca, PathB);
    }

    private static void ResetPath(List<WayPoint> waypoints, Map map, (int X, int Y)[] path)
    {
        foreach (var wayPoint in waypoints)
        {
            if (wayPoint is { Deleted: false })
            {
                wayPoint.Delete();
            }
        }

        waypoints.Clear();

        for (var i = 0; i < path.Length; i++)
        {
            var (x, y) = path[i];

            var wayPoint = new WayPoint();
            WeakEntityCollection.Add("covetous", wayPoint);
            wayPoint.MoveToWorld(new Point3D(x, y, 0), map);

            waypoints.Add(wayPoint);
        }

        for (var i = 0; i < waypoints.Count - 1; i++)
        {
            waypoints[i].NextPoint = waypoints[i + 1];
        }
    }
}
