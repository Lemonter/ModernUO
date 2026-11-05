using System;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Generate.cs). Places the Forsaken Foes
///     content on both facets: the Mining Cooperative Merchant and the Fellowship Adept, the eight Blackthorn
///     dungeon decoration addons, the Fellowship house and its NPCs, the blockers and line-of-sight blockers that
///     keep players on the chain, the eight workers who trade Fellowship coins for chain progress, and the fourteen
///     teleporters that move a player along it.
///
///     ServUO ran this from <c>Initialize</c> behind <c>Core.EJ</c> and re-synced it against the points system on
///     every world save. Neither exists here, so — per the season-port rules — the content is set up once when the
///     server starts, and every placement is guarded by a spatial presence check so a second run adds nothing.
///     There is no season switch: the static content is part of the world, and the Fellowship shop itself is only
///     usable while <see cref="FellowshipData.Enabled" /> is on.
///
///     Dropped because the dependency is not part of this port:
///     <list type="bullet">
///         <item>
///             <c>XmlSpawner.LocateFile("RevampedSpawns/BlackthornDungeonCreature.xml")</c> and the
///             <c>XmlUnLoadFromFile</c>/<c>XmlLoadFromFile</c> pair around it — this port has no <c>XmlSpawner</c>
///             and no <c>Distribution/Data/Spawns/RevampedSpawns/BlackthornDungeonCreature.xml</c>, so the dungeon
///             creatures are not spawned from it.
///         </item>
///         <item>
///             <c>Siege.SiegeShard</c> guards — this codebase has no siege shard flag, so both facets get the
///             content.
///         </item>
///         <item>
///             <c>Remove</c>/<c>RemoveDecoration</c>/<c>OtherQuestContent</c>'s counterpart removal paths — they
///             only existed so the season could be switched off at run time, and the static <c>InstanceTram</c>/
///             <c>InstanceFel</c> they keyed on are not used for presence here.
///         </item>
///     </list>
/// </summary>
public static class ForsakenFoesGeneration
{
    private static readonly ILogger Logger = LogFactory.GetLogger(typeof(ForsakenFoesGeneration));

    private static readonly Point3D[] BlockerPoints =
    {
        new(6408, 2667, 0), new(6409, 2667, 0), new(6410, 2667, 0), new(6411, 2667, 0),
        new(6412, 2667, 0), new(6396, 2677, 0), new(6396, 2678, 0), new(6396, 2679, 0),
        new(6396, 2680, 0), new(6396, 2681, 0), new(6407, 2692, 0), new(6408, 2692, 0),
        new(6409, 2692, 0), new(6410, 2692, 0), new(6370, 2744, 0), new(6371, 2744, 0),
        new(6372, 2744, 0), new(6373, 2744, 0), new(6374, 2744, 0), new(6375, 2744, 0),
        new(6331, 2653, 0), new(6332, 2653, 0), new(6333, 2653, 0), new(6410, 2783, 0),
        new(6305, 2678, 0), new(6305, 2679, 0), new(6305, 2680, 0), new(6411, 2783, 0),
        new(6422, 2708, 0), new(6422, 2709, 0), new(6422, 2710, 0), new(6422, 2711, 0),
        new(6406, 2783, 0), new(6407, 2783, 0), new(6408, 2783, 0), new(6409, 2783, 0),
        new(6442, 2744, 0), new(6443, 2744, 0), new(6444, 2744, 0), new(6445, 2744, 0),
        new(6446, 2744, 0), new(6390, 2617, 0), new(6390, 2618, 0), new(6390, 2619, 0),
        new(6390, 2620, 0), new(6390, 2621, 0), new(6390, 2622, 0), new(6390, 2623, 0),
        new(6390, 2624, 0), new(6390, 2625, 0), new(6434, 2708, 0), new(6434, 2709, 0),
        new(6434, 2710, 0), new(6434, 2711, 0)
    };

    private static readonly Point3D[] WorkerLocations =
    {
        new(6406, 2680, 0), new(6386, 2621, 0), new(6444, 2738, 0), new(6371, 2742, 0),
        new(6332, 2650, 0), new(6299, 2679, 0), new(6428, 2709, 0), new(6409, 2787, 0)
    };

