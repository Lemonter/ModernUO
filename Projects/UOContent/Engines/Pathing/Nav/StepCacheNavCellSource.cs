using System;
using Server.Engines.Pathing.Cache;

namespace Server.Engines.Pathing.Nav;

/// <summary>
/// Reads a map's static walkability out of the step cache. Its chunks are 16x16 like the nav
/// clusters, so one chunk fills one cluster, and a baked .swb makes this a file read rather than a
/// terrain probe.
///
/// Static-only by construction: houses and boats never enter the graph. The follower deals with
/// them live, the way it deals with doors and mobiles.
/// </summary>
public sealed class StepCacheNavCellSource : INavCellSource
{
    private readonly Map _map;

    public StepCacheNavCellSource(Map map) => _map = map;

    public int Width => _map.Width;
    public int Height => _map.Height;

    public void FillCluster(int clusterX, int clusterY, NavClusterCells cells)
    {
        cells.Reset(clusterX, clusterY);

        var chunk = StepCache.Instance.GetStaticChunk(_map, clusterX, clusterY);
        if (chunk == null)
        {
            return;
        }

        var baseX = clusterX << NavClusterCells.Shift;
        var baseY = clusterY << NavClusterCells.Shift;

        Span<sbyte> destZ = stackalloc sbyte[8];
        var strata = chunk.StrataData;

        for (var cell = 0; cell < NavClusterCells.CellCount; cell++)
        {
            if (baseX + (cell & 15) >= _map.Width || baseY + (cell >> 4) >= _map.Height)
            {
                continue;
            }

            var offset = chunk.GetStrataOffset(cell);
            if (offset != StepChunk.NoStrata)
            {
                // Stacked surfaces: one stratum record each — zCenter, walkMask, wetMask,
                // walkZ N..NW, swimZ N..NW (see StepChunk.StrataData).
                var count = strata[offset];
                var pos = offset + 1;

                for (var i = 0; i < count; i++, pos += StepChunk.StratumByteLength)
                {
                    var z = (sbyte)strata[pos];
                    var mask = strata[pos + 1];
                    if (mask == 0)
                    {
                        continue;
                    }

                    for (var d = 0; d < 8; d++)
                    {
                        destZ[d] = (sbyte)strata[pos + 3 + d];
                    }

                    cells.AddNode(cell, z, mask, destZ);
                }

                continue;
            }

            var walkMask = chunk.WalkMask[cell];

            // No walkable step out of the cell: deep water, solid rock — or a one-cell pit, which
            // is no loss to the graph either.
            if (walkMask == 0)
            {
                continue;
            }

            destZ[0] = chunk.WalkZN[cell];
            destZ[1] = chunk.WalkZNE[cell];
            destZ[2] = chunk.WalkZE[cell];
            destZ[3] = chunk.WalkZSE[cell];
            destZ[4] = chunk.WalkZS[cell];
            destZ[5] = chunk.WalkZSW[cell];
            destZ[6] = chunk.WalkZW[cell];
            destZ[7] = chunk.WalkZNW[cell];

            cells.AddNode(cell, chunk.SourceZ[cell], walkMask, destZ);
        }
    }
}
