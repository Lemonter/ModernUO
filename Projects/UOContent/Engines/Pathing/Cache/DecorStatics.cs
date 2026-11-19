using System;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Engines.Pathing.Nav;
using Server.Items;
using Server.Logging;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Неподвижный декор, который ходьба видит как статику: мосты из плит над каналами, полы на
/// пустоте, стены и заграждения, поставленные предметами (Static, части аддонов, Blocker).
///
/// Кэш шагов и граф навигации строятся по статике карты и предметов не знают. В подземельях
/// это рвало граф на сотни островов: в Deceit мост через канал сложен из предметов-плит, и боты не
/// находили дороги дальше первого зала, хотя игрок проходит его, не замечая. Теперь StepProbe
/// берёт из этого индекса такие предметы наравне со статикой — и граф видит ровно то, что видит
/// проверка движения.
///
/// Индекс строится при старте по миру, а при перемещении и удалении предметов-декора правится на
/// ходу (ловушки в Static, AddonComponent, Blocker) — вместе с заплатой графа вокруг.
/// </summary>
public static class DecorStatics
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(DecorStatics));

    private static readonly Dictionary<long, Item[]>[] _cells = new Dictionary<long, Item[]>[0x100];
    private static readonly Dictionary<Item, (int map, int x, int y)> _where = new();
    private static bool _built;

    public static int Count => _where.Count;

    private static long Key(int x, int y) => ((long)x << 16) | (uint)y;

    /// <summary>Считается ли предмет частью «статики»: неподвижный декор, на котором стоят или в который упираются.</summary>
    public static bool Counts(Item item) =>
        item is Static or AddonComponent or Blocker && !item.Deleted && !item.Movable && item.Parent == null &&
        item.Map is { } map && map != Map.Internal && (item.ItemData.Surface || item.ItemData.Impassable);

    /// <summary>Предметы-декор в клетке; пусто почти везде.</summary>
    public static ReadOnlySpan<Item> At(Map map, int x, int y)
    {
        if (map == null || _cells[map.MapIndex] is not { } cells || !cells.TryGetValue(Key(x, y), out var items))
        {
            return ReadOnlySpan<Item>.Empty;
        }

        return items;
    }

    /// <summary>Строится раньше графа навигации (у того приоритет 60): заплаты накопятся и лягут при его загрузке.</summary>
    [CallPriority(55)]
    public static void Initialize()
    {
        var watch = Stopwatch.StartNew();
        var clusters = new HashSet<(Map, int, int)>();

        foreach (var item in World.Items.Values)
        {
            if (Counts(item))
            {
                Add(item);
                clusters.Add((item.Map, item.X >> 4, item.Y >> 4));
            }
        }

        _built = true;

        // Файлы графа и кэша шагов запечены без декора: области с ним пересобираются.
        foreach (var (map, cx, cy) in clusters)
        {
            NavSystem.InvalidateArea(map, cx << 4, cy << 4, 16, 16);
        }

        logger.Information(
            "Decor statics: {Items} items in {Clusters} clusters ({Elapsed:F0} ms)",
            _where.Count,
            clusters.Count,
            watch.Elapsed.TotalMilliseconds
        );
    }

    /// <summary>Предмет-декор переехал, сменил карту или появился.</summary>
    public static void Update(Item item)
    {
        if (!_built)
        {
            return;
        }

        if (_where.TryGetValue(item, out var old))
        {
            if (Counts(item) && old.map == item.Map.MapIndex && old.x == item.X && old.y == item.Y)
            {
                Touch(item.Map, item.X, item.Y); // высота могла смениться
                return;
            }

            RemoveCore(item, old);
            Touch(Map.Maps[old.map], old.x, old.y);
        }

        if (Counts(item))
        {
            Add(item);
            Touch(item.Map, item.X, item.Y);
        }
    }

    public static void Remove(Item item)
    {
        if (_built && _where.TryGetValue(item, out var old))
        {
            RemoveCore(item, old);
            Touch(Map.Maps[old.map], old.x, old.y);
        }
    }

    private static void Touch(Map map, int x, int y) => NavSystem.InvalidateArea(map, x, y, 1, 1);

    private static void Add(Item item)
    {
        var mapIndex = item.Map.MapIndex;
        var cells = _cells[mapIndex] ??= new Dictionary<long, Item[]>();
        var key = Key(item.X, item.Y);

        if (cells.TryGetValue(key, out var items))
        {
            Array.Resize(ref items, items.Length + 1);
            items[^1] = item;
            cells[key] = items;
        }
        else
        {
            cells[key] = [item];
        }

        _where[item] = (mapIndex, item.X, item.Y);
    }

    private static void RemoveCore(Item item, (int map, int x, int y) at)
    {
        _where.Remove(item);

        if (_cells[at.map] is not { } cells || !cells.TryGetValue(Key(at.x, at.y), out var items))
        {
            return;
        }

        var index = Array.IndexOf(items, item);
        if (index < 0)
        {
            return;
        }

        if (items.Length == 1)
        {
            cells.Remove(Key(at.x, at.y));
            return;
        }

        var rest = new Item[items.Length - 1];
        Array.Copy(items, 0, rest, 0, index);
        Array.Copy(items, index + 1, rest, index, items.Length - index - 1);
        cells[Key(at.x, at.y)] = rest;
    }
}