    // [source, destination] pairs. Every even index starts a new link of the chain; its odd neighbour is the
    // return trip through the link the player just crossed.
    private static readonly Point3D[,] Teleporters =
    {
        { new(6405, 2679, 0), new(6387, 2620, 0) },
        { new(6387, 2620, 0), new(6405, 2679, 0) },
        { new(6385, 2620, 0), new(6445, 2737, 0) },
        { new(6445, 2737, 1), new(6385, 2620, 0) },
        { new(6443, 2737, 1), new(6372, 2741, 0) },
        { new(6372, 2741, 0), new(6443, 2737, 0) },
        { new(6370, 2741, 0), new(6333, 2649, 0) },
        { new(6333, 2649, 0), new(6370, 2741, 0) },
        { new(6331, 2649, 0), new(6300, 2678, 0) },
        { new(6300, 2678, 0), new(6331, 2649, 0) },
        { new(6298, 2678, 0), new(6429, 2708, 0) },
        { new(6429, 2708, 0), new(6298, 2678, 0) },
        { new(6427, 2708, 0), new(6410, 2786, 0) },
        { new(6410, 2786, 0), new(6427, 2708, 0) }
    };

    private static readonly Point3D MerchantLocation = new(2497, 432, 15);
    private static readonly Point3D AdeptLocation = new(1711, 1570, 44);
    private static readonly Point3D FellowshipHouseLocation = new(1720, 1562, 17);
    private static readonly Point3D FollowerLocation = new(1720, 1565, 49);
    private static readonly Point3D SoulcleanserLocation = new(1721, 1565, 50);
    private static readonly Point3D DonationBoxLocation = new(1721, 1554, 49);
    private static readonly Point3D StaffLocation = new(1718, 1559, 55);
    private static readonly Point3D TomeLocation = new(1721, 1554, 54);
    private static readonly Point3D CoinLocation = new(1721, 1555, 59);
    private static readonly Point3D IconLocation = new(1721, 1555, 49);
    private static readonly Point3D WestCandelabraLocation = new(1719, 1554, 49);
    private static readonly Point3D EastCandelabraLocation = new(1722, 1554, 49);

    private static readonly Point3D BlackthornEntryLocation = new(6409, 2677, 0);
    private static readonly Point3D BlackthornStep2Location = new(6378, 2612, 0);
    private static readonly Point3D BlackthornStep3Location = new(6454, 2741, 0);
    private static readonly Point3D BlackthornStep4Location = new(6363, 2739, 0);
    private static readonly Point3D BlackthornStep5Location = new(6327, 2647, 0);
    private static readonly Point3D BlackthornStep6Location = new(6297, 2677, -4);
    private static readonly Point3D BlackthornStep7Location = new(6429, 2709, 0);
    private static readonly Point3D BlackthornStep8Location = new(6408, 2792, 0);

    public static void Initialize()
    {
        EventSink.ServerStarted += OnServerStarted;
    }

    private static void OnServerStarted() => Timer.StartTimer(TimeSpan.FromSeconds(12), EnsureSetup, out _);

    /// <summary>
    ///     Контент — часть мира: при старте проверяем, стоит ли вход в подземелье Блэкторна, и если
    ///     нет, разворачиваем всё заново. Проверка идемпотентная, поэтому на обычной загрузке ничего
    ///     не дублируется.
    /// </summary>
    private static void EnsureSetup()
    {
        if (HasBlackthornEntry())
        {
            return;
        }

        try
        {
            Generate();
            Logger.Information("Forsaken Foes is set up");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Forsaken Foes setup failed");
        }
    }

    private static bool HasBlackthornEntry() => AnyNearby<BlackthornEntry>(BlackthornEntryLocation, Map.Trammel);

    public static void Generate()
    {
        GenerateMap(Map.Trammel);
        GenerateMap(Map.Felucca);
    }

    private static void GenerateMap(Map map)
    {
        GenerateVendors(map);
        GenerateBlackthornDecoration(map);
        GenerateFellowshipContent(map);
    }

    private static void GenerateVendors(Map map)
    {
        if (!AnyMobile<MiningCooperativeMerchant>(MerchantLocation, map))
        {
            var merchant = new MiningCooperativeMerchant();
            merchant.MoveToWorld(MerchantLocation, map);
            merchant.Home = MerchantLocation;
            merchant.RangeHome = 5;
        }

        if (!AnyMobile<FellowshipAdept>(AdeptLocation, map))
        {
            var adept = new FellowshipAdept();
            adept.MoveToWorld(AdeptLocation, map);
            adept.Home = AdeptLocation;
            adept.RangeHome = 5;
        }
    }

