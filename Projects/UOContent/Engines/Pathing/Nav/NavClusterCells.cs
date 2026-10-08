using System;

namespace Server.Engines.Pathing.Nav;

/// <summary>
/// Raw walkability of one 16x16 cluster, plus its connected-component labels. A node is one
/// standable surface of one cell; a cell holds as many nodes as it has stacked surfaces (a bridge
/// over a path, the floors of a building).
///
/// The same instance is refilled for cluster after cluster, so every array is reused and grows
/// only when a cluster with more nodes than any before it comes along.
/// </summary>
public sealed class NavClusterCells
{
    public const int Size = 16;
    public const int Shift = 4;
    public const int CellCount = Size * Size;

    /// <summary>Matches a step's destination Z to a surface. Surfaces stand at least PersonHeight
    /// (16) apart, so anything under half of that is unambiguous.</summary>
    public const int SurfaceTolerance = 8;

    public const ushort Unlabeled = ushort.MaxValue;

    private readonly byte[] _nodeCount = new byte[CellCount];
    private readonly ushort[] _nodeStart = new ushort[CellCount];

    private sbyte[] _z = new sbyte[CellCount];
    private byte[] _mask = new byte[CellCount];
    private sbyte[] _destZ = new sbyte[CellCount * 8];
    private ushort[] _component = new ushort[CellCount];
    private byte[] _cell = new byte[CellCount];
    private int[] _stack = new int[CellCount];

    private int _lastCell = -1;

    public int ClusterX { get; private set; }
    public int ClusterY { get; private set; }
    public int NodeCount { get; private set; }
    public int ComponentCount { get; private set; }

    public void Reset(int clusterX, int clusterY)
    {
        ClusterX = clusterX;
        ClusterY = clusterY;
        NodeCount = 0;
        ComponentCount = 0;
        _lastCell = -1;
        Array.Clear(_nodeCount);
    }

    /// <summary>
    /// Adds a standable surface. Cells must arrive in ascending cell order and each cell's surfaces
    /// in ascending Z — the order a sweep over the cluster produces naturally.
    /// </summary>
    /// <param name="cell">Cell index, (localY &lt;&lt; 4) | localX.</param>
    /// <param name="z">Standing Z of the surface.</param>
    /// <param name="mask">Bit d set when a walker can step from here in Direction d. Raw: no
    /// diagonal corner-cut applied.</param>
    /// <param name="destZ">Destination Z per direction; read only where the mask bit is set.</param>
    public void AddNode(int cell, sbyte z, byte mask, ReadOnlySpan<sbyte> destZ)
    {
        if (cell < _lastCell)
        {
            throw new ArgumentException("Cells must be added in ascending order.", nameof(cell));
        }

        if (cell != _lastCell)
        {
            _nodeStart[cell] = (ushort)NodeCount;
            _lastCell = cell;
        }

        EnsureCapacity(NodeCount + 1);

        var node = NodeCount++;
        _nodeCount[cell]++;
        _z[node] = z;
        _mask[node] = mask;
        destZ[..8].CopyTo(_destZ.AsSpan(node * 8, 8));
        _component[node] = Unlabeled;
        _cell[node] = (byte)cell;
    }

    private void EnsureCapacity(int nodes)
    {
        if (nodes <= _z.Length)
        {
            return;
        }

        var size = Math.Max(nodes, _z.Length * 2);
        Array.Resize(ref _z, size);
        Array.Resize(ref _mask, size);
        Array.Resize(ref _component, size);
        Array.Resize(ref _cell, size);
        Array.Resize(ref _stack, size);
        Array.Resize(ref _destZ, size * 8);
    }

    public int GetNodeCount(int cell) => _nodeCount[cell];

    public int GetNodeStart(int cell) => _nodeStart[cell];

    public sbyte GetZ(int node) => _z[node];

    public ushort GetComponent(int node) => _component[node];

    /// <summary>Raw step mask of a node — see <see cref="AddNode"/>.</summary>
    public byte GetRawMask(int node) => _mask[node];

    /// <summary>
    /// The step mask with the strict diagonal corner-cut a non-GM player obeys: a diagonal step
    /// needs both flanking cardinals open. Bots walk as players, so the graph is built on this.
    /// </summary>
    public byte GetMask(int node)
    {
        var raw = _mask[node];
        var mask = raw;

        for (var d = 1; d < 8; d += 2)
        {
            var left = 1 << ((d - 1) & 7);
            var right = 1 << ((d + 1) & 7);

            if ((raw & left) == 0 || (raw & right) == 0)
            {
                mask &= (byte)~(1 << d);
            }
        }

        return mask;
    }

    public sbyte GetDestZ(int node, int direction) => _destZ[node * 8 + direction];

    /// <summary>The node of <paramref name="cell"/> whose surface is nearest <paramref name="z"/>,
    /// within <see cref="SurfaceTolerance"/>; -1 when there is none.</summary>
    public int FindNode(int cell, int z)
    {
        var count = _nodeCount[cell];
        if (count == 0)
        {
            return -1;
        }

        var start = _nodeStart[cell];
        var best = -1;
        var bestDelta = SurfaceTolerance + 1;

        for (var i = 0; i < count; i++)
        {
            var delta = Math.Abs(_z[start + i] - z);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                best = start + i;
            }
        }

        return best;
    }

    /// <summary>The cell a node belongs to.</summary>
    public int GetCell(int node) => _cell[node];

    /// <summary>
    /// Labels the nodes into components that are mutually walkable without leaving the cluster: two
    /// neighbours join only when each can step to the other. One-way steps (a drop off a ledge)
    /// stay out of the labelling and become directed graph edges instead, so every component is a
    /// place a walker can cross in any direction.
    ///
    /// Deterministic for identical input — the runtime relabels clusters to locate positions and
    /// must land on the same component numbers the bake did.
    /// </summary>
    public void Label()
    {
        ComponentCount = 0;
        _component.AsSpan(0, NodeCount).Fill(Unlabeled);

        var stack = _stack;

        for (var seed = 0; seed < NodeCount; seed++)
        {
            if (_component[seed] != Unlabeled)
            {
                continue;
            }

            var comp = (ushort)ComponentCount++;
            _component[seed] = comp;
            var top = 0;
            stack[top++] = seed;

            while (top > 0)
            {
                var node = stack[--top];

                var cell = GetCell(node);
                var mask = GetMask(node);

                for (var d = 0; d < 8; d++)
                {
                    if ((mask & (1 << d)) == 0)
                    {
                        continue;
                    }

                    var neighbour = FindNeighbourInCluster(cell, d, GetDestZ(node, d));
                    if (neighbour < 0 || _component[neighbour] != Unlabeled)
                    {
                        continue;
                    }

                    // Mutual: the neighbour must be able to step straight back.
                    var back = (d + 4) & 7;
                    if ((GetMask(neighbour) & (1 << back)) == 0 ||
                        Math.Abs(GetDestZ(neighbour, back) - _z[node]) > SurfaceTolerance)
                    {
                        continue;
                    }

                    _component[neighbour] = comp;
                    stack[top++] = neighbour;
                }
            }
        }
    }

    /// <summary>The node a step lands on when it stays inside this cluster; -1 when the step leaves
    /// the cluster or lands on no known surface.</summary>
    public int FindNeighbourInCluster(int cell, int direction, int destZ)
    {
        var x = (cell & 15) + NavMath.DirX(direction);
        var y = (cell >> 4) + NavMath.DirY(direction);

        if ((uint)x >= Size || (uint)y >= Size)
        {
            return -1;
        }

        return FindNode((y << 4) | x, destZ);
    }
}
