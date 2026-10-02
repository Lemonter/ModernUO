using System.Collections.Generic;
using Server.Items;
using Server.Systems.MahaonSeasons;

namespace Server.Systems.Bots;

/// <summary>A patch of field: the crop tiles of one 16x16 area, harvested together.</summary>
public sealed class FieldPatch
{
    public Map Map;
    public Point3D Center;
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
            foreach (var tile in patch.Tiles)
            {
                sx += tile.X;
                sy += tile.Y;
            }

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