    private static void GenerateBlackthornDecoration(Map map)
    {
        EnsureAddon<BlackthornEntry>(BlackthornEntryLocation, map);
        EnsureAddon<BlackthornStep2>(BlackthornStep2Location, map);
        EnsureAddon<BlackthornStep3>(BlackthornStep3Location, map);
        EnsureAddon<BlackthornStep4>(BlackthornStep4Location, map);
        EnsureAddon<BlackthornStep5>(BlackthornStep5Location, map);
        EnsureAddon<BlackthornStep6>(BlackthornStep6Location, map);
        EnsureAddon<BlackthornStep7>(BlackthornStep7Location, map);
        EnsureAddon<BlackthornStep8>(BlackthornStep8Location, map);

        for (var i = 0; i < BlockerPoints.Length; i++)
        {
            var p = BlockerPoints[i];

            if (!AnyNearby<Blocker>(p, map))
            {
                new Blocker().MoveToWorld(p, map);
            }

            if (!AnyNearby<LOSBlocker>(p, map))
            {
                new LOSBlocker().MoveToWorld(p, map);
            }
        }

        for (var i = 0; i < WorkerLocations.Length; i++)
        {
            var p = WorkerLocations[i];

            if (!AnyMobile<Worker>(p, map))
            {
                new Worker((FellowshipChain)(i + 1)).MoveToWorld(p, map);
            }
        }

        for (var t = 0; t < Teleporters.Length / 2; t++)
        {
            var p = Teleporters[t, 0];

            if (AnyNearby<BlackthornDungeonTeleporter>(p, map))
            {
                continue;
            }

            // The chain gate is fixed by the pair index rather than by a counter that only advances on placement:
            // a partially generated map must keep the same gate on every teleporter.
            var teleporter = t % 2 == 0
                ? new BlackthornDungeonTeleporter((FellowshipChain)(t / 2 + 1))
                : new BlackthornDungeonTeleporter();

            teleporter.Dest = Teleporters[t, 1];
            teleporter.MoveToWorld(p, map);
        }
    }

    private static void GenerateFellowshipContent(Map map)
    {
        EnsureAddon<TheFellowshipHouse>(FellowshipHouseLocation, map);

        if (!AnyMobile<Follower>(FollowerLocation, map))
        {
            new Follower().MoveToWorld(FollowerLocation, map);
        }

        if (!AnyNearby<EtherealSoulcleanser>(SoulcleanserLocation, map))
        {
            new EtherealSoulcleanser().MoveToWorld(SoulcleanserLocation, map);
        }

        if (!AnyNearby<FellowshipDonationBox>(DonationBoxLocation, map))
        {
            new FellowshipDonationBox().MoveToWorld(DonationBoxLocation, map);
        }

        if (!AnyNearby<TheFellowshipStaff>(StaffLocation, map))
        {
            new TheFellowshipStaff().MoveToWorld(StaffLocation, map);
        }

        if (!AnyNearby<Static>(TomeLocation, map))
        {
            new Static(0x42BF) { Hue = 1910, Name = "Фолиант Fellowship" }.MoveToWorld(TomeLocation, map);
        }

        if (!AnyNearby<Static>(CoinLocation, map))
        {
            new Static(0x2F60) { Hue = 1912, Name = "Монета Fellowship" }.MoveToWorld(CoinLocation, map);
        }

        if (!AnyNearby<Static>(IconLocation, map))
        {
            new Static(0x3155) { Hue = 1912, Name = "Икона Fellowship" }.MoveToWorld(IconLocation, map);
        }

        if (!AnyNearby<Candelabra>(WestCandelabraLocation, map))
        {
            new Candelabra { Hue = 1105 }.MoveToWorld(WestCandelabraLocation, map);
        }

        if (!AnyNearby<Candelabra>(EastCandelabraLocation, map))
        {
            new Candelabra { Hue = 1105 }.MoveToWorld(EastCandelabraLocation, map);
        }
    }

    private static void EnsureAddon<T>(Point3D p, Map map) where T : BaseAddon, new()
    {
        if (!AnyNearby<T>(p, map))
        {
            new T().MoveToWorld(p, map);
        }
    }

    private static bool AnyNearby<T>(Point3D p, Map map) where T : Item
    {
        foreach (var item in map.GetItemsInRange<T>(p, 0))
        {
            if (!item.Deleted)
            {
                return true;
            }
        }

        return false;
    }

    private static bool AnyMobile<T>(Point3D p, Map map) where T : Mobile
    {
        foreach (var mobile in map.GetMobilesInRange<T>(p, 0))
        {
            if (!mobile.Deleted)
            {
                return true;
            }
        }

        return false;
    }
}
