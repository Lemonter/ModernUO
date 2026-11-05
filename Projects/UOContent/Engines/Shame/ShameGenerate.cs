using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.ShameRevamped;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (Generate.cs) — `[GenerateNewShame` builds the revamped
///     dungeon on both facets: three guardian altars, three troll-blocked walls with their pass-through
///     teleporters, and the spawn file. `[DeleteShame` takes it back down and re-enables the stock
///     spawners.
///
///     The spawn file ships as Distribution/XmlSpawner/ShameRevamped.xml (copied from ServUO's
///     RevampedSpawns folder), so the load command is a plain `[XmlLoad ShameRevamped.xml` here instead
///     of ServUO's RevampedSpawns/ path. ServUO's `Core.ML` guard is kept: the dungeon's creatures are
///     Mondain's Legacy era.
/// </summary>
public static class ShameGenerator
{
    public static void Initialize()
    {
        EventSink.ServerStarted += OnServerStarted;
    }

    /// <summary>Shame is part of the world: after every load the dungeon is checked and completed if any
    /// part is missing. The altars persist, so a normal boot finds them and does nothing.</summary>
    private static void OnServerStarted() => Timer.DelayCall(TimeSpan.FromSeconds(6), EnsureSetup);

    private static void EnsureSetup()
    {
        if (!Core.ML || CheckForAltar(new Point3D(5403, 43, 30), Map.Trammel))
        {
            return;
        }

        try
        {
            Generate(null);
            _logger.Information("Shame Revamped is set up.");
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex, "Shame Revamped setup failed");
        }
    }

    private static readonly Server.Logging.ILogger _logger =
        Server.Logging.LogFactory.GetLogger(typeof(ShameGenerator));

    public static void Generate(CommandEventArgs e)
    {
        if (!Core.ML)
        {
            return;
        }

        RemoveItems();

        XmlSpawner.XmlLoadDefaults("ShameRevamped.xml", e?.Mobile);

        // Level 1
        CreateAltar(
            typeof(QuartzElemental),
            new Point3D(5403, 43, 30),
            new Point3D(5490, 19, -25),
            new Point3D(5514, 10, 5),
            new Point3D(5387, 11, 30),
            10
        );

        // Level 2
        CreateAltar(
            typeof(FlameElemental),
            new Point3D(5577, 54, 2),
            new Point3D(5604, 102, 5),
            new Point3D(5514, 147, 25),
            new Point3D(5571, 115, 3),
            20
        );

        // Level 3
        CreateAltar(
            typeof(WindElemental),
            new Point3D(5390, 145, 20),
            new Point3D(5538, 170, 5),
            new Point3D(5513, 176, 5),
            new Point3D(5618, 223, 0),
            30
        );

        // Wall 1
        CreateWall(
            new Point3D(5403, 82, 10),
            new Dictionary<Point3D, int>
            {
                [new Point3D(-1, 0, 0)] = 2272,
                [new Point3D(0, 0, 0)] = 2272,
                [new Point3D(1, 0, 0)] = 2272,
                [new Point3D(2, 0, 0)] = 2272
            },
            new Point3D(5405, 90, 10)
        );

        // Wall 2
        CreateWall(
            new Point3D(5465, 26, -10),
            new Dictionary<Point3D, int>
            {
                [new Point3D(0, -1, 0)] = 2272,
                [new Point3D(0, 0, 0)] = 2272,
                [new Point3D(0, 1, 0)] = 2272,
                [new Point3D(0, 2, 0)] = 2272
            },
            new Point3D(5472, 26, -30)
        );

        // Wall 3
        CreateWall(
            new Point3D(5619, 57, 0),
            new Dictionary<Point3D, int>
            {
                [new Point3D(-1, 0, 0)] = 1059,
                [new Point3D(0, 0, 0)] = 1059,
                [new Point3D(1, 0, 0)] = 1059
            },
            new Point3D(5621, 43, 0)
        );

        e?.Mobile.SendMessage(0x40, "Подземелье Shame установлено.");
    }

    private static void CreateAltar(
        Type guardianType,
        Point3D altarLocation,
        Point3D teleporterLocation,
        Point3D teleporterDestination,
        Point3D spawnLocation,
        int cost
    )
    {
        foreach (var map in new[] { Map.Trammel, Map.Felucca })
        {
            if (CheckForAltar(altarLocation, map))
            {
                continue;
            }

            var altar = new ShameAltar(guardianType, teleporterLocation, teleporterDestination, spawnLocation, cost);
            WeakEntityCollection.Add("newshame", altar);
            altar.MoveToWorld(altarLocation, map);
        }
    }

    private static void CreateWall(Point3D wallLocation, Dictionary<Point3D, int> details, Point3D trollSpawn)
    {
        foreach (var map in new[] { Map.Trammel, Map.Felucca })
        {
            if (CheckForAltar(wallLocation, map))
            {
                continue;
            }

            var wall = new ShameWall(details, wallLocation, trollSpawn, map);
            WeakEntityCollection.Add("newshame", wall);
            wall.MoveToWorld(wallLocation, map);
            ShameWall.AddTeleporters(wall);
        }
    }

    public static void RemoveItems()
    {
        foreach (var map in new[] { Map.Trammel, Map.Felucca })
        {
            RemoveItem(new Point3D(5490, 19, -25), map, typeof(Teleporter));
            RemoveItem(new Point3D(5604, 102, 5), map, typeof(Teleporter));
            RemoveItem(new Point3D(5538, 170, 5), map, typeof(Teleporter));
        }

        ResetOldSpawners();
    }

    public static void ResetOldSpawners(bool reset = true)
    {
        foreach (var map in new[] { Map.Trammel, Map.Felucca })
        {
            var region = Region.Find(new Point3D(5538, 170, 5), map);

            if (region == null)
            {
                continue;
            }

            foreach (var item in region.GetItems())
            {
                if (item is not XmlSpawner spawner)
                {
                    continue;
                }

                var name = spawner.Name?.ToLowerInvariant();

                if (name is "shame_revamped" or "shame_chest")
                {
                    continue;
                }

                if (reset)
                {
                    spawner.DoReset = true;
                }
                else
                {
                    spawner.DoRespawn = true;
                }
            }
        }
    }

    public static bool RemoveItem(Point3D p, Map map, Type type)
    {
        foreach (var item in map.GetItemsInRange(p, 0))
        {
            if (item.GetType() == type)
            {
                item.Delete();
                return true;
            }
        }

        return false;
    }

    public static bool CheckForAltar(Point3D p, Map map)
    {
        foreach (var item in map.GetItemsInRange(p, 0))
        {
            if (item is ShameAltar or ShameWall)
            {
                return true;
            }
        }

        return false;
    }
}
