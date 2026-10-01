using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonWorld;

public static class MahaonMarkerAreaSystem
{
    private static readonly List<MahaonMarkerArea> Areas = new();
    private static bool _scanned;

    private static void EnsureScanned()
    {
        if (_scanned)
        {
            return;
        }

        _scanned = true;

        foreach (var item in World.Items.Values)
        {
            if (item is MahaonMarkerArea { Deleted: false } area)
            {
                Areas.Add(area);
            }
        }
    }

    public static MahaonMarkerArea Create(MahaonMarkerAreaKind kind, Map map, Rectangle2D bounds, string label)
    {
        EnsureScanned();

        var area = new MahaonMarkerArea(kind, map, bounds, label);
        Areas.Add(area);
        return area;
    }

    public static void Remove(MahaonMarkerArea area)
    {
        EnsureScanned();
        Areas.Remove(area);
        area.Delete();
    }

    public static IReadOnlyList<MahaonMarkerArea> All()
    {
        EnsureScanned();
        return Areas;
    }

    public static bool IsInside(Mobile from, MahaonMarkerAreaKind kind)
    {
        EnsureScanned();

        foreach (var area in Areas)
        {
            if (area.Deleted)
            {
                continue;
            }

            if (area.Kind == kind && area.Contains(from))
            {
                return true;
            }
        }

        return false;
    }
}
