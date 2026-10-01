using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonCities;

namespace Server.Systems.MahaonQuests;

public enum VersaAction
{
    DoNothing,
    SpawnRaid,
    Meteor,
    Earthquake,
    Plague
}

/// <summary>
///     Named after Goddess Versailles from "The Legendary Moonlight Sculptor" — the AI
///     that runs that novel's whole game world autonomously, including a long-running
///     hidden agenda steering unrelated players' quests toward one goal. This is a much
///     smaller, honest version of that idea: periodically (NOT every tick — see
///     DecisionInterval) picks ONE action from a small CLOSED list and actually executes
///     it, rather than freely generating arbitrary effects. Currently rolls randomly
///     between actions; the closed-list shape is exactly what would let
///     Systems.MahaonAi.MahaonAiTextGenerator pick the action instead once that's
///     actually wired to a running LM Studio instance — same list, same executor
///     functions, just a different thing choosing the enum value.
///
///     Was originally real features on Mahaon per the person running this shard — meteor
///     strikes, earthquakes, and a city-wide rat plague — implemented here as actual
///     mechanical effects (real damage/spawns), not just flavor text.
/// </summary>
public static class MahaonVersaSystem
{
    private static readonly TimeSpan DecisionInterval = TimeSpan.FromHours(1);

    // Meteor/Earthquake are real events with consequences — kept rarer than a plain raid
    // (which already has its own more frequent, GM-triggered path) so they stay notable.
    private static readonly (VersaAction action, double weight)[] ActionWeights =
    {
        (VersaAction.DoNothing, 0.40),
        (VersaAction.SpawnRaid, 0.25),
        (VersaAction.Meteor, 0.15),
        (VersaAction.Earthquake, 0.10),
        (VersaAction.Plague, 0.10)
    };

    public static void Initialize()
    {
        Timer.DelayCall(DecisionInterval, DecisionInterval, Decide);
    }

    private static void Decide()
    {
        var action = RollAction();

        if (action == VersaAction.DoNothing)
        {
            return;
        }

        if (CityControlSystem.Cities.Count == 0)
        {
            return;
        }

        var cityEntry = new List<string>(CityControlSystem.Cities.Keys).RandomElement();
        var (spawn, map) = CityControlSystem.Cities[cityEntry];

        switch (action)
        {
            case VersaAction.SpawnRaid:
                TriggerRaidNear(cityEntry, spawn, map);
                break;

            case VersaAction.Meteor:
                TriggerMeteor(cityEntry, spawn, map);
                break;

            case VersaAction.Earthquake:
                TriggerEarthquake(cityEntry, spawn, map);
                break;

            case VersaAction.Plague:
                TriggerPlague(cityEntry, spawn, map);
                break;
        }
    }

    private static VersaAction RollAction()
    {
        var total = 0.0;
        foreach (var (_, weight) in ActionWeights)
        {
            total += weight;
        }

        var roll = Utility.RandomDouble() * total;
        var acc = 0.0;

        foreach (var (action, weight) in ActionWeights)
        {
            acc += weight;
            if (roll <= acc)
            {
                return action;
            }
        }

        return VersaAction.DoNothing;
    }

    // -- Actions --------------------------------------------------------------------------

    private static void TriggerRaidNear(string city, Point3D center, Map map)
    {
        MahaonRaidSpawner nearestSpawner = null;
        var nearestDistSq = double.MaxValue;

        foreach (var item in World.Items.Values)
        {
            if (item is not MahaonRaidSpawner spawner || spawner.Deleted || spawner.Map != map)
            {
                continue;
            }

            var distSq = Utility.GetDistanceToSqrt(spawner.Location, center);

            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearestSpawner = spawner;
            }
        }

        if (nearestSpawner == null)
        {
            // No spawner placed near this city at all — a GM hasn't set one up here yet,
            // nothing to actually trigger. Still worth the rumor/atmosphere even without
            // a real event, same as any other "quiet night" outcome.
            MahaonBots.BotRumors.Spread($"Дозорные {city} что-то заметили у стен, но тревога оказалась ложной.");
            return;
        }

        var (raiderCount, cityLabel) = nearestSpawner.TriggerSpawn();

        if (raiderCount == 0)
        {
            return;
        }

        MahaonBots.BotRumors.Spread($"Набег на {cityLabel}! {raiderCount} врагов у стен.");
        World.Broadcast(0x22, true, $"[Версаль] {city} подвергается набегу!");
        Server.Systems.MahaonAi.MahaonForumBridge.OnRaidTriggered(cityLabel, raiderCount);
    }

    private static void TriggerMeteor(string city, Point3D center, Map map)
    {
        var target = new Point3D(
            center.X + Utility.RandomMinMax(-15, 15),
            center.Y + Utility.RandomMinMax(-15, 15),
            center.Z
        );

        Effects.PlaySound(target, map, 0x307); // explosion
        Effects.SendLocationEffect(target, map, 0x36BD, 30, 10, 0, 0); // fire/impact visual

        foreach (var mobile in map.GetMobilesInRange<Mobile>(target, 3))
        {
            if (mobile.Alive && mobile.AccessLevel == AccessLevel.Player)
            {
                mobile.Damage(Utility.RandomMinMax(10, 25));
                mobile.SendMessage(0x22, "С неба упал метеорит!");
            }
        }

        // A small reward for whoever investigates — meteoric iron.
        var ore = new IronOre(Utility.RandomMinMax(5, 15));
        ore.MoveToWorld(target, map);

        MahaonBots.BotRumors.Spread($"Метеорит упал неподалёку от {city}! Говорят, там осталась руда.");
        World.Broadcast(0x22, true, $"[Версаль] Небо над {city} прочертил огненный след...");
    }

    private static void TriggerEarthquake(string city, Point3D center, Map map)
    {
        foreach (var mobile in map.GetMobilesInRange<Mobile>(center, 20))
        {
            if (mobile.Alive && mobile.AccessLevel == AccessLevel.Player)
            {
                mobile.Damage(Utility.RandomMinMax(3, 8));
                mobile.SendMessage(0x22, "Земля содрогнулась под ногами!");
                Effects.PlaySound(mobile.Location, map, 0x21F); // rumble/impact
            }
        }

        MahaonBots.BotRumors.Spread($"В {city} произошло землетрясение — дома трясло, но обошлось без жертв.");
        World.Broadcast(0x22, true, $"[Версаль] Земля дрогнула под {city}...");
    }

    private static void TriggerPlague(string city, Point3D center, Map map)
    {
        const int ratCount = 12;

        for (var i = 0; i < ratCount; i++)
        {
            var spot = new Point3D(
                center.X + Utility.RandomMinMax(-10, 10),
                center.Y + Utility.RandomMinMax(-10, 10),
                center.Z
            );

            var rat = Utility.RandomBool() ? (BaseCreature)new MahaonPlagueRat() : new MahaonPlagueGiantRat();
            rat.MoveToWorld(spot, map);

            // Self-clears after a while rather than leaving a permanent rat problem.
            var toDelete = rat;
            Timer.DelayCall(TimeSpan.FromMinutes(20), () =>
            {
                if (!toDelete.Deleted)
                {
                    toDelete.Delete();
                }
            });
        }

        MahaonBots.BotRumors.Spread($"В {city} вспышка чумы — повсюду больные крысы, держитесь подальше от переулков.");
        World.Broadcast(0x22, true, $"[Версаль] В {city} началась эпидемия...");
    }
}
