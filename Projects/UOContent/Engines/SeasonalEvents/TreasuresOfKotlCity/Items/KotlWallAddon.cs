using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/KotlWallAddon.cs).
///     A YAAAG-generated rock barrier for the Kotl City entrance; it has no deed.
/// </summary>
[SerializationGenerator(0, false)]
public partial class KotlWallAddon : BaseAddon
{
    private static readonly int[,] _components =
    {
        { 6270, -1, 3, 0 }, { 6270, -3, 4, 0 }, { 6270, -3, 2, 0 },
        { 4943, -3, 1, 0 }, { 4944, -2, 1, 0 }, { 4954, 0, 1, 0 },
        { 3117, 1, 4, 0 }, { 18129, 0, 5, 0 }, { 4958, -1, 2, 0 },
        { 4957, -1, 3, 0 }, { 4956, -2, 4, 1 }, { 17864, 0, 6, 0 },
        { 18116, 0, 2, 0 }, { 18129, 0, 3, 0 }, { 4954, -1, 1, 0 },
        { 4954, -2, 6, 0 }, { 4955, -2, 5, 0 }, { 17850, 0, 7, 0 },
        { 6270, -3, -3, 0 }, { 4945, -2, 0, 0 }, { 4954, -2, -4, 0 },
        { 4955, -2, -5, 0 }, { 4955, 0, 0, 0 }, { 3117, 1, -7, 0 },
        { 3117, -1, -1, 0 }, { 18129, 0, -5, 0 }, { 4949, -1, -4, 0 },
        { 4955, -1, 0, 0 }, { 17864, 0, -6, 0 }, { 4952, -1, -2, 0 },
        { 17850, 0, -7, 0 }, { 4953, -1, -3, 0 }, { 4950, -1, -5, 0 },
        { 18111, 0, -4, 0 }, { 4951, -2, -1, 0 }, { 18116, 0, -2, 0 },
        { 4948, -1, -7, 0 }, { 4947, -1, -6, 0 }, { 18129, 0, -1, 0 },
        { 4962, -2, 7, 0 }
    };

    [Constructible]
    public KotlWallAddon()
    {
        for (var i = 0; i < _components.Length / 4; i++)
        {
            AddComponent(
                new AddonComponent(_components[i, 0]) { Hue = 2591 },
                _components[i, 1],
                _components[i, 2],
                _components[i, 3]
            );
        }
    }

    public override BaseAddonDeed Deed => null;
}
