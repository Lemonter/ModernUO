using System;
using System.Collections.Generic;
using System.IO;
using Server.Items;
using Server.Logging;

namespace Server.Systems.Bots;

/// <summary>
/// Where the resurrecting ankhs stand: virtue shrines, the Chaos shrine, town ankhs. Read from the
/// decoration files [Decorate places them from, so no world scan is needed; each spot is checked
/// for an actual ankh before a ghost is sent there, because a shard may not have decorated.
/// </summary>
public static class ShrineAtlas
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ShrineAtlas));

    private sealed class Spot
    {
        public Map Map;
        public Point3D Location;
        public Item Ankh;
    }

    private static readonly List<Spot> _spots = [];

    // Same folders and facets as [Decorate.
    private static readonly (string Folder, Map[] Maps)[] _sources =
    [
        ("Britannia", [Map.Trammel, Map.Felucca]),
        ("Trammel", [Map.Trammel]),
        ("Felucca", [Map.Felucca]),
        ("Ilshenar", [Map.Ilshenar]),
        ("Malas", [Map.Malas]),
        ("Tokuno", [Map.Tokuno])
    ];

    public static int Count => _spots.Count;

    public static void Rebuild()
    {
        _spots.Clear();

        foreach (var (folder, maps) in _sources)
        {
            var path = Path.Combine(Core.BaseDirectory, "Data", "Decoration", folder);
            if (!Directory.Exists(path))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(path, "*.cfg"))
            {
                try
                {
                    ReadFile(file, maps);
                }
                catch (Exception e)
                {
                    logger.Warning(e, "Bots: could not read ankhs from {File}", file);
                }
            }
        }
    }

    private static void ReadFile(string file, Map[] maps)
    {
        var inAnkh = false;

        foreach (var raw in File.ReadLines(file))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                inAnkh = false;
                continue;
            }

            if (line[0] == '#')
            {
                continue;
            }

            if (!char.IsDigit(line[0]) && line[0] != '-')
            {
                inAnkh = line.StartsWith("AnkhWest", StringComparison.Ordinal) ||
                         line.StartsWith("AnkhNorth", StringComparison.Ordinal);
                continue;
            }

            if (!inAnkh)
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3 || !int.TryParse(parts[0], out var x) || !int.TryParse(parts[1], out var y) ||
                !int.TryParse(parts[2], out var z))
            {
                continue;
            }

            foreach (var map in maps)
            {
                _spots.Add(new Spot { Map = map, Location = new Point3D(x, y, z) });
            }
        }
    }

    internal static void AddSpot(Map map, Point3D location) =>
        _spots.Add(new Spot { Map = map, Location = location });

    internal static void Clear() => _spots.Clear();

    /// <summary>The nearest standing ankh on <paramref name="map"/> that <paramref name="allowed"/>
    /// accepts, or null.</summary>
    public static Item FindNearest(Map map, Point3D from, Func<Item, bool> allowed = null)
    {
        Item best = null;
        var bestDist = double.MaxValue;

        foreach (var spot in _spots)
        {
            if (spot.Map != map)
            {
                continue;
            }

            var dist = spot.Location.GetDistanceToSqrt(from);
            if (dist >= bestDist)
            {
                continue;
            }

            var ankh = Resolve(spot);
            if (ankh != null && allowed?.Invoke(ankh) != false)
            {
                bestDist = dist;
                best = ankh;
            }
        }

        return best;
    }

    private static Item Resolve(Spot spot)
    {
        if (spot.Ankh is { Deleted: false })
        {
            return spot.Ankh;
        }

        // Empty spots are looked at again each time: a ghost picks its target once per death, and
        // a shard decorated after startup should still be found.
        spot.Ankh = null;

        foreach (var item in spot.Map.GetItemsInRange(spot.Location, 1))
        {
            if (item is AnkhWest or AnkhNorth && item.Parent == null)
            {
                spot.Ankh = item;
                break;
            }
        }

        return spot.Ankh;
    }
}
