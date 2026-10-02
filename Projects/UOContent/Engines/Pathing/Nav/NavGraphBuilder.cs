using System;
using System.Collections.Generic;

namespace Server.Engines.Pathing.Nav;

/// <summary>
/// Builds a <see cref="NavMapGraph"/> from static walkability. One sweep over the clusters, row by
/// row, holding three rows of labelled clusters at a time — a step leaves a cluster for at most
/// one of its eight neighbours, so the rows above and below are all an edge can reach.
/// </summary>
public static class NavGraphBuilder
{
    private struct TempEdge
    {
        public int SrcRegion;
        public int DstCluster;
        public ushort DstComponent;
        public ushort FromX, FromY, ToX, ToY;
        public sbyte FromZ, ToZ;
    }

    private struct Candidate
    {
        public ushort FromX, FromY, ToX, ToY;
        public sbyte FromZ, ToZ;
    }

    public static NavMapGraph Build(int mapId, INavCellSource source, Action<int, int> progress = null)
    {
        var width = source.Width;
        var height = source.Height;
        var cols = (width + NavClusterCells.Size - 1) >> NavClusterCells.Shift;
        var rows = (height + NavClusterCells.Size - 1) >> NavClusterCells.Shift;

        var clusterRegionStart = new int[cols * rows + 1];
        var regionX = new List<ushort>();
        var regionY = new List<ushort>();
        var regionZ = new List<sbyte>();
        var edges = new List<TempEdge>();

        // window[0] = row above, window[1] = current row, window[2] = row below.
        var window = new NavClusterCells[3][];
        for (var i = 0; i < 3; i++)
        {
            window[i] = new NavClusterCells[cols];
            for (var c = 0; c < cols; c++)
            {
                window[i][c] = new NavClusterCells();
            }
        }

        FillRow(source, window[1], 0, cols);
        if (rows > 1)
        {
            FillRow(source, window[2], 1, cols);
        }

        var groups = new Dictionary<(int srcComp, int dstCluster, int dstComp), List<Candidate>>();
        var sumX = new List<long>();
        var sumY = new List<long>();
        var counts = new List<int>();

        var row = 0;
        NavClusterCells lookup(int ncx, int ncy) => window[1 + ncy - row][ncx];

        for (var cy = 0; cy < rows; cy++)
        {
            row = cy;
            for (var cx = 0; cx < cols; cx++)
            {
                var clusterIndex = cy * cols + cx;
                var cells = window[1][cx];
                var firstRegion = regionX.Count;
                clusterRegionStart[clusterIndex] = firstRegion;

                AddRegions(cells, cx, cy, regionX, regionY, regionZ, sumX, sumY, counts);
                CollectEdges(lookup, cells, cx, cy, cols, rows, groups);
                EmitEdges(groups, firstRegion, edges);
            }

            progress?.Invoke(cy + 1, rows);

            // Slide the window down a row, reusing the oldest row's storage for the new one.
            var recycled = window[0];
            window[0] = window[1];
            window[1] = window[2];
            window[2] = recycled;

            if (cy + 2 < rows)
            {
                FillRow(source, window[2], cy + 2, cols);
            }
            else
            {
                foreach (var cells in window[2])
                {
                    cells.Reset(0, 0);
                }
            }
        }

        clusterRegionStart[cols * rows] = regionX.Count;

        return Finish(mapId, width, height, clusterRegionStart, regionX, regionY, regionZ, edges);
    }

