using Server.Items;

namespace Server.Gumps;

/// <summary>
///     Подземелья для камня путешествий.
///
///     Координаты не выдуманы и не взяты из вики: это GoLocation из собственных данных
///     шарда (Distribution/Data/regions.json, записи DungeonRegion) — те самые точки,
///     которые движок считает «серединой» региона. Сгенерировано из файла, а не набрано
///     руками: семьдесят шесть записей по четыре числа — это гарантированная опечатка.
///
///     Важная особенность: у подземелий Фелуки и Трамеля GoLocation ведёт ВНУТРЬ, в
///     подземельную часть карты (координаты за 5000), а не к наземному входу. Для камня это
///     и нужно — он переносит в подземелье, а не к дырке в скале.
///
///     Список богаче того, что знают боты (DungeonTarget.Known): у них только Фелука и
///     только наземные входы, потому что ходят они туда пешком и через фасеты не умеют.
/// </summary>
public static class DungeonTravelTargets
{
    public static readonly (string name, Point3D loc, Map map)[] Felucca =
    {
        ("Blighted Grove", new Point3D(6478, 863, 11), Map.Felucca),
        ("Covetous", new Point3D(5456, 1862, 0), Map.Felucca),
        ("Deceit", new Point3D(5187, 635, 0), Map.Felucca),
        ("Despise", new Point3D(5501, 570, 59), Map.Felucca),
        ("Destard", new Point3D(5243, 1004, 0), Map.Felucca),
        ("Fire", new Point3D(5760, 2908, 15), Map.Felucca),
        ("Hythloth", new Point3D(5905, 22, 44), Map.Felucca),
        ("Ice", new Point3D(5210, 2322, 30), Map.Felucca),
        ("Khaldun", new Point3D(5571, 1302, 0), Map.Felucca),
        ("Misc Dungeons", new Point3D(6032, 1499, 0), Map.Felucca),
        ("Orc Cave", new Point3D(5137, 2015, 0), Map.Felucca),
        ("Sanctuary", new Point3D(6174, 23, 0), Map.Felucca),
        ("Shame", new Point3D(5395, 126, 0), Map.Felucca),
        ("Terathan Keep", new Point3D(5451, 3143, -60), Map.Felucca),
        ("The Painted Caves", new Point3D(6308, 892, -1), Map.Felucca),
        ("The Palace of Paroxysmus", new Point3D(6222, 335, 60), Map.Felucca),
        ("The Prism of Light", new Point3D(6474, 188, 0), Map.Felucca),
        ("Wrong", new Point3D(5825, 599, 0), Map.Felucca),
    };

    public static readonly (string name, Point3D loc, Map map)[] Trammel =
    {
        ("Blighted Grove", new Point3D(6478, 863, 11), Map.Trammel),
        ("Covetous", new Point3D(5456, 1862, 0), Map.Trammel),
        ("Deceit", new Point3D(5187, 635, 0), Map.Trammel),
        ("Despise", new Point3D(5501, 570, 59), Map.Trammel),
        ("Destard", new Point3D(5243, 1004, 0), Map.Trammel),
        ("Fire", new Point3D(5760, 2908, 15), Map.Trammel),
        ("Hythloth", new Point3D(5905, 22, 44), Map.Trammel),
        ("Ice", new Point3D(5210, 2322, 30), Map.Trammel),
        ("Misc Dungeons", new Point3D(6032, 1499, 0), Map.Trammel),
        ("Orc Cave", new Point3D(5137, 2015, 0), Map.Trammel),
        ("Sanctuary", new Point3D(6174, 23, 0), Map.Trammel),
        ("Shame", new Point3D(5395, 126, 0), Map.Trammel),
        ("Terathan Keep", new Point3D(5451, 3143, -60), Map.Trammel),
        ("The Painted Caves", new Point3D(6308, 892, -1), Map.Trammel),
        ("The Palace of Paroxysmus", new Point3D(6222, 335, 60), Map.Trammel),
        ("The Prism of Light", new Point3D(6474, 188, 0), Map.Trammel),
        ("Wrong", new Point3D(5825, 599, 0), Map.Trammel),
    };

