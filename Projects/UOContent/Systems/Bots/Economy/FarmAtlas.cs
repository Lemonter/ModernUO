using System.Collections.Generic;
using Server.Items;
using Server.Systems.MahaonSeasons;

namespace Server.Systems.Bots;

/// <summary>A patch of field: the crop tiles of one 16x16 area, harvested together.</summary>
public sealed class FieldPatch
{
    public Map Map;
    public Point3D Center;
    public Rectangle2D Bounds;
    public readonly List<MahaonCropTile> Tiles = [];

    public int RipeCount()
    {
        if (SeasonSystem.CurrentSeason != MahaonSeason.Autumn)
        {
            return 0;
        }

        var count = 0;
        foreach (var tile in Tiles)
        {
            if (!tile.Deleted && !tile.Harvested)
            {
                count++;
            }
        }

        return count;
    }
}

/// <summary>
/// Where food grows: the field tiles laid over the world's farms, grouped into patches, and the
/// fruit trees. Fields ripen in autumn and are harvested once a year; fruit hangs in spring and
/// summer. The crop tiles and tree crowns keep their own registries, so nothing scans the world.
/// </summary>
public static class FarmAtlas
{
    private const int PatchShift = 4;
    private const int MinRipeTiles = 3;

    private static readonly List<FieldPatch> _patches = [];

    public static int PatchCount => _patches.Count;

    public static void Rebuild()
    {
        _patches.Clear();
        var byCell = new Dictionary<(Map, int, int), FieldPatch>();

        foreach (var tile in MahaonCropTile.All)
        {
            if (tile.Deleted || tile.Map == null || tile.Map == Map.Internal || tile.Parent != null)
            {
                continue;
            }

            var key = (tile.Map, tile.X >> PatchShift, tile.Y >> PatchShift);
            if (!byCell.TryGetValue(key, out var patch))
            {
                byCell[key] = patch = new FieldPatch { Map = tile.Map };
                _patches.Add(patch);
            }

            patch.Tiles.Add(tile);
        }

        foreach (var patch in _patches)
        {
            long sx = 0, sy = 0;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;
            foreach (var tile in patch.Tiles)
            {
                sx += tile.X;
                sy += tile.Y;
                minX = System.Math.Min(minX, tile.X);
                minY = System.Math.Min(minY, tile.Y);
                maxX = System.Math.Max(maxX, tile.X);
                maxY = System.Math.Max(maxY, tile.Y);
            }

            patch.Bounds = new Rectangle2D(minX, minY, maxX - minX + 1, maxY - minY + 1);

            var first = patch.Tiles[0];
            patch.Center = new Point3D((int)(sx / patch.Tiles.Count), (int)(sy / patch.Tiles.Count), first.Z);
        }
    }

    public static FieldPatch NearestRipeField(Map map, Point3D from, int maxRange)
    {
        if (SeasonSystem.CurrentSeason != MahaonSeason.Autumn)
        {
            return null;
        }

        FieldPatch best = null;
        var bestDist = (double)maxRange;

        foreach (var patch in _patches)
        {
            if (patch.Map != map)
            {
                continue;
            }

            var dist = patch.Center.GetDistanceToSqrt(from);
            if (dist < bestDist && patch.RipeCount() >= MinRipeTiles)
            {
                bestDist = dist;
                best = patch;
            }
        }

        return best;
    }

    /// <summary>
    /// Whether a bot may sow or plant here: open ground outside towns and houses, not water or
    /// rock, with nothing standing on it — or ground already ploughed for sowing.
    /// </summary>
    public static bool IsSowable(Map map, int x, int y, out int z)
    {
        z = 0;
        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
        {
            return false;
        }

        var land = map.Tiles.GetLandTile(x, y);
        var flags = TileData.LandTable[land.ID & TileData.MaxLandValue].Flags;
        if ((flags & (TileFlag.Impassable | TileFlag.Wet)) != 0)
        {
            return false;
        }

        z = map.GetAverageZ(x, y);
        var p = new Point3D(x, y, z);
        var region = Region.Find(p, map);
        if (region.IsPartOf<Regions.TownRegion>() || region.IsPartOf<Regions.HouseRegion>() ||
            region.IsPartOf<Regions.DungeonRegion>() || Multis.BaseHouse.FindHouseAt(p, map, 16) != null)
        {
            return false;
        }

        if (IsTaken(map, x, y))
        {
            return false;
        }

        return MahaonTilledEarth.Find(p, map) != null || map.CanFit(x, y, z, 16);
    }

