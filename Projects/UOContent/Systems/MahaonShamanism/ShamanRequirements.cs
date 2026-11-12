using System;
using Server.Items;
using Server.Multis;
using Server.Spells.Shamanism;

namespace Server.Systems.MahaonShamanism;

/// <summary>
///     Обязательные атрибуты разделов: без них заклинание раздела не начать и не завершить.
///
///     Смысл — чтобы традиции игрались по-разному, а не отличались цветом эффекта. Орку нужен
///     огонь, гоблину — музыка, огру — земля под ногами, древнему — начерченный круг. Каждое
///     условие вынуждает выбирать место и готовиться, а противнику даёт, что ломать: затоптать
///     костёр, выбить шамана на мостовую, вытолкать из круга.
///
///     Проверяется дважды — в начале каста и в момент срабатывания (ShamanSpell): условие,
///     пропавшее за время каста, срывает заклинание. Мёртвому (посмертные касты) условия
///     раздела не нужны — у призрака нет рюкзака, его силу даёт якорь Печати души.
/// </summary>
public static class ShamanRequirements
{
    public const int FireRange = 3;

    public static bool Check(Mobile m, ShamanTradition tradition, out string why)
    {
        why = null;

        if (m.AccessLevel >= AccessLevel.GameMaster)
        {
            return true;
        }

        switch (tradition)
        {
            case ShamanTradition.Orc when !NearFire(m):
                why = "Орочьему обряду нужен огонь: разведи костёр рядом.";
                return false;

            case ShamanTradition.Goblin when FindInstrument(m) == null:
                why = "Гоблинский обряд творят под музыку: нужен инструмент в рюкзаке.";
                return false;

            case ShamanTradition.Ogre when !OnBareGround(m, out var under):
                why = $"Огрский обряд творят босыми ногами на голой земле, а под тобой {under}.";
                return false;

            case ShamanTradition.Ancient when ShamanRitualCircle.FindAt(m) == null:
                why = "Древний обряд творят в ритуальном круге: начерти его книгой.";
                return false;
        }

        return true;
    }

    /// <summary>Платит за условие там, где оно расходуется. Пока это только инструмент.</summary>
    public static void Consume(Mobile m, ShamanTradition tradition)
    {
        if (tradition != ShamanTradition.Goblin || m.AccessLevel >= AccessLevel.GameMaster)
        {
            return;
        }

        var instrument = FindInstrument(m);

        if (instrument == null)
        {
            return;
        }

        instrument.PlayInstrumentWell(m);
        instrument.ConsumeUse(m);
    }

    // ---- Огонь ----------------------------------------------------------------------

    public static bool NearFire(Mobile m)
    {
        var map = m.Map;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        foreach (var item in map.GetItemsInRange(m.Location, FireRange))
        {
            if (item.Visible && Math.Abs(item.Z - m.Z) <= 12 && (item is Campfire || IsFire(item.ItemID)))
            {
                return true;
            }
        }

        for (var x = m.X - FireRange; x <= m.X + FireRange; x++)
        {
            for (var y = m.Y - FireRange; y <= m.Y + FireRange; y++)
            {
                foreach (var tile in map.Tiles.GetStaticTiles(x, y))
                {
                    if (Math.Abs(tile.Z - m.Z) <= 12 && IsFire(tile.ID))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Горит ли этот тайл. Отдельного флага «огонь» в тайлдате нет, поэтому по имени:
    ///     костры, кострища, жаровни, горны, очаги, огненные поля. Отсекаются предметы, у
    ///     которых «fire» только в названии (дрова, свитки, рога) и потухшие костры.
    /// </summary>
    public static bool IsFire(int itemId)
    {
        var name = TileData.ItemTable[itemId & TileData.MaxItemValue].Name;

        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        name = name.ToLowerInvariant();

        if (name.Contains(" off") || name.Contains("wood") || name.Contains("scroll") ||
            name.Contains("horn") || name.Contains("resist") || name.Contains("ring") ||
            name.Contains("beetle") || name.Contains("arrow"))
        {
            return false;
        }

        return name.Contains("fire") || name.Contains("brazier") || name.Contains("forge") ||
               name.Contains("hearth") || name.Contains("flame");
    }

    // ---- Музыка ---------------------------------------------------------------------

    public static BaseInstrument FindInstrument(Mobile m)
    {
        if (m.FindItemOnLayer(Layer.TwoHanded) is BaseInstrument held)
        {
            return held;
        }

        return BaseInstrument.GetInstrument(m) ?? m.Backpack?.FindItemByType<BaseInstrument>();
    }

    // ---- Земля ----------------------------------------------------------------------

    private static readonly string[] NotBareGround =
    {
        "cobble", "brick", "wood", "floor", "marble", "tile", "flagstone", "void", "water", "paved", "plank"
    };

    /// <summary>
    ///     Стоит ли шаман прямо на земле: не в доме, не на настиле, не на мостовой. Мостовая
    ///     в UO нарисована землёй (cobblestone), поэтому её отсекают по имени тайла.
    /// </summary>
    public static bool OnBareGround(Mobile m) => OnBareGround(m, out _);

    /// <summary>То же, но с ответом «что под ногами» — его показывают шаману в отказе.</summary>
    public static bool OnBareGround(Mobile m, out string under)
    {
        var map = m.Map;
        under = "не земля";

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        if (BaseHouse.FindHouseAt(m) != null)
        {
            under = "пол дома";
            return false;
        }

        // Высота земли — по самой низкой из четырёх вершин тайла, а не по средней: на склоне
        // игрок стоит ниже средней, и проверка по средней отказывала на любом бугре.
        map.GetAverageZ(m.X, m.Y, out var lowest, out _, out var top);

        if (m.Z > top + 3 || m.Z < lowest - 3)
        {
            under = "что-то над землёй (мост, настил, крыша)";
            return false;
        }

        foreach (var tile in map.Tiles.GetStaticTiles(m.X, m.Y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

            if (data.Surface && Math.Abs(tile.Z + data.CalcHeight - m.Z) <= 2)
            {
                under = $"настил ({data.Name})";
                return false;
            }
        }

        var land = map.Tiles.GetLandTile(m.X, m.Y);
        var landName = TileData.LandTable[land.ID & TileData.MaxLandValue].Name?.ToLowerInvariant() ?? "";

        foreach (var bad in NotBareGround)
        {
            if (landName.Contains(bad))
            {
                under = bad == "water" ? "вода" : $"мощёное ({landName})";
                return false;
            }
        }

        under = landName;
        return true;
    }
}
