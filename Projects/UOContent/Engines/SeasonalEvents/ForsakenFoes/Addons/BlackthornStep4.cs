using ModernUO.Serialization;
using static Server.Items.ForsakenFoesAddonHelper;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Addon/BlackthornStep4.cs), a YAAAG-generated
///     decoration addon — the fourth section of Blackthorn's dungeon. The component table is ServUO's, unchanged. ServUO
///     declared no deed for it (the generation pass places it at a fixed spot), so it has none here either.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BlackthornStep4 : BaseAddon
{

    private static readonly int[,] _components =
    {
        { 7036, 7, 8, 0 }, { 6773, 12, 2, 0 }, { 7822, 12, 0, 0 }, { 6773, 6, -6, 0 },
        { 7059, 6, -7, 0 }, { 12906, 3, -3, 0 }, { 12906, 1, -1, 0 }, { 12906, 1, -2, 0 },
        { 12906, 1, -3, 0 }, { 12906, 0, -3, 0 }, { 12906, 0, -2, 0 }, { 12906, 0, -1, 0 },
        { 12906, 0, 0, 0 }, { 12906, -1, -2, 0 }, { 12906, -1, -1, 0 }, { 12906, -1, 0, 0 },
        { 12906, -2, 0, 0 }, { 12906, -2, -1, 0 }, { 12906, -2, -2, 0 }, { 12906, -1, -3, 0 },
        { 12906, 2, -3, 0 }, { 12906, 3, -2, 0 }, { 12906, 2, -2, 0 }, { 12906, 2, -1, 0 },
        { 12906, 2, 0, 0 }, { 12906, 1, 0, 0 }, { 12906, 0, 2, 0 }, { 12906, 0, 1, 0 },
        { 12906, -1, 1, 0 }, { 12906, -2, 1, 0 }, { 12906, -3, 0, 0 }, { 12906, -4, -1, 0 },
        { 12906, -3, -1, 0 }, { 12906, -3, -2, 0 }, { 12906, -3, -3, 0 }, { 12906, -2, -3, 0 },
        { 15723, -2, -7, 0 }, { 15721, -11, -7, 0 }, { 12906, 3, -5, 0 }, { 12906, 3, -4, 0 },
        { 12906, -1, -4, 0 }, { 12906, 0, -4, 0 }, { 12906, 1, -4, 0 }, { 12906, 2, -4, 0 },
        { 12906, -2, -4, 0 }, { 12906, -2, -5, 0 }, { 12906, -1, -5, 0 }, { 12906, 0, -5, 0 },
        { 12906, 1, -5, 0 }, { 12906, 2, -5, 0 }, { 12906, 2, -6, 0 }, { 12906, 3, -6, 0 },
        { 12906, 3, -7, 0 }, { 12906, 1, -8, 0 }, { 12906, 0, -6, 0 }, { 12906, 0, -7, 0 },
        { 12906, 2, -7, 0 }, { 12906, 1, -6, 0 }, { 12906, 1, -7, 0 }, { 12906, 3, -8, 0 },
        { 12906, 2, -8, 0 },
    };

    [Constructible]
    public BlackthornStep4()
    {
        for (var i = 0; i < _components.Length / 4; i++)
        {
            AddComponent(new AddonComponent(_components[i, 0]), _components[i, 1], _components[i, 2], _components[i, 3]);
        }

        AddComplexComponent(this, 3307, 12, 7, 0, 667, -1);
        AddComplexComponent(this, 3307, 11, 7, 0, 667, -1);
        AddComplexComponent(this, 3307, 10, 7, 0, 667, -1);
        AddComplexComponent(this, 3307, 9, 7, 0, 667, -1);
        AddComplexComponent(this, 3307, 8, 7, 0, 667, -1);
        AddComplexComponent(this, 3307, 7, 7, 0, 667, -1);
        AddComplexComponent(this, 6571, 9, 8, 0, 0, 1);
        AddComplexComponent(this, 7074, 12, 0, 0, 2500, -1);
        AddComplexComponent(this, 7075, 11, 0, 0, 2500, -1);
        AddComplexComponent(this, 7077, 11, -3, 0, 2500, -1);
        AddComplexComponent(this, 7090, 12, -1, 0, 2500, -1);
        AddComplexComponent(this, 7082, 12, -3, 0, 2500, -1);
        AddComplexComponent(this, 4944, 5, -2, 0, 1175, -1);
        AddComplexComponent(this, 6571, 10, -7, 0, 0, 1);
        AddComplexComponent(this, 4784, 12, -8, 0, 2548, -1);
        AddComplexComponent(this, 6942, 9, -8, 0, 2500, -1);
        AddComplexComponent(this, 4947, 5, -4, 0, 1175, -1);
        AddComplexComponent(this, 3119, -11, 1, 1, 2075, -1);
        AddComplexComponent(this, 3120, -11, 1, 0, 2075, -1);
        AddComplexComponent(this, 3120, -11, 0, 1, 2075, -1);
        AddComplexComponent(this, 3119, -11, 0, 0, 2075, -1);
        AddComplexComponent(this, 3119, -11, -1, 0, 2075, -1);
        AddComplexComponent(this, 6773, 0, -2, 0, 872, -1);
        AddComplexComponent(this, 6571, -10, 2, 0, 0, 1);
        AddComplexComponent(this, 4955, -4, 0, 0, 1175, -1);
        AddComplexComponent(this, 4957, -3, 1, 0, 1175, -1);
        AddComplexComponent(this, 4955, -4, -1, 0, 1175, -1);
        AddComplexComponent(this, 4946, -4, -2, 0, 1175, -1);
        AddComplexComponent(this, 4947, -3, -3, 0, 1175, -1);
        AddComplexComponent(this, 4947, -2, 2, 0, 1175, -1);
        AddComplexComponent(this, 4962, -1, 2, 0, 1175, -1);
        AddComplexComponent(this, 4963, -1, 3, 0, 1175, -1);
        AddComplexComponent(this, 4963, 0, 3, 5, 1175, -1);
        AddComplexComponent(this, 4962, 0, 3, 0, 1175, -1);
        AddComplexComponent(this, 4952, 1, 3, 0, 1175, -1);
        AddComplexComponent(this, 4947, 2, 2, 0, 1175, -1);
        AddComplexComponent(this, 4944, 3, 2, 0, 1175, -1);
        AddComplexComponent(this, 4943, 3, 1, 0, 1175, -1);
        AddComplexComponent(this, 4947, 4, 0, 0, 1175, -1);
        AddComplexComponent(this, 4956, 4, -1, 0, 1175, -1);
        AddComplexComponent(this, 4956, 3, -2, 0, 1175, -1);
        AddComplexComponent(this, 4954, 4, -3, 0, 1175, -1);
        AddComplexComponent(this, 8708, -10, -8, 0, 2075, -1);
        AddComplexComponent(this, 6571, -3, -8, 0, 0, 1);
        AddComplexComponent(this, 4944, -2, -4, 0, 1175, -1);
        AddComplexComponent(this, 4957, -2, -5, 0, 1175, -1);
        AddComplexComponent(this, 4958, -1, -6, 0, 1175, -1);
        AddComplexComponent(this, 4947, 0, -6, 0, 1175, -1);
        AddComplexComponent(this, 4949, 0, -7, 0, 1175, -1);
        AddComplexComponent(this, 4952, 1, -8, 0, 1175, -1);
        AddComplexComponent(this, 4954, 4, -4, 0, 1175, -1);
        AddComplexComponent(this, 4954, 4, -5, 0, 1175, -1);
        AddComplexComponent(this, 4963, 4, -7, 5, 1175, -1);
        AddComplexComponent(this, 4963, 4, -6, 0, 1175, -1);
        AddComplexComponent(this, 4962, 4, -7, 0, 1175, -1);
        AddComplexComponent(this, 4952, 4, -8, 0, 1175, -1);
        AddComplexComponent(this, 3097, -12, 2, 0, 2075, -1);
        AddComplexComponent(this, 3118, -12, 1, 0, 2075, -1);
        AddComplexComponent(this, 3119, -12, -1, 0, 2075, -1);
        AddComplexComponent(this, 3109, -12, 0, 0, 2075, -1);
        AddComplexComponent(this, 8700, -12, -8, 0, 2075, -1);
        AddComplexComponent(this, 8708, -12, -6, 0, 2075, -1);
    }
}