    /// <summary>
    /// A graph with some clusters re-read from the source and everything else carried over from
    /// <paramref name="old"/>: the regions of the changed clusters, and the edges out of them and
    /// out of their neighbours (an edge into a changed cluster names one of its regions). Labelling
    /// is deterministic, so an unchanged cluster relabelled here gets the same component numbers
    /// the old graph used.
    /// </summary>
    public static NavMapGraph Patch(NavMapGraph old, INavCellSource source, IReadOnlyCollection<int> changedClusters)
    {
        var cols = old.ClusterCols;
        var rows = old.ClusterRows;

        var changed = new HashSet<int>(changedClusters);
        var edgesRedone = new HashSet<int>();
        foreach (var index in changed)
        {
            var cx = index % cols;
            var cy = index / cols;
            for (var oy = -1; oy <= 1; oy++)
            {
                for (var ox = -1; ox <= 1; ox++)
                {
                    if ((uint)(cx + ox) < cols && (uint)(cy + oy) < rows)
                    {
                        edgesRedone.Add((cy + oy) * cols + cx + ox);
                    }
                }
            }
        }

        var labelled = new Dictionary<int, NavClusterCells>();
        NavClusterCells lookup(int ncx, int ncy)
        {
            var index = ncy * cols + ncx;
            if (!labelled.TryGetValue(index, out var cells))
            {
                cells = new NavClusterCells();
                source.FillCluster(ncx, ncy, cells);
                cells.Label();
                labelled[index] = cells;
            }

            return cells;
        }

        var clusterRegionStart = new int[cols * rows + 1];
        var regionX = new List<ushort>(old.RegionCount);
        var regionY = new List<ushort>(old.RegionCount);
        var regionZ = new List<sbyte>(old.RegionCount);
        var edges = new List<TempEdge>(old.EdgeCount);
        var groups = new Dictionary<(int srcComp, int dstCluster, int dstComp), List<Candidate>>();
        var sumX = new List<long>();
        var sumY = new List<long>();
        var counts = new List<int>();

        for (var cy = 0; cy < rows; cy++)
        {
            for (var cx = 0; cx < cols; cx++)
            {
                var index = cy * cols + cx;
                var firstRegion = regionX.Count;
                clusterRegionStart[index] = firstRegion;

                var oldFirst = old.ClusterRegionStart[index];
                var oldEnd = old.ClusterRegionStart[index + 1];

                if (changed.Contains(index))
                {
                    AddRegions(lookup(cx, cy), cx, cy, regionX, regionY, regionZ, sumX, sumY, counts);
                }
                else
                {
                    for (var r = oldFirst; r < oldEnd; r++)
                    {
                        regionX.Add(old.RegionX[r]);
                        regionY.Add(old.RegionY[r]);
                        regionZ.Add(old.RegionZ[r]);
                    }
                }

                if (edgesRedone.Contains(index))
                {
                    CollectEdges(lookup, lookup(cx, cy), cx, cy, cols, rows, groups);
                    EmitEdges(groups, firstRegion, edges);
                    continue;
                }

                for (var r = oldFirst; r < oldEnd; r++)
                {
                    for (int e = old.EdgeStart[r], end = old.EdgeStart[r + 1]; e < end; e++)
                    {
                        var target = old.EdgeTarget[e];
                        var targetCluster = old.GetCluster(target);
                        edges.Add(
                            new TempEdge
                            {
                                SrcRegion = firstRegion + r - oldFirst,
                                DstCluster = targetCluster,
                                DstComponent = (ushort)(target - old.ClusterRegionStart[targetCluster]),
                                FromX = old.PortalFromX[e],
                                FromY = old.PortalFromY[e],
                                FromZ = old.PortalFromZ[e],
                                ToX = old.PortalToX[e],
                                ToY = old.PortalToY[e],
                                ToZ = old.PortalToZ[e]
                            }
                        );
                    }
                }
            }
        }

        clusterRegionStart[cols * rows] = regionX.Count;

        return Finish(old.MapId, old.Width, old.Height, clusterRegionStart, regionX, regionY, regionZ, edges);
    }

    private static void FillRow(INavCellSource source, NavClusterCells[] row, int cy, int cols)
    {
        for (var cx = 0; cx < cols; cx++)
        {
            source.FillCluster(cx, cy, row[cx]);
            row[cx].Label();
        }
    }

