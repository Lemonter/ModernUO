using System;
using System.Collections.Generic;
using Server.Engines.Spawners;
using Server.Logging;
using Server.Mobiles;

namespace Server.Systems.Bots;

public sealed class HuntSpot
{
    public HuntSpot(Map map, Point3D center, int range, int fame)
    {
        Map = map;
        Center = center;
        Range = range;
        Fame = fame;
    }

    public Map Map { get; }
    public Point3D Center { get; }
    public int Range { get; }

    /// <summary>The toughest creature that spawns here, by fame.</summary>
    public int Fame { get; }
}

/// <summary>
/// Where to hunt: every spawner in the world that spawns something a bot may fairly kill, rated by
/// the fame of its toughest creature. Built once the world is loaded; creature fame is read from a
/// throwaway instance of each type, created and deleted once and cached.
/// </summary>
public static class HuntingAtlas
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(HuntingAtlas));

    private static readonly List<HuntSpot> _spots = [];
    private static readonly Dictionary<Type, int> _fameByType = new(); // -1 = not prey

    public static IReadOnlyList<HuntSpot> Spots => _spots;

    public static void Rebuild()
    {
        _spots.Clear();

        foreach (var map in Map.AllMaps)
        {
            if (map == null || map == Map.Internal)
            {
                continue;
            }

            var bounds = new Rectangle2D(0, 0, map.Width, map.Height);

            foreach (var spawner in map.GetItemsInBounds<BaseSpawner>(bounds))
            {
                var fame = -1;
                foreach (var entry in spawner.Entries)
                {
                    fame = Math.Max(fame, FameOf(entry.SpawnedName));
                }

                Add(map, spawner.Location, spawner.WalkingRange, fame);
            }

            foreach (var spawner in map.GetItemsInBounds<XmlSpawner>(bounds))
            {
                var fame = -1;
                foreach (var obj in spawner.SpawnObjects)
                {
                    fame = Math.Max(fame, FameOf(obj.TypeName));
                }

                Add(map, spawner.Location, spawner.HomeRange, fame);
            }
        }

        logger.Information("Hunting atlas: {Spots} hunting spots from spawners", _spots.Count);
    }

    private static void Add(Map map, Point3D location, int range, int fame)
    {
        if (fame >= 0)
        {
            _spots.Add(new HuntSpot(map, location, Math.Clamp(range, 4, 30), fame));
        }
    }

    /// <summary>Fame of a spawnable type name if it is fair prey, else -1.</summary>
    private static int FameOf(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return -1;
        }

        // XmlSpawner lines may carry arguments and properties after the type name.
        var end = typeName.IndexOfAny([' ', '/', ',']);
        var type = AssemblyHandler.FindTypeByName(end < 0 ? typeName : typeName[..end]);

        if (type == null || !typeof(BaseCreature).IsAssignableFrom(type))
        {
            return -1;
        }

        if (_fameByType.TryGetValue(type, out var cached))
        {
            return cached;
        }

        var fame = -1;
        try
        {
            if (Activator.CreateInstance(type) is BaseCreature creature)
            {
                fame = IsFairPrey(creature) ? Math.Max(creature.Fame, 1) : -1;
                creature.Delete();
            }
        }
        catch
        {
            // A creature that can't be built without arguments isn't spawned by name anyway.
        }

        _fameByType[type] = fame;
        return fame;
    }

    /// <summary>
    /// What a bot hunts: monsters, and animals. Townsfolk, vendors, escorts and the blessed are
    /// not prey — killing them is murder or impossible.
    /// </summary>
    public static bool IsFairPrey(BaseCreature c) =>
        c is not BaseVendor && !c.Blessed && !c.Controlled && !c.IsInvulnerable &&
        (c.Karma <= 0 || c.Body.IsAnimal) && c is not BaseEscortable;

    /// <summary>A spot near <paramref name="near"/> worth this bot's time: as tough as it can take,
    /// not trivially weak; a random pick among the closest few.</summary>
    public static HuntSpot Pick(Map map, Point3D near, int range, int maxFame)
    {
        var minFame = maxFame / 8;
        HuntSpot best = null;
        var bestScore = double.MaxValue;

        foreach (var spot in _spots)
        {
            if (spot.Map != map || spot.Fame > maxFame || spot.Fame < minFame)
            {
                continue;
            }

            var dist = spot.Center.GetDistanceToSqrt(near);
            if (dist > range)
            {
                continue;
            }

            // Closer is better; tougher (closer to the bot's limit) is better; a little noise
            // keeps every bot from walking to the same spawner.
            var score = dist * (1.5 - (double)spot.Fame / maxFame) * (0.7 + Utility.RandomDouble() * 0.6);
            if (score < bestScore)
            {
                bestScore = score;
                best = spot;
            }
        }

        return best;
    }
}
