using System;
using System.Collections.Generic;
using Server.Systems.MahaonWorld;

namespace Server.Systems.MahaonFarming;

/// <summary>
///     Деление тайлов на поля и выбор культуры на поле.
///
///     ---- Зачем ----
///
///     Засев ставил грядку на каждый тайл по той графике, что там нарисована, и на одной
///     делянке оказывалась мешанина: арбузы вперемешку с капустой, потому что декоратор
///     ставил статику как попало. Поле должно быть полем — одна делянка, одна культура.
///
///     Второе: у многих делянок статики нарисованы только по краям или кусками, а середина
///     — голая вспаханная земля. Игрок видит прямоугольник пашни и справедливо ждёт, что на
///     нём что-то растёт. Поэтому поле — это не только тайлы со статикой, но и вся смежная
///     пашня (тайлы земли «furrows»).
///
///     ---- Как ----
///
///     Поле собирается заливкой по соседству: берём тайл, приклеиваем всех соседей, которые
///     тоже поле, и так пока растёт. Соседство восьмистороннее — грядки на карте лежат по
///     диагонали, и при четырёхстороннем одна делянка распалась бы на полосы.
///
///     Культуру выбираем по большинству нарисованного: если на делянке было девять арбузов
///     и одна капуста, это арбузное поле. Если статики не было вовсе (голая пашня) — культура
///     назначается от координат, а не случайно: повторный засев должен дать то же самое,
///     иначе каждый запуск перепахивал бы игрокам весь мир.
/// </summary>
public static class MahaonFieldPlots
{
    /// <summary>
    ///     Тайлы земли, считающиеся вспаханной делянкой. «furrows» в клиенте — это
    ///     0x0009..0x0015 и 0x0150..0x015C; ими и нарисованы коричневые прямоугольники
    ///     ферм.
    /// </summary>
    public static bool IsPlotGround(int landId) =>
        landId is >= 0x0009 and <= 0x0015 or >= 0x0150 and <= 0x015C;

    /// <summary>Насколько далеко от нарисованных грядок искать примыкающую пашню.</summary>
    private const int PlotSearchPadding = 4;

    /// <summary>Потолок на размер поля — страховка от заливки через полконтинента.</summary>
    private const int MaxPlotTiles = 4000;

    public sealed class Plot
    {
        public readonly List<(int X, int Y, int Z)> Tiles = new();
        public MahaonCropType Crop;
    }

    /// <summary>
    ///     Делит найденные тайлы на поля и назначает каждому культуру.
    ///
    ///     На вход — то, что нашлось в статике: координаты и вид культуры по графике.
    ///     На выход — поля, каждое со своей единственной культурой.
    /// </summary>
    public static List<Plot> Build(Map map, Dictionary<(int X, int Y), (int Z, MahaonCropKind Kind)> statics)
    {
        var plots = new List<Plot>();
        var visited = new HashSet<(int, int)>();

        foreach (var (start, _) in statics)
        {
            if (visited.Contains(start))
            {
                continue;
            }

            var plot = Flood(map, start, statics, visited);

            if (plot.Tiles.Count > 0)
            {
                plots.Add(plot);
            }
        }

        return plots;
    }

