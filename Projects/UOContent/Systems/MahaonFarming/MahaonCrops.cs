using System;
using System.Collections.Generic;
using Server.Items;
using Server.Systems.MahaonWorld;

namespace Server.Systems.MahaonFarming;

public enum MahaonCropKind
{
    Wheat,
    Cotton,
    Flax,
    Cabbage,
    Carrot,
    Onion,
    Lettuce,
    Pumpkin,
    Turnip,
    Corn,
    Garlic,
    Ginseng,

    // Второй заход по tiledata: всё, что лежало на картах полями, но у нас пропускалось.
    Watermelon,
    YellowGourd,
    GreenGourd,
    Squash,
    HoneydewMelon,
    Cantaloupe,
    Grapes,
    Mandrake,
    Nightshade
}

/// <summary>Всё, что надо знать про одну культуру.</summary>
public sealed class MahaonCropInfo
{
    public MahaonCropKind Kind { get; init; }

    /// <summary>Как называется по-русски — в названии семян и в сообщениях.</summary>
    public string RuName { get; init; }

    /// <summary>Что получается при сборе.</summary>
    public Func<int, Item> Produce { get; init; }

    /// <summary>Графика созревшего поля. Первый элемент — то, чем становится своя грядка;
    /// весь список нужен, чтобы узнавать чужие, уже стоящие на карте поля.</summary>
    public int[] RipeTiles { get; init; }

    /// <summary>Сколько зреет от посева до сбора.</summary>
    public TimeSpan GrowTime { get; init; }

    /// <summary>Сколько отдаёт одна своя грядка.</summary>
    public int MinYield { get; init; }

    public int MaxYield { get; init; }
}

/// <summary>
///     Таблица культур.
///
///     Графика взята из tiledata.mul самого клиента, а не по памяти: полоса 0x0C4F..0x0C82
///     — это классические статики полей Британии (хлопок, репа, тыква, лук, салат,
///     морковь, капуста, кукуруза, пшеница), и именно они лежат на всех декоративных
///     грядках, которые расставляет [Decorate.
/// </summary>
public static class MahaonCrops
{
    /// <summary>Общий росток для всех культур: отдельной графики проростка у большинства
    /// в клиенте нет, а «на грядке что-то проклюнулось» читается и так.</summary>
    public const int SproutTile = 0x1EBE;

    /// <summary>Свежевспаханная земля.</summary>
    public static readonly int[] TilledTiles = { 0x0911, 0x0912, 0x0913, 0x0914 };

