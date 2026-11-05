using ModernUO.Serialization;
using static Server.Items.ForsakenFoesAddonHelper;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Addon/BlackthornStep2.cs), a YAAAG-generated
///     decoration addon — the second section of Blackthorn's dungeon. The component table is ServUO's, unchanged. ServUO
///     declared no deed for it (the generation pass places it at a fixed spot), so it has none here either.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BlackthornStep2 : BaseAddon
{

    private static readonly int[,] _components =
    {
        { 15874, 6, 13, 0 }, { 15874, 7, 13, 0 }, { 15874, 8, 13, 0 }, { 15874, 9, 13, 0 },
        { 15874, 10, 13, 0 }, { 15874, 11, 13, 0 }, { 6584, 20, 15, 0 }, { 6584, 21, 16, 1 },
        { 17729, 13, 13, 0 }, { 41325, 6, -2, 6 }, { 15874, 6, 5, 0 }, { 15874, 7, 5, 0 },
        { 15874, 8, 5, 0 }, { 15874, 9, 5, 0 }, { 15874, 10, 5, 0 }, { 15874, 11, 5, 0 },
        { 4309, 6, -8, 6 }, { 17081, -1, 19, 0 }, { 17077, 5, 25, 12 }, { 17088, 1, 25, 13 },
        { 40737, 4, 26, 1 }, { 17082, -1, 26, 0 }, { 40735, -1, 24, 1 }, { 40737, -1, 20, 1 },
        { 16388, 1, 21, 13 }, { 17083, 5, 21, 13 }, { 40738, 4, 19, 1 }, { 16649, 1, 17, 0 },
        { 15874, -4, 13, 0 }, { 15874, -3, 13, 0 }, { 15874, -2, 13, 0 }, { 15874, -5, 13, 0 },
        { 15874, -6, 13, 0 }, { 4306, 0, -2, 2 }, { 4306, 2, -4, 5 }, { 41195, 1, 1, 0 },
        { 41156, 0, 7, 0 }, { 12906, -10, 9, 0 }, { 12906, -9, -4, 0 }, { 12906, -9, -3, 0 },
        { 12906, -10, -4, 0 }, { 12906, -10, -3, 0 }, { 12906, -9, -2, 0 }, { 12906, -10, -2, 0 },
        { 12906, -10, -1, 0 }, { 12906, -9, -1, 0 }, { 12906, -9, 0, 0 }, { 12906, -9, 1, 0 },
        { 12906, -10, 0, 0 }, { 12906, -10, 1, 0 }, { 12906, -8, 3, 0 }, { 12906, -9, 2, 0 },
        { 12906, -10, 2, 0 }, { 12906, -10, 3, 0 }, { 12906, -9, 3, 0 }, { 12906, -9, 4, 0 },
        { 12906, -8, 4, 0 }, { 4963, -10, 4, 0 }, { 4967, -7, 5, 0 }, { 15874, -6, 5, 0 },
        { 15874, -5, 5, 0 }, { 15874, -4, 5, 0 }, { 15874, -3, 5, 0 }, { 15874, -2, 5, 0 },
        { 15874, -1, 5, 0 }, { 15874, 0, 5, 0 }, { 15874, 1, 4, 0 }, { 15874, 2, 4, 0 },
        { 15719, 2, -5, 6 }, { 4317, -1, -10, 2 }, { 4313, -1, -10, 0 }, { 41317, 2, -9, 6 },
        { 16642, 4, -10, 0 }, { 12906, -9, -20, 0 }, { 12906, -10, -20, 0 }, { 12906, -10, -19, 0 },
        { 12906, -9, -19, 0 }, { 12906, -9, -18, 0 }, { 12906, -10, -18, 0 }, { 12906, -10, -17, 0 },
        { 12906, -9, -17, 0 }, { 12906, -9, -16, 0 }, { 12906, -10, -16, 0 }, { 12906, -10, -15, 0 },
        { 12906, -9, -15, 0 }, { 12906, -9, -14, 0 }, { 12906, -10, -14, 0 }, { 12906, -10, -13, 0 },
        { 12906, -9, -13, 0 }, { 12906, -9, -12, 0 }, { 12906, -10, -12, 0 }, { 12906, -10, -11, 0 },
        { 12906, -9, -11, 0 }, { 12906, -9, -10, 0 }, { 12906, -10, -10, 0 }, { 12906, -10, -8, 0 },
        { 12906, -9, -8, 0 }, { 12906, -9, -9, 0 }, { 12906, -10, -9, 0 }, { 12906, -10, -7, 0 },
        { 12906, -9, -7, 0 }, { 12906, -9, -6, 0 }, { 12906, -10, -6, 0 }, { 12906, -10, -5, 0 },
        { 12906, -9, -5, 0 }, { 4410, -9, -22, 0 }, { 12906, -10, -21, 0 }, { 12906, -10, -22, 0 },
        { 12906, -9, -22, 0 }, { 12906, -9, -21, 0 }, { 12906, -9, -23, 0 }, { 12906, -10, -23, 0 },
        { 12906, -10, -24, 0 }, { 12906, -9, -24, 0 }, { 12906, -9, -25, 0 }, { 12906, -10, -25, 0 },
        { 12906, -11, 12, 0 }, { 12906, -12, 12, 0 }, { 12906, -13, 12, 0 }, { 12906, -14, 12, 0 },
        { 12906, -15, 12, 0 }, { 12906, -16, 12, 0 }, { 12906, -17, 12, 0 }, { 12906, -18, 12, 0 },
        { 12906, -19, 12, 0 }, { 12906, -20, 12, 0 }, { 12906, -21, 12, 0 }, { 12906, -22, 12, 0 },
        { 40364, -16, 9, 0 }, { 19100, -18, 9, 0 }, { 15800, -14, 11, 0 }, { 12865, -19, -4, 0 },
        { 12865, -21, -3, 0 }, { 12865, -21, -1, 0 }, { 12865, -20, 0, 0 }, { 12865, -20, 3, 0 },
        { 12865, -21, 2, 0 }, { 12865, -21, 5, 0 }, { 12865, -19, 4, 0 }, { 12865, -20, 7, 0 },
        { 12865, -21, 8, 0 }, { 12865, -20, 9, 0 }, { 12865, -20, 10, 0 }, { 12865, -21, 10, 0 },
        { 12906, -11, 10, 0 }, { 12906, -12, 11, 0 }, { 12906, -11, 11, 0 }, { 12906, -13, 9, 0 },
        { 12906, -12, 10, 0 }, { 12906, -13, 11, 0 }, { 12906, -14, 11, 0 }, { 12906, -14, 10, 0 },
        { 12906, -14, 9, 0 }, { 12906, -13, 10, 0 }, { 12906, -15, 10, 0 }, { 12906, -15, 11, 0 },
        { 12906, -16, 11, 0 }, { 12906, -16, 10, 0 }, { 12906, -17, 10, 0 }, { 12906, -17, 11, 0 },
        { 12906, -17, 8, 0 }, { 12906, -16, 9, 0 }, { 12906, -17, 9, 0 }, { 12906, -18, 8, 0 },
        { 12906, -18, 9, 0 }, { 12906, -18, 9, 0 }, { 12906, -19, 7, 0 }, { 12906, -19, 6, 0 },
        { 12906, -19, 5, 0 }, { 12906, -19, 4, 0 }, { 12906, -19, 3, 0 }, { 12906, -19, 2, 0 },
        { 12906, -19, 1, 0 }, { 12906, -19, 0, 0 }, { 12906, -19, -1, 0 }, { 12906, -19, -2, 0 },
        { 12906, -19, -3, 0 }, { 12906, -19, -4, 0 }, { 12906, -20, -4, 0 }, { 12906, -21, -4, 0 },
        { 12906, -22, -4, 0 }, { 12906, -22, -3, 0 }, { 12906, -21, -3, 0 }, { 12906, -20, -3, 0 },
        { 12906, -20, -2, 0 }, { 12906, -20, -1, 0 }, { 12906, -20, 0, 0 }, { 12906, -20, 1, 0 },
        { 12906, -20, 2, 0 }, { 12906, -20, 3, 0 }, { 12906, -20, 4, 0 }, { 12906, -20, 5, 0 },
        { 12906, -20, 6, 0 }, { 12906, -20, 7, 0 }, { 12906, -20, 9, 0 }, { 12906, -20, 8, 0 },
        { 12906, -19, 8, 0 }, { 12906, -19, 9, 0 }, { 12906, -19, 11, 0 }, { 12906, -19, 10, 0 },
        { 12906, -20, 10, 0 }, { 12906, -20, 11, 0 }, { 12906, -21, 11, 0 }, { 12906, -21, 10, 0 },
        { 12906, -21, 9, 0 }, { 12906, -21, 8, 0 }, { 12906, -21, 7, 0 }, { 12906, -21, 6, 0 },
        { 12906, -21, 5, 0 }, { 12906, -21, 4, 0 }, { 12906, -21, 3, 0 }, { 12906, -21, 2, 0 },
        { 12906, -21, 1, 0 }, { 12906, -21, 0, 0 }, { 12906, -21, -1, 0 }, { 12906, -21, -2, 0 },
        { 12906, -22, -2, 0 }, { 12906, -22, -1, 0 }, { 12906, -22, 0, 0 }, { 12906, -22, 1, 0 },
        { 12906, -22, 2, 0 }, { 12906, -22, 3, 0 }, { 12906, -22, 4, 0 }, { 12906, -22, 5, 0 },
        { 12906, -22, 6, 0 }, { 12906, -22, 7, 0 }, { 12906, -22, 8, 0 }, { 12906, -22, 9, 0 },
        { 12906, -22, 10, 0 }, { 12906, -22, 11, 0 }, { 12906, -18, 10, 0 }, { 12906, -18, 11, 0 },
        { 4963, -18, 3, 0 }, { 4963, -18, 7, 0 }, { 12906, -11, -3, 0 }, { 12906, -11, -4, 0 },
        { 12906, -12, -4, 0 }, { 12906, -12, -3, 0 }, { 12906, -13, -4, 0 }, { 12906, -14, -4, 0 },
        { 12906, -14, -3, 0 }, { 12906, -13, -3, 0 }, { 12906, -11, -2, 0 }, { 12906, -12, -2, 0 },
        { 12906, -12, -1, 0 }, { 12906, -11, -1, 0 }, { 12906, -11, 0, 0 }, { 12906, -11, 1, 0 },
        { 12906, -12, 1, 0 }, { 12906, -12, 0, 0 }, { 12906, -14, -2, 0 }, { 12906, -13, -2, 0 },
        { 12906, -14, -1, 0 }, { 12906, -13, -1, 0 }, { 12906, -14, 0, 0 }, { 12906, -13, 0, 0 },
        { 12906, -14, 1, 0 }, { 12906, -13, 1, 0 }, { 12906, -13, 1, 0 }, { 12906, -12, 2, 0 },
        { 12906, -11, 2, 0 }, { 4969, -14, -3, 0 }, { 4971, -14, -1, 0 }, { 12865, -21, -5, 0 },
        { 12906, -20, -13, 0 }, { 12906, -20, -14, 0 }, { 12906, -20, -15, 0 }, { 12906, -20, -16, 0 },
        { 12906, -20, -17, 0 }, { 12906, -20, -18, 0 }, { 12906, -20, -19, 0 }, { 12906, -20, -20, 0 },
        { 12906, -21, -20, 0 }, { 12906, -21, -19, 0 }, { 12906, -21, -19, 0 }, { 12906, -21, -18, 0 },
        { 12906, -21, -17, 0 }, { 12906, -21, -16, 0 }, { 12906, -21, -15, 0 }, { 12906, -21, -14, 0 },
        { 12906, -21, -13, 0 }, { 12906, -22, -13, 0 }, { 12906, -22, -14, 0 }, { 12906, -22, -15, 0 },
        { 12906, -22, -16, 0 }, { 12906, -22, -17, 0 }, { 12906, -22, -18, 0 }, { 12906, -22, -19, 0 },
        { 12906, -22, -20, 0 }, { 12906, -19, -12, 0 }, { 12906, -20, -12, 0 }, { 12906, -21, -12, 0 },
        { 12906, -22, -12, 0 }, { 12906, -22, -11, 0 }, { 12906, -21, -11, 0 }, { 12906, -20, -11, 0 },
        { 12906, -19, -11, 0 }, { 12906, -19, -10, 0 }, { 12906, -20, -10, 0 }, { 12906, -21, -10, 0 },
        { 12906, -22, -10, 0 }, { 12906, -22, -9, 0 }, { 12906, -21, -9, 0 }, { 12906, -20, -9, 0 },
        { 12906, -20, -9, 0 }, { 12906, -19, -9, 0 }, { 12906, -19, -8, 0 }, { 12906, -20, -8, 0 },
        { 12906, -21, -8, 0 }, { 12906, -22, -8, 0 }, { 12906, -22, -7, 0 }, { 12906, -21, -7, 0 },
        { 12906, -20, -7, 0 }, { 12906, -19, -7, 0 }, { 12906, -19, -6, 0 }, { 12906, -20, -6, 0 },
        { 12906, -21, -6, 0 }, { 12906, -22, -6, 0 }, { 12906, -22, -5, 0 }, { 12906, -21, -5, 0 },
        { 12906, -20, -5, 0 }, { 12906, -19, -5, 0 }, { 19121, -12, -16, 0 }, { 12906, -14, -12, 0 },
        { 12906, -14, -11, 0 }, { 12906, -14, -15, 0 }, { 12906, -14, -16, 0 }, { 12906, -14, -19, 0 },
        { 12906, -13, -19, 0 }, { 12906, -14, -20, 0 }, { 12906, -11, -10, 0 }, { 12906, -11, -11, 0 },
        { 12906, -11, -12, 0 }, { 12906, -11, -13, 0 }, { 12906, -11, -14, 0 }, { 12906, -11, -15, 0 },
        { 12906, -11, -16, 0 }, { 12906, -11, -17, 0 }, { 12906, -11, -18, 0 }, { 12906, -11, -19, 0 },
        { 12906, -11, -20, 0 }, { 12906, -12, -20, 0 }, { 12906, -12, -19, 0 }, { 12906, -12, -18, 0 },
        { 12906, -12, -17, 0 }, { 12906, -12, -16, 0 }, { 12906, -12, -15, 0 }, { 12906, -12, -14, 0 },
        { 12906, -12, -13, 0 }, { 12906, -12, -12, 0 }, { 12906, -12, -11, 0 }, { 12906, -12, -10, 0 },
        { 12906, -13, -20, 0 }, { 12906, -13, -19, 0 }, { 12906, -13, -18, 0 }, { 12906, -13, -17, 0 },
        { 12906, -13, -16, 0 }, { 12906, -13, -15, 0 }, { 12906, -13, -14, 0 }, { 12906, -13, -13, 0 },
        { 12906, -13, -12, 0 }, { 12906, -13, -11, 0 }, { 12906, -13, -10, 0 }, { 12906, -11, -9, 0 },
        { 12906, -12, -9, 0 }, { 12906, -13, -9, 0 }, { 12906, -14, -9, 0 }, { 12906, -14, -8, 0 },
        { 12906, -13, -8, 0 }, { 12906, -12, -8, 0 }, { 12906, -11, -8, 0 }, { 12906, -11, -7, 0 },
        { 12906, -11, -6, 0 }, { 12906, -12, -6, 0 }, { 12906, -12, -7, 0 }, { 12906, -13, -7, 0 },
        { 12906, -14, -7, 0 }, { 12906, -14, -5, 0 }, { 12906, -13, -6, 0 }, { 12906, -13, -5, 0 },
        { 12906, -12, -5, 0 }, { 12906, -11, -5, 0 }, { 4966, -14, -6, 0 }, { 4971, -14, -14, 0 },
        { 4971, -14, -13, 0 }, { 12906, -22, -21, 0 }, { 12906, -21, -21, 0 }, { 12906, -21, -22, 0 },
        { 12906, -22, -22, 0 }, { 12906, -22, -23, 0 }, { 12906, -21, -24, 0 }, { 12906, -21, -23, 0 },
        { 12906, -22, -24, 0 }, { 12906, -22, -25, 0 }, { 15716, -12, -21, 0 }, { 12906, -14, -23, 0 },
        { 12906, -14, -24, 0 }, { 12906, -11, -21, 0 }, { 12906, -11, -22, 0 }, { 12906, -12, -22, 0 },
        { 12906, -12, -21, 0 }, { 12906, -11, -23, 0 }, { 12906, -12, -23, 0 }, { 12906, -12, -24, 0 },
        { 12906, -11, -24, 0 }, { 12906, -11, -25, 0 }, { 12906, -12, -25, 0 }, { 12906, -13, -25, 0 },
        { 12906, -13, -24, 0 }, { 12906, -13, -23, 0 }, { 12906, -13, -22, 0 }, { 12906, -13, -21, 0 },
        { 4971, -14, -22, 0 },
    };

    [Constructible]
    public BlackthornStep2()
    {
        for (var i = 0; i < _components.Length / 4; i++)
        {
            AddComponent(new AddonComponent(_components[i, 0]), _components[i, 1], _components[i, 2], _components[i, 3]);
        }

        AddComplexComponent(this, 22000, 22, 11, 0, 667, -1);
        AddComplexComponent(this, 22000, 22, 10, 0, 667, -1);
        AddComplexComponent(this, 22000, 22, 9, 0, 667, -1);
        AddComplexComponent(this, 22000, 22, 7, 0, 667, -1);
        AddComplexComponent(this, 22000, 22, 8, 0, 667, -1);
        AddComplexComponent(this, 6585, 19, 16, 1, 2721, -1);
        AddComplexComponent(this, 6585, 20, 16, 1, 2721, -1);
        AddComplexComponent(this, 6585, 21, 16, 1, 2721, -1);
        AddComplexComponent(this, 6585, 21, 16, 0, 2721, -1);
        AddComplexComponent(this, 6585, 20, 16, 1, 2721, -1);
        AddComplexComponent(this, 6585, 20, 16, 0, 2721, -1);
        AddComplexComponent(this, 6584, 19, 16, 0, 2721, -1);
        AddComplexComponent(this, 6584, 20, 16, 0, 2721, -1);
        AddComplexComponent(this, 6584, 21, 16, 1, 2721, -1);
        AddComplexComponent(this, 6586, 21, 16, 0, 2721, -1);
        AddComplexComponent(this, 6586, 20, 16, 0, 2721, -1);
        AddComplexComponent(this, 6586, 19, 16, 0, 2721, -1);
        AddComplexComponent(this, 6585, 21, 15, 0, 2721, -1);
        AddComplexComponent(this, 6585, 20, 15, 0, 2721, -1);
        AddComplexComponent(this, 6585, 19, 15, 0, 2721, -1);
        AddComplexComponent(this, 17738, 13, 12, 0, 1910, -1);
        AddComplexComponent(this, 22000, 21, 12, 0, 667, -1);
        AddComplexComponent(this, 22000, 21, 13, 0, 667, -1);
        AddComplexComponent(this, 22000, 21, 14, 0, 667, -1);
        AddComplexComponent(this, 22000, 16, 13, 0, 667, -1);
        AddComplexComponent(this, 22000, 15, 13, 0, 667, -1);
        AddComplexComponent(this, 22000, 14, 13, 0, 667, -1);
        AddComplexComponent(this, 22000, 20, 12, 0, 667, -1);
        AddComplexComponent(this, 22000, 19, 12, 0, 667, -1);
        AddComplexComponent(this, 22000, 19, 13, 0, 667, -1);
        AddComplexComponent(this, 22000, 20, 13, 0, 667, -1);
        AddComplexComponent(this, 22000, 20, 14, 0, 667, -1);
        AddComplexComponent(this, 22000, 19, 14, 0, 667, -1);
        AddComplexComponent(this, 22000, 18, 14, 0, 667, -1);
        AddComplexComponent(this, 22000, 18, 13, 0, 667, -1);
        AddComplexComponent(this, 22000, 18, 12, 0, 667, -1);
        AddComplexComponent(this, 22000, 17, 13, 0, 667, -1);
        AddComplexComponent(this, 22000, 17, 12, 0, 667, -1);
        AddComplexComponent(this, 22000, 16, 12, 0, 667, -1);
        AddComplexComponent(this, 22000, 15, 12, 0, 667, -1);
        AddComplexComponent(this, 22000, 14, 12, 0, 667, -1);
        AddComplexComponent(this, 17729, 13, 13, 5, 1910, -1);
        AddComplexComponent(this, 17724, 13, 12, 5, 1910, -1);
        AddComplexComponent(this, 17729, 13, 12, 0, 1910, -1);
        AddComplexComponent(this, 6585, 21, 4, 0, 2721, -1);
        AddComplexComponent(this, 6585, 21, 5, 0, 2721, -1);
        AddComplexComponent(this, 6585, 20, 4, 2, 2721, -1);
        AddComplexComponent(this, 6584, 20, 4, 1, 2721, -1);
        AddComplexComponent(this, 6584, 20, 3, 0, 2721, -1);
        AddComplexComponent(this, 6584, 20, 4, 1, 2721, -1);
        AddComplexComponent(this, 6584, 19, 4, 0, 2721, -1);
        AddComplexComponent(this, 6584, 19, 5, 1, 2721, -1);
        AddComplexComponent(this, 6585, 19, 5, 0, 2721, -1);
        AddComplexComponent(this, 6585, 19, 4, 0, 2721, -1);
        AddComplexComponent(this, 6585, 20, 5, 0, 2721, -1);
        AddComplexComponent(this, 6585, 19, 4, 0, 2721, -1);
        AddComplexComponent(this, 6571, 19, 10, 0, 0, 1);
        AddComplexComponent(this, 6571, 14, 6, 0, 0, 1);
        AddComplexComponent(this, 40832, 18, 10, 0, 2076, -1);
        AddComplexComponent(this, 22000, 21, 10, 0, 667, -1);
        AddComplexComponent(this, 22000, 21, 11, 0, 667, -1);
        AddComplexComponent(this, 22000, 21, 5, 0, 667, -1);
        AddComplexComponent(this, 22000, 19, 7, 0, 667, -1);
        AddComplexComponent(this, 22000, 19, 8, 0, 667, -1);
        AddComplexComponent(this, 22000, 20, 8, 0, 667, -1);
        AddComplexComponent(this, 22000, 20, 7, 0, 667, -1);
        AddComplexComponent(this, 22000, 21, 6, 0, 667, -1);
        AddComplexComponent(this, 22000, 21, 7, 0, 667, -1);
        AddComplexComponent(this, 22000, 21, 8, 0, 667, -1);
        AddComplexComponent(this, 22000, 21, 9, 0, 667, -1);
        AddComplexComponent(this, 22000, 20, 9, 0, 667, -1);
        AddComplexComponent(this, 22000, 19, 9, 0, 667, -1);
        AddComplexComponent(this, 22000, 20, 10, 0, 667, -1);
        AddComplexComponent(this, 22000, 19, 10, 0, 667, -1);
        AddComplexComponent(this, 22000, 19, 11, 0, 667, -1);
        AddComplexComponent(this, 22000, 20, 11, 0, 667, -1);
        AddComplexComponent(this, 22000, 18, 11, 0, 667, -1);
        AddComplexComponent(this, 22000, 18, 10, 0, 667, -1);
        AddComplexComponent(this, 22000, 18, 9, 0, 667, -1);
        AddComplexComponent(this, 22000, 18, 8, 0, 667, -1);
        AddComplexComponent(this, 22000, 18, 7, 0, 667, -1);
        AddComplexComponent(this, 22000, 17, 11, 0, 667, -1);
        AddComplexComponent(this, 22000, 17, 10, 0, 667, -1);
        AddComplexComponent(this, 22000, 17, 9, 0, 667, -1);
        AddComplexComponent(this, 22000, 17, 8, 0, 667, -1);
        AddComplexComponent(this, 22000, 17, 7, 0, 667, -1);
        AddComplexComponent(this, 22000, 18, 6, 0, 667, -1);
        AddComplexComponent(this, 22000, 19, 6, 0, 667, -1);
        AddComplexComponent(this, 22000, 20, 6, 0, 667, -1);
        AddComplexComponent(this, 22000, 20, 5, 0, 667, -1);
        AddComplexComponent(this, 22000, 19, 5, 0, 667, -1);
        AddComplexComponent(this, 22000, 18, 5, 0, 667, -1);
        AddComplexComponent(this, 22000, 17, 6, 0, 667, -1);
        AddComplexComponent(this, 22000, 16, 6, 0, 667, -1);
        AddComplexComponent(this, 22000, 16, 7, 0, 667, -1);
        AddComplexComponent(this, 22000, 16, 8, 0, 667, -1);
        AddComplexComponent(this, 22000, 16, 9, 0, 667, -1);
        AddComplexComponent(this, 22000, 16, 10, 0, 667, -1);
        AddComplexComponent(this, 22000, 16, 11, 0, 667, -1);
        AddComplexComponent(this, 22000, 15, 11, 0, 667, -1);
        AddComplexComponent(this, 22000, 15, 10, 0, 667, -1);
        AddComplexComponent(this, 22000, 15, 9, 0, 667, -1);
        AddComplexComponent(this, 22000, 15, 8, 0, 667, -1);
        AddComplexComponent(this, 22000, 15, 7, 0, 667, -1);
        AddComplexComponent(this, 22000, 15, 6, 0, 667, -1);
        AddComplexComponent(this, 22000, 17, 5, 0, 667, -1);
        AddComplexComponent(this, 22000, 16, 5, 0, 667, -1);
        AddComplexComponent(this, 22000, 15, 5, 0, 667, -1);
        AddComplexComponent(this, 22000, 14, 5, 0, 667, -1);
        AddComplexComponent(this, 22000, 14, 6, 0, 667, -1);
        AddComplexComponent(this, 22000, 14, 7, 0, 667, -1);
        AddComplexComponent(this, 22000, 14, 9, 0, 667, -1);
        AddComplexComponent(this, 22000, 14, 10, 0, 667, -1);
        AddComplexComponent(this, 22000, 14, 11, 0, 667, -1);
        AddComplexComponent(this, 22000, 13, 11, 0, 667, -1);
        AddComplexComponent(this, 22000, 13, 10, 0, 667, -1);
        AddComplexComponent(this, 22000, 13, 9, 0, 667, -1);
        AddComplexComponent(this, 22000, 13, 8, 0, 667, -1);
        AddComplexComponent(this, 22000, 14, 8, 0, 667, -1);
        AddComplexComponent(this, 22000, 13, 7, 0, 667, -1);
        AddComplexComponent(this, 22000, 13, 6, 0, 667, -1);
        AddComplexComponent(this, 22000, 13, 5, 0, 667, -1);
        AddComplexComponent(this, 17724, 13, 11, 5, 1910, -1);
        AddComplexComponent(this, 17724, 13, 10, 10, 1910, -1);
        AddComplexComponent(this, 17724, 13, 9, 5, 1910, -1);
        AddComplexComponent(this, 17734, 13, 8, 10, 1910, -1);
        AddComplexComponent(this, 17714, 13, 7, 5, 1910, -1);
        AddComplexComponent(this, 17725, 13, 6, 0, 1910, -1);
        AddComplexComponent(this, 17738, 13, 11, 0, 1910, -1);
        AddComplexComponent(this, 17741, 13, 10, 0, 1910, -1);
        AddComplexComponent(this, 17739, 13, 9, 0, 1910, -1);
        AddComplexComponent(this, 17737, 13, 8, 0, 1910, -1);
        AddComplexComponent(this, 17732, 13, 7, 0, 1910, -1);
        AddComplexComponent(this, 17730, 13, 6, 0, 1910, -1);
        AddComplexComponent(this, 17747, 13, 5, 0, 1910, -1);
        AddComplexComponent(this, 4958, -9, 12, 0, 2429, -1);
        AddComplexComponent(this, 4957, -9, 13, 0, 2429, -1);
        AddComplexComponent(this, 4960, -10, 9, 0, 2429, -1);
        AddComplexComponent(this, 4953, -9, 9, 0, 2429, -1);
        AddComplexComponent(this, 4952, -9, 11, 0, 2429, -1);
        AddComplexComponent(this, 4960, -7, 5, 4, 2429, -1);
        AddComplexComponent(this, 4963, -7, 5, 0, 2429, -1);
        AddComplexComponent(this, 4962, -8, 5, 2, 2429, -1);
        AddComplexComponent(this, 4952, -9, 5, 2, 2429, -1);
        AddComplexComponent(this, 4970, -7, 5, 2, 2429, -1);
        AddComplexComponent(this, 4967, -8, 5, 0, 2429, -1);
        AddComplexComponent(this, 4957, -17, 8, 3, 2429, -1);
        AddComplexComponent(this, 4958, -14, 9, 3, 2429, -1);
        AddComplexComponent(this, 4962, -18, 0, 3, 2429, -1);
        AddComplexComponent(this, 4950, -18, 3, 3, 2429, -1);
        AddComplexComponent(this, 4962, -18, 7, 3, 2429, -1);
        AddComplexComponent(this, 4960, -18, -2, 3, 2429, -1);
        AddComplexComponent(this, 4963, -18, -4, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, -3, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, -2, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, -1, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, 0, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, 1, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, 2, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, 4, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, 5, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, 6, 0, 2429, -1);
        AddComplexComponent(this, 4963, -17, 7, 0, 2429, -1);
        AddComplexComponent(this, 4963, -17, 8, 0, 2429, -1);
        AddComplexComponent(this, 4963, -16, 8, 0, 2429, -1);
        AddComplexComponent(this, 4963, -15, 8, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, 9, 0, 2429, -1);
        AddComplexComponent(this, 4963, -13, 9, 0, 2429, -1);
        AddComplexComponent(this, 4963, -12, 9, 0, 2429, -1);
        AddComplexComponent(this, 4963, -11, 9, 0, 2429, -1);
        AddComplexComponent(this, 4961, -11, 9, 0, 2429, -1);
        AddComplexComponent(this, 4960, -12, 3, 3, 2429, -1);
        AddComplexComponent(this, 4950, -14, -2, 3, 2429, -1);
        AddComplexComponent(this, 4967, -14, -1, 0, 2429, -1);
        AddComplexComponent(this, 4967, -14, -3, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, -4, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, -2, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, 0, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, 1, 0, 2429, -1);
        AddComplexComponent(this, 4944, -14, 2, 0, 2429, -1);
        AddComplexComponent(this, 4963, -13, 2, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, 3, 0, 2429, -1);
        AddComplexComponent(this, 4963, -13, 3, 0, 2429, -1);
        AddComplexComponent(this, 4963, -12, 3, 0, 2429, -1);
        AddComplexComponent(this, 4963, -11, 3, 0, 2429, -1);
        AddComplexComponent(this, 4963, -11, 3, 5, 2429, -1);
        AddComplexComponent(this, 4956, -11, 3, 0, 2429, -1);
        AddComplexComponent(this, 4960, -19, -14, 3, 2429, -1);
        AddComplexComponent(this, 4950, -18, -5, 3, 2429, -1);
        AddComplexComponent(this, 4962, -19, -20, 3, 2429, -1);
        AddComplexComponent(this, 4962, -18, -12, 3, 2429, -1);
        AddComplexComponent(this, 4962, -19, -17, 3, 2429, -1);
        AddComplexComponent(this, 4960, -18, -8, 3, 2429, -1);
        AddComplexComponent(this, 4963, -19, -20, 0, 2429, -1);
        AddComplexComponent(this, 4963, -19, -19, 0, 2429, -1);
        AddComplexComponent(this, 4963, -19, -18, 0, 2429, -1);
        AddComplexComponent(this, 4963, -19, -17, 0, 2429, -1);
        AddComplexComponent(this, 4963, -19, -16, 0, 2429, -1);
        AddComplexComponent(this, 4963, -19, -15, 0, 2429, -1);
        AddComplexComponent(this, 4963, -19, -14, 0, 2429, -1);
        AddComplexComponent(this, 4963, -19, -13, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, -12, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, -11, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, -10, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, -9, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, -8, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, -7, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, -6, 0, 2429, -1);
        AddComplexComponent(this, 4963, -18, -5, 0, 2429, -1);
        AddComplexComponent(this, 4960, -14, -16, 2, 2429, -1);
        AddComplexComponent(this, 4960, -14, -15, 2, 2429, -1);
        AddComplexComponent(this, 4960, -14, -10, 3, 2429, -1);
        AddComplexComponent(this, 4960, -14, -19, 3, 2429, -1);
        AddComplexComponent(this, 4949, -14, -13, 2, 2429, -1);
        AddComplexComponent(this, 4946, -14, -18, 3, 2429, -1);
        AddComplexComponent(this, 4950, -14, -6, 0, 2429, -1);
        AddComplexComponent(this, 4966, -14, -20, 3, 2429, -1);
        AddComplexComponent(this, 4966, -14, -17, 3, 2429, -1);
        AddComplexComponent(this, 4966, -14, -12, 3, 2429, -1);
        AddComplexComponent(this, 4966, -14, -11, 3, 2429, -1);
        AddComplexComponent(this, 4966, -14, -8, 3, 2429, -1);
        AddComplexComponent(this, 4967, -14, -9, 0, 2429, -1);
        AddComplexComponent(this, 4967, -14, -7, 0, 2429, -1);
        AddComplexComponent(this, 4967, -14, -14, 0, 2429, -1);
        AddComplexComponent(this, 4967, -14, -13, 0, 2429, -1);
        AddComplexComponent(this, 4967, -14, -15, 0, 2429, -1);
        AddComplexComponent(this, 4967, -14, -16, 0, 2429, -1);
        AddComplexComponent(this, 4969, -14, -16, 0, 2429, -1);
        AddComplexComponent(this, 4969, -14, -15, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, -20, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, -19, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, -18, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, -17, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, -12, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, -11, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, -10, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, -8, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, -5, 0, 2429, -1);
        AddComplexComponent(this, 4960, -20, -24, 3, 2429, -1);
        AddComplexComponent(this, 4960, -21, -24, 3, 2429, -1);
        AddComplexComponent(this, 4963, -21, -25, 0, 2429, -1);
        AddComplexComponent(this, 4963, -21, -24, 0, 2429, -1);
        AddComplexComponent(this, 4963, -20, -24, 0, 2429, -1);
        AddComplexComponent(this, 4963, -20, -23, 0, 2429, -1);
        AddComplexComponent(this, 4963, -20, -22, 0, 2429, -1);
        AddComplexComponent(this, 4963, -20, -21, 0, 2429, -1);
        AddComplexComponent(this, 4950, -14, -21, 7, 2429, -1);
        AddComplexComponent(this, 4961, -14, -21, 2, 2429, -1);
        AddComplexComponent(this, 4960, -14, -24, 2, 2429, -1);
        AddComplexComponent(this, 4960, -14, -25, 3, 2429, -1);
        AddComplexComponent(this, 4948, -14, -22, 2, 2429, -1);
        AddComplexComponent(this, 4966, -14, -23, 3, 2429, -1);
        AddComplexComponent(this, 4967, -14, -21, 0, 2429, -1);
        AddComplexComponent(this, 4967, -14, -24, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, -25, 0, 2429, -1);
        AddComplexComponent(this, 4963, -14, -23, 0, 2429, -1);
    }
}
