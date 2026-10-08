namespace Server.Systems.MahaonMetals;

/// <summary>24 металла, полностью заменяет ванильный CraftResource для добычи/ковки —
/// у ванильного всего 9 тиров и не совпадающие названия/эффекты с тем, что тут нужно.
/// Порядок значений = порядок редкости/требуемого навыка добычи, от Iron (0) до
/// Lemium (23).</summary>
public enum MahaonMetal : byte
{
    Iron = 0,
    Cobalt = 1,
    Shadow = 2,
    Cooper = 3,
    Bronze = 4,
    Silver = 5,
    Gold = 6,
    Melchior = 7,

    // Уникальные металлы
    Ice = 8,
    Crystal = 9,
    Agapite = 10,
    Winter = 11,
    Verite = 12,
    Onyx = 13,
    Sky = 14,

    // Раритетные металлы
    Doom = 15,
    Diamond = 16,
    Valorite = 17,
    Titanium = 18,

    // Мифические металлы
    Mytheril = 19,
    Mahaon = 20,
    Kotium = 21,
    Pawerium = 22,
    Lemium = 23
}

public enum MahaonMetalTier
{
    Обычный,
    Уникальный,
    Раритетный,
    Мифический
}
