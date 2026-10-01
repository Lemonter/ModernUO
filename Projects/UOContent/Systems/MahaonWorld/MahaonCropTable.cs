using System;
using Server.Items;
using Server.Systems.MahaonSeasons;

namespace Server.Systems.MahaonWorld;

public enum MahaonCropType
{
    Onion,
    Garlic,

    // Дописаны строго в конец: значение лежит в сохранении у каждой грядки и у каждого
    // мешочка семян, и вставка в середину перепахала бы игрокам все посевы.
    Wheat,
    Cotton,
    Flax,
    Cabbage,
    Carrot,
    Lettuce,
    Pumpkin,
    Turnip,
    Corn,
    Ginseng,

    // Второй заход — тоже строго в конец, по той же причине.
    Watermelon,
    HoneydewMelon,
    Cantaloupe,
    Squash,
    YellowGourd,
    GreenGourd,
    Grapes,
    Mandrake,
    Nightshade
}

public readonly struct MahaonCropDefinition
{
    public readonly string NameRu;
    public readonly int SpringGraphic; // росток
    public readonly int SummerGraphic; // почти созрел
    public readonly int AutumnGraphic; // ripe — this is the one that's actually harvestable
    public readonly Func<Item> CreateHarvest;

    public MahaonCropDefinition(string nameRu, int springGraphic, int summerGraphic, int autumnGraphic, Func<Item> createHarvest)
    {
        NameRu = nameRu;
        SpringGraphic = springGraphic;
        SummerGraphic = summerGraphic;
        AutumnGraphic = autumnGraphic;
        CreateHarvest = createHarvest;
    }

    public int GraphicFor(MahaonSeason season) => season switch
    {
        MahaonSeason.Spring => SpringGraphic,
        MahaonSeason.Summer => SummerGraphic,
        MahaonSeason.Autumn => AutumnGraphic,
        _                   => AutumnGraphic // Winter is handled separately (tile goes invisible)
    };
}

