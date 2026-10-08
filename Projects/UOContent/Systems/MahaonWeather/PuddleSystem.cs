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
    // ---- Почему лужи сделаны из кровавых пятен --------------------------------------
    //
    // Раньше здесь стояли водяные статики 0x1559 и 0x1796..0x17A8 — по названию как раз
    // «water», выбор напрашивающийся. Беда в том, что ВСЕ они помечены в tiledata флагом
    // Impassable: об такую лужу игрок спотыкается. OnMoveOver у MahaonPuddle это не
    // лечит — непроходимость решается раньше, на проверке проходимости тайла, и до
    // OnMoveOver дело просто не доходит.
    //
    // Проходимой водяной графики в клиенте нет вовсе: проверено перебором tiledata, среди
    // тайлов со словом water в названии проходимых нет ни одного.
    //
    // Зато есть кровавые пятна: проходимые, плоские, неправильной формы. Перекрашенные в
    // синий они читаются именно как лужи. Приём на шардах известный, новой графики не
    // требует, а главное — по ним можно ходить.
    //
    // Два набора оставлены: A — крупные разливы, B — мелкие брызги. Смешиваясь, они дают
    // лужам разный размер, ради чего пулы и заводились.

    // Крупные разливы.
    private static readonly int[] PoolA = { 0x122A, 0x122D };

    // Мелкие брызги и потёки.
    private static readonly int[] PoolB = { 0x122B, 0x122C, 0x122E };

    /// <summary>Синева воды. Чистая косметика — правится на глаз.</summary>
    private const int WaterHue = 0x481;

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

        WarnAboutImpassableGraphics();
    }

    /// <summary>
    ///     Ругается, если в наборах оказалась непроходимая графика.
    ///
    ///     Это не паранойя, а память о конкретной ошибке: здесь стояли водяные статики —
    ///     выбор по названию очевидный, — и все они помечены Impassable. Игроки
    ///     спотыкались о лужи, и заметить это по коду было нельзя: флаг живёт в tiledata, а
    ///     не в исходниках. Тестом такое тоже не поймать — в тестовом хосте tiledata не
    ///     загружен. Проверка на старте, на настоящих данных, единственное надёжное место.
    /// </summary>
    private static void WarnAboutImpassableGraphics()
    {
        foreach (var graphic in PoolA)
        {
            Complain(graphic);
        }

        foreach (var graphic in PoolB)
        {
            Complain(graphic);
        }

        static void Complain(int graphic)
        {
            if ((TileData.ItemTable[graphic & TileData.MaxItemValue].Flags & TileFlag.Impassable) == 0)
            {
                return;
            }

            Utility.PushColor(ConsoleColor.Red);
            Console.WriteLine(
                $"[Puddles] графика {graphic:X4} непроходима — об такую лужу игроки будут спотыкаться."
            );
            Utility.PopColor();
        }
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

                    var puddle = new MahaonPuddle(graphic, PuddleLifespan) { Hue = WaterHue };
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
