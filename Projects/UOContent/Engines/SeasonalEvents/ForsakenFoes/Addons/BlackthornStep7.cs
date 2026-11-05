using ModernUO.Serialization;
using static Server.Items.ForsakenFoesAddonHelper;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Addon/BlackthornStep7.cs), a YAAAG-generated
///     decoration addon — the seventh section of Blackthorn's dungeon. The component table is ServUO's, unchanged. ServUO
///     declared no deed for it (the generation pass places it at a fixed spot), so it has none here either.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BlackthornStep7 : BaseAddon
{

    private static readonly int[,] _components =
    {
        { 6009, -9, 1, 0 }, { 6009, -9, 2, 0 }, { 6012, -9, 2, 0 }, { 6012, -8, 2, 0 },
        { 7099, -8, 1, 0 }, { 7087, -8, 0, 0 }, { 7087, -8, -1, 0 }, { 6004, -9, -1, 0 },
        { 6011, -9, -1, 0 }, { 6009, -9, 0, 0 }, { 6012, -9, 2, 0 }, { 6011, -8, 1, 0 },
        { 7403, -1, -3, 0 }, { 7816, -3, 9, 0 }, { 7390, -1, -8, 0 }, { 9779, -3, -8, 0 },
    };

    [Constructible]
    public BlackthornStep7()
    {
        for (var i = 0; i < _components.Length / 4; i++)
        {
            AddComponent(new AddonComponent(_components[i, 0]), _components[i, 1], _components[i, 2], _components[i, 3]);
        }

        AddComplexComponent(this, 7100, 9, 1, 0, 2500, -1);
        AddComplexComponent(this, 7087, 9, 0, 0, 2500, -1);
        AddComplexComponent(this, 7088, 9, -1, 0, 2500, -1);
        AddComplexComponent(this, 7054, 8, -1, 0, 1175, -1);
        AddComplexComponent(this, 7056, 7, -1, 0, 1175, -1);
        AddComplexComponent(this, 7059, 8, 1, 0, 1175, -1);
        AddComplexComponent(this, 7076, 6, 1, 0, 2500, -1);
        AddComplexComponent(this, 7049, 6, 0, 6, 2500, -1);
        AddComplexComponent(this, 12320, 6, 0, 0, 1175, -1);
        AddComplexComponent(this, 7075, 6, -1, 0, 2500, -1);
        AddComplexComponent(this, 4962, 3, 2, 0, 1109, -1);
        AddComplexComponent(this, 4545, -6, 2, 0, 1109, -1);
        AddComplexComponent(this, 4545, -5, 2, 0, 1109, -1);
        AddComplexComponent(this, 4545, -4, 2, 0, 1109, -1);
        AddComplexComponent(this, 4545, -3, 2, 0, 1109, -1);
        AddComplexComponent(this, 4545, -2, 2, 0, 1109, -1);
        AddComplexComponent(this, 4545, -1, 2, 0, 1109, -1);
        AddComplexComponent(this, 4545, 0, 2, 0, 1109, -1);
        AddComplexComponent(this, 4545, 1, 2, 0, 1109, -1);
        AddComplexComponent(this, 4545, 2, 2, 0, 1109, -1);
        AddComplexComponent(this, 22000, -4, 4, 0, 2548, -1);
        AddComplexComponent(this, 22000, -5, 4, 0, 2548, -1);
        AddComplexComponent(this, 22000, -5, 5, 0, 2548, -1);
        AddComplexComponent(this, 22000, -4, 5, 0, 2548, -1);
        AddComplexComponent(this, 22000, -4, 6, 0, 2548, -1);
        AddComplexComponent(this, 22000, -5, 6, 0, 2548, -1);
        AddComplexComponent(this, 22000, -5, 7, 0, 2548, -1);
        AddComplexComponent(this, 22000, -4, 7, 0, 2548, -1);
        AddComplexComponent(this, 22000, -4, 8, 0, 2548, -1);
        AddComplexComponent(this, 22000, -5, 8, 0, 2548, -1);
        AddComplexComponent(this, 22000, -5, 9, 0, 2548, -1);
        AddComplexComponent(this, 22000, -4, 9, 0, 2548, -1);
        AddComplexComponent(this, 22000, -3, 9, 0, 2548, -1);
        AddComplexComponent(this, 22000, -2, 9, 0, 2548, -1);
        AddComplexComponent(this, 22000, -1, 9, 0, 2548, -1);
        AddComplexComponent(this, 22000, 0, 9, 0, 2548, -1);
        AddComplexComponent(this, 22000, 1, 9, 0, 2548, -1);
        AddComplexComponent(this, 22000, 2, 8, 0, 2548, -1);
        AddComplexComponent(this, 22000, 2, 7, 0, 2548, -1);
        AddComplexComponent(this, 22000, 2, 6, 0, 2548, -1);
        AddComplexComponent(this, 22000, 2, 5, 0, 2548, -1);
        AddComplexComponent(this, 22000, 2, 4, 0, 2548, -1);
        AddComplexComponent(this, 22000, 2, 3, 0, 2548, -1);
        AddComplexComponent(this, 22000, 1, 3, 0, 2548, -1);
        AddComplexComponent(this, 22000, 0, 3, 0, 2548, -1);
        AddComplexComponent(this, 22000, -1, 3, 0, 2548, -1);
        AddComplexComponent(this, 22000, -2, 3, 0, 2548, -1);
        AddComplexComponent(this, 22000, -3, 3, 0, 2548, -1);
        AddComplexComponent(this, 22000, -4, 3, 0, 2548, -1);
        AddComplexComponent(this, 22000, -6, 3, 0, 2548, -1);
        AddComplexComponent(this, 22000, -5, 3, 0, 2548, -1);
        AddComplexComponent(this, 4967, 2, 2, 0, 1109, -1);
        AddComplexComponent(this, 4964, 1, 2, 2, 1109, -1);
        AddComplexComponent(this, 4970, 1, 2, 0, 1109, -1);
        AddComplexComponent(this, 4963, 0, 2, 0, 1109, -1);
        AddComplexComponent(this, 4967, -1, 2, 0, 1109, -1);
        AddComplexComponent(this, 4963, -2, 2, 0, 1109, -1);
        AddComplexComponent(this, 4964, -3, 2, 2, 1109, -1);
        AddComplexComponent(this, 4970, -3, 2, 0, 1109, -1);
        AddComplexComponent(this, 4967, -4, 2, 0, 1109, -1);
        AddComplexComponent(this, 4963, -5, 2, 0, 1109, -1);
        AddComplexComponent(this, 4962, -6, 2, 0, 1109, -1);
        AddComplexComponent(this, 22000, -1, -7, 0, 2548, -1);
        AddComplexComponent(this, 22000, -1, -8, 0, 2548, -1);
        AddComplexComponent(this, 22000, -1, -9, 0, 2548, -1);
        AddComplexComponent(this, 22000, -2, -9, 0, 2548, -1);
        AddComplexComponent(this, 22000, -2, -8, 0, 2548, -1);
        AddComplexComponent(this, 22000, -2, -7, 0, 2548, -1);
        AddComplexComponent(this, 22000, -3, -7, 0, 2548, -1);
        AddComplexComponent(this, 22000, -3, -8, 0, 2548, -1);
        AddComplexComponent(this, 22000, -3, -9, 0, 2548, -1);
        AddComplexComponent(this, 22000, -4, -9, 0, 2548, -1);
        AddComplexComponent(this, 22000, -4, -8, 0, 2548, -1);
        AddComplexComponent(this, 22000, -5, -8, 0, 2548, -1);
        AddComplexComponent(this, 22000, -4, -7, 0, 2548, -1);
        AddComplexComponent(this, 22000, -5, -7, 0, 2548, -1);
        AddComplexComponent(this, 3220, 0, -8, 0, 1109, -1);
        AddComplexComponent(this, 3220, 0, -7, 0, 1109, -1);
        AddComplexComponent(this, 3255, -5, -6, 0, 1109, -1);
        AddComplexComponent(this, 3220, -4, -6, 0, 1109, -1);
    }
}
