namespace Server.Systems.MahaonMining;

/// <summary>
///     Mine interiors aren't real dungeons made of map tiles — they're carved out of item
///     placement (floor tiles + rock-wall tiles you dig through) in a reserved grid of
///     plots off in unused map space, the same trick the engine already uses for jail
///     cells (see JailSystem's "Felucca void" comment). Each dug mountainside gets its own
///     plot so mines never bleed into each other.
/// </summary>
public static class MineVoidAllocator
{
    // Same neighborhood as JailSystem's void (5276-5306, 1164-1184) but offset well clear
    // of it. Adjust if your shard's actual void-safe coordinates differ.
    public const int VoidBaseX = 5400;
    public const int VoidBaseY = 1400;
    public const int PlotSize = 21; // odd, so there's a true center tile
    public const int PlotSpacing = 30; // gap between plots so digging can't cross into a neighbor
    public const int GridWidth = 40; // plots per row before wrapping to the next row

    private static MineVoidAllocatorPersistence _instance;

    public static void Configure()
    {
        _instance = new MineVoidAllocatorPersistence();
    }

    public static int NextPlotIndex() => MineVoidAllocatorPersistence.Allocate();

    public static Point3D GetPlotOrigin(int plotIndex)
    {
        var col = plotIndex % GridWidth;
        var row = plotIndex / GridWidth;

        return new Point3D(VoidBaseX + col * PlotSpacing, VoidBaseY + row * PlotSpacing, 0);
    }

    public static Point3D GetPlotCenter(int plotIndex)
    {
        var origin = GetPlotOrigin(plotIndex);
        var half = PlotSize / 2;
        return new Point3D(origin.X + half, origin.Y + half, 0);
    }

    /// <summary>
    ///     True if the given point falls within any allocated plot's bounds (used to keep
    ///     digging from expanding a floor past its own plot into the buffer zone).
    /// </summary>
    public static bool TryGetPlotLocal(Point3D loc, out int plotIndex, out int localX, out int localY)
    {
        var relX = loc.X - VoidBaseX;
        var relY = loc.Y - VoidBaseY;

        var col = (int)System.Math.Floor(relX / (double)PlotSpacing);
        var row = (int)System.Math.Floor(relY / (double)PlotSpacing);

        localX = relX - col * PlotSpacing;
        localY = relY - row * PlotSpacing;

        plotIndex = row * GridWidth + col;

        return col >= 0 && localX < PlotSize && localY < PlotSize && localY >= 0;
    }

    private class MineVoidAllocatorPersistence : GenericPersistence
    {
        private static int _next;

        public MineVoidAllocatorPersistence() : base("MahaonMineVoidAllocator", 1)
        {
        }

        public static int Allocate() => _next++;

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt(_next);
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version
            _next = reader.ReadEncodedInt();
        }
    }
}
