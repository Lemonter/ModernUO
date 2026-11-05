using Server.Items;
using Server.Mobiles;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Generation.cs).
///     Per the season-port rules this controller is <b>not auto-started</b>: <see cref="Enabled" /> defaults to
///     false and nothing is placed until a GM flips it and calls <see cref="Generate" />. ServUO's automatic
///     on/off sync against <c>PointsSystem.TreasuresOfDoom.InSeason</c> is gone with the points system; the vase
///     spawner is replaced by a direct "top the vases up" pass (ServUO's <c>VaseSpawner</c> is not ported).
/// </summary>
public static class TreasuresOfDoomGeneration
{
    private const int VaseCount = 3;

    private static readonly Point3D VaseCenter = new(381, 133, -20);
    private static readonly Rectangle2D VaseBounds = new(257, 5, 249, 257);

    /// <summary>Автозапуск запрещён: включается вручную.</summary>
    public static bool Enabled { get; set; }

    public static void Generate()
    {
        Carey.Instance ??= Place(new Carey(), Carey.SpawnLocation, 2);
        Elizabeth.Instance ??= Place(new Elizabeth(), Elizabeth.SpawnLocation, 2);
        Owain.Instance ??= Place(new Owain(), Owain.SpawnLocation, 10);

        CreateVases(VaseCount - CountVases());

        var p = new Point3D(395, 220, -18);

        if (!AnyNearby<DoomPlaque>(p))
        {
            new DoomPlaque().MoveToWorld(p, Map.Malas);
        }

        p = new Point3D(388, 221, -20);

        if (!AnyNearby<DoomSign>(p))
        {
            new DoomSign().MoveToWorld(p, Map.Malas);
        }

        p = new Point3D(66, 223, -1);

        if (!AnyNearby<Moongate>(p))
        {
            var moongate = new Moongate
            {
                ItemID = 0x4BCB,
                Hue = 2676,
                Dispellable = false,
                Target = new Point3D(396, 220, -20),
                TargetMap = Map.Malas
            };

            moongate.MoveToWorld(p, Map.Malas);
        }
    }

    public static void Remove()
    {
        Carey.Instance?.Delete();
        Elizabeth.Instance?.Delete();
        Owain.Instance?.Delete();
    }

    private static T Place<T>(T mobile, Point3D location, int range) where T : BaseCreature
    {
        mobile.MoveToWorld(location, Map.Malas);
        mobile.Home = location;
        mobile.RangeHome = range;

        return mobile;
    }

    private static bool AnyNearby<T>(Point3D p) where T : Item
    {
        foreach (var item in Map.Malas.GetItemsInRange<T>(p, 0))
        {
            if (!item.Deleted)
            {
                return true;
            }
        }

        return false;
    }

    private static int CountVases()
    {
        var count = 0;

        foreach (var vase in Map.Malas.GetItemsInRange<AncientClayVase>(VaseCenter, 180))
        {
            if (!vase.Deleted && VaseBounds.Contains(vase.Location))
            {
                count++;
            }
        }

        return count;
    }

    private static void CreateVases(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Point3D p;

            do
            {
                p = Utility.RandomPointIn(VaseBounds, Map.Malas);
            }
            while (p == Point3D.Zero || !Map.Malas.CanSpawnMobile(p));

            var vase = new AncientClayVase(true)
            {
                Movable = false
            };

            vase.MoveToWorld(p, Map.Malas);
        }
    }
}
