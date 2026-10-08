using System;
using Server.Regions;

namespace Server.Systems.MahaonMapEdits;

/// <summary>
///     Наведение мостов и настилов.
///
///     Созидательная половина правок карты. До сих пор игрок умел только ломать (подрыв) и
///     копать (лопата) — а между «снести дом» и «вырыть ров» в замысле стоял ещё мост, и
///     без него система выходила однобокой: мир можно было только портить.
///
///     ---- Почему нельзя просто класть доски куда угодно ----
///
///     Настил — проходимая поверхность, и разрешив ставить его на ровном месте, мы получим
///     не мосты, а заасфальтированный мир: любой сможет застелить площадь досками просто
///     потому, что может. Поэтому настил кладётся только туда, где под ним ПРОВАЛ: вода
///     либо земля хотя бы на MinGap ниже того места, где стоит строитель. То есть мост
///     соединяет берега, а не украшает лужайку.
///
///     ---- Высота ----
///
///     Доска ложится на высоте строителя, а не земли под ней. Иначе мост уходил бы в воду
///     вслед за дном, что и мостом-то назвать нельзя.
/// </summary>
public static class MahaonBridging
{
    /// <summary>
    ///     Настил. Проходимая поверхность без блокировки — проверено по tiledata: у всех
    ///     этих тайлов Surface без Impassable и нулевая высота, то есть по ним ходят, а не
    ///     об них спотыкаются.
    /// </summary>
    public static readonly int[] PlankGraphics =
    {
        0x04A1, 0x04A2, 0x04A3, 0x04A4
    };

    /// <summary>Насколько ниже строителя должна быть земля, чтобы это считалось провалом.</summary>
    public const int MinGap = 3;

    /// <summary>Досок на один тайл настила.</summary>
    public const int BoardsPerTile = 2;

    public enum BuildResult
    {
        Built,
        NoGap,
        Occupied,
        Forbidden
    }

    /// <summary>Наш ли это настил — чтобы взрыв мог его снести, а гору не мог.</summary>
    public static bool IsPlank(int graphic) => Array.IndexOf(PlankGraphics, graphic) >= 0;

    /// <summary>
    ///     Кладёт доску. Высота берётся от строителя: мост держит уровень берега, а не
    ///     повторяет рельеф дна.
    /// </summary>
    public static BuildResult Build(Map map, int x, int y, int builderZ, string reason)
    {
        if (map == null || map == Map.Internal)
        {
            return BuildResult.Forbidden;
        }

        var point = new Point3D(x, y, builderZ);

        if (Region.Find(point, map)?.GetRegion<GuardedRegion>() != null)
        {
            return BuildResult.Forbidden;
        }

        var land = MahaonMapEdits.LandAt(map, x, y);
        var flags = TileData.LandTable[land.Graphic & TileData.MaxLandValue].Flags;
        var overWater = (flags & TileFlag.Wet) != 0;

        if (!overWater && builderZ - land.Z < MinGap)
        {
            return BuildResult.NoGap;
        }

        // Уже настелено — второй слой не кладём.
        foreach (var s in MahaonMapEdits.StaticsAt(map, x, y))
        {
            if (IsPlank(s.Graphic) && Math.Abs(s.Z - builderZ) <= 1)
            {
                return BuildResult.Occupied;
            }
        }

        MahaonMapEdits.AddStatic(
            map, x, y, builderZ,
            PlankGraphics[Utility.Random(PlankGraphics.Length)], 0, reason
        );

        return BuildResult.Built;
    }

    /// <summary>Снимает настил в точке — разобрать мост можно и без взрыва.</summary>
    public static int Dismantle(Map map, int x, int y, string reason)
    {
        var removed = 0;

        foreach (var s in new System.Collections.Generic.List<MapStatic>(MahaonMapEdits.StaticsAt(map, x, y)))
        {
            if (IsPlank(s.Graphic))
            {
                removed += MahaonMapEdits.RemoveStatics(map, x, y, s.Graphic, reason);
            }
        }

        return removed;
    }
}
