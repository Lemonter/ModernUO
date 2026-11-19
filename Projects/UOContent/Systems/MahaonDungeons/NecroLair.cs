using System;
using System.Collections.Generic;
using System.Linq;
using Server.Commands;
using Server.Engines.Pathing.Nav;
using Server.Engines.Spawners;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.Systems.MahaonDungeons;

/// <summary>
/// Логово некроманта — первое собственное подземелье шарда, на отдельном фасете «Сумрак».
///
/// ---- Фасет ----
///
/// Сумрак (индекс 32 в Data/map-definitions.json) берёт файлы карты Ильшенара (клиентский
/// номер 2): клиенту не нужно ничего нового. Логово стоит в чёрной пустоте этой карты
/// (1936..2303, 328..695 — ни земли, ни статики), и всё в нём — предметы: пол, стены, свет,
/// убранство. Поэтому его видит любой клиент без правки файлов, а граф ботов знает его через
/// индекс декора (Engines.Pathing.Cache.DecorStatics).
///
/// ---- Устройство ----
///
/// Шесть залов, соединённых проходами в две клетки:
///   Преддверие → Склеп → Лаборатория → Ритуальный зал (хозяин);
///   Преддверие → Костница → Ритуальный зал (второй путь);
///   Склеп → Темница с камерами за решётками.
/// Стены выводятся из пола сами: стеной становится всякая клетка рядом с полом, и вид её
/// куска (вдоль X, вдоль Y, угол, столбик) выбирается по соседям, как в подземельях EA.
///
/// Вход — спуск на Британском кладбище (Фелукка), выход — портал в Преддверии. Генерация
/// повторяема: [GenNecroLair сносит прежнее логово и ставит заново; её же зовёт [Decorate.
/// </summary>
public static class NecroLair
{
    public const int FacetIndex = 32;
    public const string RegionName = "Логово некроманта";

    private const int OriginX = 2000;
    private const int OriginY = 400;
    private const int FloorZ = 0;
    private const int Width = 64;
    private const int Height = 50;

    private const string GateName = "спуск в Сумрак";

    public static Map Facet => Map.Maps[FacetIndex];

    /// <summary>Куда приходит спустившийся: середина Преддверия.</summary>
    public static Point3D Arrival => new(OriginX + 8, OriginY + 6, FloorZ);

    private static readonly Point3D GraveyardCenter = new(1384, 1492, 10);

    private enum Floor : byte
    {
        None,
        Pavers,
        Cave,
        Marble
    }

    private readonly record struct Room(string Name, int X0, int Y0, int X1, int Y1, Floor Floor);

    // Залы и проходы: прямоугольники пола, включительно, от угла логова.
    private static readonly Room[] Rooms =
    [
        new("Преддверие", 2, 2, 12, 9, Floor.Pavers),
        new("Склеп", 18, 2, 40, 12, Floor.Pavers),
        new("Лаборатория", 46, 2, 59, 18, Floor.Pavers),
        new("Костница", 2, 16, 20, 32, Floor.Cave),
        new("Темница", 24, 17, 40, 27, Floor.Pavers),
        new("Ритуальный зал", 28, 31, 59, 45, Floor.Marble)
    ];

    private static readonly Room[] Passages =
    [
        new("преддверие-склеп", 13, 5, 17, 6, Floor.Pavers),
        new("склеп-лаборатория", 41, 6, 45, 7, Floor.Pavers),
        new("преддверие-костница", 6, 10, 7, 15, Floor.Cave),
        new("склеп-темница", 28, 13, 29, 16, Floor.Pavers),
        new("лаборатория-ритуал", 52, 19, 53, 30, Floor.Pavers),
        new("костница-ритуал", 21, 29, 27, 30, Floor.Cave)
    ];

    // Колонны: одиночные клетки, вынутые из пола залов.
    private static readonly (int x, int y)[] Pillars =
    [
        (23, 5), (23, 9), (29, 5), (29, 9), (35, 5), (35, 9),
        (34, 34), (34, 42), (53, 34), (53, 42),
        (8, 21), (14, 21), (8, 27), (14, 27)
    ];

    public static void Configure()
    {
        CommandSystem.Register("GenNecroLair", AccessLevel.Developer, OnGenerate);
    }

