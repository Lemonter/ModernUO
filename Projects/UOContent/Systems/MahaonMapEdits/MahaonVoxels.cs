using System;

namespace Server.Systems.MahaonMapEdits;

/// <summary>Чем занята клетка пространства. Флаги, а не перечисление: вода непроходима, но
/// свет пропускает, а крыша проходима сверху, но свет держит.</summary>
[Flags]
public enum Voxel : byte
{
    Empty = 0x00,

    /// <summary>Сквозь клетку нельзя пройти: земля, стена, скала, вода.</summary>
    Solid = 0x01,

    /// <summary>Клетка держит свет и взгляд. Стекло и вода — нет, крыша — да.</summary>
    Opaque = 0x02,

    /// <summary>В клетке вода (любая: нарисованная или налитая).</summary>
    Water = 0x04,

    /// <summary>На этой клетке можно стоять: поверхность земли, пол, мост.</summary>
    Surface = 0x08
}

/// <summary>
///     Решётка занятости для куска мира: что в каждой клетке пространства.
///
///     ---- Зачем она вообще ----
///
///     В ультиме два несовместимых слоя: земля как карта высот (одна высота на тайл) и
///     статика как отдельные предметы на целых Z. Ни в одном нет понятия «клетка
///     пространства» — а свет, дым, газ и взгляд распространяются именно по пространству.
///     Поэтому в обычной ультиме каждое такое явление подделано отдельно и по-своему: свет
///     радиусом сквозь стены, тени нарисованы в спрайте, дым — анимация.
///
///     Решётка сводит их к одному обходу в ширину по соседним клеткам. Четыре подделки
///     превращаются в один механизм.
///
///     ---- Решётка производная ----
///
///     Её не хранят, не сохраняют и не рассылают. Она ВЫВОДИТСЯ из земли и статики по флагам
///     tiledata и живёт ровно столько, сколько считается то, ради чего её построили. Значит
///     источник истины один — карта, и рассинхрона быть не может.
///
///     ---- Почему только кусок ----
///
///     Арифметика не оставляет выбора. Фелукка 7168x4096, Z от -128 до 127:
///
///         7168 * 4096 * 256 = 7,5 миллиарда клеток — 7,5 ГБ даже по байту на клетку.
///
///     А кусок 64x64 на 48 уровней — 196 тысяч клеток, 196 КБ. Поэтому решётка строится
///     вокруг места, где что-то происходит, и выбрасывается.
///
///     Этот же набор правил повторяет клиент (см. VOXEL-WORLD-TASK.md в Unity-проекте):
///     свет и дым должны вести себя одинаково у всех, иначе игроки увидят разное.
/// </summary>
public sealed class MahaonVoxels
{
    /// <summary>Насколько высокой берём решётку по умолчанию: столько уровней вверх от
    /// самого низкого дна в куске. Больше незачем — всё, что нас занимает, у поверхности.</summary>
    public const int DefaultHeight = 48;

    /// <summary>Сколько уровней берём НИЖЕ поверхности. Подвал, пещера, вырытая яма.</summary>
    public const int DefaultBelow = 12;

    private readonly Voxel[] _cells;

    public Map Map { get; }
    public int X0 { get; }
    public int Y0 { get; }
    public int Z0 { get; }
    public int Width { get; }
    public int Depth { get; }
    public int Height { get; }

    private MahaonVoxels(Map map, int x0, int y0, int z0, int width, int depth, int height)
    {
        Map = map;
        X0 = x0;
        Y0 = y0;
        Z0 = z0;
        Width = width;
        Depth = depth;
        Height = height;

        _cells = new Voxel[width * depth * height];
    }

    public bool Contains(int x, int y, int z) =>
        x >= X0 && x < X0 + Width && y >= Y0 && y < Y0 + Depth && z >= Z0 && z < Z0 + Height;

    public Voxel this[int x, int y, int z] =>
        Contains(x, y, z) ? _cells[Index(x, y, z)] : Voxel.Empty;

    private int Index(int x, int y, int z) =>
        ((x - X0) * Depth + (y - Y0)) * Height + (z - Z0);

    private void Add(int x, int y, int z, Voxel what)
    {
        if (Contains(x, y, z))
        {
            _cells[Index(x, y, z)] |= what;
        }
    }

