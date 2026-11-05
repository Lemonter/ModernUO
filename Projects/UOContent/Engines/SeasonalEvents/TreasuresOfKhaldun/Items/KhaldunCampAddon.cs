using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/KhaldunCampAddon.cs),
///     a YAAAG-generated decoration addon. It has no deed (it is placed by the generation pass).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KhaldunCampAddon : BaseAddon
{
    private static readonly int[,] _components =
    {
        { 2879, -2, 6, 4 }, { 2883, 1, 10, 4 }, { 2879, -2, 7, 4 },
        { 2880, 0, 6, 4 }, { 2880, -1, 6, 4 }, { 875, 5, 5, 4 },
        { 875, -3, 5, 5 }, { 3014, 6, 6, 12 }, { 2421, 5, 8, 3 },
        { 2884, 2, 10, 4 }, { 2879, -2, 8, 4 }, { 875, 5, 10, 5 },
        { 2880, 0, -2, 4 }, { 2988, 0, -5, 20 }, { 2880, -2, -9, 3 },
        { 2880, -3, -9, 3 }, { 2990, 2, 2, 23 }, { 2880, -2, -2, 4 },
        { 2880, -3, -2, 4 }, { 2880, -1, -2, 4 }, { 2879, -5, -8, 3 },
        { 2879, -5, -6, 3 }, { 2879, -5, -7, 3 }, { 2880, -5, -9, 3 },
        { 880, -6, 1, 1 }, { 875, -6, -10, 1 }, { 2880, -4, -9, 3 },
        { 2880, -4, -2, 4 }, { 2879, -5, 0, 4 }, { 2879, -5, -2, 4 },
        { 2879, -5, -1, 4 }, { 875, -6, -3, 5 },
        { 2880, 1, 6, 4 }, { 2880, 2, 6, 4 }, { 2980, 6, 11, 13 },
        { 542, 7, 5, -1 }, { 542, 7, 6, -1 }, { 542, 7, 7, -1 },
        { 542, 7, 8, -1 }, { 542, 7, 9, -1 }, { 542, 7, 10, -1 }
    };

    [Constructible]
    public KhaldunCampAddon()
    {
        for (var i = 0; i < _components.Length / 4; i++)
        {
            AddComponent(new AddonComponent(_components[i, 0]), _components[i, 1], _components[i, 2], _components[i, 3]);
        }

        AddComplexComponent(this, 880, -3, 9, 5, 2500);
        AddComplexComponent(this, 880, -3, 8, 5, 1835);
        AddComplexComponent(this, 881, 0, 5, 5, 2500);
        AddComplexComponent(this, 873, -2, 10, 5, 2500);
        AddComplexComponent(this, 876, -3, 10, 5, 1835);
        AddComplexComponent(this, 881, -1, 5, 5, 1835);
        AddComplexComponent(this, 880, -3, 6, 5, 1835);
        AddComplexComponent(this, 873, 0, 10, 5, 2500);
        AddComplexComponent(this, 873, -1, 10, 5, 1835);
        AddComplexComponent(this, 880, -3, 7, 5, 2500);
        AddComplexComponent(this, 881, 1, 5, 5, 1835);
        AddComplexComponent(this, 4012, 5, 8, 3, 0, 2);
        AddComplexComponent(this, 881, -2, 5, 5, 2500);
        AddComplexComponent(this, 881, 0, -3, 5, 1159);
        AddComplexComponent(this, 877, -1, -10, 0, 1911);
        AddComplexComponent(this, 874, -1, -9, 1, 1912);
        AddComplexComponent(this, 874, -1, -6, 3, 1912);
        AddComplexComponent(this, 881, -2, -3, 5, 1159);
        AddComplexComponent(this, 881, -3, -3, 5, 1917);
        AddComplexComponent(this, 873, -2, -5, 3, 1912);
        AddComplexComponent(this, 877, 1, -3, 5, 1917);
        AddComplexComponent(this, 872, -1, -5, 3, 1911);
        AddComplexComponent(this, 881, -1, -3, 5, 1917);
        AddComplexComponent(this, 873, -3, 2, 5, 1917);
        AddComplexComponent(this, 873, -2, 2, 5, 1159);
        AddComplexComponent(this, 873, -1, 2, 5, 1917);
        AddComplexComponent(this, 873, 0, 2, 5, 1159);
        AddComplexComponent(this, 873, -3, -5, 3, 1911);
        AddComplexComponent(this, 872, 1, 2, 4, 1917);
        AddComplexComponent(this, 874, 1, 1, 5, 1159);
        AddComplexComponent(this, 874, 1, -2, 5, 1159);
        AddComplexComponent(this, 878, -3, -10, 3, 1911);
        AddComplexComponent(this, 878, -2, -10, 2, 1912);
        AddComplexComponent(this, 876, -6, -5, 4, 1912);
        AddComplexComponent(this, 876, -6, 2, 5, 1159);
        AddComplexComponent(this, 873, -5, 2, 5, 1917);
        AddComplexComponent(this, 873, -4, 2, 5, 1159);
        AddComplexComponent(this, 873, -5, -5, 3, 1911);
        AddComplexComponent(this, 873, -4, -5, 3, 1912);
        AddComplexComponent(this, 880, -6, -1, 5, 1917);
        AddComplexComponent(this, 880, -6, 0, 5, 1159);
        AddComplexComponent(this, 881, -4, -3, 5, 1159);
        AddComplexComponent(this, 881, -5, -3, 4, 1917);
        AddComplexComponent(this, 880, -6, 1, 5, 1917);
        AddComplexComponent(this, 880, -6, -2, 5, 1159);
        AddComplexComponent(this, 880, -6, -8, 3, 1911);
        AddComplexComponent(this, 880, -6, -9, 3, 1912);
        AddComplexComponent(this, 880, -6, -7, 3, 1912);
        AddComplexComponent(this, 878, -4, -10, 3, 1912);
        AddComplexComponent(this, 878, -5, -10, 3, 1911);
        AddComplexComponent(this, 880, -6, -6, 4, 1911);
    }

    public override BaseAddonDeed Deed => null;

    private static void AddComplexComponent(BaseAddon addon, int item, int xOffset, int yOffset, int zOffset, int hue, int lightSource = -1)
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