    /// <summary>Регион подземелья не хранится в сохранении — заводится при каждом старте.</summary>
    public static void Initialize()
    {
        if (Facet == null)
        {
            return;
        }

        var region = new DungeonRegion(
            RegionName,
            Facet,
            50,
            new Rectangle3D(OriginX - 2, OriginY - 2, -128, Width + 4, Height + 4, 256)
        )
        {
            GoLocation = Arrival,
            Entrance = GraveyardCenter
        };
        region.Register();
    }

    [Usage("GenNecroLair")]
    [Description("Строит (заново) логово некроманта на фасете Сумрак и спуск к нему на Британском кладбище.")]
    private static void OnGenerate(CommandEventArgs e) => Generate(e.Mobile);

    public static void Generate(Mobile from)
    {
        var map = Facet;
        if (map == null)
        {
            from?.SendMessage(0x22, "Фасета Сумрак нет: в Data/map-definitions.json нужен индекс 32.");
            return;
        }

        Clear(map);

        var floor = new Dictionary<(int x, int y), Floor>();
        foreach (var r in Rooms.Concat(Passages))
        {
            for (var x = r.X0; x <= r.X1; x++)
            {
                for (var y = r.Y0; y <= r.Y1; y++)
                {
                    floor[(x, y)] = r.Floor;
                }
            }
        }

        foreach (var p in Pillars)
        {
            floor.Remove(p);
        }

        // Камеры темницы: перегородки между камерами и решётчатая стена с дверями.
        var gates = new List<(int x, int y)>();
        for (var x = 24; x <= 40; x++)
        {
            if (x is 26 or 31 or 36)
            {
                gates.Add((x, 21));
                continue;
            }

            floor.Remove((x, 21));
        }

        foreach (var x in new[] { 29, 34 })
        {
            for (var y = 17; y <= 20; y++)
            {
                floor.Remove((x, y));
            }
        }

        var items = 0;

        // Пол.
        foreach (var ((x, y), kind) in floor)
        {
            var id = kind switch
            {
                Floor.Cave   => Utility.RandomList(0x53B, 0x53C, 0x53D, 0x53E, 0x53F),
                Floor.Marble => Utility.RandomList(0x50D, 0x50F, 0x511, 0x512, 0x513, 0x514),
                _            => Utility.RandomList(0x519, 0x521, 0x522, 0x523, 0x524)
            };
            Put(map, id, x, y, FloorZ, kind == Floor.Marble ? 0x455 : 0);
            items++;
        }

        // Стены: всякая клетка рядом с полом (с диагональю), сама не пол.
        var walls = new HashSet<(int x, int y)>();
        foreach (var (x, y) in floor.Keys)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    var c = (x + dx, y + dy);
                    if (!floor.ContainsKey(c))
                    {
                        walls.Add(c);
                    }
                }
            }
        }

        foreach (var (x, y) in walls)
        {
            Put(map, WallPiece(walls, x, y), x, y, FloorZ, 0);
            items++;
        }

        // Решётки темницы.
        foreach (var (x, y) in gates)
        {
            var gate = new IronGate(DoorFacing.WestCW);
            gate.MoveToWorld(At(x, y), map);
            items++;
        }

        items += Furnish(map, floor);
        items += Spawners(map);
        items += Portals(map);

        NavSystem.InvalidateArea(map, OriginX - 2, OriginY - 2, Width + 4, Height + 4);

        from?.SendMessage(0x44, $"Логово некроманта построено: {items} предметов, вход — на Британском кладбище.");
    }

    private static Point3D At(int x, int y, int z = FloorZ) => new(OriginX + x, OriginY + y, z);

    private static Item Put(Map map, int id, int x, int y, int z, int hue, string name = null)
    {
        var item = new Static(id) { Hue = hue };
        if (name != null)
        {
            item.Name = name;
        }

        item.MoveToWorld(At(x, y, z), map);
        return item;
    }

    /// <summary>
    /// Кусок стены по соседям-стенам, как в подземельях EA (снято со статики Фелукки):
    /// 0x241 — вдоль X, 0x242 — вдоль Y, 0x244 — северо-западный угол, 0x243 — столбик.
    /// </summary>
    private static int WallPiece(HashSet<(int x, int y)> walls, int x, int y)
    {
        var n = walls.Contains((x, y - 1));
        var s = walls.Contains((x, y + 1));
        var e = walls.Contains((x + 1, y));
        var w = walls.Contains((x - 1, y));

        if (s && e && !n && !w)
        {
            return 0x244;
        }

        if (n && w)
        {
            return 0x243;
        }

        if ((e || w) && !n)
        {
            return 0x241;
        }

        if (n || s)
        {
            return 0x242;
        }

        return 0x243;
    }

    /// <summary>Свободные клетки зала: пол, не у стены (чтобы предметы не запирали проходы).</summary>
    private static List<(int x, int y)> Inner(Dictionary<(int x, int y), Floor> floor, string room)
    {
        var r = Rooms.First(q => q.Name == room);
        var list = new List<(int x, int y)>();
        for (var x = r.X0 + 1; x < r.X1; x++)
        {
            for (var y = r.Y0 + 1; y < r.Y1; y++)
            {
                if (floor.ContainsKey((x, y)) && floor.ContainsKey((x - 1, y)) && floor.ContainsKey((x + 1, y)) &&
                    floor.ContainsKey((x, y - 1)) && floor.ContainsKey((x, y + 1)))
                {
                    list.Add((x, y));
                }
            }
        }

        return list;
    }

    private static int Furnish(Map map, Dictionary<(int x, int y), Floor> floor)
    {
        var count = 0;
        var taken = new HashSet<(int x, int y)>();

        // Клетки прибытия и выходных врат с соседями — пустые: пришедший не должен встать на
        // надгробие, а врата — потеряться за жаровней.
        foreach (var (ax, ay) in new[] { (Arrival.X - OriginX, Arrival.Y - OriginY), (3, 3) })
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    taken.Add((ax + dx, ay + dy));
                }
            }
        }

        void Scatter(string room, int[] ids, double share, bool blocking, int hue = 0)
        {
            foreach (var c in Inner(floor, room))
            {
                if (taken.Contains(c) || Utility.RandomDouble() >= share)
                {
                    continue;
                }

                // Непроходимое — не вплотную к другому непроходимому: проходы не запираются.
                if (blocking && Neighbours(c).Any(taken.Contains))
                {
                    continue;
                }

                Put(map, ids.RandomElement(), c.x, c.y, FloorZ, hue);
                if (blocking)
                {
                    taken.Add(c);
                }

                count++;
            }
        }

        void Light(string room, int id, int step)
        {
            var r = Rooms.First(q => q.Name == room);
            for (var x = r.X0 + 1; x < r.X1; x += step)
            {
                foreach (var y in new[] { r.Y0, r.Y1 })
                {
                    if (floor.ContainsKey((x, y)) && taken.Add((x, y)))
                    {
                        Put(map, id, x, y, FloorZ, 0);
                        count++;
                    }
                }
            }
        }

        var bones = new[] { 0xECA, 0xECB, 0xECC, 0xECD, 0xECE, 0xECF, 0xED0, 0xED1, 0xED2 };
        var blood = new[] { 0x122A, 0x122B, 0x122C, 0x122D, 0x122E };
        var webs = new[] { 0x10B8, 0x10B9, 0x10BA, 0x10BB, 0x10BC, 0x10BD };
        var graves = new[] { 0xED4, 0xED5, 0xED6, 0xED7, 0xED8, 0xEDB, 0xEDC };
        var skeletons = new[] { 0x1A01, 0x1A02, 0x1A03, 0x1A05, 0x1A06, 0x1A09, 0x1A0A };

        // Свет: жаровни вдоль стен, черепа со свечами, канделябры в ритуальном зале.
        Light("Преддверие", 0xE31, 4);
        Light("Склеп", 0x1854, 5);
        Light("Костница", 0x1854, 6);
        Light("Темница", 0xE31, 8);
        Light("Лаборатория", 0xB26, 4);
        Light("Ритуальный зал", 0xE31, 5);

        Scatter("Преддверие", graves, 0.04, true);
        Scatter("Преддверие", webs, 0.03, false);

        Scatter("Склеп", graves, 0.10, true);
        Scatter("Склеп", bones, 0.06, false);
        Scatter("Склеп", webs, 0.03, false);

        Scatter("Костница", bones, 0.30, false);
        Scatter("Костница", skeletons, 0.05, true);
        Scatter("Костница", new[] { 0x1B17, 0x1B18 }, 0.04, false);
        Scatter("Костница", blood, 0.05, false);

        Scatter("Темница", skeletons, 0.05, true);
        Scatter("Темница", new[] { 0x1A07, 0x1A08 }, 0.04, false);
        Scatter("Темница", blood, 0.06, false);

        // Лаборатория: книжные шкафы у северной стены, котлы и столы, кровь и кости.
        var lab = Rooms.First(q => q.Name == "Лаборатория");
        for (var x = lab.X0 + 1; x < lab.X1; x += 2)
        {
            if (taken.Add((x, lab.Y0)))
            {
                Put(map, Utility.RandomList(0xA97, 0xA98, 0xA99, 0xA9A), x, lab.Y0, FloorZ, 0);
                count++;
            }
        }

        foreach (var (x, y, id) in new[] { (49, 8, 0x974), (55, 8, 0x975), (49, 13, 0x2DD3), (55, 13, 0x2DD4) })
        {
            taken.Add((x, y));
            Put(map, id, x, y, FloorZ, 0);
            count++;
        }

        Scatter("Лаборатория", bones, 0.05, false);
        Scatter("Лаборатория", blood, 0.05, false);

        var diary = MakeDiary();
        diary.MoveToWorld(At(52, 10, FloorZ + 1), map);
        count++;

        // Ритуальный зал: пентаграмма, круг канделябров, алтарь, кровь.
        var cx = 44;
        var cy = 38;
        var pentagram = new PentagramAddon();
        pentagram.MoveToWorld(At(cx, cy), map);
        foreach (var dx in new[] { -1, 0, 1 })
        {
            foreach (var dy in new[] { -1, 0, 1 })
            {
                taken.Add((cx + dx, cy + dy));
            }
        }

        foreach (var (dx, dy) in new[] { (-3, -3), (3, -3), (-3, 3), (3, 3), (0, -4), (0, 4), (-4, 0), (4, 0) })
        {
            taken.Add((cx + dx, cy + dy));
            Put(map, 0xB26, cx + dx, cy + dy, FloorZ, 0);
            count++;
        }

        foreach (var (dx, id) in new[] { (-1, 0x1210), (0, 0x1211), (1, 0x1212) })
        {
            taken.Add((cx + dx, cy - 6));
            Put(map, id, cx + dx, cy - 6, FloorZ, 0x455);
            count++;
        }

        Scatter("Ритуальный зал", blood, 0.06, false);
        Scatter("Ритуальный зал", bones, 0.04, false);

        return count + 10;
    }

    private static IEnumerable<(int x, int y)> Neighbours((int x, int y) c)
    {
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx != 0 || dy != 0)
                {
                    yield return (c.x + dx, c.y + dy);
                }
            }
        }
    }

    private static BaseBook MakeDiary()
    {
        string[][] pages =
        [
            ["Дневник.", "День первый.", "Пустота под", "Ильшенаром", "слушает лучше", "любого ученика.", "Здесь меня", "никто не найдёт."],
            ["Совет магов", "изгнал меня", "за то, что я", "спросил смерть,", "а не жизнь.", "Смерть ответила", "честнее."],
            ["Кости помнят", "форму, если их", "позвать по", "имени. Имена", "я беру с", "надгробий", "Британии."],
            ["Ученики боятся", "темницы. Зря:", "в камерах сидят", "только те, кто", "боялся сильнее."],
            ["Пентаграмма", "готова. Когда", "свечи догорят,", "Сумрак откроет", "дверь шире.", "И тогда сам", "Лорд Бритиш", "придёт ко мне."],
            ["Если ты читаешь", "это, значит,", "ты прошёл мимо", "моих слуг.", "Ритуальный зал", "на юге.", "Я жду.", "— Вельзар"]
        ];

        var book = new BlueBook(pages.Length, false)
        {
            Title = "Дневник Вельзара",
            Author = "Вельзар Безглазый",
            Movable = false,
            Hue = 0x455
        };

        for (var i = 0; i < pages.Length; i++)
        {
            book.Pages[i].Lines = pages[i];
        }

        return book;
    }

    private static int Spawners(Map map)
    {
        var count = 0;

        void Spawn(string room, int amount, int minMinutes, int maxMinutes, params string[] creatures)
        {
            var r = Rooms.First(q => q.Name == room);
            var bounds = new Rectangle3D(OriginX + r.X0, OriginY + r.Y0, FloorZ - 5, r.X1 - r.X0 + 1, r.Y1 - r.Y0 + 1, 30);
            var spawner = new Spawner(
                amount,
                TimeSpan.FromMinutes(minMinutes),
                TimeSpan.FromMinutes(maxMinutes),
                0,
                bounds,
                creatures
            );
            spawner.MoveToWorld(At((r.X0 + r.X1) / 2, (r.Y0 + r.Y1) / 2), map);
            spawner.Respawn();
            count++;
        }

        Spawn("Склеп", 7, 2, 5, "Zombie", "Skeleton", "Ghoul");
        Spawn("Костница", 8, 2, 5, "Skeleton", "BoneMagi", "SkeletalMage", "Ghoul", "Mummy");
        Spawn("Темница", 5, 3, 6, "Spectre", "Wraith", "Shade", "RestlessSoul");
        Spawn("Лаборатория", 4, 4, 8, "Lich", "BoneMagi", "SkeletalMage");
        Spawn("Ритуальный зал", 4, 5, 10, "LichLord", "BoneKnight", "SkeletalKnight");
        Spawn("Ритуальный зал", 1, 30, 45, "SumrakNecromancer");

        return count;
    }

    /// <summary>Спуск на Британском кладбище и выход из Преддверия обратно к нему.</summary>
    private static int Portals(Map map)
    {
        var felucca = Map.Felucca;
        var spot = FreeSpot(felucca, GraveyardCenter);
        var back = FreeSpot(felucca, new Point3D(spot.X + 2, spot.Y + 1, spot.Z), spot);

        // Наверху: тёмные врата и под ними телепорт вниз.
        var gateDown = new Static(0xDDA) { Hue = 0x455, Name = GateName };
        gateDown.MoveToWorld(spot, felucca);
        var down = new Teleporter(Arrival, map);
        down.MoveToWorld(spot, felucca);

        // Внизу: такие же врата в углу Преддверия, ведут к кладбищу рядом со спуском.
        var exit = At(3, 3);
        var gateUp = new Static(0xDDA) { Hue = 0x455, Name = "путь наверх" };
        gateUp.MoveToWorld(exit, map);
        var up = new Teleporter(back, felucca);
        up.MoveToWorld(exit, map);

        NavLinks.Rebuild();
        return 4;
    }

    private static Point3D FreeSpot(Map map, Point3D near, Point3D? not = null)
    {
        for (var r = 0; r <= 12; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                    {
                        continue;
                    }

                    var x = near.X + dx;
                    var y = near.Y + dy;
                    var z = map.GetAverageZ(x, y);
                    var p = new Point3D(x, y, z);
                    if (not is { } n && n.X == x && n.Y == y)
                    {
                        continue;
                    }

                    if (!map.CanSpawnMobile(p))
                    {
                        continue;
                    }

                    var empty = true;
                    foreach (var _ in map.GetItemsAt(x, y))
                    {
                        empty = false;
                        break;
                    }

                    if (empty)
                    {
                        return p;
                    }
                }
            }
        }

        return near;
    }

    /// <summary>Сносит прежнее логово (предметы и его обитателей) и прежний спуск на кладбище.</summary>
    private static void Clear(Map map)
    {
        var rect = new Rectangle2D(OriginX - 3, OriginY - 3, Width + 6, Height + 6);

        var items = new List<Item>();
        foreach (var item in map.GetItemsInBounds(rect))
        {
            if (item.Parent == null)
            {
                items.Add(item);
            }
        }

        var mobiles = new List<Mobile>();
        foreach (var m in map.GetMobilesInBounds(rect))
        {
            if (m is BaseCreature { Controlled: false, Summoned: false })
            {
                mobiles.Add(m);
            }
        }

        foreach (var item in Map.Felucca.GetItemsInRange(GraveyardCenter, 20))
        {
            if (item.Parent == null && (item.Name == GateName || item is Teleporter t && t.MapDest == map))
            {
                items.Add(item);
            }
        }

        foreach (var m in mobiles)
        {
            m.Delete();
        }

        foreach (var item in items)
        {
            if (!item.Deleted)
            {
                item.Delete();
            }
        }
    }
}
