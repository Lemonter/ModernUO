using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonWorld;

/// <summary>
///     Планировка подвала, общая для городских квартир (MahaonCityHouse) и ставящихся домов
///     (BaseHouse, MahaonHouseBasementSystem): стены, факелы, угол со ступенями и минимальный
///     размер. Пол, люк и учёт деталей — у каждого дома свои.
///
///     Стены — только северная и западная. Каменная стена UO стоит на ЮЖНОМ или ВОСТОЧНОМ
///     ребре своей клетки, поэтому северную стену пола несут клетки к северу от него («\»), а
///     западную — клетки к западу («/»). Южную и восточную стены камера видит спереди, и они
///     закрывали бы подвал — их нет, а уйти с пола всё равно некуда: земля в пятидесяти
///     единицах выше.
///
///     Факелы висят на этих же стенах через клетку. Как в декорациях городов (Минок, Бакс):
///     настенный факел стоит не на клетке стены, а на клетке пола перед ней, на пятнадцать
///     выше пола — картинка факела сама прижата к дальнему ребру.
/// </summary>
public static class MahaonBasementLayout
{
    /// <summary>Подвал не уже стольких клеток ни с одной стороны, как бы мал ни был дом.</summary>
    public const int MinSide = 4;

    // Тонкая каменная стена — те же детали, что у старой ограды: «\» идёт вдоль X, «/» вдоль Y.
    public const int WallAlongX = 0x001C;
    public const int WallAlongY = 0x001B;

    // Высота факела над полом — по декорациям городов.
    private const int TorchHeight = 15;