    public static readonly (string name, Point3D loc, Map map)[] Ilshenar =
    {
        ("Ancient Lair", new Point3D(86, 744, -28), Map.Ilshenar),
        ("Ankh Dungeon", new Point3D(156, 1484, -28), Map.Ilshenar),
        ("Blood Dungeon", new Point3D(2114, 839, -28), Map.Ilshenar),
        ("Exodus Dungeon", new Point3D(1966, 117, -28), Map.Ilshenar),
        ("Rock Dungeon", new Point3D(2187, 316, -7), Map.Ilshenar),
        ("Sorcerer's Dungeon", new Point3D(429, 108, -28), Map.Ilshenar),
        ("Spectre Dungeon", new Point3D(1983, 1107, -18), Map.Ilshenar),
        ("Spider Cave", new Point3D(1785, 991, -28), Map.Ilshenar),
        ("Twisted Weald", new Point3D(2189, 1253, 0), Map.Ilshenar),
        ("Wisp Dungeon", new Point3D(628, 1524, -28), Map.Ilshenar),
    };

    public static readonly (string name, Point3D loc, Map map)[] Malas =
    {
        ("Bedlam", new Point3D(117, 1681, 0), Map.Malas),
        ("Doom", new Point3D(2366, 1268, -85), Map.Malas),
        ("Doom Gauntlet", new Point3D(429, 340, -1), Map.Malas),
        ("Fan Dancer's Dojo", new Point3D(71, 337, 0), Map.Malas),
        ("Labyrinth", new Point3D(336, 1970, 0), Map.Malas),
        ("The Citadel", new Point3D(106, 1884, 0), Map.Malas),
        ("Yomotsu Mines", new Point3D(6, 118, 0), Map.Malas),
    };

    public static readonly (string name, Point3D loc, Map map)[] TerMur =
    {
        ("Abyss", new Point3D(946, 72, 72), Map.TerMur),
        ("Atoll Bend", new Point3D(1118, 3408, -42), Map.TerMur),
        ("Chicken Chase", new Point3D(560, 3412, 37), Map.TerMur),
        ("Fishermans Reach", new Point3D(631, 3035, 36), Map.TerMur),
        ("Gated Isle", new Point3D(703, 3934, -31), Map.TerMur),
        ("Great Ape Lair", new Point3D(926, 1464, 0), Map.TerMur),
        ("High Plain", new Point3D(863, 2931, 38), Map.TerMur),
        ("Kepetch Waste", new Point3D(447, 3188, 20), Map.TerMur),
        ("Kotl City", new Point3D(542, 2473, 0), Map.TerMur),
        ("Lost Settlement", new Point3D(526, 3822, -44), Map.TerMur),
        ("Myrmidex Queen Lair", new Point3D(766, 2306, 0), Map.TerMur),
        ("Northern Steppes", new Point3D(822, 3063, 61), Map.TerMur),
        ("Raptor Island", new Point3D(816, 3778, -42), Map.TerMur),
        ("Royal Park", new Point3D(711, 3255, -42), Map.TerMur),
        ("Slith Valley", new Point3D(1078, 3331, -42), Map.TerMur),
        ("Spider Island", new Point3D(1115, 3730, -42), Map.TerMur),
        ("Stygian Dragon Lair", new Point3D(367, 155, 0), Map.TerMur),
        ("Talon Point", new Point3D(676, 3831, -39), Map.TerMur),
        ("Toxic Desert", new Point3D(1047, 2980, 62), Map.TerMur),
        ("Void Island", new Point3D(440, 3577, 38), Map.TerMur),
        ("Volcano", new Point3D(407, 3010, -24), Map.TerMur),
        ("Walled Circus", new Point3D(370, 3273, 0), Map.TerMur),
        ("Waterfall Point", new Point3D(661, 2894, 39), Map.TerMur),
        ("Zipactriotl Lair", new Point3D(896, 2304, -19), Map.TerMur),
    };
}
