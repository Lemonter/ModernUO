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
    ///     Где в этой области стоит НАША вода — то есть та, которой в исходном мире не было.
    ///
    ///     Раньше это выяснялось проходом по журналу правок: у каждой записи была пометка,
    ///     и по последней записи с нашей было видно, налили мы тут или убрали. Проход был
    ///     по ВСЕМУ журналу и повторялся на каждый взмах лопаты, так что копка замедлялась
    ///     тем сильнее, чем больше уже выкопано.
    ///
    ///     Теперь вопрос решается сравнением двух миров, которые и так хранятся: водяная
    ///     статика, которой нет в слепке «как было», — наша. Это и дешевле, и честнее:
    ///     прежний ответ зависел от того, какие слова написали в причине правки.
    ///
    ///     Текучую воду (MahaonFluids) отличаем по её собственному списку клеток: графика у
    ///     неё та же, и без этого разлив осушал бы ручей на первом же пересчёте.
    /// </summary>
    private static Dictionary<(int X, int Y), int> Ours(Map map, int x0, int y0, int x1, int y1)
    {
        var ours = new Dictionary<(int X, int Y), int>();

        for (var x = x0; x <= x1; x++)
        {
            for (var y = y0; y <= y1; y++)
            {
                if (!MahaonMapEdits.IsChanged(map, x, y) || MahaonFluids.Holds(map, x, y))
                {
                    continue;
                }

                foreach (var s in MahaonMapEdits.StaticsAt(map, x, y))
                {
                    if (!IsWaterStatic(s.Graphic) || WasThereBefore(map, x, y, s))
                    {
                        continue;
                    }

                    if (!ours.TryGetValue((x, y), out var have) || s.Z > have)
                    {
                        ours[(x, y)] = s.Z;
                    }
                }
            }
        }

        return ours;
    }

    /// <summary>Стоял ли этот самый статик здесь изначально.</summary>
    private static bool WasThereBefore(Map map, int x, int y, MapStatic s)
    {
        if (!MahaonMapEdits.TryGetOriginalStatics(map.MapID, MahaonMapEdits.BlockIdFor(map, x, y), out var before))
        {
            return false;
        }

        foreach (var o in before)
        {
            if (o.X == s.X && o.Y == s.Y && o.Graphic == s.Graphic && o.Z == s.Z)
            {
                return true;
            }
        }

        return false;
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

                MahaonMapEdits.AddStatic(map, x, y, z, RandomWaterStatic(), 0);
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
                MahaonMapEdits.RemoveStatics(map, x, y, s.Graphic);
            }
        }
    }
    // ---- Почему тут сухо -------------------------------------------------------------

    /// <summary>Ближайшая вода любого рода и её уровень. distance — по Чебышёву, −1 если нет.</summary>
    public static bool NearestWater(Map map, int x, int y, int look, out int level, out int distance)
    {
        level = 0;
        distance = -1;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        for (var nx = Math.Max(0, x - look); nx <= Math.Min(map.Width - 1, x + look); nx++)
        {
            for (var ny = Math.Max(0, y - look); ny <= Math.Min(map.Height - 1, y + look); ny++)
            {
                if (nx == x && ny == y || SurfaceZ(map, nx, ny) is not { } found)
                {
                    continue;
                }

                var d = Math.Max(Math.Abs(nx - x), Math.Abs(ny - y));

                if (distance < 0 || d < distance)
                {
                    distance = d;
                    level = found;
                }
            }
        }

        return distance >= 0;
    }

    /// <summary>
    ///     Докуда дойдёт вода и что её держит.
    ///
    ///     Отвечает на единственный вопрос, который задают этой системе: почему в яме
    ///     сухо. Считает ТЕМ ЖЕ обходом, что и сам разлив, — иначе объяснение и поведение
    ///     разошлись бы, и команда уверяла бы, что всё хорошо, пока вода не идёт.
    /// </summary>
    public static bool Explain(Map map, int tx, int ty, out string reason)
    {
        reason = "здесь не считается";

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        var x0 = Math.Max(0, tx - Reach);
        var y0 = Math.Max(0, ty - Reach);
        var x1 = Math.Min(map.Width - 1, tx + Reach);
        var y1 = Math.Min(map.Height - 1, ty + Reach);

        var ours = Ours(map, x0, y0, x1, y1);
        var flooded = Spread(map, x0, y0, x1, y1, ours);

        if (IsFlooded(map, tx, ty) || flooded.ContainsKey((tx, ty)))
        {
            reason = "вода сюда доходит";
            return true;
        }

        // Кромка: ближайший к цели тайл, до которого вода дошла. Сперва среди залитых —
        // это и есть голова канавы; если не залито ничего, берём сам источник.
        (int X, int Y, int Z)? edge = null;
        var best = int.MaxValue;

        foreach (var ((fx, fy), fz) in flooded)
        {
            var d = Math.Max(Math.Abs(fx - tx), Math.Abs(fy - ty));

            if (d < best)
            {
                best = d;
                edge = (fx, fy, fz);
            }
        }

        if (edge == null)
        {
            for (var x = x0; x <= x1; x++)
            {
                for (var y = y0; y <= y1; y++)
                {
                    if (SurfaceZ(map, x, y) is not { } sz)
                    {
                        continue;
                    }

                    var d = Math.Max(Math.Abs(x - tx), Math.Abs(y - ty));

                    if (d < best)
                    {
                        best = d;
                        edge = (x, y, sz);
                    }
                }
            }
        }

        if (edge is not { } stop)
        {
            reason = $"воды в {Reach} тайлах вокруг нет — наливаться нечему";
            return false;
        }

        // Что держит: сосед кромки, который ближе к цели и воду не пустил.
        var bx = stop.X + Math.Sign(tx - stop.X);
        var by = stop.Y + Math.Sign(ty - stop.Y);

        if (bx < 0 || by < 0 || bx >= map.Width || by >= map.Height)
        {
            reason = $"вода стоит на ({stop.X}, {stop.Y}), уровень {stop.Z} — дальше край карты";
            return false;
        }

        var bz = MahaonMapEdits.LandAt(map, bx, by).Z;

        reason =
            $"вода доходит до ({stop.X}, {stop.Y}), уровень {stop.Z}, это в {best} тайлах от цели; " +
            $"держит ({bx}, {by}): дно {bz}, нужно ниже {stop.Z}";

        return false;
    }
}