    /// <summary>
    ///     Добирает подвал до <see cref="MinSide"/> клеток по каждой стороне: прямоугольник
    ///     вокруг пола растёт поровну в обе стороны, и под пол уходят клетки, которые
    ///     <paramref name="allowed"/> разрешает (не чужой подвал). Возвращает добавленные.
    /// </summary>
    public static List<Point2D> ExpandToMinimum(HashSet<Point2D> columns, Func<Point2D, bool> allowed)
    {
        var added = new List<Point2D>();
        if (columns.Count == 0)
        {
            return added;
        }

        GetBounds(columns, out var minX, out var minY, out var maxX, out var maxY);

        Grow(ref minX, ref maxX);
        Grow(ref minY, ref maxY);

        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                var p = new Point2D(x, y);
                if (!columns.Contains(p) && allowed(p))
                {
                    columns.Add(p);
                    added.Add(p);
                }
            }
        }

        return added;
    }

    // Ширина меньше минимума — по клетке в обе стороны, пока не хватит (лишняя — на восток/юг).
    private static void Grow(ref int min, ref int max)
    {
        var lowSide = true;
        while (max - min + 1 < MinSide)
        {
            if (lowSide)
            {
                max++;
            }
            else
            {
                min--;
            }

            lowSide = !lowSide;
        }
    }

    public static void GetBounds(HashSet<Point2D> columns, out int minX, out int minY, out int maxX, out int maxY)
    {
        minX = minY = int.MaxValue;
        maxX = maxY = int.MinValue;

        foreach (var c in columns)
        {
            minX = Math.Min(minX, c.X);
            minY = Math.Min(minY, c.Y);
            maxX = Math.Max(maxX, c.X);
            maxY = Math.Max(maxY, c.Y);
        }
    }

    /// <summary>
    ///     От какой высоты отсчитывать глубину подвала: от пола дома или от земли под ним —
    ///     что ниже. Квартира бывает на втором этаже (пол на 50 выше улицы), и подвал «на
    ///     пятьдесят ниже пола» лёг бы ровно на мостовую, к соседям первого этажа.
    /// </summary>
    public static int Below(Map map, HashSet<Point2D> columns, int floorZ)
    {
        var z = floorZ;
        foreach (var c in columns)
        {
            z = Math.Min(z, Systems.MahaonMapEdits.MahaonMapEdits.LandAt(map, c.X, c.Y).Z);
        }

        return z;
    }

    /// <summary>Короче ли подвал минимума хоть по одной стороне.</summary>
    public static bool IsUndersized(HashSet<Point2D> columns)
    {
        if (columns.Count == 0)
        {
            return false;
        }

        GetBounds(columns, out var minX, out var minY, out var maxX, out var maxY);
        return maxX - minX + 1 < MinSide || maxY - minY + 1 < MinSide;
    }

    /// <summary>Северо-западный угол пола — там ступени наверх.</summary>
    public static Point2D Corner(HashSet<Point2D> columns)
    {
        var corner = default(Point2D);
        var best = int.MaxValue;
        foreach (var c in columns)
        {
            if (c.X + c.Y < best || c.X + c.Y == best && c.X < corner.X)
            {
                best = c.X + c.Y;
                corner = c;
            }
        }

        return corner;
    }

    /// <summary>Где игрок появляется внизу: рядом со ступенями, не на них — иначе щелчок по
    /// ступеням у себя под ногами попадал бы в персонажа.</summary>
    public static Point3D Arrival(HashSet<Point2D> columns, Point2D corner, int z)
    {
        foreach (var (dx, dy) in new[] { (1, 0), (0, 1), (1, 1) })
        {
            if (columns.Contains(new Point2D(corner.X + dx, corner.Y + dy)))
            {
                return new Point3D(corner.X + dx, corner.Y + dy, z);
            }
        }

        return new Point3D(corner.X, corner.Y, z);
    }

    /// <summary>Северная и западная стены вокруг пола.</summary>
    public static void BuildWalls(HashSet<Point2D> columns, int z, Map map, List<Item> into)
    {
        foreach (var c in columns)
        {
            if (!columns.Contains(new Point2D(c.X, c.Y - 1)))
            {
                Place(new Static(WallAlongX), c.X, c.Y - 1, z, map, into);
            }

            if (!columns.Contains(new Point2D(c.X - 1, c.Y)))
            {
                Place(new Static(WallAlongY), c.X - 1, c.Y, z, map, into);
            }
        }
    }

    /// <summary>
    ///     Факелы на стенах через клетку. Чётность X+Y чередуется вдоль любой прямой стены,
    ///     поэтому «через одну» выходит само и не зависит от порядка обхода. Угол со ступенями
    ///     пропускается; клетка, у которой стены с двух сторон, получает один факел — на
    ///     северной.
    /// </summary>
    public static int PlaceTorches(HashSet<Point2D> columns, int z, Map map, Point2D skip, List<Item> into)
    {
        var placed = 0;
        foreach (var c in columns)
        {
            if (c == skip || ((c.X + c.Y) & 1) != 0)
            {
                continue;
            }

            var northWall = !columns.Contains(new Point2D(c.X, c.Y - 1));
            var westWall = !columns.Contains(new Point2D(c.X - 1, c.Y));

            if (!northWall && !westWall)
            {
                continue;
            }

            var torch = new WallTorch();
            if (northWall)
            {
                torch.Flip(); // 0xA0A/0xA0C — на северной стене, свет NorthBig
            }

            Place(torch, c.X, c.Y, z + TorchHeight, map, into);
            torch.Ignite();
            placed++;
        }

        return placed;
    }

    public static bool IsTorch(Item item) => item is WallTorch;

    private static void Place(Item item, int x, int y, int z, Map map, List<Item> into)
    {
        item.Movable = false;
        item.MoveToWorld(new Point3D(x, y, z), map);
        into.Add(item);
    }

    /// <summary>Сколько из девяти клеток вокруг (и самой) — пол подвала: люк ставится там, где
    /// подвал есть со всех сторон, подальше от стен.</summary>
    public static int CountColumns(HashSet<Point2D> columns, int x, int y)
    {
        var count = 0;
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (columns.Contains(new Point2D(x + dx, y + dy)))
                {
                    count++;
                }
            }
        }

        return count;
    }
}
