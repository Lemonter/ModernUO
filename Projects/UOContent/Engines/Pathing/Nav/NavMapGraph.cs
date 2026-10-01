using System;
using System.Collections.Generic;

namespace Server.Engines.Pathing.Nav;

/// <summary>
/// The navigation graph of one map. A region is one connected component of one 16x16 cluster —
/// a patch of ground a walker can cross in any direction without leaving the cluster. Edges join
/// regions a single step apart and carry the exact cells of that step (the portal), so a route
/// over regions turns into a chain of short legs the local A* always has room to solve.
///
/// Everything is flat arrays in CSR form: regions grouped by cluster, edges grouped by source
/// region. Felucca comes to a few hundred thousand regions; this keeps it to tens of MB with no
/// per-region objects.
/// </summary>
public sealed class NavMapGraph
{
    // Labelled clusters are cached for locating positions; a handful covers the bots' working set
    // around any one spot, and relabelling a cluster costs well under a millisecond.
    private const int LocateCacheSize = 256;

    private readonly Dictionary<int, LabelledCluster> _locateCache = new();
    private readonly Queue<int> _locateOrder = new();

    private INavCellSource _source;

    public NavMapGraph(
        int mapId, int width, int height,
        int[] clusterRegionStart,
        ushort[] regionX, ushort[] regionY, sbyte[] regionZ,
        int[] edgeStart, int[] edgeTarget, ushort[] edgeCost,
        ushort[] portalFromX, ushort[] portalFromY, sbyte[] portalFromZ,
        ushort[] portalToX, ushort[] portalToY, sbyte[] portalToZ
    )
    {
        MapId = mapId;
        Width = width;
        Height = height;
        ClusterCols = (width + NavClusterCells.Size - 1) >> NavClusterCells.Shift;
        ClusterRows = (height + NavClusterCells.Size - 1) >> NavClusterCells.Shift;
        ClusterRegionStart = clusterRegionStart;
        RegionX = regionX;
        RegionY = regionY;
        RegionZ = regionZ;
        EdgeStart = edgeStart;
        EdgeTarget = edgeTarget;
        EdgeCost = edgeCost;
        PortalFromX = portalFromX;
        PortalFromY = portalFromY;
        PortalFromZ = portalFromZ;
        PortalToX = portalToX;
        PortalToY = portalToY;
        PortalToZ = portalToZ;
    }

    public int MapId { get; }
    public int Width { get; }
    public int Height { get; }
    public int ClusterCols { get; }
    public int ClusterRows { get; }

    /// <summary>First region of each cluster; length ClusterCols * ClusterRows + 1.</summary>
    public int[] ClusterRegionStart { get; }

    /// <summary>Representative cell of each region — the cell nearest its centroid.</summary>
    public ushort[] RegionX { get; }
    public ushort[] RegionY { get; }
    public sbyte[] RegionZ { get; }

    /// <summary>First edge of each region; length RegionCount + 1.</summary>
    public int[] EdgeStart { get; }
    public int[] EdgeTarget { get; }

    /// <summary>Tenths of a tile: region centre → portal → next region centre.</summary>
    public ushort[] EdgeCost { get; }

    public ushort[] PortalFromX { get; }
    public ushort[] PortalFromY { get; }
    public sbyte[] PortalFromZ { get; }
    public ushort[] PortalToX { get; }
    public ushort[] PortalToY { get; }
    public sbyte[] PortalToZ { get; }

    public int RegionCount => RegionX.Length;
    public int EdgeCount => EdgeTarget.Length;

    /// <summary>Wires the cell source used to relabel clusters when locating positions.</summary>
    public void AttachSource(INavCellSource source)
    {
        _source = source;
        _locateCache.Clear();
        _locateOrder.Clear();
    }

    public int GetClusterIndex(int clusterX, int clusterY) => clusterY * ClusterCols + clusterX;

    public int GetCluster(int region)
    {
        // Regions are stored cluster-major, so the owning cluster is a binary search away.
        var idx = Array.BinarySearch(ClusterRegionStart, region);
        if (idx >= 0)
        {
            // Several empty clusters can share a start; the owner is the last one.
            while (idx + 1 < ClusterRegionStart.Length && ClusterRegionStart[idx + 1] == region)
            {
                idx++;
            }

            return idx;
        }

        return ~idx - 1;
    }

