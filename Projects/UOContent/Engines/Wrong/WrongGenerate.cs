using System;
using System.IO;
using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Wrong;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Generate.cs) — `[GenWrongRevamp` rebuilds Wrong:
///     clears the old dungeon objects, loads the spawn file, scatters the maze decoration, and plants the
///     shortcut teleporters and the seven hot chests.
///
///     Data shipped alongside: Distribution/XmlSpawner/WrongRevamped.xml (spawns) and
///     Distribution/Data/Decoration/Wrong/WrongMaze.cfg (the invisible maze walls).
///
///     ServUO's `BedrollSpawner` stage is not part of this pass — the bedroll/prisoner mechanic needs its
///     own port; the chests, teleporters, decoration and spawns are here.
/// </summary>
public static class WrongGenerator
{
    private static readonly Rectangle2D DungeonBounds = new(5633, 511, 253, 510);

    public static void Initialize()
    {
        EventSink.ServerStarted += OnServerStarted;
    }

    /// <summary>Wrong is part of the world: after every load the dungeon is checked and completed when its
    /// bedroll spawner is missing. The spawner persists, so a normal boot finds it and does nothing.</summary>
    private static void OnServerStarted() => Timer.DelayCall(TimeSpan.FromSeconds(7), EnsureSetup);

    private static void EnsureSetup()
    {
        if (HasBedrollSpawner())
        {
            return;
        }

        try
        {
            GenerateOnCommand(null);
            _logger.Information("Wrong Revamp is set up.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Wrong Revamp setup failed");
        }
    }

    private static readonly Server.Logging.ILogger _logger =
        Server.Logging.LogFactory.GetLogger(typeof(WrongGenerator));

    private static bool HasBedrollSpawner()
    {
        foreach (var item in Map.Trammel.GetItemsInRange(new Point3D(5823, 601, 0), 0))
        {
            if (item is BedrollSpawner)
            {
                return true;
            }
        }

        return false;
    }

    private static void GenerateOnCommand(CommandEventArgs e)
    {
        DeleteOldWrong(e?.Mobile);

        XmlSpawner.XmlLoadDefaults("WrongRevamped.xml", e?.Mobile);

        Decorate.Generate(Path.Combine(Core.BaseDirectory, "Data", "Decoration", "Wrong"), Map.Trammel, Map.Felucca);

        PlaceTeleporters();
        PlaceBedrollSpawners();
        EnchantedHotItem.SpawnChests(Map.Trammel);
        EnchantedHotItem.SpawnChests(Map.Felucca);

        e?.Mobile.SendMessage(0x40, "Подземелье Wrong установлено.");
    }

    private static void PlaceBedrollSpawners()
    {
        foreach (var map in new[] { Map.Trammel, Map.Felucca })
        {
            var spawner = new BedrollSpawner();
            spawner.MoveToWorld(new Point3D(5823, 601, 0), map);
        }
    }

    private static void PlaceTeleporters()
    {
        // From -> destination pairs, planted on both facets.
        (Point3D From, Point3D To)[] pairs =
        {
            (new Point3D(5827, 590, 1), new Point3D(5690, 569, 25)),
            (new Point3D(5690, 573, 25), new Point3D(5829, 595, 0)),
            (new Point3D(5872, 532, 24), new Point3D(5690, 569, 25)),
            (new Point3D(5732, 554, 24), new Point3D(5827, 593, 0)),
            (new Point3D(5708, 625, 0), new Point3D(5703, 639, 0)),
            (new Point3D(5698, 662, 0), new Point3D(5792, 526, 10)),
            (new Point3D(5824, 631, 5), new Point3D(2041, 215, 14)),
            (new Point3D(5825, 631, 5), new Point3D(2043, 215, 14))
        };

        foreach (var map in new[] { Map.Trammel, Map.Felucca })
        {
            foreach (var (from, to) in pairs)
            {
                var teleporter = new Teleporter(to, map);
                teleporter.MoveToWorld(from, map);
            }
        }
    }

    public static void DeleteOldWrong(Mobile m)
    {
        var toDelete = new System.Collections.Generic.List<Item>();

        foreach (var map in new[] { Map.Trammel, Map.Felucca })
        {
            foreach (var item in map.GetItemsInBounds(DungeonBounds))
            {
                if (item is XmlSpawner or Teleporter || item.ItemID == 0x375A)
                {
                    toDelete.Add(item);
                }
            }
        }

        // Удаление мутирует коллекцию, по которой идёт обход, поэтому сначала собираем список.
        foreach (var item in toDelete)
        {
            item.Delete();
        }

        m?.SendMessage($"Удалено объектов: {toDelete.Count}.");
    }
}
