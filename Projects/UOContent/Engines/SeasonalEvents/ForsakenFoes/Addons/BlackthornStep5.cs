using ModernUO.Serialization;
using static Server.Items.ForsakenFoesAddonHelper;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Addon/BlackthornStep5.cs), a YAAAG-generated
///     decoration addon — the fifth section of Blackthorn's dungeon. The component table is ServUO's, unchanged. ServUO
///     declared no deed for it (the generation pass places it at a fixed spot), so it has none here either.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BlackthornStep5 : BaseAddon
{

    private static readonly int[,] _components =
    {
        { 4306, 7, 7, 0 }, { 4317, 5, 7, 0 }, { 4314, 5, 7, 0 }, { 4308, 3, 7, 0 },
        { 4280, -1, 7, 0 }, { 4286, -7, -3, 0 }, { 4287, -6, -4, 0 }, { 4288, -5, -5, 0 },
        { 9750, -7, -6, 3 }, { 4335, 7, 3, 0 }, { 4335, 4, -3, 0 }, { 4335, 3, -4, 0 },
        { 4335, 2, -4, 0 }, { 4335, -1, -4, 0 }, { 4335, -5, -3, 0 }, { 4335, -7, -2, 0 },
        { 4335, -6, 3, 0 }, { 4335, -3, 3, 0 }, { 4335, 2, 3, 0 }, { 7391, 7, -2, 0 },
        { 7390, 6, -2, 0 }, { 3118, 6, -4, 0 }, { 3118, 8, -3, 0 }, { 4338, -7, 1, 0 },
        { 4339, -3, -4, 0 }, { 3814, -4, 1, 0 }, { 3814, -3, 1, 0 }, { 3811, 8, -5, 0 },
        { 3811, 0, 1, 0 }, { 3811, 2, 0, 0 }, { 3811, 0, 0, 6 },
    };

    [Constructible]
    public BlackthornStep5()
    {
        for (var i = 0; i < _components.Length / 4; i++)
        {
            AddComponent(new AddonComponent(_components[i, 0]), _components[i, 1], _components[i, 2], _components[i, 3]);
        }

        AddComplexComponent(this, 3309, 9, -6, 0, 63, -1);
        AddComplexComponent(this, 8445, -6, -4, 17, 2500, -1);
        AddComplexComponent(this, 8424, 4, -1, 0, 2548, -1);
        AddComplexComponent(this, 3308, 3, -6, 5, 63, -1);
        AddComplexComponent(this, 3308, -4, -6, 5, 63, -1);
        AddComplexComponent(this, 3310, -5, -6, 5, 63, -1);
        AddComplexComponent(this, 3310, 2, -6, 10, 63, -1);
        AddComplexComponent(this, 3309, -7, -6, 0, 63, -1);
        AddComplexComponent(this, 3309, -6, -6, 0, 63, -1);
        AddComplexComponent(this, 3309, -2, -6, 0, 63, -1);
        AddComplexComponent(this, 3309, 0, -6, 0, 63, -1);
        AddComplexComponent(this, 3309, 5, -6, 0, 63, -1);
        AddComplexComponent(this, 3309, 6, -6, 0, 63, -1);
        AddComplexComponent(this, 8424, -8, -4, 0, 2548, -1);
        AddComplexComponent(this, 3314, -9, -5, 0, 63, -1);
        AddComplexComponent(this, 3314, -9, -2, 5, 63, -1);
        AddComplexComponent(this, 3314, -9, 2, 5, 63, -1);
        AddComplexComponent(this, 3313, -9, 0, 5, 63, -1);
        AddComplexComponent(this, 3313, -9, 5, 0, 63, -1);
        AddComplexComponent(this, 3313, -9, 4, 0, 63, -1);
    }
}
