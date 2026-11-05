/// <summary>
///     Общие данные застройки Новой Маджинции: участки, точки установки камней и их обозначения. В ServUO
///     массивы продублированы в <c>MaginciaLottoSystem</c> (<c>m_MagHousingZones</c>, <c>m_StoneLocs</c>,
///     <c>m_Identifiers</c>); здесь они вынесены в один файл, потому что те же участки нужны системе растений
///     (<c>MaginciaPlantSystem.IsValidLocation</c>) — дублирование разъехалось бы при первой правке.
/// </summary>
using Server;

namespace Server.Engines.NewMagincia;

public static class MaginciaHousingZones
{
    /// <summary>Участки под дома, в порядке <see cref="Identifiers" /> и <see cref="StoneLocations" />.</summary>
    public static readonly Rectangle2D[] Zones =
    {
        new(3686, 2125, 18, 18), // C1
        new(3686, 2086, 18, 18), // C2 / Prime
        new(3686, 2063, 18, 18), // C3 / Prime

        new(3657, 2036, 18, 18), // N1 / Prime
        new(3648, 2058, 18, 18), // N2 / Prime
        new(3636, 2081, 18, 18), // N3 / Prime

        new(3712, 2123, 16, 16), // SE3
        new(3712, 2151, 18, 16), // SE2
        new(3712, 2172, 18, 16), // SE1
        new(3729, 2135, 16, 16), // SE4

        new(3655, 2213, 18, 18), // SW1
        new(3656, 2191, 18, 16), // SW2
        new(3628, 2197, 20, 20), // SW3
        new(3628, 2175, 18, 18), // SW4
        new(3657, 2165, 18, 18), // SW5

        new(3745, 2122, 16, 18), // E1 / Prime
        new(3765, 2122, 18, 18), // E2 / Prime
        new(3787, 2130, 18, 18), // E3 / Prime
        new(3784, 2108, 18, 17), // E4 / Prime
        new(3765, 2086, 18, 18), // E5 / Prime
        new(3749, 2065, 18, 18), // E6 / Prime
        new(3715, 2090, 18, 18)  // E7 / Prime
    };

    /// <summary>Точки, где стоит камень участка.</summary>
    public static readonly Point3D[] StoneLocations =
    {
        new(3683, 2134, 20),
        new(3704, 2092, 5),
        new(3704, 2069, 5),

        new(3677, 2045, 20),
        new(3667, 2065, 20),
        new(3644, 2099, 20),

        new(3711, 2131, 20),
        new(3711, 2160, 20),
        new(3711, 2180, 20),
        new(3735, 2133, 20),

        new(3676, 2220, 20),
        new(3675, 2198, 20),
        new(3647, 2205, 22),
        new(3647, 2184, 21),
        new(3665, 2183, 22),

        new(3753, 2119, 21),
        new(3772, 2119, 21),
        new(3785, 2127, 25),
        new(3790, 2106, 30),
        new(3761, 2090, 20),
        new(3746, 2064, 23),
        new(3711, 2087, 5)
    };

    /// <summary>Обозначения участков в том же порядке, что <see cref="Zones" />.</summary>
    public static readonly string[] Identifiers =
    {
        "C-1",
        "C-2",
        "C-3",

        "N-1",
        "N-2",
        "N-3",

        "SE-1",
        "SE-2",
        "SE-3",
        "SE-4",

        "SW-1",
        "SW-2",
        "SW-3",
        "SW-4",
        "SW-5",

        "E-1",
        "E-2",
        "E-3",
        "E-4",
        "E-5",
        "E-6",
        "E-7"
    };

    /// <summary>Первый и последний индексы «премиальных» участков (для аукциона).</summary>
    public static bool IsPrime(int index) => index is > 0 and < 6 || index > 14;
}
