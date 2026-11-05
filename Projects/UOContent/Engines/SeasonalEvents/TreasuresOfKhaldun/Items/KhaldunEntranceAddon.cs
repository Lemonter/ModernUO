using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/KhaldunEntranceAddon.cs),
///     a YAAAG-generated decoration addon. It has no deed (it is placed by the generation pass).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KhaldunEntranceAddon : BaseAddon
{
    private static readonly int[,] _components =
    {
        { 476, 3, 5, 28 },
        { 465, 3, 2, 8 }, { 465, 3, 1, 10 }, { 465, 3, 0, 14 },
        { 466, 3, -1, 14 }, { 478, 2, 6, 28 }, { 476, 1, 5, 30 },
        { 465, -1, 5, 27 }, { 465, -3, 2, 5 }, { 465, -3, 1, 6 },
        { 465, -3, 0, 8 }, { 465, 2, 3, 5 }, { 465, 2, 2, 3 },
        { 1313, -2, -4, 3 }, { 1313, 0, -4, 0 }, { 1313, -1, -5, 1 },
        { 1313, 0, -6, 3 }, { 1313, -1, -6, 2 }, { 1313, -2, -6, 5 },
        { 1929, -2, 1, 4 }, { 1929, -2, 0, 8 }, { 1929, -1, 1, 4 },
        { 1929, 0, 1, 3 }, { 1929, 0, 0, 8 }, { 1929, 1, 1, 3 },
        { 1929, 1, 0, 8 }, { 1929, -1, 0, 8 }, { 1928, -2, 0, 2 },
        { 1928, -2, -1, 2 }, { 1928, -2, -2, 2 }, { 1928, -1, 0, 2 },
        { 1928, -1, -1, 2 }, { 1928, -1, -2, 0 }, { 1928, 0, 0, 1 },
        { 1928, 0, -1, 2 }, { 1928, 1, 0, 2 }, { 1928, 1, -1, 2 },
        { 1928, 1, -1, 8 }, { 1928, 1, -2, 0 }, { 1928, -2, -1, 8 },
        { 1928, -1, -1, 8 }, { 1928, 0, -1, 8 }, { 1928, 0, -2, 2 },
        { 1931, -2, -2, 8 }, { 1931, -2, -3, 1 }, { 1931, -1, -2, 6 },
        { 1931, -1, -3, 1 }, { 1931, 0, -3, 1 }, { 1931, 1, -2, 6 },
        { 1931, 1, -3, 0 }, { 1931, 0, -2, 8 }, { 1931, 0, -3, 2 },
        { 466, -3, -1, 11 }, { 466, 0, 5, 28 }, { 466, 2, 5, 24 },
        { 6005, -3, -2, 4 }, { 6005, -3, -3, 2 }
    };

    [Constructible]
    public KhaldunEntranceAddon()
    {
        for (var i = 0; i < _components.Length / 4; i++)
        {
            AddComponent(new AddonComponent(_components[i, 0]), _components[i, 1], _components[i, 2], _components[i, 3]);
        }

        AddComplexComponent(this, 1313, 0, 1, 2, 1, -1);
        AddComplexComponent(this, 1313, -1, 1, 3, 1, -1);
        AddComplexComponent(this, 1313, -1, 2, 3, 1, -1);
        AddComplexComponent(this, 1313, -2, 2, 5, 1, -1);
        AddComplexComponent(this, 1313, 0, 2, 3, 1, -1);
    }

    public override BaseAddonDeed Deed => null;

    private static void AddComplexComponent(
        BaseAddon addon, int item, int xOffset, int yOffset, int zOffset, int hue, int lightSource
    )
    {
        var component = new AddonComponent(item);

        if (hue != 0)
        {
            component.Hue = hue;
        }

        if (lightSource != -1)
        {
            component.Light = (LightType)lightSource;
        }

        addon.AddComponent(component, xOffset, yOffset, zOffset);
    }
}
