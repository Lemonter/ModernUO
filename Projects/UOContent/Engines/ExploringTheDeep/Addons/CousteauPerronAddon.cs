using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Addons/CousteauPerronAddon.cs). The frozen
///     tinker's workshop in the Ice Dungeon, exactly the components ServUO places.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CousteauPerronAddon : BaseAddon
{
    private static readonly int[,] _components =
    {
        { 2602, 1, 2, 0 }, { 7828, 1, 3, 3 }, { 4602, 0, 1, 0 },
        { 4601, -1, 1, 0 }, { 1560, 0, 0, 26 }, { 1560, 1, 0, 26 },
        { 1560, 2, 0, 26 }, { 1537, 0, 1, 14 }, { 1537, -1, 1, 14 },
        { 1537, 1, 1, 14 }, { 1544, 1, 0, 22 }, { 1544, -1, 0, 22 },
        { 1544, 0, 0, 22 }, { 740, 0, -2, 0 }, { 740, -1, -2, 0 },
        { 741, -2, -1, 0 }, { 741, -2, 0, 0 }, { 738, -2, 1, 0 },
        { 734, 1, 1, 0 }, { 736, 1, 0, 0 }, { 736, 1, -1, 0 },
        { 739, 1, -2, 0 }
    };

    [Constructible]
    public CousteauPerronAddon()
    {
        for (var i = 0; i < _components.Length / 4; i++)
        {
            AddComponent(new AddonComponent(_components[i, 0]), _components[i, 1], _components[i, 2], _components[i, 3]);
        }

        AddComponent(new AddonComponent(6571), 1, 3, 3);
        AddComponent(new AddonComponent(4012), 1, 3, 0);
    }
}