    private static readonly Dictionary<MahaonCropKind, MahaonCropInfo> _byKind = new()
    {
        [MahaonCropKind.Wheat] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Wheat, RuName = "пшеница",
            Produce = n => new WheatSheaf(n),
            RipeTiles = new[] { 0x0C55, 0x0C56, 0x0C57, 0x0C58, 0x0C59, 0x0C5A, 0x0C5B, 0x0DAE, 0x0DAF },
            GrowTime = TimeSpan.FromMinutes(25), MinYield = 3, MaxYield = 6
        },
        [MahaonCropKind.Cotton] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Cotton, RuName = "хлопок",
            Produce = n => new Cotton(n),
            RipeTiles = new[] { 0x0C4F, 0x0C50, 0x0C51, 0x0C52, 0x0C53, 0x0C54 },
            GrowTime = TimeSpan.FromMinutes(30), MinYield = 2, MaxYield = 5
        },
        [MahaonCropKind.Flax] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Flax, RuName = "лён",
            Produce = n => new Flax(n),
            RipeTiles = new[] { 0x1A99, 0x1A9A, 0x1A9B },
            GrowTime = TimeSpan.FromMinutes(30), MinYield = 2, MaxYield = 5
        },
        [MahaonCropKind.Cabbage] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Cabbage, RuName = "капуста",
            Produce = n => new Cabbage(n),
            RipeTiles = new[] { 0x0C7B, 0x0C7C, 0xA8C9, 0xA8CA },
            GrowTime = TimeSpan.FromMinutes(20), MinYield = 2, MaxYield = 4
        },
        [MahaonCropKind.Carrot] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Carrot, RuName = "морковь",
            Produce = n => new Carrot(n),
            RipeTiles = new[] { 0x0C76, 0x0C77, 0x0C78 },
            GrowTime = TimeSpan.FromMinutes(20), MinYield = 3, MaxYield = 6
        },
        [MahaonCropKind.Onion] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Onion, RuName = "лук",
            Produce = n => new Onion(n),
            RipeTiles = new[] { 0x0C6D, 0x0C6E, 0x0C6F },
            GrowTime = TimeSpan.FromMinutes(20), MinYield = 3, MaxYield = 6
        },
        [MahaonCropKind.Lettuce] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Lettuce, RuName = "салат",
            Produce = n => new Lettuce(n),
            RipeTiles = new[] { 0x0C70, 0x0C71, 0xA8A8, 0xA8A9 },
            GrowTime = TimeSpan.FromMinutes(20), MinYield = 2, MaxYield = 4
        },
        [MahaonCropKind.Pumpkin] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Pumpkin, RuName = "тыква",
            Produce = n => new Pumpkin(n),
            RipeTiles = new[] { 0x0C6A, 0x0C6B, 0x0C6C },
            GrowTime = TimeSpan.FromMinutes(35), MinYield = 1, MaxYield = 3
        },
        [MahaonCropKind.Turnip] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Turnip, RuName = "репа",
            Produce = n => new Turnip(n),
            RipeTiles = new[] { 0x0C61, 0x0C62, 0x0C63, 0x0D39, 0x0D3A },
            GrowTime = TimeSpan.FromMinutes(20), MinYield = 3, MaxYield = 6
        },
        [MahaonCropKind.Corn] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Corn, RuName = "кукуруза",
            Produce = n => new EarOfCorn(n),
            // 0x0C7D-7E — стебель, 0x0C7F-82 — початки: на картах лежит и то и другое.
            RipeTiles = new[] { 0x0C7D, 0x0C7E, 0x0C7F, 0x0C80, 0x0C81, 0x0C82 },
            GrowTime = TimeSpan.FromMinutes(30), MinYield = 2, MaxYield = 5
        },
        [MahaonCropKind.Garlic] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Garlic, RuName = "чеснок",
            Produce = n => new Garlic(n),
            // 0x18E1-E2 — кустик, 0x18E3-E4 — головки.
            RipeTiles = new[] { 0x18E1, 0x18E2, 0x18E3, 0x18E4 },
            GrowTime = TimeSpan.FromMinutes(40), MinYield = 2, MaxYield = 4
        },
        [MahaonCropKind.Ginseng] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Ginseng, RuName = "женьшень",
            Produce = n => new Ginseng(n),
            // 0x18E9-EA — кустик, 0x18EB-EC — корень.
            RipeTiles = new[] { 0x18E9, 0x18EA, 0x18EB, 0x18EC },
            GrowTime = TimeSpan.FromMinutes(40), MinYield = 2, MaxYield = 4
        },

        // --- бахча ---
        [MahaonCropKind.Watermelon] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Watermelon, RuName = "арбуз",
            Produce = n => new Watermelon(n),
            // 0x0C5E-60 «vines» лежат ровно между арбузом и репой — это арбузные плети,
            // и на картах они идут вперемешку с самими арбузами.
            RipeTiles = new[] { 0x0C5C, 0x0C5D, 0x0C5E, 0x0C5F, 0x0C60, 0xA8BF, 0xA8C0 },
            GrowTime = TimeSpan.FromMinutes(35), MinYield = 1, MaxYield = 3
        },
        [MahaonCropKind.HoneydewMelon] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.HoneydewMelon, RuName = "медовая дыня",
            Produce = n => new HoneydewMelon(n),
            RipeTiles = new[] { 0x0C74, 0x0C75 },
            GrowTime = TimeSpan.FromMinutes(35), MinYield = 1, MaxYield = 3
        },
        [MahaonCropKind.Cantaloupe] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Cantaloupe, RuName = "канталупа",
            Produce = n => new Cantaloupe(n),
            RipeTiles = new[] { 0x0C79, 0x0C7A, 0xA896, 0xA897 },
            GrowTime = TimeSpan.FromMinutes(35), MinYield = 1, MaxYield = 3
        },
        [MahaonCropKind.Squash] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Squash, RuName = "кабачок",
            Produce = n => new Squash(n),
            RipeTiles = new[] { 0x0C72, 0x0C73 },
            GrowTime = TimeSpan.FromMinutes(25), MinYield = 2, MaxYield = 4
        },

        // Четыре тайла 0x0C64..0x0C67 в клиенте зовутся одинаково — «gourd». Делим по
        // цвету так же, как это уже делает лавка фермера: 0x0C64 — жёлтая, 0x0C66 — зелёная.
        [MahaonCropKind.YellowGourd] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.YellowGourd, RuName = "жёлтая тыква-горлянка",
            Produce = n => new YellowGourd(n),
            RipeTiles = new[] { 0x0C64, 0x0C65 },
            GrowTime = TimeSpan.FromMinutes(25), MinYield = 2, MaxYield = 4
        },
        [MahaonCropKind.GreenGourd] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.GreenGourd, RuName = "зелёная тыква-горлянка",
            Produce = n => new GreenGourd(n),
            RipeTiles = new[] { 0x0C66, 0x0C67 },
            GrowTime = TimeSpan.FromMinutes(25), MinYield = 2, MaxYield = 4
        },

        [MahaonCropKind.Grapes] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Grapes, RuName = "виноград",
            Produce = n => new Grapes(n),
            // 0x1521-23 — гроздья на земле (проходимые), 0x0D1B-24 — шпалеры виноградника
            // (Impassable): к шпалере подходят вплотную и обирают сбоку, посадить её как
            // грядку нельзя — см. MahaonCropTable, где своей лозе дана 0x1521.
            RipeTiles = new[]
            {
                0x1521, 0x1522, 0x1523,
                0x0D1B, 0x0D1C, 0x0D1D, 0x0D1E, 0x0D1F, 0x0D20, 0x0D21, 0x0D22, 0x0D23, 0x0D24,
                0xA873, 0xA874 // лоза современного набора — ею же выглядит своя грядка
            },
            GrowTime = TimeSpan.FromMinutes(35), MinYield = 3, MaxYield = 6
        },

        // --- реагенты ---
        [MahaonCropKind.Mandrake] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Mandrake, RuName = "мандрагора",
            Produce = n => new MandrakeRoot(n),
            RipeTiles = new[] { 0x18DD, 0x18DE, 0x18DF, 0x18E0 },
            GrowTime = TimeSpan.FromMinutes(45), MinYield = 2, MaxYield = 4
        },
        [MahaonCropKind.Nightshade] = new MahaonCropInfo
        {
            Kind = MahaonCropKind.Nightshade, RuName = "паслён",
            Produce = n => new Nightshade(n),
            RipeTiles = new[] { 0x18E5, 0x18E6, 0x18E7, 0x18E8 },
            GrowTime = TimeSpan.FromMinutes(45), MinYield = 2, MaxYield = 4
        }
    };

    public static MahaonCropInfo Get(MahaonCropKind kind) => _byKind[kind];

    public static IEnumerable<MahaonCropInfo> All => _byKind.Values;

    /// <summary>Все созревшие тайлы всех культур — то, что считается «полем» на карте.</summary>
    public static readonly int[] AllRipeTiles = BuildAllRipe();

    private static int[] _convertibleTiles;
    private static Dictionary<MahaonCropKind, MahaonCropType> _seasonalTypes;

    /// <summary>
    ///     Тайлы полей, которые клиент перестаёт рисовать, а сервер заменяет настоящими
    ///     грядками (MahaonCropTile). Это и даёт статичным полям стадии роста: нарисованный
    ///     в карте статик перекрасить нельзя, а предмет — можно.
    ///
    ///     Это все тайлы полей без исключения — см. NeutralizePhysics ниже, из-за которого
    ///     список и перестал делиться на «безопасные» и «опасные».
    ///
    ///     Клиент держит копию этого списка (MahaonHiddenCrops.cs). Она обязана совпадать —
    ///     иначе поле покажет разом и статик, и грядку поверх него.
    /// </summary>
    public static int[] ConvertibleTiles => _convertibleTiles ??= BuildConvertible();

    private static int[] BuildConvertible()
    {
        var list = new List<int>(AllRipeTiles);

        list.Sort();

        return list.ToArray();
    }

    /// <summary>
    ///     Снимает с тайлов полей всю физику: проходимость, опору под ногами, объём.
    ///
    ///     Нужно потому, что клиент эти тайлы больше не рисует, а сервер о них помнит.
    ///     Разойдясь, они дают самый неприятный сорт бага: игрок упирается в пустоту (тайл
    ///     с Impassable — шпалеры виноградника) или встаёт на воздух (Surface/Bridge — лён,
    ///     наземные гроздья). Раньше я обходил это, просто не пряча такие тайлы, но тогда
    ///     лён и виноградники оставались без стадий роста.
    ///
    ///     Гасим флаги прямо в TileData, а не фильтруем в местах, где считается движение:
    ///     этих мест в ядре больше десятка (Map.CanFit, Item.GetWorldLocation, линия
    ///     видимости, спавн), и заплатка в каждом — это заплатка, которую однажды забудут
    ///     поставить в тринадцатом. Здесь же правка одна и действует на всё сразу.
    ///
    ///     Побочный эффект честный: предмет с такой же графикой (если декоратор поставит
    ///     шпалеру в доме) тоже станет проходимым. Для растительной бутафории это верно.
    /// </summary>
    public static void Configure()
    {
        const TileFlag physics = TileFlag.Impassable | TileFlag.Surface | TileFlag.Bridge;

        foreach (var id in ConvertibleTiles)
        {
            ref var data = ref TileData.ItemTable[id & TileData.MaxItemValue];

            data.Flags &= ~physics;
            data.Height = 0;
        }
    }

    /// <summary>
    ///     Какой сезонной культурой станет поле этого вида.
    ///
    ///     Два перечисления описывают одно и то же — MahaonCropKind тут, MahaonCropType в
    ///     сезонной таблице, — и разошлись исторически: одно росло от тайлов карты, второе
    ///     от посевов. Сводим их по именам, а не по порядку: порядок MahaonCropType лежит в
    ///     сохранении и трогать его нельзя, а имена совпадают one-to-one (это закреплено
    ///     тестом MahaonCropTableTests).
    /// </summary>
    public static MahaonCropType SeasonalTypeFor(MahaonCropKind kind)
    {
        _seasonalTypes ??= BuildSeasonalTypes();

        return _seasonalTypes[kind];
    }

    private static Dictionary<MahaonCropKind, MahaonCropType> BuildSeasonalTypes()
    {
        var map = new Dictionary<MahaonCropKind, MahaonCropType>();

        foreach (var kind in Enum.GetValues<MahaonCropKind>())
        {
            if (Enum.TryParse<MahaonCropType>(kind.ToString(), out var type))
            {
                map[kind] = type;
            }
        }

        return map;
    }

    private static int[] BuildAllRipe()
    {
        var list = new List<int>();

        foreach (var info in _byKind.Values)
        {
            list.AddRange(info.RipeTiles);
        }

        return list.ToArray();
    }

    /// <summary>Какая культура растёт на этом тайле, если вообще какая-то.</summary>
    public static MahaonCropInfo FromTile(int itemId)
    {
        foreach (var info in _byKind.Values)
        {
            if (Array.IndexOf(info.RipeTiles, itemId) >= 0)
            {
                return info;
            }
        }

        return null;
    }
}