    /// <summary>
    /// The region a walker standing at (x, y, z) is in, or -1. Looks at the exact cell first, then
    /// spirals out a few tiles — someone standing on a house floor or a dynamic item has no static
    /// surface under their feet, but the ground next to them does.
    /// </summary>
    public int Locate(int x, int y, int z, int searchRadius = 3)
    {
        var region = LocateExact(x, y, z);
        if (region >= 0)
        {
            return region;
        }

        for (var r = 1; r <= searchRadius; r++)
        {
            var best = -1;
            var bestScore = int.MaxValue;

            for (var dy = -r; dy <= r; dy++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                    {
                        continue;
                    }

                    var found = LocateNearZ(x + dx, y + dy, z, out var dz);
                    if (found >= 0 && dz < bestScore)
                    {
                        bestScore = dz;
                        best = found;
                    }
                }
            }

            if (best >= 0)
            {
                return best;
            }
        }

        return -1;
    }

    private int LocateExact(int x, int y, int z)
    {
        var region = LocateNearZ(x, y, z, out var dz);
        return dz <= NavClusterCells.SurfaceTolerance ? region : -1;
    }

    /// <summary>Region of the surface of (x, y) nearest z, any distance away; dz is how far.</summary>
    private int LocateNearZ(int x, int y, int z, out int dz)
    {
        dz = int.MaxValue;

        if (_source == null || (uint)x >= Width || (uint)y >= Height)
        {
            return -1;
        }

        var cx = x >> NavClusterCells.Shift;
        var cy = y >> NavClusterCells.Shift;
        var labelled = GetLabelled(cx, cy);
        var cell = ((y & 15) << 4) | (x & 15);

        var count = labelled.NodeCount[cell];
        if (count == 0)
        {
            return -1;
        }

        var start = labelled.NodeStart[cell];
        var best = -1;

        for (var i = 0; i < count; i++)
        {
            var delta = Math.Abs(labelled.Z[start + i] - z);
            if (delta < dz)
            {
                dz = delta;
                best = start + i;
            }
        }

        return ClusterRegionStart[GetClusterIndex(cx, cy)] + labelled.Component[best];
    }

    private LabelledCluster GetLabelled(int cx, int cy)
    {
        var key = GetClusterIndex(cx, cy);
        if (_locateCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (_locateCache.Count >= LocateCacheSize)
        {
            _locateCache.Remove(_locateOrder.Dequeue());
        }

        var cells = new NavClusterCells();
        _source.FillCluster(cx, cy, cells);
        cells.Label();

        var labelled = new LabelledCluster(cells);
        _locateCache[key] = labelled;
        _locateOrder.Enqueue(key);
        return labelled;
    }

    /// <summary>Drops cached cluster labels, e.g. after the source data changed.</summary>
    public void ClearLocateCache()
    {
        _locateCache.Clear();
        _locateOrder.Clear();
    }

    /// <summary>Compact, immutable copy of a labelled cluster — what Locate needs and no more.</summary>
    private sealed class LabelledCluster
    {
        public readonly byte[] NodeCount = new byte[NavClusterCells.CellCount];
        public readonly ushort[] NodeStart = new ushort[NavClusterCells.CellCount];
        public readonly sbyte[] Z;
        public readonly ushort[] Component;

        public LabelledCluster(NavClusterCells cells)
        {
            Z = new sbyte[cells.NodeCount];
            Component = new ushort[cells.NodeCount];

            for (var cell = 0; cell < NavClusterCells.CellCount; cell++)
            {
                NodeCount[cell] = (byte)cells.GetNodeCount(cell);
                NodeStart[cell] = (ushort)cells.GetNodeStart(cell);
            }

            for (var node = 0; node < cells.NodeCount; node++)
            {
                Z[node] = cells.GetZ(node);
                Component[node] = cells.GetComponent(node);
            }
        }
    }
}
