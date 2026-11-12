using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonGuard;

namespace Server.Systems.MahaonRaids;

/// <summary>
///     Расписание набегов: раз в 10–14 часов (пока кто-то в игре) на случайный город с
///     готовыми подходами идёт отряд одной из сторон — сам набег ведёт RaidDirector.
///     Убийство налётчика даёт очки гвардии. Здесь же хранится сила сторон.
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

        var sites = new List<RaidSite>();
        foreach (var site in RaidSites.All)
        {
            if (site.Approaches.Count > 0 && !RaidDirector.IsBusy(site))
            {
                sites.Add(site);
            }
        }

        if (sites.Count > 0)
        {
            RaidDirector.Start(sites.RandomElement());
        }

        _nextRaidTime = Core.Now + RandomInterval();
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
        writer.WriteEncodedInt(1); // version
        writer.Write(_nextRaidTime);

        // v1: сила сторон набегов
        writer.WriteEncodedInt(RaidDirector.Strength.Length);
        foreach (var strength in RaidDirector.Strength)
        {
            writer.WriteEncodedInt(strength);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();
        _nextRaidTime = reader.ReadDateTime();

        if (version >= 1)
        {
            var count = reader.ReadEncodedInt();
            for (var i = 0; i < count; i++)
            {
                var strength = reader.ReadEncodedInt();
                if (i < RaidDirector.Strength.Length)
                {
                    RaidDirector.Strength[i] = strength;
                }
            }
        }
    }
}
