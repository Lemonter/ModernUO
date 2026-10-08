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
/// or a house's own, is left alone. Upper floors up the stairs and balconies behind upstairs doors
/// are part of the same home.
///
///   [GenCityHouses — runs it over every town; safe to run again, rooms already marked are skipped.
///
/// Runs once by itself on the first start (setting cityHouses.autoGenerate).
/// </summary>
public static class MahaonCityHouseGenerator
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(MahaonCityHouseGenerator));

    private const int MinTiles = 6;
    private const int MaxTiles = 400;
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

    // A step between surfaces: floors match, stairs climb a few Z per tile.
    private const int MaxClimb = 7;

    // Doors this high above the ground are inside the building — to a balcony, between upstairs
    // rooms — and are walked through; doors on the ground floor bound the home.
    private const int UpperFloorDoor = 10;

    /// <summary>
    /// The home behind a door, every floor of it, or null when it spills into the street or is
    /// too small or too big. Walks from surface to surface like a walker: across the floor, up
    /// and down the stairs, through upstairs doors onto a balcony — never through a wall or a
    /// ground-floor door. Furniture is walked over: the floor runs on under it.
    /// </summary>
    private static List<Point3D> Flood(Map map, Point2D seed, BaseDoor door)
    {
        var start = ClosestSurface(map, seed.X, seed.Y, door.Z);
        if (start == null || IsStreet(map, seed.X, seed.Y, start.Value))
        {
            return null;
        }

        var room = new List<Point3D>();
        var first = new Point3D(seed.X, seed.Y, start.Value);
        var seen = new HashSet<Point3D> { first };
        var queue = new Queue<Point3D>();
        queue.Enqueue(first);

        while (queue.Count > 0)
        {
            var p = queue.Dequeue();

            if (IsStreet(map, p.X, p.Y, p.Z))
            {
                return null; // reached the street: not a closed home
            }

            room.Add(p);
            if (room.Count > MaxTiles)
            {
                return null;
            }

            // Four directions: a diagonal step would slip past the corner where two walls meet.
            Span<Point2D> next = [new(p.X + 1, p.Y), new(p.X - 1, p.Y), new(p.X, p.Y + 1), new(p.X, p.Y - 1)];
            foreach (var n in next)
            {
                if (n.X == door.X && n.Y == door.Y || IsWall(map, n.X, n.Y, p.Z) || IsBoundingDoor(map, n))
                {
                    continue;
                }

                if (ClosestSurface(map, n.X, n.Y, p.Z) is { } z && (z - p.Z).Abs() <= MaxClimb)
                {
                    var node = new Point3D(n.X, n.Y, z);
                    if (seen.Add(node))
                    {
                        queue.Enqueue(node);
                    }
                }
            }
        }

        return room.Count >= MinTiles ? room : null;
    }

    /// <summary>The standing height in a column nearest <paramref name="z"/>: the ground, or the
    /// top of a floor or stair tile.</summary>
    private static int? ClosestSurface(Map map, int x, int y, int z)
    {
        var land = map.Tiles.GetLandTile(x, y);
        var landFlags = TileData.LandTable[land.ID & TileData.MaxLandValue].Flags;

        int? best = null;
        if ((landFlags & (TileFlag.Impassable | TileFlag.Wet)) == 0)
        {
            best = land.Z;
        }

        foreach (var tile in map.Tiles.GetStaticTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
            if (!data.Surface || data.Impassable || data.Wet)
            {
                continue;
            }

            var top = tile.Z + data.CalcHeight;
            if (best == null || (top - z).Abs() < (best.Value - z).Abs())
            {
                best = top;
            }
        }

        return best;
    }

    /// <summary>The open street: ground level, no roof over it and no floor laid on it. A
    /// balcony or a roof terrace is open too, but up off the ground, and belongs to the house.</summary>
    private static bool IsStreet(Map map, int x, int y, int z)
    {
        var land = map.Tiles.GetLandTile(x, y);
        if (z - land.Z > 5)
        {
            return false;
        }

        foreach (var tile in map.Tiles.GetStaticTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

            if (data.Roof && tile.Z > z && tile.Z - z <= RoofSearch)
            {
                return false;
            }

            if (data.Surface && !data.Impassable && data.Height <= 1 && (tile.Z + data.CalcHeight - z).Abs() <= 2)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsBoundingDoor(Map map, Point2D p)
    {
        foreach (var door in map.GetItemsAt<BaseDoor>(p))
        {
            if (door.Z - map.Tiles.GetLandTile(p.X, p.Y).Z < UpperFloorDoor)
            {
                return true;
            }
        }

        return false;
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