/// <summary>
///     Add new crops here — one line each, everything else (season cycling, harvest,
///     yearly reset) is generic and reads from this table.
///
///     Три графики на культуру — это три стадии: росток (весна), почти созрел (лето),
///     готов к сбору (осень). Раньше в первых двух у всех стояли общие «sprouts»
///     0x0C68/0x0C69, потому что своей ранней графики у классики почти нет.
///
///     Стадии ниже расставлены не по названиям тайлов, а по самим спрайтам: art
///     распакован из artLegacyMUL.uop и просмотрен глазами. Брали из двух мест —
///     классические поля Британии (0x0C4F..0x0C82, реагенты 0x18DD..0x18EC) и
///     фермерский набор эпохи Time of Legends (0xA86A..0xA8FF), который в клиенте есть,
///     но не использован ни у нас, ни у ServUO. Все выбранные тайлы проверены в
///     tiledata на проходимость: грядкой нельзя замуровать игрока.
///
///     Где своей ранней графики нет вовсе (кабачок, горлянки, салат), в росток идёт
///     общий побег 0xA86A — он хотя бы честно отличается от следующей стадии.
/// </summary>
public static class MahaonCropTable
{
    public static readonly MahaonCropDefinition[] Data =
    {
        //     название            росток  почти   готов
        new("Лук", 0x0C6E, 0x0C6F, 0x0C6D, () => new Onion()),

        // Real Garlic's default look is already 0xF84 — no ItemID override needed, this
        // is just the class as-is (correct name, weight, reagent behavior, all of it).
        // Стадии: побег -> перо -> головки наружу (0x18E3).
        new("Чеснок", 0xA86A, 0x18E1, 0x18E3, () => new Garlic()),

        // Пшеница: 0xA8F8 — зелёная, ещё не выколосилась; 0x0C55 — низкая жёлтая.
        new("Пшеница", 0xA8F8, 0x0C55, 0x0C57, () => new WheatSheaf()),

        // Хлопок: голый стебель -> листва с первыми коробочками -> куст в коробочках.
        new("Хлопок", 0x0C51, 0x0C52, 0x0C53, () => new Cotton()),

        // Лён — редкий случай, когда клиент сам нарисовал три размера подряд.
        new("Лён", 0x1A99, 0x1A9A, 0x1A9B, () => new Flax()),

        // Капуста и салат целиком на современном наборе: классические кочаны (0x0C7B/7C,
        // 0x0C70/71) мельче современной розетки, и спелая стадия выходила визуально меньше
        // предыдущей — читается как «завяло», а не «созрело».
        new("Капуста", 0xA86A, 0xA8C9, 0xA8CA, () => new Cabbage()),

        // Морковь: 0xA8CB — одна ботва без корнеплода; 0x0C78 — одна морковь.
        new("Морковь", 0xA8CB, 0x0C78, 0x0C76, () => new Carrot()),

        new("Салат", 0xA86A, 0xA8A8, 0xA8A9, () => new Lettuce()),

        // Тыква: 0xA8DD — плети в жёлтом цвету, завязи ещё нет.
        new("Тыква", 0xA8DD, 0x0C6C, 0x0C6A, () => new Pumpkin()),

        new("Репа", 0x0D39, 0x0C63, 0x0C62, () => new Turnip()),
        new("Кукуруза", 0xA86A, 0xA89B, 0x0C7D, () => new EarOfCorn()),

        // Женьшень: осенью корень выходит наружу (0x18EB) — видно, что пора копать.
        new("Женьшень", 0xA8D2, 0x18E9, 0x18EB, () => new Ginseng()),

        // Бахча. Где у культуры есть своя плеть в современном наборе — берём её: цветёт,
        // плода ещё нет, стадия читается сразу. У арбуза и канталупы такая плеть своя и
        // подписана в tiledata именно их именем.
        // ВАЖНО: порядок строк = порядок MahaonCropType, Data индексируется (int)кодом.
        // Переставишь строки местами — и у всех уже посаженных грядок сменится культура.
        new("Арбуз", 0xA8BF, 0xA8C0, 0x0C5E, () => new Watermelon()),

        // У медовой дыни своей ранней графики нет ни в классике, ни в современном наборе —
        // только два почти одинаковых плода. Подставлять сюда чужую плеть («gourds»
        // 0xA8D4) было бы враньём: игрок увидел бы на грядке горлянку.
        new("Медовая дыня", 0xA86A, 0x0C75, 0x0C74, () => new HoneydewMelon()),

        new("Канталупа", 0xA86A, 0xA896, 0xA897, () => new Cantaloupe()),
        new("Кабачок", 0xA86A, 0x0C73, 0x0C72, () => new Squash()),
        new("Жёлтая тыква-горлянка", 0xA86A, 0x0C65, 0x0C64, () => new YellowGourd()),
        new("Зелёная тыква-горлянка", 0xA86A, 0x0C67, 0x0C66, () => new GreenGourd()),

        // Виноград: своя лоза — 0xA873 (пустая) и 0xA874 (с гроздьями). Шпалеры
        // виноградника (0x0D1B-24) помечены в tiledata как Impassable: поставь такую
        // грядку — и игрок сам себя замурует. Обирать чужие шпалеры на карте это не
        // мешает, они есть в MahaonCrops.
        new("Виноград", 0xA86A, 0xA873, 0xA874, () => new Grapes()),

        // Реагенты. Растут дольше прочего — это не еда, а сырьё для магии. У мандрагоры,
        // как и у женьшеня, спелая стадия — та, где из земли торчит корень.
        new("Мандрагора", 0xA86A, 0x18DF, 0x18DD, () => new MandrakeRoot()),
        new("Паслён", 0x18E8, 0x18E7, 0x18E5, () => new Nightshade())
    };
}
