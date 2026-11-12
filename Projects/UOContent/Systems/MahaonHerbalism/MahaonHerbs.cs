using System;
using Server.Items;
using Server.Systems.MahaonSeasons;
using Server.Systems.MahaonWorld;

namespace Server.Systems.MahaonHerbalism;

/// <summary>
///     Дикие травы. Номер — порядок в сохранении у каждого куста, поэтому новые
///     дописываются строго в конец.
/// </summary>
public enum MahaonHerbKind
{
    Plantain,
    Yarrow,
    StJohnsWort,
    Comfrey,
    Wormwood,
    MountainRoot,
    Snowbloom,
    Moonwort,

    WildGarlic,
    WildGinseng,
    WildMandrake,
    WildNightshade
}

/// <summary>
///     Где растёт трава. Определяется по названию тайла земли в tiledata — других сведений
///     о местности у сервера нет, а названия у этих тайлов осмысленные («grass», «forest»,
///     «snow»). Болота отдельным типом земли в UO нет: болото здесь — трава или лес у воды.
/// </summary>
[Flags]
public enum MahaonHabitat
{
    None = 0,
    Meadow = 1,
    Forest = 2,
    Wetland = 4,
    Sand = 8,
    Rock = 16,
    Snow = 32
}

[Flags]
public enum MahaonSeasonMask
{
    Spring = 1,
    Summer = 2,
    Autumn = 4,
    Winter = 8,
    Warm = Spring | Summer | Autumn,
    Any = Warm | Winter
}

public sealed class MahaonHerbInfo
{
    public MahaonHerbKind Kind { get; init; }
    public string NameRu { get; init; }

    /// <summary>Ниже этого навыка куст не опознать и не собрать.</summary>
    public double Required { get; init; }

    public MahaonHabitat Habitat { get; init; }
    public MahaonSeasonMask Seasons { get; init; }

    /// <summary>Появляется только ночью — за ней надо выходить в темноте.</summary>
    public bool NightOnly { get; init; }

    /// <summary>Графика куста в мире.</summary>
    public int NodeGraphic { get; init; }

    /// <summary>Что получает собравший. Количество выставит сбор.</summary>
    public Func<Item> Produce { get; init; }

    /// <summary>
    ///     Дикие реагенты дают ещё и семена — так травник приносит в поле то, что фермер
    ///     потом размножит. Null — семян нет.
    /// </summary>
    public MahaonCropType? Seed { get; init; }

    /// <summary>Относительная частота: чем больше, тем чаще попадается среди подходящих.</summary>
    public int Weight { get; init; } = 10;
}

public static class MahaonHerbs
{
    /// <summary>Шанс найти семена у дикого реагента.</summary>
    public const double SeedChance = 0.25;