    private static void AddRegions(
        NavClusterCells cells, int cx, int cy,
        List<ushort> regionX, List<ushort> regionY, List<sbyte> regionZ,
        List<long> sumX, List<long> sumY, List<int> counts
    )
    {
        var components = cells.ComponentCount;
        if (components == 0)
        {
            return;
        }

        var baseX = cx << NavClusterCells.Shift;
        var baseY = cy << NavClusterCells.Shift;

        sumX.Clear();
        sumY.Clear();
        counts.Clear();
        for (var i = 0; i < components; i++)
        {
            sumX.Add(0);
            sumY.Add(0);
            counts.Add(0);
        }

        for (var node = 0; node < cells.NodeCount; node++)
        {
            var comp = cells.GetComponent(node);
            var cell = cells.GetCell(node);
            sumX[comp] += cell & 15;
            sumY[comp] += cell >> 4;
            counts[comp]++;
        }

        // Representative: the component's own cell nearest its centroid, so it is always a place
        // the walker can actually stand.
        for (var comp = 0; comp < components; comp++)
        {
            var meanX10 = (int)(sumX[comp] * 10 / counts[comp]);
            var meanY10 = (int)(sumY[comp] * 10 / counts[comp]);
            var bestNode = -1;
            var bestDist = int.MaxValue;

            for (var node = 0; node < cells.NodeCount; node++)
            {
                if (cells.GetComponent(node) != comp)
                {
                    continue;
                }

                var cell = cells.GetCell(node);
                var dx = (cell & 15) * 10 - meanX10;
                var dy = (cell >> 4) * 10 - meanY10;
                var dist = dx * dx + dy * dy;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestNode = node;
                }
            }

            var bestCell = cells.GetCell(bestNode);
            regionX.Add((ushort)(baseX + (bestCell & 15)));
            regionY.Add((ushort)(baseY + (bestCell >> 4)));
            regionZ.Add(cells.GetZ(bestNode));
        }
    }

    private static void CollectEdges(
        Func<int, int, NavClusterCells> lookup, NavClusterCells cells, int cx, int cy, int cols, int rows,
        Dictionary<(int, int, int), List<Candidate>> groups
    )
    {
        var baseX = cx << NavClusterCells.Shift;
        var baseY = cy << NavClusterCells.Shift;

        for (var node = 0; node < cells.NodeCount; node++)
        {
            var cell = cells.GetCell(node);
            var lx = cell & 15;
            var ly = cell >> 4;
            var mask = cells.GetMask(node);
            var comp = cells.GetComponent(node);

            for (var d = 0; d < 8; d++)
            {
                if ((mask & (1 << d)) == 0)
                {
                    continue;
                }

                var nx = lx + NavMath.DirX(d);
                var ny = ly + NavMath.DirY(d);

                // Which neighbouring cluster the step lands in: -1, 0 or +1 on each axis.
                var ox = nx < 0 ? -1 : nx >= NavClusterCells.Size ? 1 : 0;
                var oy = ny < 0 ? -1 : ny >= NavClusterCells.Size ? 1 : 0;
                var ncx = cx + ox;
                var ncy = cy + oy;

                if ((uint)ncx >= cols || (uint)ncy >= rows)
                {
                    continue;
                }

                var target = lookup(ncx, ncy);
                var targetCell = ((ny & 15) << 4) | (nx & 15);
                var destZ = cells.GetDestZ(node, d);
                var targetNode = target.FindNode(targetCell, destZ);

                if (targetNode < 0)
                {
                    continue;
                }

                var targetComp = target.GetComponent(targetNode);
                if (ox == 0 && oy == 0 && targetComp == comp)
                {
                    continue; // inside the region
                }

                var key = (comp, ncy * cols + ncx, (int)targetComp);
                if (!groups.TryGetValue(key, out var list))
                {
                    groups[key] = list = [];
                }

                list.Add(
                    new Candidate
                    {
                        FromX = (ushort)(baseX + lx),
                        FromY = (ushort)(baseY + ly),
                        FromZ = cells.GetZ(node),
                        ToX = (ushort)((ncx << NavClusterCells.Shift) + (nx & 15)),
                        ToY = (ushort)((ncy << NavClusterCells.Shift) + (ny & 15)),
                        ToZ = target.GetZ(targetNode)
                    }
                );
            }
        }
    }

    /// <summary>
    /// One edge per (source region, target region): the candidate step nearest the middle of all
    /// of them. A wide opening yields a portal in its centre, which keeps legs off walls.
    /// </summary>
    private static void EmitEdges(
        Dictionary<(int srcComp, int dstCluster, int dstComp), List<Candidate>> groups,
        int firstRegion, List<TempEdge> edges
    )
    {
        if (groups.Count == 0)
        {
            return;
        }

        var keys = new List<(int srcComp, int dstCluster, int dstComp)>(groups.Keys);
        keys.Sort();

        foreach (var key in keys)
        {
            var list = groups[key];
            long sx = 0, sy = 0;
            foreach (var c in list)
            {
                sx += c.FromX;
                sy += c.FromY;
            }

            var meanX10 = sx * 10 / list.Count;
            var meanY10 = sy * 10 / list.Count;
            var best = list[0];
            var bestDist = long.MaxValue;

            foreach (var c in list)
            {
                var dx = c.FromX * 10L - meanX10;
                var dy = c.FromY * 10L - meanY10;
                var dist = dx * dx + dy * dy;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = c;
                }
            }

            edges.Add(
                new TempEdge
                {
                    SrcRegion = firstRegion + key.srcComp,
                    DstCluster = key.dstCluster,
                    DstComponent = (ushort)key.dstComp,
                    FromX = best.FromX,
                    FromY = best.FromY,
                    FromZ = best.FromZ,
                    ToX = best.ToX,
                    ToY = best.ToY,
                    ToZ = best.ToZ
                }
            );
        }

        groups.Clear();
    }

    private static NavMapGraph Finish(
        int mapId, int width, int height, int[] clusterRegionStart,
        List<ushort> regionX, List<ushort> regionY, List<sbyte> regionZ, List<TempEdge> edges
    )
    {
        var regionCount = regionX.Count;
        var edgeCount = edges.Count;

        var edgeStart = new int[regionCount + 1];
        var edgeTarget = new int[edgeCount];
        var edgeCost = new ushort[edgeCount];
        var fromX = new ushort[edgeCount];
        var fromY = new ushort[edgeCount];
        var fromZ = new sbyte[edgeCount];
        var toX = new ushort[edgeCount];
        var toY = new ushort[edgeCount];
        var toZ = new sbyte[edgeCount];

        // Edges were emitted cluster by cluster and sorted by source component inside each, so
        // they are already grouped by source region.
        var region = 0;
        for (var i = 0; i < edgeCount; i++)
        {
            var e = edges[i];
            while (region <= e.SrcRegion)
            {
                edgeStart[region++] = i;
            }

            var dst = clusterRegionStart[e.DstCluster] + e.DstComponent;
            edgeTarget[i] = dst;
            fromX[i] = e.FromX;
            fromY[i] = e.FromY;
            fromZ[i] = e.FromZ;
            toX[i] = e.ToX;
            toY[i] = e.ToY;
            toZ[i] = e.ToZ;

            var cost = NavMath.Octile(regionX[e.SrcRegion], regionY[e.SrcRegion], e.FromX, e.FromY)
                       + NavMath.Octile(e.FromX, e.FromY, e.ToX, e.ToY)
                       + NavMath.Octile(e.ToX, e.ToY, regionX[dst], regionY[dst]);
            edgeCost[i] = (ushort)Math.Clamp(cost, 1, ushort.MaxValue);
        }

        while (region <= regionCount)
        {
            edgeStart[region++] = edgeCount;
        }

        return new NavMapGraph(
            mapId, width, height, clusterRegionStart,
            regionX.ToArray(), regionY.ToArray(), regionZ.ToArray(),
            edgeStart, edgeTarget, edgeCost,
            fromX, fromY, fromZ, toX, toY, toZ
        );
    }
}