    /// <summary>
    /// A small field for a bot to sow: up to <paramref name="count"/> sowable tiles side by side,
    /// somewhere between <paramref name="minDist"/> and <paramref name="maxDist"/> tiles from
    /// <paramref name="around"/>. Empty when no open ground turns up.
    /// </summary>
    public static List<Point3D> FindPlot(Map map, Point3D around, int minDist, int maxDist, int count)
    {
        var plot = new List<Point3D>();

        for (var attempt = 0; attempt < 40 && plot.Count == 0; attempt++)
        {
            var angle = Utility.RandomDouble() * System.Math.PI * 2;
            var dist = Utility.RandomMinMax(minDist, maxDist);
            var cx = around.X + (int)(System.Math.Cos(angle) * dist);
            var cy = around.Y + (int)(System.Math.Sin(angle) * dist);

            // Rows of four, as a field is laid out.
            for (var dy = 0; dy < 3 && plot.Count < count; dy++)
            {
                for (var dx = 0; dx < 4 && plot.Count < count; dx++)
                {
                    if (IsSowable(map, cx + dx, cy + dy, out var z))
                    {
                        plot.Add(new Point3D(cx + dx, cy + dy, z));
                    }
                }
            }

            // A scrap of ground between rocks is no field.
            if (plot.Count < count / 2)
            {
                plot.Clear();
            }
        }

        return plot;
    }

    private static bool IsTaken(Map map, int x, int y)
    {
        foreach (var item in map.GetItemsAt(new Point2D(x, y)))
        {
            // A crop tile counts even when it is invisible (winter, already picked).
            if (item is MahaonCropTile or MahaonPlantedSapling or MahaonTree || item.Visible && item is not MahaonTilledEarth)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A spot near <paramref name="near"/> for a tree, with nothing else growing or
    /// standing within two tiles, so trees don't wall off paths.</summary>
    public static Point3D? FindTreeSpot(Map map, Point3D near)
    {
        for (var attempt = 0; attempt < 16; attempt++)
        {
            var x = near.X + Utility.RandomMinMax(-6, 6);
            var y = near.Y + Utility.RandomMinMax(-6, 6);

            if (!IsSowable(map, x, y, out var z))
            {
                continue;
            }

            var crowded = false;
            foreach (var item in map.GetItemsInRange(new Point3D(x, y, z), 2))
            {
                if (item is MahaonTreeFoliage or MahaonTree or MahaonPlantedSapling or MahaonCropTile || item.Visible && !item.Movable)
                {
                    crowded = true;
                    break;
                }
            }

            if (!crowded)
            {
                return new Point3D(x, y, z);
            }
        }

        return null;
    }

    public static bool HasFruit(MahaonTreeFoliage tree) =>
        !tree.Deleted && tree.BearsFruit && tree.FruitRemaining > 0 &&
        SeasonSystem.CurrentSeason is MahaonSeason.Spring or MahaonSeason.Summer;

    public static MahaonTreeFoliage NearestFruitTree(Map map, Point3D from, int maxRange)
    {
        if (SeasonSystem.CurrentSeason is not (MahaonSeason.Spring or MahaonSeason.Summer))
        {
            return null;
        }

        MahaonTreeFoliage best = null;
        var bestDist = (double)maxRange;

        foreach (var tree in MahaonTreeFoliage.All)
        {
            if (tree.Map != map || tree.Parent != null)
            {
                continue;
            }

            var dist = tree.GetDistanceToSqrt(from);
            if (dist < bestDist && HasFruit(tree))
            {
                bestDist = dist;
                best = tree;
            }
        }

        return best;
    }
}