    public static readonly MahaonHerbInfo[] All =
    {
        new()
        {
            Kind = MahaonHerbKind.Plantain, NameRu = "подорожник", Required = 0,
            Habitat = MahaonHabitat.Meadow | MahaonHabitat.Forest, Seasons = MahaonSeasonMask.Warm,
            NodeGraphic = 0x0CC7, Produce = () => new MahaonPlantain(), Weight = 30
        },
        new()
        {
            Kind = MahaonHerbKind.Yarrow, NameRu = "тысячелистник", Required = 15,
            Habitat = MahaonHabitat.Meadow, Seasons = MahaonSeasonMask.Spring | MahaonSeasonMask.Summer,
            NodeGraphic = 0x0C8B, Produce = () => new MahaonYarrow(), Weight = 20
        },
        new()
        {
            Kind = MahaonHerbKind.StJohnsWort, NameRu = "зверобой", Required = 25,
            Habitat = MahaonHabitat.Meadow | MahaonHabitat.Forest, Seasons = MahaonSeasonMask.Summer | MahaonSeasonMask.Autumn,
            NodeGraphic = 0x0D2B, Produce = () => new MahaonStJohnsWort(), Weight = 15
        },
        new()
        {
            Kind = MahaonHerbKind.Comfrey, NameRu = "окопник", Required = 30,
            Habitat = MahaonHabitat.Wetland, Seasons = MahaonSeasonMask.Warm,
            NodeGraphic = 0x0C84, Produce = () => new MahaonComfrey(), Weight = 20
        },
        new()
        {
            Kind = MahaonHerbKind.Wormwood, NameRu = "полынь", Required = 35,
            Habitat = MahaonHabitat.Sand, Seasons = MahaonSeasonMask.Summer | MahaonSeasonMask.Autumn,
            NodeGraphic = 0x0C93, Produce = () => new MahaonWormwood(), Weight = 20
        },
        new()
        {
            Kind = MahaonHerbKind.MountainRoot, NameRu = "горный корень", Required = 50,
            Habitat = MahaonHabitat.Rock, Seasons = MahaonSeasonMask.Warm,
            NodeGraphic = 0x0CA9, Produce = () => new MahaonMountainRoot(), Weight = 15
        },
        new()
        {
            Kind = MahaonHerbKind.Snowbloom, NameRu = "снежноцвет", Required = 60,
            Habitat = MahaonHabitat.Snow, Seasons = MahaonSeasonMask.Any,
            NodeGraphic = 0x0C88, Produce = () => new MahaonSnowbloom(), Weight = 15
        },
        new()
        {
            Kind = MahaonHerbKind.Moonwort, NameRu = "лунница", Required = 75,
            Habitat = MahaonHabitat.Forest | MahaonHabitat.Meadow, Seasons = MahaonSeasonMask.Warm, NightOnly = true,
            NodeGraphic = 0x0C85, Produce = () => new MahaonMoonwort(), Weight = 8
        },

        // Дикие реагенты: то же, что продаёт травник в лавке, но с шансом на семена.
        new()
        {
            Kind = MahaonHerbKind.WildGarlic, NameRu = "дикий чеснок", Required = 10,
            Habitat = MahaonHabitat.Meadow, Seasons = MahaonSeasonMask.Warm,
            NodeGraphic = 0x18E1, Produce = () => new Garlic(), Seed = MahaonCropType.Garlic, Weight = 10
        },
        new()
        {
            Kind = MahaonHerbKind.WildGinseng, NameRu = "дикий женьшень", Required = 40,
            Habitat = MahaonHabitat.Forest, Seasons = MahaonSeasonMask.Summer | MahaonSeasonMask.Autumn,
            NodeGraphic = 0x18E9, Produce = () => new Ginseng(), Seed = MahaonCropType.Ginseng, Weight = 8
        },
        new()
        {
            Kind = MahaonHerbKind.WildMandrake, NameRu = "дикая мандрагора", Required = 60,
            Habitat = MahaonHabitat.Wetland, Seasons = MahaonSeasonMask.Autumn,
            NodeGraphic = 0x18DF, Produce = () => new MandrakeRoot(), Seed = MahaonCropType.Mandrake, Weight = 6
        },
        new()
        {
            Kind = MahaonHerbKind.WildNightshade, NameRu = "дикий паслён", Required = 60,
            Habitat = MahaonHabitat.Wetland | MahaonHabitat.Forest, Seasons = MahaonSeasonMask.Summer | MahaonSeasonMask.Autumn,
            NightOnly = true, NodeGraphic = 0x18E5, Produce = () => new Nightshade(), Seed = MahaonCropType.Nightshade,
            Weight = 6
        }
    };

    public static MahaonHerbInfo Get(MahaonHerbKind kind) => All[(int)kind];

    public static MahaonSeasonMask CurrentSeasonMask => SeasonSystem.CurrentSeason switch
    {
        MahaonSeason.Spring => MahaonSeasonMask.Spring,
        MahaonSeason.Summer => MahaonSeasonMask.Summer,
        MahaonSeason.Autumn => MahaonSeasonMask.Autumn,
        _                   => MahaonSeasonMask.Winter
    };

