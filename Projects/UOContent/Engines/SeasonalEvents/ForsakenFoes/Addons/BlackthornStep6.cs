using ModernUO.Serialization;
using static Server.Items.ForsakenFoesAddonHelper;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Addon/BlackthornStep6.cs), a YAAAG-generated
///     decoration addon — the sixth section of Blackthorn's dungeon. The component table is ServUO's, unchanged. ServUO
///     declared no deed for it (the generation pass places it at a fixed spot), so it has none here either.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BlackthornStep6 : BaseAddon
{

    private static readonly int[,] _components =
    {
        { 18324, 11, 1, 4 }, { 18324, 9, 1, 4 }, { 3793, -9, 1, 4 }, { 4306, -3, -2, 4 },
        { 18325, 3, -4, 4 }, { 40248, 2, -4, 4 }, { 40248, -4, 2, 4 }, { 3793, -4, 3, 4 },
        { 3792, -12, 6, 4 }, { 3792, -12, 5, 4 }, { 4306, -17, 5, 14 }, { 4967, -11, 3, 4 },
        { 4954, -13, 4, 4 }, { 4962, -15, 1, 4 }, { 4954, -10, 0, 4 },
    };

    [Constructible]
    public BlackthornStep6()
    {
        for (var i = 0; i < _components.Length / 4; i++)
        {
            AddComponent(new AddonComponent(_components[i, 0]), _components[i, 1], _components[i, 2], _components[i, 3]);
        }

        AddComplexComponent(this, 17856, 17, 2, 4, 1109, -1);
        AddComplexComponent(this, 17855, 17, 3, 4, 1109, -1);
        AddComplexComponent(this, 17858, 16, 3, 4, 1109, -1);
        AddComplexComponent(this, 18001, 14, -5, 4, 1109, -1);
        AddComplexComponent(this, 18001, 13, -5, 4, 1109, -1);
        AddComplexComponent(this, 18012, 11, -5, 4, 1109, -1);
        AddComplexComponent(this, 18003, 12, -5, 4, 1109, -1);
        AddComplexComponent(this, 18012, 10, -5, 4, 1109, -1);
        AddComplexComponent(this, 18020, 10, -4, 4, 1109, -1);
        AddComplexComponent(this, 17992, 9, 1, 4, 1109, -1);
        AddComplexComponent(this, 18017, 10, 1, 4, 1109, -1);
        AddComplexComponent(this, 18006, 10, 2, 2, 1109, -1);
        AddComplexComponent(this, 18006, 10, 3, 4, 1109, -1);
        AddComplexComponent(this, 18018, 10, 3, 3, 1109, -1);
        AddComplexComponent(this, 18012, 10, -6, 4, 1109, -1);
        AddComplexComponent(this, 18012, 9, -6, 4, 1109, -1);
        AddComplexComponent(this, 40734, 5, -3, 7, 2500, -1);
        AddComplexComponent(this, 6049, 5, -3, 7, 2548, -1);
        AddComplexComponent(this, 40735, 2, -2, 5, 2500, -1);
        AddComplexComponent(this, 40737, -2, -2, 5, 2500, -1);
        AddComplexComponent(this, 40442, 6, 7, 4, 2075, -1);
        AddComplexComponent(this, 40738, 2, 6, 5, 2500, -1);
        AddComplexComponent(this, 40737, -2, 6, 5, 2500, -1);
        AddComplexComponent(this, 22000, -9, 1, 4, 2548, -1);
        AddComplexComponent(this, 22000, -9, 2, 4, 2548, -1);
        AddComplexComponent(this, 22000, -6, 3, 4, 2548, -1);
        AddComplexComponent(this, 22000, -7, 3, 4, 2548, -1);
        AddComplexComponent(this, 22000, -7, 1, 4, 2548, -1);
        AddComplexComponent(this, 22000, -8, 1, 4, 2548, -1);
        AddComplexComponent(this, 22000, -8, 2, 4, 2548, -1);
        AddComplexComponent(this, 22000, -7, 2, 4, 2548, -1);
        AddComplexComponent(this, 22000, -6, 2, 4, 2548, -1);
        AddComplexComponent(this, 22000, -6, 1, 4, 2548, -1);
        AddComplexComponent(this, 22000, -5, 3, 4, 2548, -1);
        AddComplexComponent(this, 22000, -5, 2, 4, 2548, -1);
        AddComplexComponent(this, 22000, -5, 1, 4, 2548, -1);
        AddComplexComponent(this, 22000, -4, 4, 4, 2548, -1);
        AddComplexComponent(this, 22000, -4, 3, 4, 2548, -1);
        AddComplexComponent(this, 22000, -4, 2, 4, 2548, -1);
        AddComplexComponent(this, 22000, -4, 1, 4, 2548, -1);
        AddComplexComponent(this, 22111, -3, 4, 4, 2548, -1);
        AddComplexComponent(this, 22111, -3, 3, 4, 2548, -1);
        AddComplexComponent(this, 22111, -3, 2, 4, 2548, -1);
        AddComplexComponent(this, 22112, -3, 1, 4, 2548, -1);
        AddComplexComponent(this, 22000, -10, 1, 4, 2548, -1);
        AddComplexComponent(this, 22000, -10, 2, 4, 2548, -1);
        AddComplexComponent(this, 6046, -11, -3, 4, 2548, -1);
        AddComplexComponent(this, 6046, -11, -2, 4, 2548, -1);
        AddComplexComponent(this, 6046, -11, -1, 4, 2548, -1);
        AddComplexComponent(this, 6051, -14, -3, 4, 2548, -1);
        AddComplexComponent(this, 6051, -14, -2, 4, 2548, -1);
        AddComplexComponent(this, 6051, -14, 0, 4, 2548, -1);
        AddComplexComponent(this, 6051, -14, -1, 4, 2548, -1);
        AddComplexComponent(this, 22000, -13, 0, 4, 2548, -1);
        AddComplexComponent(this, 41041, -11, 3, 0, 2548, -1);
        AddComplexComponent(this, 22000, -12, -1, 4, 2548, -1);
        AddComplexComponent(this, 22000, -13, -1, 4, 2548, -1);
        AddComplexComponent(this, 22000, -12, -3, 4, 2548, -1);
        AddComplexComponent(this, 22000, -12, -2, 4, 2548, -1);
        AddComplexComponent(this, 22000, -13, -2, 4, 2548, -1);
        AddComplexComponent(this, 22000, -13, -3, 4, 2548, -1);
    }
}
