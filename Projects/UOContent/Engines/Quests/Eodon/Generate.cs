using System;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.Quests.Eodon;

/// <summary>
///     World setup for the Eodon quest content, ported from ServUO
///     (Scripts/Quests/Eodon/MyrmidexThreat/Generate.cs). ServUO ran it from a GM command and used
///     <c>XmlSpawner</c> entries for the quest NPCs; here it is idempotent startup content like
///     Engines/ExploringTheDeep/Generate.cs — the anchor is the Myrmidex idol, and if it is already in the world
///     nothing is placed again.
///
///     Dropped, because the subsystems they belong to do not exist in this codebase:
///     <list type="bullet">
///         <item>
///             <c>MoonstonePowerGeneratorAddon</c> (8 of them around the Antechamber), <c>AllegianceIdol</c>,
///             <c>BattleSpawner</c>, <c>HealerCamp</c>, <c>GeoffreyCampAddon</c> and <c>BattleFlag</c> — all part of
///             the Myrmidex Invasion battle system.
///         </item>
///         <item>The <c>XmlSpawner("MyrmidexQueen/Cantwalk/true")</c> entry; the queen is created directly below.</item>
///         <item>The allegiance-gated pit teleporters keep their destinations but no longer check allegiance.</item>
///     </list>
/// </summary>
public static class GenerateEodonQuests
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GenerateEodonQuests));

    private static readonly Point3D IdolLocation = new(488, 1856, 111);

    public static void Initialize() => EventSink.ServerStarted += OnServerStarted;

    private static void OnServerStarted() => Timer.StartTimer(TimeSpan.FromSeconds(12), EnsureSetup);

    private static bool HasIdol()
    {
        foreach (var item in Map.TerMur.GetItemsInRange(IdolLocation, 2))
        {
            if (item is MyrmidexIdol)
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureSetup()
    {
        if (HasIdol())
        {
            return;
        }

        try
        {
            Generate();
            logger.Information("Eodon quest content is set up");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Eodon quest setup failed");
        }
    }

    private static void Generate()
    {
        var map = Map.TerMur;

        GenerateAntechamber(map);

        // Quest givers of the Myrmidex Threat line.
        Place(new Yar(), new Point3D(169, 1813, 80), map);
        Place(new EllieRafkin(), new Point3D(261, 1742, 80), map);
        Place(new Foxx(), new Point3D(269, 1727, 80), map);
        Place(new Bront(), new Point3D(1448, 3774, 0), Map.Trammel);
        Place(new CollectorOfOddities(), new Point3D(4305, 1003, 0), Map.Trammel);
        Place(new Carroll(), new Point3D(2878, 723, 0), Map.Trammel);
        Place(new Eriathwen(), new Point3D(1426, 1653, 0), Map.Trammel);
        Place(new Yero(), new Point3D(3692, 2252, 26), Map.Trammel);
        Place(new Alida(), new Point3D(5257, 4012, 40), Map.Trammel);

        Place(new MyrmidexQueen(), new Point3D(768, 2303, 0), map);
    }

    private static void GenerateAntechamber(Map map)
    {
        // Static shell of the Kotl Antechamber.
        int[] outer = [40142];
        var z = -19;

        PlaceStatics(map, outer[0], z, (888, 2303), (889, 2304), (896, 2295), (897, 2296), (904, 2303), (905, 2304),
            (896, 2311), (897, 2312));

        PlaceStatics(map, 40169, z,
            (887, 2304), (888, 2305), (895, 2296), (896, 2297), (903, 2304), (904, 2305), (895, 2312), (896, 2313));

        z = -20;

        PlaceStatics(map, 40161, z, (897, 2305));
        PlaceStatics(map, 40165, z, (894, 2306));

        Place(new MyrmidexIdol(), IdolLocation, map);

        Place(new Teleporter(new Point3D(487, 1857, 95), map), new Point3D(896, 2304, 45), map);

        var queens = new[]
        {
            (Source: new Point3D(121, 1682, -3), Dest: new Point3D(711, 2302, 0)),
            (Source: new Point3D(121, 1683, -2), Dest: new Point3D(711, 2303, 0)),
            (Source: new Point3D(121, 1684, -1), Dest: new Point3D(711, 2304, 0))
        };

        foreach (var queen in queens)
        {
            Place(new MyrmidexQueenTeleporter(queen.Dest, map), queen.Source, map);
        }

        var queenExits = new[]
        {
            (Source: new Point3D(711, 2302, 0), Dest: new Point3D(120, 1682, -1)),
            (Source: new Point3D(711, 2303, 0), Dest: new Point3D(120, 1683, 1)),
            (Source: new Point3D(711, 2304, 0), Dest: new Point3D(120, 1684, 2)),
            (Source: new Point3D(711, 2305, 0), Dest: new Point3D(120, 1684, 2))
        };

        foreach (var exit in queenExits)
        {
            Place(new Teleporter(exit.Dest, map), exit.Source, map);
        }

        var pitEntrances = new[]
        {
            (Source: new Point3D(176, 1835, 90), Dest: new Point3D(985, 1883, 0)),
            (Source: new Point3D(177, 1835, 90), Dest: new Point3D(986, 1883, 0)),
            (Source: new Point3D(224, 1724, 6), Dest: new Point3D(853, 1777, 0)),
            (Source: new Point3D(224, 1725, 6), Dest: new Point3D(854, 1777, 0))
        };

        foreach (var entrance in pitEntrances)
        {
            Place(new MyrmidexPitTeleporter(entrance.Dest, map), entrance.Source, map);
        }

        var pitExits = new[]
        {
            (Source: new Point3D(985, 1887, 0), Dest: new Point3D(176, 1834, 90)),
            (Source: new Point3D(986, 1887, 0), Dest: new Point3D(177, 1834, 90)),
            (Source: new Point3D(853, 1776, 11), Dest: new Point3D(225, 1724, 6)),
            (Source: new Point3D(854, 1776, 11), Dest: new Point3D(225, 1725, 6)),
            (Source: new Point3D(855, 1776, 11), Dest: new Point3D(225, 1725, 6))
        };

        foreach (var exit in pitExits)
        {
            Place(new Teleporter(exit.Dest, map), exit.Source, map);
        }

        Place(new LOSBlocker(), new Point3D(121, 1682, -3), map);
        Place(new LOSBlocker(), new Point3D(121, 1683, -3), map);
        Place(new LOSBlocker(), new Point3D(121, 1684, -3), map);
    }

    private static void PlaceStatics(Map map, int itemID, int z, params (int X, int Y)[] points)
    {
        foreach (var point in points)
        {
            Place(new Static(itemID), new Point3D(point.X, point.Y, z), map);
        }
    }

    private static void Place(Item item, Point3D location, Map map) => item.MoveToWorld(location, map);

    private static void Place(Mobile mobile, Point3D location, Map map) => mobile.MoveToWorld(location, map);
}
