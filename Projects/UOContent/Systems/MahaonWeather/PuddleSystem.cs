using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Systems.MahaonMapEdits;

// MahaonMapEdits — и пространство имён, и класс в нём; без псевдонима имя разрешается
// в пространство.
using MapEdits = Server.Systems.MahaonMapEdits.MahaonMapEdits;

namespace Server.Systems.MahaonWeather;

/// <summary>
///     Дождевая вода: во что дождь наливается и как потом высыхает.
///
///     ---- Что тут изменилось и почему ----
///
///     Раньше это была чистая декорация: во время дождя рядом с игроками рассыпались
///     кляксы «куда попало». Причём из КРОВАВЫХ ПЯТЕН — потому что вся водяная графика
///     клиента помечена Impassable, об неё спотыкаешься, а перекрашенная в синее кровь
///     хотя бы проходима.
///
///     Оба решения теперь не нужны.
///
///     Во-первых, у земли появился рельеф: лопата роет ямы, и вода в мире стоит на своём
///     уровне (MahaonWaterFlow). Значит дождь обязан наливаться ТУДА, КУДА ВОДА И ТЕЧЁТ —
///     в низины. Лужа посреди бугра выглядит ошибкой, потому что ошибкой и является.
///
///     Во-вторых, проходимая водяная графика в клиенте всё-таки есть: 0x1FA3..0x1FCA,
///     «wave» — Translucent|Wet|Animation, высота 0, Impassable НЕ стоит. Прошлый перебор
///     её не нашёл, потому что искал по слову water в названии. Это настоящая анимированная
///     вода, по которой можно ходить, — ровно то, чем мелкая лужа и должна быть.
///
///     ---- Правила ----
///
///     Наливается только в котловину: тайл, у которого ВСЕ соседи вокруг выше. Дно ямы
///     заливается целиком, борта — нет. Ровное поле котловиной не считается, иначе лужи
///     расползлись бы по всему лугу.
///
///     Дождевая вода ВРЕМЕННАЯ: кончился дождь — через минуту её нет. Рыбы в ней нет тоже
///     (см. HarvestTarget): это лужа, а не пруд. Настоящий пруд получается иначе — копкой
///     канавы к реке, и он остаётся.
/// </summary>
public static class PuddleSystem
{
    /// <summary>
    ///     Проходимая анимированная вода. Единственный такой набор в нашей тайлдате:
    ///     Translucent|Wet|Animation без Impassable.
    ///
    ///     Если на глаз покажется слишком «морской пеной» — рядом есть ещё два проходимых
    ///     мокрых набора: болотная рябь 0x320D..0x324A и всплеск 0x5675. Подменяется
    ///     здесь, одной строкой.
    /// </summary>
    private static readonly int[] RainwaterGraphics =
    {
        0x1FA3, 0x1FA4, 0x1FA5, 0x1FA6, 0x1FA7, 0x1FA8, 0x1FA9, 0x1FAA, 0x1FAB, 0x1FAC,
        0x1FAD, 0x1FAE, 0x1FAF, 0x1FB0, 0x1FB1, 0x1FB2, 0x1FB3, 0x1FB4, 0x1FB5, 0x1FB6,
        0x1FB7, 0x1FB8, 0x1FB9, 0x1FBA, 0x1FBB, 0x1FBC, 0x1FBD, 0x1FBE, 0x1FBF, 0x1FC0,
        0x1FC1, 0x1FC2, 0x1FC3, 0x1FC4, 0x1FC5, 0x1FC6, 0x1FC7, 0x1FC8, 0x1FC9, 0x1FCA
    };

    /// <summary>Дождевая ли это графика. Нужно рыбалке: в луже рыбы нет.</summary>
    public static bool IsRainwater(int graphic) => Array.IndexOf(RainwaterGraphics, graphic) >= 0;

    private static readonly TimeSpan SpawnTick = TimeSpan.FromSeconds(6);

    /// <summary>Сколько держится вода после того, как дождь кончился.</summary>
    public static readonly TimeSpan DryingDelay = TimeSpan.FromMinutes(1);

    private const int SpawnRadius = 12;        // где ищем низину вокруг игрока
    private const int MaxAttemptsPerPlayer = 6; // столько мест пробуем за такт
    private const double SpawnChancePerTick = 0.5;

    /// <summary>Сколько тайлов дна мы согласны считать одной лужей. Больше — это уже
    /// равнина, а не котловина, и заливать её не надо.</summary>
    private const int MaxBasin = 40;

    /// <summary>Предохранитель на весь мир.</summary>
    private const int MaxPuddles = 600;

    private static readonly List<MahaonPuddle> ActivePuddles = new();
    private static readonly HashSet<(int Map, int X, int Y)> Taken = new();

    private static Timer _spawnTimer;

    /// <summary>Номер текущей «погодной эпохи»: по нему отложенное высыхание понимает,
    /// что дождь успел начаться заново, и отменяет себя само.</summary>
    private static int _epoch;

    public static int Count => ActivePuddles.Count;

    public static void Configure()
    {
        WeatherSystem.OnWeatherChanged += OnWeatherChanged;

        WarnAboutImpassableGraphics();
    }

