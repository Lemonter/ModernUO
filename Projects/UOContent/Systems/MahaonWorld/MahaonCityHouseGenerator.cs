using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Engines.Spawners;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;

namespace Server.Systems.MahaonWorld;

/// <summary>
/// Finds the rooms of town buildings and puts them up for sale as city apartments, with no GM
/// marking. Every town door opens onto a room: the floor reached from it, under a roof or on laid
/// floor, without crossing a wall or another door, is one apartment — provided it closes off (it
/// doesn't spill out into the street) and nobody works there: a room with a vendor or a spawner,
/// or a house's own, is left alone. Ground floors only.
///
///   [GenCityHouses — runs it over every town; safe to run again, rooms already marked are skipped.
///
/// Runs once by itself on the first start (setting cityHouses.autoGenerate).
/// </summary>
public static class MahaonCityHouseGenerator
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(MahaonCityHouseGenerator));

    private const int MinTiles = 6;
    private const int MaxTiles = 300;
    private const int RoofSearch = 40;

    private const string AutoSetting = "cityHouses.autoGenerate";

    public static void Configure()
    {
        CommandSystem.Register("GenCityHouses", AccessLevel.Administrator, e =>
        {
            var made = GenerateAll();
            e.Mobile.SendMessage(0x59, $"Городских домов найдено и выставлено на продажу: {made}.");
        });
    }

    // After the world (and its doors) and the existing city houses are loaded.
    [CallPriority(80)]
    public static void Initialize()
    {
        if (!ServerConfiguration.GetOrUpdateSetting(AutoSetting, true))
        {
            return;
        }

        var made = GenerateAll();
        ServerConfiguration.SetSetting(AutoSetting, false);
        logger.Information("City houses: {Count} rooms found in towns and put up for sale", made);
    }

    public static int GenerateAll()
    {
        var made = 0;
        var seenDoors = new HashSet<Item>();

        foreach (var region in Region.Regions)
        {
            if (region is not TownRegion town || town.Map == null || town.Map == Map.Internal)
            {
                continue;
            }

            foreach (var area in town.Area)
            {
                var rect = new Rectangle2D(area.Start.X, area.Start.Y, area.End.X - area.Start.X, area.End.Y - area.Start.Y);
                foreach (var door in town.Map.GetItemsInBounds<BaseDoor>(rect))
                {
                    if (seenDoors.Add(door) && TryMake(town, door))
                    {
                        made++;
                    }
                }
            }
        }

        return made;
    }

    private static bool TryMake(TownRegion town, BaseDoor door)
    {
        var map = door.Map;
        if (door is BaseHouseDoor or MahaonHouseFenceGate || BaseHouse.FindHouseAt(door) != null || door.Parent != null)
        {
            return false;
        }

        // Each side of the door: the room is on whichever side closes off.
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx != 0 && dy != 0 || dx == 0 && dy == 0)
                {
                    continue;
                }

                var seed = new Point2D(door.X + dx, door.Y + dy);
                if (MahaonCityHouseSystem.IsInsideAnyHouse(map, seed.X, seed.Y))
                {
                    return false; // this door already leads into a marked house
                }

                var room = Flood(map, seed, door);
                if (room == null || !IsFree(map, room))
                {
                    continue;
                }

                var outside = new Point3D(door.X - dx, door.Y - dy, map.GetAverageZ(door.X - dx, door.Y - dy));
                Create(town, map, room, outside);
                return true;
            }
        }

        return false;
    }

    /// <summary>The room behind a door, or null when the floor spills outside, is too small or
    /// too big to be a room.</summary>
    private static List<Point3D> Flood(Map map, Point2D seed, BaseDoor door)
    {
        if (!IsIndoors(map, seed.X, seed.Y, out _))
        {
            return null;
        }

        var room = new List<Point3D>();
        var seen = new HashSet<Point2D> { seed, new(door.X, door.Y) };
        var queue = new Queue<Point2D>();
        queue.Enqueue(seed);

        while (queue.Count > 0)
        {
            var p = queue.Dequeue();

            if (!IsIndoors(map, p.X, p.Y, out var floorZ))
            {
                return null; // reached the street: not a closed room
            }

            room.Add(new Point3D(p.X, p.Y, floorZ));
            if (room.Count > MaxTiles)
            {
                return null;
            }

            // Four directions: a diagonal step would slip past the corner where two walls meet.
            Span<Point2D> next = [new(p.X + 1, p.Y), new(p.X - 1, p.Y), new(p.X, p.Y + 1), new(p.X, p.Y - 1)];
            foreach (var n in next)
            {
                if (seen.Add(n) && !IsWall(map, n.X, n.Y, floorZ) && !HasDoor(map, n))
                {
                    queue.Enqueue(n);
                }
            }
        }

        return room.Count >= MinTiles ? room : null;
    }

    /// <summary>Under a roof or on laid floor: inside a building. The floor height is the top of the
    /// floor tile, or the ground's.</summary>
    private static bool IsIndoors(Map map, int x, int y, out int floorZ)
    {
        var land = map.Tiles.GetLandTile(x, y);
        floorZ = land.Z;
        var floor = false;
        var roof = false;

        foreach (var tile in map.Tiles.GetStaticTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

            if (data.Roof && tile.Z > land.Z && tile.Z - land.Z <= RoofSearch)
            {
                roof = true;
            }
            else if (data.Surface && !data.Impassable && data.Height <= 1 && tile.Z >= land.Z - 1 && tile.Z - land.Z <= 5)
            {
                floor = true;
                floorZ = tile.Z + data.CalcHeight;
            }
        }

        return floor || roof;
    }

    private static bool IsWall(Map map, int x, int y, int floorZ)
    {
        foreach (var tile in map.Tiles.GetStaticTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
            if (data.Wall && tile.Z + data.CalcHeight > floorZ && tile.Z < floorZ + 16)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasDoor(Map map, Point2D p)
    {
        foreach (var _ in map.GetItemsAt<BaseDoor>(p))
        {
            return true;
        }

        return false;
    }

    /// <summary>Nobody works or spawns here, and it isn't anyone's house already.</summary>
    private static bool IsFree(Map map, List<Point3D> room)
    {
        foreach (var p in room)
        {
            if (BaseHouse.FindHouseAt(p, map, 16) != null)
            {
                return false;
            }

            foreach (var item in map.GetItemsAt(p.X, p.Y))
            {
                if (item is ISpawner or AnkhWest or AnkhNorth or MahaonCityHouse)
                {
                    return false;
                }
            }

            foreach (var m in map.GetMobilesAt(new Point2D(p.X, p.Y)))
            {
                if (m is BaseVendor or BaseHealer or Banker)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void Create(TownRegion town, Map map, List<Point3D> room, Point3D signSpot)
    {
        var number = 1;
        foreach (var existing in MahaonCityHouseSystem.All())
        {
            if (existing.Label?.StartsWith(town.Name, StringComparison.Ordinal) == true)
            {
                number++;
            }
        }

        var house = new MahaonCityHouse(map, room, $"{town.Name}, дом №{number}");
        house.ApplyFurnitureHiding();
        MahaonCityHouseSystem.Register(house);

        var sign = new MahaonCityHouseSign(house);
        sign.MoveToWorld(signSpot, map);
    }
}
