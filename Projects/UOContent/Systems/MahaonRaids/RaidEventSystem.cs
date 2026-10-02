using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonGuard;

namespace Server.Systems.MahaonRaids;

/// <summary>
///     Mahaon "Raid" mechanic: every ~12 hours a pack of mobs runs into a city and holds
///     position there. Killing them grants guard points (no gold — removed per the shard
///     owner's ask). Dragon raids pay x20. Per the project's "world pauses without the
///     player" design goal, raids are only rolled while at least one player is online — no
///     one is progressing/farming while you're away.
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
    // Mahaon: raised per the shard owner's ask ("повысь награды за набеги в очках
    // гвардии") — was 2, felt small next to GuardQuestSystem's own 20-per-turn-in pacing.
    private const int BaseGuardPoints = 10;

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

    /// <summary>How long until the next scheduled raid roll — never negative (a raid due
    /// but not yet rolled, e.g. because no one was online, shows as "any moment now"
    /// rather than a stale negative timer). Used by MahaonGuardSergeantGump.</summary>
    public static TimeSpan GetTimeUntilNextRaid()
    {
        var remaining = _nextRaidTime - Core.Now;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    /// <summary>Live count of raid-spawned creatures still standing anywhere in the world
    /// right now — a cheap enough scan for an occasional gump open, not a hot path. Used
    /// to show "a raid is happening right now" instead of just a countdown.</summary>
    public static int GetActiveRaiderCount()
    {
        var count = 0;

        foreach (var mobile in World.Mobiles.Values)
        {
            if (mobile is IRaidSpawn and BaseCreature { Deleted: false, Alive: true })
            {
                count++;
            }
        }

        return count;
    }

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

        RaidAlarm.Raise(map, loc, cityName);
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
    [OnEvent(nameof(CreatureEvents.CreatureDeathEvent))]
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

        var pointsReward = BaseGuardPoints * multiplier;
        GuardSystem.AddPoints(player, pointsReward);
        player.SendMessage(0x59, $"Ты получаешь {pointsReward} очков гвардии за участие в набеге.");
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
