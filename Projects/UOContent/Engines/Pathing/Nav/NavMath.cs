using System;
using System.Runtime.CompilerServices;

namespace Server.Engines.Pathing.Nav;

public static class NavMath
{
    // Direction order: North, Right (NE), East, Down (SE), South, Left (SW), West, Up (NW).
    private static ReadOnlySpan<sbyte> DirXTable => [0, 1, 1, 1, 0, -1, -1, -1];
    private static ReadOnlySpan<sbyte> DirYTable => [-1, -1, 0, 1, 1, 1, 0, -1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DirX(int direction) => DirXTable[direction & 7];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DirY(int direction) => DirYTable[direction & 7];

    /// <summary>Octile distance in tenths of a tile: a straight step costs 10, a diagonal 14.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Octile(int x1, int y1, int x2, int y2)
    {
        var dx = Math.Abs(x1 - x2);
        var dy = Math.Abs(y1 - y2);
        return dx > dy ? 10 * dx + 4 * dy : 10 * dy + 4 * dx;
    }
}
