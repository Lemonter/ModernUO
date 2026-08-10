using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonGuard;

namespace Server.Systems.MahaonRaids;

/// <summary>
///     Mahaon "Raid" mechanic: every ~12 hours a pack of mobs runs into a city and holds
///     position there. Killing them grants guard points and gold. Dragon raids pay x20.
///     Per the project's "world pauses without the player" design goal, raids are only
///     rolled while at least one player is online — no one is progressing/farming while
///     you're away.
/// </summary>
public class RaidEventSystem : GenericPersistence
{
    private static RaidEventSystem _instance;

    // How often we roll for a raid, once someone is online. Randomized a bit so it's not
    // perfectly predictable.
    private static readonly TimeSpan MinInterval = TimeSpan.FromHours(10);
    private static readonly TimeSpan MaxInterval = TimeSpan.FromHours(14);

    // How often we check whether a raid is due (cheap poll, not a hot loop).
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    // Base reward for a raid kill, before the mob's RewardMultiplier is applied.
    private const int BaseGuardPoints = 2;
    private const int BaseGoldReward = 5;

    private static DateTime _nextRaidTime;
    private static Timer _pollTimer;

    private static readonly List<RaidDefinition> RaidTypes =
    [
        new RaidDefinition(
            "набег бандитов",
            [typeof(RaidBandit)],
            countMin: 6,
            countMax: 10
        ),
        new RaidDefinition(
            "высадка пиратов",
            [typeof(RaidPirate)],
            countMin: 6,
            countMax: 10
        ),
        new RaidDefinition(
            "вторжение нежити",
            [typeof(RaidSkeleton), typeof(RaidZombie)],
            countMin: 8,
            countMax: 14
        ),
        new RaidDefinition(
            "орочья орда",
            [typeof(RaidOrc)],
            countMin: 60,
            countMax: 150 // "hundreds" — start big but not server-melting; tune upward once load-tested
        ),
        new RaidDefinition(
            "атака дракона",
            [typeof(RaidDragon)],
            countMin: 1,
            countMax: 2
        )
    ];

    // Candidate city spawn points. Fill in real Mahaon-relevant coordinates as they're decided;
    // these are stock Felucca town locations as placeholders.
    private static readonly (Point3D loc, Map map, string cityName)[] RaidLocations =
    [
        (new Point3D(1438, 1613, 10), Map.Felucca, "Britain"),
        (new Point3D(2500, 634, 0), Map.Felucca, "Minoc"),
        (new Point3D(561, 964, 0), Map.Felucca, "Yew"),
        (new Point3D(4408, 1173, 0), Map.Felucca, "Moonglow"),
        (new Point3D(3697, 2218, 20), Map.Felucca, "Magincia")
    ];

    public RaidEventSystem() : base("MahaonRaids", 1)
    {
    }

    public static void Configure()
    {
        _instance = new RaidEventSystem();
    }

    public static void Initialize()
    {
        if (_nextRaidTime == default)
        {
            _nextRaidTime = Core.Now + RandomInterval();
        }

        _pollTimer = Timer.DelayCall(PollInterval, PollInterval, CheckForRaid);
    }

    private static TimeSpan RandomInterval() =>
        MinInterval + (MaxInterval - MinInterval) * Utility.RandomDouble();

    private static void CheckForRaid()
    {
        if (Core.Now < _nextRaidTime)
        {
            return;
        }

        if (NetState.Instances.Count == 0)
        {
            // No one online — don't burn the schedule while the world is "paused".
            // We'll re-check next poll instead of advancing _nextRaidTime.
            return;
        }

        SpawnRaid(RaidTypes.RandomElement());
        _nextRaidTime = Core.Now + RandomInterval();
    }

    private static void SpawnRaid(RaidDefinition raid)
    {
        var (loc, map, cityName) = RaidLocations.RandomElement();
        var count = Utility.RandomMinMax(raid.CountMin, raid.CountMax);

        for (var i = 0; i < count; i++)
        {
            var type = raid.MobTypes.RandomElement();

            if (Activator.CreateInstance(type) is not BaseCreature creature)
            {
                continue;
            }

            // Scatter within a small radius of the target point so they don't all stack
            // on one tile, but keep them holding the city rather than wandering off.
            var spawnLoc = FindNearbySpawnPoint(loc, map, 8);
            creature.MoveToWorld(spawnLoc, map);
            creature.RangeHome = 6;
            creature.Home = spawnLoc;
        }

        BroadcastRaidAlert(raid.Name, cityName);
    }

    private static Point3D FindNearbySpawnPoint(Point3D center, Map map, int radius)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var x = center.X + Utility.RandomMinMax(-radius, radius);
            var y = center.Y + Utility.RandomMinMax(-radius, radius);
            var z = map.GetAverageZ(x, y);
            var candidate = new Point3D(x, y, z);

            if (map.CanSpawnMobile(candidate))
            {
                return candidate;
            }
        }

        return center;
    }

    private static void BroadcastRaidAlert(string raidName, string cityName)
    {
        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile is PlayerMobile player)
            {
                player.SendMessage(0x22, $"Приходит весть: {raidName} обрушился на {cityName}!");
            }
        }
    }

    /// <summary>
    ///     Hooked to BaseCreature.CreatureDeathEvent (see OnEvent attribute below). Grants
    ///     guard points and gold to the killer if the creature was a raid spawn.
    /// </summary>
    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        if (bc is not IRaidSpawn raid)
        {
            return;
        }

        var killer = bc.LastKiller is BaseCreature masterCreature
            ? masterCreature.GetDamageMaster(bc)
            : bc.LastKiller;

        if (killer is not PlayerMobile player)
        {
            return;
        }

        var multiplier = raid.RewardMultiplier;

        GuardSystem.AddPoints(player, BaseGuardPoints * multiplier);

        var goldReward = BaseGoldReward * multiplier;
        if (player.Backpack != null)
        {
            CurrencyHelper.DepositCopperValue(player.Backpack, CurrencyHelper.ToCopperValue(goldReward, 0, 0));
            player.SendMessage(0x59, $"Ты получаешь {goldReward} золота за участие в набеге.");
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.Write(_nextRaidTime);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();
        _nextRaidTime = reader.ReadDateTime();
    }

    private readonly struct RaidDefinition
    {
        public readonly string Name;
        public readonly Type[] MobTypes;
        public readonly int CountMin;
        public readonly int CountMax;

        public RaidDefinition(string name, Type[] mobTypes, int countMin, int countMax)
        {
            Name = name;
            MobTypes = mobTypes;
            CountMin = countMin;
            CountMax = countMax;
        }
    }
}