    /// <summary>
    ///     Строит решётку вокруг точки: квадрат <paramref name="radius"/> тайлов в каждую
    ///     сторону, по высоте — от DefaultBelow ниже самого низкого дна и вверх на
    ///     DefaultHeight.
    /// </summary>
    public static MahaonVoxels Around(Map map, int cx, int cy, int radius)
    {
        var x0 = Math.Max(0, cx - radius);
        var y0 = Math.Max(0, cy - radius);
        var x1 = Math.Min(map.Width - 1, cx + radius);
        var y1 = Math.Min(map.Height - 1, cy + radius);

        // Низ решётки — по самому глубокому дну в куске, иначе вырытая яма окажется за её
        // пределами и для света её как бы не существует.
        var lowest = int.MaxValue;

        for (var x = x0; x <= x1; x++)
        {
            for (var y = y0; y <= y1; y++)
            {
                var z = MahaonMapEdits.LandAt(map, x, y).Z;

                if (z < lowest)
                {
                    lowest = z;
                }
            }
        }

        var z0 = Math.Max(sbyte.MinValue, lowest - DefaultBelow);
        var grid = new MahaonVoxels(map, x0, y0, z0, x1 - x0 + 1, y1 - y0 + 1, DefaultHeight);

        grid.Fill();

        return grid;
    }

    private void Fill()
    {
        for (var x = X0; x < X0 + Width; x++)
        {
            for (var y = Y0; y < Y0 + Depth; y++)
            {
                FillLand(x, y);
                FillStatics(x, y);
            }
        }
    }

    private void FillLand(int x, int y)
    {
        var land = MahaonMapEdits.LandAt(Map, x, y);
        var flags = TileData.LandTable[land.Graphic & TileData.MaxLandValue].Flags;

        // Всё ниже поверхности — камень и земля: непроходимо и света не пропускает.
        for (var z = Z0; z < land.Z; z++)
        {
            Add(x, y, z, Voxel.Solid | Voxel.Opaque);
        }

        if ((flags & TileFlag.Wet) != 0)
        {
            // Земля, помеченная водой: море и русло. Непроходима, но свет пропускает —
            // потому и видно дно, а в нашей тайлдате вода ещё и полупрозрачная.
            Add(x, y, land.Z, Voxel.Solid | Voxel.Water);
            return;
        }

        if ((flags & TileFlag.Impassable) != 0)
        {
            Add(x, y, land.Z, Voxel.Solid | Voxel.Opaque);
            return;
        }

        Add(x, y, land.Z, Voxel.Surface);
    }

    private void FillStatics(int x, int y)
    {
        foreach (var s in MahaonMapEdits.StaticsAt(Map, x, y))
        {
            var data = TileData.ItemTable[s.Graphic & TileData.MaxItemValue];
            var flags = data.Flags;

            var what = Voxel.Empty;

            if ((flags & TileFlag.Wet) != 0)
            {
                what |= Voxel.Solid | Voxel.Water;
            }
            else if ((flags & (TileFlag.Impassable | TileFlag.Wall)) != 0)
            {
                what |= Voxel.Solid;

                // Прозрачное и полупрозрачное свет пропускает: стекло, решётка, вода.
                if ((flags & (TileFlag.Transparent | TileFlag.Translucent)) == 0)
                {
                    what |= Voxel.Opaque;
                }
            }

            if ((flags & (TileFlag.Surface | TileFlag.Bridge)) != 0)
            {
                what |= Voxel.Surface;

                // Крыша и пол проходимы сверху, но света не пропускают — иначе солнце
                // светило бы в дом через крышу, а в подвал через пол.
                if ((flags & (TileFlag.Transparent | TileFlag.Translucent)) == 0)
                {
                    what |= Voxel.Opaque;
                }
            }

            if ((flags & TileFlag.Roof) != 0)
            {
                what |= Voxel.Opaque;
            }

            if (what == Voxel.Empty)
            {
                continue; // трава, цветы, мелочь — пространство не занимают
            }

            // Высота ноль означает «толщиной в одну клетку», а не «нулевой».
            var top = s.Z + Math.Max(1, data.CalcHeight);

            for (var z = s.Z; z < top; z++)
            {
                Add(x, y, z, what);
            }
        }
    }

    /// <summary>Клетка держит свет и взгляд.</summary>
    public bool Blocks(int x, int y, int z) => (this[x, y, z] & Voxel.Opaque) != 0;

    /// <summary>В клетку можно войти: не занята и не вода.</summary>
    public bool Open(int x, int y, int z) => (this[x, y, z] & Voxel.Solid) == 0;

    /// <summary>Высота, на которой на этом тайле стоят — самая низкая поверхность не ниже
    /// <paramref name="from"/>. Нет такой — null.</summary>
    public int? SurfaceAt(int x, int y, int from)
    {
        for (var z = Math.Max(Z0, from); z < Z0 + Height; z++)
        {
            if ((this[x, y, z] & Voxel.Surface) != 0)
            {
                return z;
            }
        }

        return null;
    }
}
