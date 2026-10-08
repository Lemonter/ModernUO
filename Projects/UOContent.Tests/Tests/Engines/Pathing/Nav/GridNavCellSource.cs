using System;
using System.Collections.Generic;
using Server.Engines.Pathing.Nav;

namespace Server.Tests.Pathfinding.Nav;

/// <summary>
/// A synthetic map for nav-graph tests, drawn as text. Each layer is a set of rows at one Z:
/// '.' is walkable floor at the layer's Z, a digit d is floor at Z = layerZ + d * 2 (a ramp),
/// anything else is nothing. A walker steps to a neighbour whose surface is within 2 Z of its own,
/// so ramps climb between layers and a bridge 20 Z above a road stays separate from it.
/// </summary>
public sealed class GridNavCellSource : INavCellSource
{
    private readonly List<(int z, string[] rows)> _layers = [];

    public GridNavCellSource(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; }
    public int Height { get; }

    public GridNavCellSource Layer(int z, params string[] rows)
    {
        _layers.Add((z, rows));
        return this;
    }

    /// <summary>Fills a rectangle of the first layer (or a new one at <paramref name="z"/>) with
    /// floor or walls — handy for large open grids.</summary>
    public GridNavCellSource Fill(int z, int x, int y, int w, int h, char c)
    {
        var layerIndex = _layers.FindIndex(l => l.z == z);
        if (layerIndex < 0)
        {
            var rows = new string[Height];
            Array.Fill(rows, new string(' ', Width));
            _layers.Add((z, rows));
            layerIndex = _layers.Count - 1;
        }

        var layer = _layers[layerIndex].rows;
        for (var yy = y; yy < y + h; yy++)
        {
            var chars = layer[yy].PadRight(Width).ToCharArray();
            for (var xx = x; xx < x + w; xx++)
            {
                chars[xx] = c;
            }

            layer[yy] = new string(chars);
        }

        return this;
    }

    private int Surfaces(int x, int y, Span<int> zs)
    {
        var count = 0;
        if ((uint)x >= Width || (uint)y >= Height)
        {
            return 0;
        }

        foreach (var (z, rows) in _layers)
        {
            if (y >= rows.Length || x >= rows[y].Length)
            {
                continue;
            }

            var c = rows[y][x];
            if (c == '.')
            {
                zs[count++] = z;
            }
            else if (c is >= '0' and <= '9')
            {
                zs[count++] = z + (c - '0') * 2;
            }
        }

        zs[..count].Sort();
        return count;
    }

    public void FillCluster(int clusterX, int clusterY, NavClusterCells cells)
    {
        cells.Reset(clusterX, clusterY);

        Span<int> zs = stackalloc int[8];
        Span<int> nzs = stackalloc int[8];
        Span<sbyte> destZ = stackalloc sbyte[8];

        for (var cell = 0; cell < NavClusterCells.CellCount; cell++)
        {
            var x = (clusterX << 4) + (cell & 15);
            var y = (clusterY << 4) + (cell >> 4);
            var count = Surfaces(x, y, zs);

            for (var i = 0; i < count; i++)
            {
                byte mask = 0;
                destZ.Clear();

                for (var d = 0; d < 8; d++)
                {
                    var n = Surfaces(x + NavMath.DirX(d), y + NavMath.DirY(d), nzs);
                    for (var j = 0; j < n; j++)
                    {
                        if (Math.Abs(nzs[j] - zs[i]) <= 2)
                        {
                            mask |= (byte)(1 << d);
                            destZ[d] = (sbyte)nzs[j];
                            break;
                        }
                    }
                }

                cells.AddNode(cell, (sbyte)zs[i], mask, destZ);
            }
        }
    }
}