    /// <summary>Заливка одного поля из точки.</summary>
    private static Plot Flood(
        Map map,
        (int X, int Y) start,
        Dictionary<(int X, int Y), (int Z, MahaonCropKind Kind)> statics,
        HashSet<(int, int)> visited
    )
    {
        var plot = new Plot();
        var votes = new Dictionary<MahaonCropKind, int>();
        var queue = new Queue<(int X, int Y)>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0 && plot.Tiles.Count < MaxPlotTiles)
        {
            var (x, y) = queue.Dequeue();

            var hasStatic = statics.TryGetValue((x, y), out var info);
            var z = hasStatic ? info.Z : map.GetAverageZ(x, y);

            plot.Tiles.Add((x, y, z));

            if (hasStatic)
            {
                votes.TryGetValue(info.Kind, out var n);
                votes[info.Kind] = n + 1;
            }

            // Соседство восьмистороннее: грядки на карте лежат по диагонали, и при
            // четырёхстороннем одна делянка распалась бы на полосы.
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    var nx = x + dx;
                    var ny = y + dy;

                    if (nx < 0 || ny < 0 || nx >= map.Width || ny >= map.Height)
                    {
                        continue;
                    }

                    if (!visited.Add((nx, ny)))
                    {
                        continue;
                    }

                    if (statics.ContainsKey((nx, ny)) || IsPlotGround(map.Tiles.GetLandTile(nx, ny).ID))
                    {
                        queue.Enqueue((nx, ny));
                    }
                    else
                    {
                        // Не поле — но метку снимаем, иначе соседнее поле не сможет
                        // подойти к этому тайлу со своей стороны и потеряет край.
                        visited.Remove((nx, ny));
                    }
                }
            }
        }

        plot.Crop = ChooseCrop(votes, start);

        Erode(plot.Tiles);

        return plot;
    }

    /// <summary>
    ///     Снимает один тайл по периметру делянки.
    ///
    ///     Причина не в красоте межи, а в размерах спрайтов. Грядка рисуется куда крупнее
    ///     своего тайла — пшеница, кукуруза, тыквенные плети занимают заметно больше
    ///     клетки, — и посев вплотную до края вылезает за пашню на траву. Поле выглядит
    ///     так, будто его посеяли мимо.
    ///
    ///     Отступ в один тайл прячет этот выход: спрайт крайней грядки ложится на ту самую
    ///     полосу пашни, которую мы освободили, а не на газон за ней.
    ///
    ///     Край определяем по неполному окружению: тайл, у которого есть все восемь соседей
    ///     внутри делянки, — внутренний, остальные — периметр. Считается по ИСХОДНОМУ
    ///     набору, а не по мере удаления, иначе размывание пошло бы вглубь и съело узкие
    ///     делянки целиком.
    /// </summary>
    private static void Erode(List<(int X, int Y, int Z)> tiles)
    {
        if (tiles.Count == 0)
        {
            return;
        }

        var occupied = new HashSet<(int, int)>(tiles.Count);

        foreach (var (x, y, _) in tiles)
        {
            occupied.Add((x, y));
        }

        var inner = new List<(int X, int Y, int Z)>(tiles.Count);

        foreach (var tile in tiles)
        {
            var surrounded = true;

            for (var dx = -1; dx <= 1 && surrounded; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if ((dx != 0 || dy != 0) && !occupied.Contains((tile.X + dx, tile.Y + dy)))
                    {
                        surrounded = false;
                        break;
                    }
                }
            }

            if (surrounded)
            {
                inner.Add(tile);
            }
        }

        // Узкую делянку размывание съело бы подчистую. Лучше засеять её как есть, чем
        // оставить игроку голый прямоугольник без единого ростка.
        if (inner.Count > 0)
        {
            tiles.Clear();
            tiles.AddRange(inner);
        }
    }

    /// <summary>
    ///     Культура поля: по большинству нарисованного, а при отсутствии статики — от
    ///     координат.
    ///
    ///     От координат, а не случайно, намеренно: засев идемпотентен, и повторный запуск
    ///     обязан дать тот же результат. Со случайным выбором каждый прогон перепахивал бы
    ///     игрокам весь мир заново.
    /// </summary>
    private static MahaonCropType ChooseCrop(Dictionary<MahaonCropKind, int> votes, (int X, int Y) seed)
    {
        if (votes.Count > 0)
        {
            var best = default(MahaonCropKind);
            var bestCount = -1;

            foreach (var (kind, count) in votes)
            {
                // При равенстве голосов берём меньший код — лишь бы ответ был устойчивым.
                if (count > bestCount || (count == bestCount && (int)kind < (int)best))
                {
                    best = kind;
                    bestCount = count;
                }
            }

            return MahaonCrops.SeasonalTypeFor(best);
        }

        var all = Enum.GetValues<MahaonCropType>();
        var hash = seed.X * 73856093 ^ seed.Y * 19349663;

        return all[Math.Abs(hash) % all.Length];
    }

    /// <summary>
    ///     Заливка голой делянки от указанной точки — для ручной посадки там, где
    ///     нарисованной статики нет вовсе и автоматический засев такое поле не находит.
    /// </summary>
    public static List<(int X, int Y, int Z)> FloodBarePlot(Map map, int x, int y)
    {
        var tiles = new List<(int X, int Y, int Z)>();

        if (map == null || !IsPlotGround(map.Tiles.GetLandTile(x, y).ID))
        {
            return tiles;
        }

        var visited = new HashSet<(int, int)> { (x, y) };
        var queue = new Queue<(int X, int Y)>();

        queue.Enqueue((x, y));

        while (queue.Count > 0 && tiles.Count < MaxPlotTiles)
        {
            var (cx, cy) = queue.Dequeue();

            tiles.Add((cx, cy, map.GetAverageZ(cx, cy)));

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    var nx = cx + dx;
                    var ny = cy + dy;

                    if (nx < 0 || ny < 0 || nx >= map.Width || ny >= map.Height || !visited.Add((nx, ny)))
                    {
                        continue;
                    }

                    if (IsPlotGround(map.Tiles.GetLandTile(nx, ny).ID))
                    {
                        queue.Enqueue((nx, ny));
                    }
                }
            }
        }

        // Тот же отступ от края, что и при автоматическом засеве: спрайты грядок крупнее
        // тайла и вылезли бы за пашню.
        Erode(tiles);

        return tiles;
    }
}
