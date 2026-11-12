namespace Server.Systems.MahaonMapEdits;

/// <summary>
///     Брод: мелкая вода проходима пешком, но шаг в ней медленнее.
///
///     ---- Почему это правило, а не тайлдата ----
///
///     Вода у нас — статика Translucent|Impassable|Wet высотой 0 поверх настоящей земли
///     (см. MahaonWaterFlow). Снять с неё Impassable в тайлдате нельзя: тогда проходимым
///     стало бы и море, и любая налитая игроками яма любой глубины. Нужна ГЛУБИНА, а она
///     есть только у пары «уровень воды − высота дна», то есть у клетки, а не у графики.
///
///     Глубина считается от высоты тайла земли — той же, которую MahaonWaterFlow называет
///     дном и показывает .diginfo. Не от средней высоты клетки: в яме средняя выше из-за
///     соседних углов, и одна и та же лужа то пускала бы, то нет в зависимости от краёв.
///
///     ---- Совпадение с клиентом ----
///
///     Те же правила стоят в клиенте Primaris (Primaris.Map.Movement.Wading). Клиент ходит
///     предиктивно: разойдись пороги — игрока будет откатывать на каждом шаге в воду, а
///     разойдись замедление — сервер начнёт держать шаги в очереди троттлинга.
/// </summary>
public static class MahaonWading
{
    /// <summary>Глубже — уже не брод, вода снова непроходима.</summary>
    public const int MaxDepth = 3;

    /// <summary>Шаг в броду во столько раз длиннее обычного (3/2).</summary>
    public const int SlowNumerator = 3;

    public const int SlowDenominator = 2;

    /// <summary>
    ///     Вода в смысле брода: мокрая непроходимая плоская статика. Высоту 0 требуем, чтобы
    ///     сюда не попали фонтаны, водопады и кадки — они тоже Wet|Impassable, но стоят
    ///     стеной, а не лежат гладью.
    /// </summary>
    public static bool IsWater(ItemData data) => data.Wet && data.Impassable && data.Height == 0;

    /// <summary>Не мешает ли эта вода пройти: мелко над дном bottomZ.</summary>
    public static bool IsWadeable(ItemData data, int waterZ, int bottomZ) =>
        IsWater(data) && waterZ - bottomZ <= MaxDepth;

    /// <summary>
    ///     Идёт ли шаг в клетку (x, y) по воде для персонажа, стоящего сейчас на высоте z.
    ///     Вода должна быть не ниже дна (над берегом — не брод) и не ниже ног (с моста над
    ///     мелководьем идут посуху). Смотрим статику, мульти и предметы — как клиент.
    /// </summary>
    public static bool IsWadingStep(Map map, int x, int y, int z)
    {
        if (map == null || map == Map.Internal || x < 0 || y < 0 || x >= map.Width || y >= map.Height)
        {
            return false;
        }

        var bottom = map.Tiles.GetLandTile(x, y).Z;

        foreach (var tile in map.Tiles.GetStaticAndMultiTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

            if (IsWadeable(data, tile.Z, bottom) && tile.Z >= bottom && tile.Z >= z)
            {
                return true;
            }
        }

        // Предметы — тоже: клиент держит статику и предметы клетки одним списком
        // (MovementCheck.CollectObjects), и вода-предмет должна тормозить у обоих одинаково.
        foreach (var item in map.GetItemsAt(x, y))
        {
            if (!item.Visible)
            {
                continue;
            }

            var data = item.ItemData;

            if (IsWadeable(data, item.Z, bottom) && item.Z >= bottom && item.Z >= z)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Длительность шага в броду.</summary>
    public static int Slow(int milliseconds) => milliseconds * SlowNumerator / SlowDenominator;
}
