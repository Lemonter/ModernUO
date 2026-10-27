using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonMapEdits;

/// <summary>
///     Разлив воды по рельефу.
///
///     ---- Как вода устроена именно у нас ----
///
///     Тайлдата шарда правлена: вода — это ПОЛУПРОЗРАЧНАЯ СТАТИКА (0x1796..0x17B2,
///     Translucent|Impassable|Wet, высота 0), которая кладётся поверх настоящей земли.
///     Под ней видно песок. Земляные водяные тайлы (0x00A8 и далее) остаются только там,
///     где их нарисовал художник, — в открытом море и в реках.
///
///     Это важнее, чем кажется. Раньше разлив перекрашивал в воду САМУ ЗЕМЛЮ, и высота тайла
///     уходила под поверхность воды — а дно переставало существовать и для клиента, и для
///     лопаты. Приходилось восстанавливать его по журналу правок (GroundUnder), и лопата всё
///     равно норовила ударить по поверхности вместо дна.
///
///     Со статикой всё это исчезает: земля всегда земля, её высота всегда дно, а вода лежит
///     сверху на своём уровне. Клиент получает и дно, и поверхность — то есть у него есть
///     всё, чтобы нарисовать объём.
///
///     ---- Почему это не симуляция объёма ----
///
///     Воде негде лежать по объёму: на тайл одна высота земли и одна статика воды. Но для
///     игры этого довольно — вода стоит на одном уровне, и нужен лишь обход в ширину от
///     источника по тайлам, чья земля НИЖЕ уровня воды.
///
///     Строго ниже, не «не выше»: иначе ровный луг на одной высоте с рекой залило бы до
///     горизонта. Отсюда правило игры: воду приводит канава, а не близость.
///
///     Тика нет, пересчёт идёт от правки земли. Текучую воду со временем (водопад, прилив,
///     лава) считает MahaonFluids — он пользуется этим же представлением.
/// </summary>
public static class MahaonWaterFlow
{
    /// <summary>Докуда расходится пересчёт от правки земли.</summary>
    public const int Reach = 24;

    /// <summary>Предохранитель: столько тайлов за один пересчёт мы согласны залить.</summary>
    private const int MaxFlooded = 4000;

    /// <summary>Слово, с которого начинается причина: по нему своя вода отличается от речной.</summary>
    public const string FlowTag = "разлив";

    /// <summary>Полупрозрачная вода-статика нашей тайлдаты. Высота у всех ноль.</summary>
    private static readonly int[] WaterStatics =
    {
        0x1796, 0x1797, 0x1798, 0x1799, 0x179A, 0x179B, 0x179C, 0x179D, 0x179E, 0x179F,
        0x17A0, 0x17A1, 0x17A2, 0x17A3, 0x17A4, 0x17A5, 0x17A6, 0x17A7, 0x17A8, 0x17A9,
        0x17AA, 0x17AB, 0x17AC, 0x17AD, 0x17AE, 0x17AF, 0x17B0, 0x17B1, 0x17B2
    };

    public static int RandomWaterStatic() => WaterStatics[Utility.Random(WaterStatics.Length)];

    public static bool IsWaterStatic(int graphic) => Array.IndexOf(WaterStatics, graphic) >= 0;

    /// <summary>
    ///     Стоит ли на тайле вода — любая: нарисованная художником или налитая нами.
    ///     Смотрим оба слоя, потому что у нас используются оба.
    /// </summary>
    public static bool IsFlooded(Map map, int x, int y) => SurfaceZ(map, x, y) != null;

