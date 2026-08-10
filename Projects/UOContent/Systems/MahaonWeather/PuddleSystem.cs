using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.Systems.MahaonWeather;

/// <summary>
///     Spawns decorative puddles near online players while it's raining — only on
///     passable outdoor ground (not indoors, not in a dungeon/cave), same rules a real
///     rain-fx script would check. Two graphic pools ("A" and "B" per the request); every
///     puddle picks one pool, then one graphic from it, at random.
/// </summary>
public static class PuddleSystem
{
    // Type A — 4 flat colors, no variants.
    private static readonly int[] PoolA = { 0x17A6, 0x17A8, 0x17A5, 0x17A7 };

    // Type B — 5 color groups; every listed ID (base + bracketed alternates) goes into one
    // flat pool. If the alternates were actually meant to be placed together as a
    // multi-tile cluster (e.g. splash + ripple) rather than picked as independent
    // single-tile options, this'll need revisiting — not something I can tell apart from
    // hex IDs alone.
    private static readonly int[] PoolB =
    {
        0x1559, 0x1797, 0x1798, 0x1799, 0x179A, 0x179B, 0x179C, // grey
        0x179D, 0x179E,                                         // brown
        0x179F, 0x17A0,                                         // purple
        0x17A1, 0x17A2,                                         // pink
        0x17A3, 0x17A4                                          // white
    };

    private static readonly TimeSpan SpawnTick = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan PuddleLifespan = TimeSpan.FromMinutes(3);

    private const int SpawnRadius = 10; // roll a spot within this many tiles of a player
    private const int MaxAttemptsPerPlayer = 4; // give up on this player's roll after this many bad spots
    private const double SpawnChancePerTick = 0.35; // per eligible player, per tick

    private static readonly List<MahaonPuddle> ActivePuddles = new();
    private static Timer _spawnTimer;

    public static void Configure()
    {
        WeatherSystem.OnWeatherChanged += OnWeatherChanged;
    }

    private static void OnWeatherChanged(MahaonWeather weather)
    {
        if (weather == MahaonWeather.Rain)
        {
            _spawnTimer ??= Timer.DelayCall(SpawnTick, SpawnTick, SpawnTick_OnTick);
            return;
        }

        // Rain stopped (or it's snowing/storming/clear) — no more puddles, and clear out
        // whatever's already down instead of leaving them to melt away on their own timers.
        _spawnTimer?.Stop();
        _spawnTimer = null;

        foreach (var puddle in ActivePuddles)
        {
            if (!puddle.Deleted)
            {
                puddle.Delete();
            }
        }

        ActivePuddles.Clear();
    }

    private static void SpawnTick_OnTick()
    {
        ActivePuddles.RemoveAll(p => p.Deleted);

        foreach (var player in World.Mobiles.Values)
        {
            if (player is not PlayerMobile pm || pm.NetState == null || pm.Map == null || !pm.Alive)
            {
                continue;
            }

            if (Utility.RandomDouble() >= SpawnChancePerTick)
            {
                continue;
            }

            TrySpawnNear(pm);
        }
    }

    private static void TrySpawnNear(Mobile player)
    {
        var map = player.Map;

        for (var attempt = 0; attempt < MaxAttemptsPerPlayer; attempt++)
        {
            var centerX = player.X + Utility.RandomMinMax(-SpawnRadius, SpawnRadius);
            var centerY = player.Y + Utility.RandomMinMax(-SpawnRadius, SpawnRadius);

            if (!IsValidPuddleSpot(map, centerX, centerY, out _))
            {
                continue;
            }

            // A single tile read as a speck, not a puddle — lay down a small cluster
            // instead, either 2x2 (4 tiles) or 3x3 (9 tiles), picked randomly per puddle
            // for a bit of size variety. Every tile in the cluster is checked on its own —
            // a cell that fails the outdoor/passable/dungeon check is just skipped, not a
            // reason to abort the whole cluster (edges of a valid area shouldn't block the
            // rest of it from getting puddles).
            var clusterSize = Utility.RandomBool() ? 2 : 3;
            var placed = 0;

            for (var dx = 0; dx < clusterSize; dx++)
            {
                for (var dy = 0; dy < clusterSize; dy++)
                {
                    var x = centerX + dx;
                    var y = centerY + dy;

                    if (!IsValidPuddleSpot(map, x, y, out var z))
                    {
                        continue;
                    }

                    var pool = Utility.RandomBool() ? PoolA : PoolB;
                    var graphic = pool.RandomElement();

                    var puddle = new MahaonPuddle(graphic, PuddleLifespan);
                    puddle.MoveToWorld(new Point3D(x, y, z), map);
                    ActivePuddles.Add(puddle);
                    placed++;
                }
            }

            if (placed > 0)
            {
                return;
            }
        }
    }

    /// <summary>Passable, outdoor (clear line of sight straight up — no roof), and not
    /// inside a dungeon/cave region.</summary>
    private static bool IsValidPuddleSpot(Map map, int x, int y, out int z)
    {
        z = 0;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        var landTile = map.Tiles.GetLandTile(x, y);
        var flags = TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags;

        if ((flags & TileFlag.Impassable) != 0)
        {
            return false; // not walkable ground
        }

        z = map.GetAverageZ(x, y);
        var loc = new Point3D(x, y, z);

        if (!map.CanFit(x, y, z, 16))
        {
            return false; // something's already occupying this spot
        }

        if (Region.Find(loc, map).IsPartOf<DungeonRegion>())
        {
            return false; // dungeon/cave
        }

        // No roof overhead — a blocked line of sight straight up means something (a house
        // roof, a cave ceiling, whatever) is covering this tile.
        return map.LineOfSight(new Point3D(x, y, z + 1), new Point3D(x, y, z + 200));
    }
}