    /// <summary>Ночь по местному времени — то же, что показывают часы в игре.</summary>
    public static bool IsNight(Map map, int x, int y)
    {
        Clock.GetTime(map, x, y, out int hours, out int _);
        return hours < 6 || hours >= 21;
    }

    /// <summary>
    ///     Местность тайла. Вода рядом превращает траву и лес в болото — и только в него:
    ///     у самой воды луговые травы не растут, а окопнику и мандрагоре нужна именно она.
    /// </summary>
    public static MahaonHabitat HabitatAt(Map map, int x, int y)
    {
        var name = TileData.LandTable[map.Tiles.GetLandTile(x, y).ID & TileData.MaxLandValue].Name ?? "";
        name = name.ToLowerInvariant();

        if (name.Contains("snow"))
        {
            return MahaonHabitat.Snow;
        }

        if (name.Contains("sand"))
        {
            return MahaonHabitat.Sand;
        }

        if (name.Contains("rock") || name.Contains("cave"))
        {
            return MahaonHabitat.Rock;
        }

        // Подножия. Сами скалы в UO почти всегда — крутые склоны, на которые не встать,
        // и горный корень там не вырос бы ни разу (живой прогон: ноль кустов на скалах).
        // Поэтому скалистой считаем и проходимую землю вплотную к скале.
        if (NearLand(map, x, y, 2, "rock"))
        {
            return MahaonHabitat.Rock;
        }

        var green = name.Contains("forest") || name.Contains("jungle") || name.Contains("leaves")
            ? MahaonHabitat.Forest
            : name.Contains("grass")
                ? MahaonHabitat.Meadow
                : MahaonHabitat.None;

        if (green == MahaonHabitat.None)
        {
            return MahaonHabitat.None;
        }

        return NearWater(map, x, y, 2) ? MahaonHabitat.Wetland : green;
    }

    private static bool NearLand(Map map, int x, int y, int range, string namePart)
    {
        for (var dx = -range; dx <= range; dx++)
        {
            for (var dy = -range; dy <= range; dy++)
            {
                var land = map.Tiles.GetLandTile(x + dx, y + dy);
                var n = TileData.LandTable[land.ID & TileData.MaxLandValue].Name;
                if (n != null && n.Contains(namePart, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool NearWater(Map map, int x, int y, int range)
    {
        for (var dx = -range; dx <= range; dx++)
        {
            for (var dy = -range; dy <= range; dy++)
            {
                var land = map.Tiles.GetLandTile(x + dx, y + dy);
                if ((TileData.LandTable[land.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0)
                {
                    return true;
                }

                foreach (var st in map.Tiles.GetStaticAndMultiTiles(x + dx, y + dy))
                {
                    if ((TileData.ItemTable[st.ID & TileData.MaxItemValue].Flags & TileFlag.Wet) != 0)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // ---- навык ----------------------------------------------------------------------

    public static double Value(Mobile m) => m.Skills[SkillName.Herbalism].Value;

    public static bool CanGather(Mobile m, MahaonHerbInfo info) => Value(m) >= info.Required;

    /// <summary>
    ///     Сколько трав с куста при удаче: 0 → 1, 40 → 2, 80 → 3, 120 → 4. Реагенты щедрее
    ///     на единицу — их и так встретишь реже обычных трав.
    /// </summary>
    public static int Yield(Mobile m, MahaonHerbInfo info) =>
        1 + (int)(Value(m) / 40.0) + (info.Seed != null ? 1 : 0);

    /// <summary>
    ///     Проверка при сборе: на пороге шанс около 40%, через 50 очков сверх порога — не
    ///     срывается. Неудача не губит куст: можно попробовать ещё раз.
    /// </summary>
    public static bool CheckGather(Mobile m, MahaonHerbInfo info) =>
        m.CheckSkill(SkillName.Herbalism, info.Required - 30.0, info.Required + 45.0);
}