    /// <summary>Уровень воды на тайле, или null, если воды нет. Из двух — берём верхнюю.</summary>
    public static int? SurfaceZ(Map map, int x, int y)
    {
        int? best = null;

        foreach (var s in MahaonMapEdits.StaticsAt(map, x, y))
        {
            if ((TileData.ItemTable[s.Graphic & TileData.MaxItemValue].Flags & TileFlag.Wet) != 0 &&
                (best == null || s.Z > best.Value))
            {
                best = s.Z;
            }
        }

        var land = MahaonMapEdits.LandAt(map, x, y);

        if ((TileData.LandTable[land.Graphic & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0 &&
            (best == null || land.Z > best.Value))
        {
            best = land.Z;
        }

        return best;
    }

    /// <summary>
    ///     Пересчитывает разлив вокруг правки земли и возвращает, сколько тайлов изменилось.
    ///
    ///     Считаем целиком, а записываем только разницу. Иначе каждая копка у воды сперва
    ///     осушала бы весь пруд, потом заливала заново — по две записи в журнал на каждый
    ///     тайл воды, и журнал пухнет на ровном месте.
    /// </summary>
    public static int Recalculate(Map map, int cx, int cy)
    {
        if (map == null || map == Map.Internal)
        {
            return 0;
        }

        var x0 = Math.Max(0, cx - Reach);
        var y0 = Math.Max(0, cy - Reach);
        var x1 = Math.Min(map.Width - 1, cx + Reach);
        var y1 = Math.Min(map.Height - 1, cy + Reach);

        var ours = Ours(map, x0, y0, x1, y1);
        var flooded = Spread(map, x0, y0, x1, y1, ours);

        return Apply(map, ours, flooded);
    }

    /// <summary>
    ///     Где в этой области стоит НАША вода. Одним проходом по журналу: у каждой правки
    ///     есть причина, и по последней правке с нашей пометкой видно, налили мы тут или
    ///     убрали.
    /// </summary>
    private static Dictionary<(int X, int Y), int> Ours(Map map, int x0, int y0, int x1, int y1)
    {
        var last = new Dictionary<(int X, int Y), MapEditEntry>();

        foreach (var e in MahaonMapEdits.Entries)
        {
            if (e.MapId != map.MapID || e.X < x0 || e.X > x1 || e.Y < y0 || e.Y > y1 ||
                e.Kind == MapEditKind.SetLand ||
                !e.Reason.StartsWith(FlowTag, StringComparison.Ordinal))
            {
                continue;
            }

            last[(e.X, e.Y)] = e;
        }

        var ours = new Dictionary<(int X, int Y), int>();

        foreach (var (key, entry) in last)
        {
            if (entry.Kind == MapEditKind.AddStatic)
            {
                ours[key] = entry.Z;
            }
        }

        return ours;
    }

    /// <summary>
    ///     Куда и на какой уровень дотечёт вода. Источники обходим от высокого к низкому,
    ///     чтобы спорный тайл достался той воде, что стоит выше.
    /// </summary>
    private static Dictionary<(int X, int Y), int> Spread(
        Map map, int x0, int y0, int x1, int y1, Dictionary<(int X, int Y), int> ours
    )
    {
        var flooded = new Dictionary<(int X, int Y), int>();
        var sources = new List<(int X, int Y, int Z)>();

        // Источник — чужая вода: речная, морская или налитая соседом. Своя в счёт не идёт,
        // иначе разлив кормил бы сам себя и никогда не высыхал.
        for (var x = x0; x <= x1; x++)
        {
            for (var y = y0; y <= y1; y++)
            {
                if (ours.ContainsKey((x, y)))
                {
                    continue;
                }

                if (SurfaceZ(map, x, y) is { } level)
                {
                    sources.Add((x, y, level));
                }
            }
        }

        if (sources.Count == 0)
        {
            return flooded;
        }

        sources.Sort((a, b) => b.Z.CompareTo(a.Z));

        var queue = new Queue<(int X, int Y)>();

        foreach (var (sx, sy, sz) in sources)
        {
            queue.Clear();
            queue.Enqueue((sx, sy));

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();

                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        // Только по четырём сторонам: через угол вода не перетекает, иначе
                        // она просачивается по диагонали между двумя перемычками.
                        if (dx != 0 && dy != 0 || dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        var nx = x + dx;
                        var ny = y + dy;

                        if (nx < x0 || nx > x1 || ny < y0 || ny > y1 || flooded.ContainsKey((nx, ny)))
                        {
                            continue;
                        }

                        // Чужая вода — это сам источник, её не трогаем.
                        if (!ours.ContainsKey((nx, ny)) && IsFlooded(map, nx, ny))
                        {
                            continue;
                        }

                        // Строго ниже уровня воды. На одной высоте — берег, а не дно.
                        if (MahaonMapEdits.LandAt(map, nx, ny).Z >= sz)
                        {
                            continue;
                        }

                        if (!MahaonExcavation.CanDigHere(map, nx, ny, out _))
                        {
                            continue; // город, чужой дом, скала — туда не разливаемся
                        }

                        flooded[(nx, ny)] = sz;

                        if (flooded.Count >= MaxFlooded)
                        {
                            return flooded;
                        }

                        queue.Enqueue((nx, ny));
                    }
                }
            }
        }

        return flooded;
    }

    /// <summary>Пишем только разницу: что налилось, что высохло, что поднялось в уровне.</summary>
    private static int Apply(
        Map map, Dictionary<(int X, int Y), int> ours, Dictionary<(int X, int Y), int> flooded
    )
    {
        var changed = 0;

        MahaonMapEdits.BeginBatch();

        try
        {
            foreach (var ((x, y), z) in flooded)
            {
                if (ours.TryGetValue((x, y), out var have) && have == z)
                {
                    continue; // уже налито, и ровно на этом уровне
                }

                if (ours.ContainsKey((x, y)))
                {
                    Drain(map, x, y);
                }

                MahaonMapEdits.AddStatic(map, x, y, z, RandomWaterStatic(), 0, FlowTag);
                changed++;
            }

            foreach (var ((x, y), _) in ours)
            {
                if (flooded.ContainsKey((x, y)))
                {
                    continue;
                }

                Drain(map, x, y);
                changed++;
            }
        }
        finally
        {
            MahaonMapEdits.EndBatch();
        }

        return changed;
    }

    /// <summary>
    ///     Убирает нашу воду с тайла. Снимаем по графике, а не всё подряд: на тайле могут
    ///     стоять и чужие статики, и их сносить мы не вправе.
    /// </summary>
    private static void Drain(Map map, int x, int y)
    {
        foreach (var s in MahaonMapEdits.StaticsAt(map, x, y))
        {
            if (IsWaterStatic(s.Graphic))
            {
                MahaonMapEdits.RemoveStatics(map, x, y, s.Graphic, FlowTag);
            }
        }
    }
}
