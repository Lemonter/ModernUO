using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Generate.cs).
///     ServUO registered a GM command that also ran <c>[xmlload Spawns/TreasuresOfKotl.xml</c>; this codebase has no
///     such spawn file, so the command places only the static content. Skipped: the <c>KotlCityPuzzle</c> and the
///     <c>KotlBattleSimulator</c> (both are separate systems that are not part of this pack). The controller is not
///     auto-started: nothing is placed until a GM runs the command.
/// </summary>
public static class TreasuresOfKotlCityGeneration
{
    public static void Configure()
    {
        CommandSystem.Register("GenerateTreasuresOfKotlCity", AccessLevel.GameMaster, Generate_OnCommand);
    }

    private static void Generate_OnCommand(CommandEventArgs e)
    {
        Generate();

        e.Mobile.SendMessage("Контент Treasures of Kotl City размещён.");
    }

    public static void Generate()
    {
        var map = Map.TerMur;

        Place<WheelsOfTime>(new Point3D(607, 2323, 0), map);
        Place<KotlDoor>(new Point3D(610, 2319, 0), map);

        PlaceEnergyTiles(new Point3D(592, 2393, 0), map);
        PlaceEnergyTiles(new Point3D(600, 2393, 0), map);
        PlaceEnergyTiles(new Point3D(608, 2393, 0), map);
        PlaceEnergyTiles(new Point3D(616, 2393, 0), map);
        PlaceEnergyTiles(new Point3D(624, 2393, 0), map);

        GenTeleporters(map);
        GenStations(map);
        GenLOSBlockers(map);
        GenChests(map);

        if (!FindItem(new Point3D(489, 1606, 40), map))
        {
            new Hal().MoveToWorld(new Point3D(489, 1606, 40), map);
        }

        KotlCityData.Enabled = true;
    }

    private static void Place<T>(Point3D p, Map map) where T : Item, new()
    {
        if (FindItem(p, map))
        {
            return;
        }

        new T().MoveToWorld(p, map);
    }

    private static void PlaceEnergyTiles(Point3D p, Map map)
    {
        if (FindItem(p, map))
        {
            return;
        }

        new EnergyTileAddon(13, Direction.South).MoveToWorld(p, map);
    }

    private static bool FindItem(Point3D p, Map map)
    {
        foreach (var item in map.GetItemsInRange(p, 0))
        {
            if (!item.Deleted)
            {
                return true;
            }
        }

        return false;
    }

    private static void GenTeleporters(Map map)
    {
        Teleport(new Point3D(492, 1613, 40), new Point3D(542, 2478, 0), map);
        Teleport(new Point3D(493, 1613, 40), new Point3D(543, 2478, 0), map);
        Teleport(new Point3D(492, 1614, 40), new Point3D(542, 2479, 0), map);
        Teleport(new Point3D(493, 1614, 40), new Point3D(543, 2479, 0), map);

        Teleport(new Point3D(542, 2478, 0), new Point3D(492, 1613, 40), map);
        Teleport(new Point3D(543, 2478, 0), new Point3D(493, 1613, 40), map);
        Teleport(new Point3D(542, 2479, 0), new Point3D(492, 1614, 40), map);
        Teleport(new Point3D(543, 2479, 0), new Point3D(493, 1614, 40), map);

        Teleport(new Point3D(643, 2307, 0), new Point3D(542, 2478, 0), map);
        Teleport(new Point3D(644, 2307, 0), new Point3D(543, 2478, 0), map);
        Teleport(new Point3D(643, 2308, 0), new Point3D(542, 2479, 0), map);
        Teleport(new Point3D(644, 2308, 0), new Point3D(543, 2479, 2), map);

        Teleport(new Point3D(638, 2399, 0), new Point3D(631, 2423, -20), map);
        Teleport(new Point3D(639, 2399, 0), new Point3D(632, 2423, -20), map);
        Teleport(new Point3D(638, 2400, 0), new Point3D(631, 2424, -20), map);
        Teleport(new Point3D(639, 2400, 0), new Point3D(632, 2424, -20), map);

        Teleport(new Point3D(575, 2463, 0), new Point3D(543, 2303, 0), map);
        Teleport(new Point3D(576, 2463, 0), new Point3D(544, 2303, 0), map);
        Teleport(new Point3D(575, 2464, 0), new Point3D(543, 2304, 0), map);
        Teleport(new Point3D(576, 2464, 0), new Point3D(544, 2304, 0), map);
    }

    private static void Teleport(Point3D from, Point3D to, Map map)
    {
        if (FindItem(from, map))
        {
            return;
        }

        new Teleporter(to, map).MoveToWorld(from, map);
    }

    private static void GenStations(Map map)
    {
        var locations = new[]
        {
            new Point3D(623, 2447, -20), new Point3D(639, 2447, -20), new Point3D(647, 2455, -20),
            new Point3D(647, 2463, -20), new Point3D(639, 2471, -20), new Point3D(615, 2455, -20),
            new Point3D(615, 2463, -20), new Point3D(623, 2471, -20)
        };

        foreach (var p in locations)
        {
            if (FindItem(p, map))
            {
                continue;
            }

            new PowerCoreDockingStation(true).MoveToWorld(p, map);
        }
    }

    private static void GenLOSBlockers(Map map)
    {
        for (var y = 2392; y < 2407; y++)
        {
            var p = new Point3D(583, y, 0);

            if (!FindItem(p, map))
            {
                new LOSBlocker().MoveToWorld(p, map);
            }
        }
    }

    private static void GenChests(Map map)
    {
        var locations = new[]
        {
            new Point3D(484, 2289, 0), new Point3D(483, 2305, 0), new Point3D(485, 2321, 0),
            new Point3D(497, 2289, 0), new Point3D(499, 2305, 0), new Point3D(500, 2321, 0),
            new Point3D(634, 2321, 0), new Point3D(633, 2292, 0), new Point3D(649, 2289, 0),
            new Point3D(649, 2321, 0)
        };

        for (var i = 0; i < locations.Length; i++)
        {
            if (FindItem(locations[i], map))
            {
                continue;
            }

            var chest = new KotlRegalChest();

            if (i == 7)
            {
                chest.ItemID = 0x4D0D;
            }

            chest.MoveToWorld(locations[i], map);
        }
    }
}