    /// <summary>
    ///     Ругается, если в наборе оказалась непроходимая графика.
    ///
    ///     Это не паранойя, а память о конкретной ошибке: здесь стояли водяные статики —
    ///     выбор по названию очевидный, — и все они помечены Impassable. Игроки
    ///     спотыкались о лужи, и заметить это по коду было нельзя: флаг живёт в tiledata, а
    ///     не в исходниках. Тестом такое тоже не поймать — в тестовом хосте tiledata не
    ///     загружен. Проверка на старте, на настоящих данных, единственное надёжное место.
    /// </summary>
    private static void WarnAboutImpassableGraphics()
    {
        foreach (var graphic in RainwaterGraphics)
        {
            if ((TileData.ItemTable[graphic & TileData.MaxItemValue].Flags & TileFlag.Impassable) == 0)
            {
                continue;
            }

            Utility.PushColor(ConsoleColor.Red);
            Console.WriteLine(
                $"[Дождь] графика {graphic:X4} непроходима — об такую лужу игроки будут спотыкаться."
            );
            Utility.PopColor();
        }
    }

    private static bool Raining(MahaonWeather weather) =>
        weather is MahaonWeather.Rain or MahaonWeather.Storm;

    private static void OnWeatherChanged(MahaonWeather weather)
    {
        _epoch++;

        if (Raining(weather))
        {
            _spawnTimer ??= Timer.DelayCall(SpawnTick, SpawnTick, SpawnTick_OnTick);
            return;
        }

        _spawnTimer?.Stop();
        _spawnTimer = null;

        // Не сразу: вода уходит в землю не мгновенно. Если за эту минуту дождь пойдёт
        // снова, эпоха сменится и высыхание отменится само.
        var epoch = _epoch;

        Timer.DelayCall(
            DryingDelay,
            () =>
            {
                if (epoch == _epoch)
                {
                    DryEverything();
                }
            }
        );
    }

    public static void DryEverything()
    {
        foreach (var puddle in ActivePuddles)
        {
            if (!puddle.Deleted)
            {
                puddle.Delete();
            }
        }

        ActivePuddles.Clear();
        Taken.Clear();
    }

    private static void SpawnTick_OnTick()
    {
        ActivePuddles.RemoveAll(p => p.Deleted);

        if (ActivePuddles.Count >= MaxPuddles)
        {
            return;
        }

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
            var x = player.X + Utility.RandomMinMax(-SpawnRadius, SpawnRadius);
            var y = player.Y + Utility.RandomMinMax(-SpawnRadius, SpawnRadius);

            if (!TryFindBasin(map, x, y, out var floor, out var level))
            {
                continue;
            }

            var placed = 0;

            foreach (var (bx, by) in floor)
            {
                var puddle = new MahaonPuddle(RainwaterGraphics.RandomElement());
                puddle.MoveToWorld(new Point3D(bx, by, level), map);

                ActivePuddles.Add(puddle);
                Taken.Add((map.MapID, bx, by));
                placed++;
            }

            if (placed > 0)
            {
                return;
            }
        }
    }

    /// <summary>
    ///     Котловина ли тут, и если да — какое у неё дно.
    ///
    ///     Разливаемся по соседям РОВНО ТОЙ ЖЕ высоты: это плоское дно ямы. Наткнулись на
    ///     соседа ниже — значит мы не на дне, настоящее дно где-то там, и эта попытка не
    ///     наша. Дно разрослось больше MaxBasin — значит это не яма, а равнина.
    /// </summary>
    private static bool TryFindBasin(Map map, int x, int y, out List<(int X, int Y)> floor, out int level)
    {
        floor = null;
        level = 0;

        if (map == null || map == Map.Internal || x < 0 || y < 0 || x >= map.Width || y >= map.Height)
        {
            return false;
        }

        if (!IsOpenGround(map, x, y))
        {
            return false;
        }

        level = MahaonExcavation.GroundZ(map, x, y);

        var found = new List<(int X, int Y)>();
        var seen = new HashSet<(int X, int Y)> { (x, y) };
        var queue = new Queue<(int X, int Y)>();
        var walled = false;

        queue.Enqueue((x, y));

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            found.Add((cx, cy));

            if (found.Count > MaxBasin)
            {
                return false; // равнина, а не яма
            }

            Span<(int X, int Y)> around = [(cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)];

            foreach (var (nx, ny) in around)
            {
                if (nx < 0 || ny < 0 || nx >= map.Width || ny >= map.Height || !seen.Add((nx, ny)))
                {
                    continue;
                }

                var nz = MahaonExcavation.GroundZ(map, nx, ny);

                if (nz < level)
                {
                    return false; // ниже нас — значит дно не здесь
                }

                if (nz > level)
                {
                    walled = true; // борт ямы
                    continue;
                }

                if (!IsOpenGround(map, nx, ny))
                {
                    return false; // дно упирается в крышу или в чужую стройку — не наша лужа
                }

                queue.Enqueue((nx, ny));
            }
        }

        if (!walled)
        {
            return false;
        }

        floor = found;
        return true;
    }

    /// <summary>Проходимая земля под открытым небом, без своей воды и без чужого добра.</summary>
    private static bool IsOpenGround(Map map, int x, int y)
    {
        if (Taken.Contains((map.MapID, x, y)))
        {
            return false;
        }

        // Настоящая вода уже стоит — доливать нечего.
        if (MahaonWaterFlow.IsFlooded(map, x, y))
        {
            return false;
        }

        var land = MapEdits.LandAt(map, x, y);

        if ((TileData.LandTable[land.Graphic & TileData.MaxLandValue].Flags & TileFlag.Impassable) != 0)
        {
            return false;
        }

        var z = land.Z;
        var loc = new Point3D(x, y, z);

        if (!map.CanFit(x, y, z, 16))
        {
            return false;
        }

        if (Region.Find(loc, map).IsPartOf<DungeonRegion>())
        {
            return false; // под землёй дождя нет
        }

        // Крыша над головой — значит не льёт.
        return map.LineOfSight(new Point3D(x, y, z + 1), new Point3D(x, y, z + 200));
    }
}
