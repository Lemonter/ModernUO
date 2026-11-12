using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Server.Commands;
using Server.Engines.Pathing.Nav;
using Server.Logging;
using Server.Multis;
using Server.Regions;

namespace Server.Systems.MahaonRaids;

/// <summary>Один подход к городу: где налётчики собираются за городом и у каких «ворот» их встречает стража.</summary>
public sealed class RaidApproach
{
    public Point3D Spawn;
    public Point3D Gate;
}

/// <summary>Город, на который ходят набеги: банк (цель), подходы и имя для вестей.</summary>
public sealed class RaidSite
{
    public string City;      // ключ CityControlSystem.Cities
    public string Genitive;  // «Британии», «Минока» — для «защита Британии провалена»
    public Map Map;
    public Point3D Bank;
    public readonly List<RaidApproach> Approaches = [];
}

/// <summary>
///     Точки набегов по городам. Хранятся не в мире, а в Data/mahaon-raid-sites.json — их
///     правят командой [RaidSite, стоя на месте, и они переживают любой откат мира.
///
///     Подходы можно подобрать и автоматически ([RaidSite auto): за границей городского
///     региона по восьми направлениям ищется сухая проходимая клетка, от которой навигация
///     ведёт к банку не в обход полкарты; «ворота» — точка, где этот путь входит в город.
/// </summary>
public static class RaidSites
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(RaidSites));

    private const string FileName = "mahaon-raid-sites.json";

    // Сколько клеток за границей города стоит лагерь налётчиков.
    private const int SpawnOutside = 22;

    // Дальше стольких клеток от банка граница города для лагеря не ищется (Британия с полями —
    // почти две сотни клеток от банка до края).
    private const int MaxEdge = 220;

    // Ворота — на пути лагерь→банк, за столько клеток пути до банка.
    private const int GateToBank = 25;

    private static readonly Dictionary<string, RaidSite> _sites = new(StringComparer.OrdinalIgnoreCase);

    // Банки Фелукки — по точкам банкиров из Data/Spawns; родительный падеж — для вестей.
    private static readonly (string city, string genitive, int x, int y, int z)[] Defaults =
    [
        ("Britain", "Британии", 1425, 1690, 0),
        ("Minoc", "Минока", 2503, 552, 0),
        ("Yew", "Ю", 652, 820, 0),
        ("Moonglow", "Мунглоу", 4471, 1156, 0),
        ("Trinsic", "Тринсика", 1813, 2825, 0),
        ("Magincia", "Маджинции", 3734, 2149, 20),
        ("Jhelom", "Джелома", 1317, 3773, 0),
        ("Skara Brae", "Скара-Брэй", 587, 2146, 0),
        ("Vesper", "Веспера", 2881, 684, 0),
        ("Ocllo", "Окло", 3695, 2511, 0)
    ];

    public static IEnumerable<RaidSite> All => _sites.Values;

    public static RaidSite Get(string city) => city != null && _sites.TryGetValue(city, out var s) ? s : null;

    public static void Configure()
    {
        CommandSystem.Register("RaidSite", AccessLevel.GameMaster, RaidSite_OnCommand);
    }

    public static void Initialize()
    {
        foreach (var (city, genitive, x, y, z) in Defaults)
        {
            _sites[city] = new RaidSite { City = city, Genitive = genitive, Map = Map.Felucca, Bank = new Point3D(x, y, z) };
        }

        Load();
    }

    /// <summary>Ближайший к точке город с готовыми подходами.</summary>
    public static RaidSite Nearest(Map map, Point3D p)
    {
        RaidSite best = null;
        var bestDist = double.MaxValue;
        foreach (var site in _sites.Values)
        {
            if (site.Map != map || site.Approaches.Count == 0)
            {
                continue;
            }

            var d = site.Bank.GetDistanceToSqrt(p);
            if (d < bestDist)
            {
                bestDist = d;
                best = site;
            }
        }

        return best;
    }

    // ---- файл -----------------------------------------------------------------------------

    private sealed class SiteDto
    {
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("genitive")] public string Genitive { get; set; }
        [JsonPropertyName("map")] public string Map { get; set; }
        [JsonPropertyName("bank")] public int[] Bank { get; set; }
        [JsonPropertyName("approaches")] public List<ApproachDto> Approaches { get; set; }
    }

    private sealed class ApproachDto
    {
        [JsonPropertyName("spawn")] public int[] Spawn { get; set; }
        [JsonPropertyName("gate")] public int[] Gate { get; set; }
    }

    private static string FilePath => Path.Combine(Core.BaseDirectory, "Data", FileName);

    private static void Load()
    {
        if (!File.Exists(FilePath))
        {
            return;
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<SiteDto>>(File.ReadAllText(FilePath));
            foreach (var dto in list ?? [])
            {
                if (string.IsNullOrEmpty(dto.City))
                {
                    continue;
                }

                var site = Get(dto.City) ?? (_sites[dto.City] = new RaidSite { City = dto.City });
                site.Genitive = dto.Genitive ?? site.Genitive ?? dto.City;
                site.Map = Map.Parse(dto.Map) ?? site.Map ?? Map.Felucca;
                if (dto.Bank is { Length: 3 })
                {
                    site.Bank = ToPoint(dto.Bank);
                }

                site.Approaches.Clear();
                foreach (var a in dto.Approaches ?? [])
                {
                    if (a.Spawn is { Length: 3 } && a.Gate is { Length: 3 })
                    {
                        site.Approaches.Add(new RaidApproach { Spawn = ToPoint(a.Spawn), Gate = ToPoint(a.Gate) });
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Raid sites: failed to read {File}", FilePath);
        }
    }

    public static void Save()
    {
        var list = new List<SiteDto>();
        foreach (var site in _sites.Values)
        {
            var dto = new SiteDto
            {
                City = site.City,
                Genitive = site.Genitive,
                Map = site.Map?.Name ?? "Felucca",
                Bank = FromPoint(site.Bank),
                Approaches = []
            };
            foreach (var a in site.Approaches)
            {
                dto.Approaches.Add(new ApproachDto { Spawn = FromPoint(a.Spawn), Gate = FromPoint(a.Gate) });
            }

            list.Add(dto);
        }

        File.WriteAllText(
            FilePath,
            JsonSerializer.Serialize(
                list,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }
            )
        );
    }

    private static Point3D ToPoint(int[] a) => new(a[0], a[1], a[2]);

    private static int[] FromPoint(Point3D p) => [p.X, p.Y, p.Z];

    // ---- автоподбор -----------------------------------------------------------------------

    /// <summary>Городской регион вокруг банка.</summary>
    public static Region TownOf(RaidSite site) =>
        Region.Find(site.Bank, site.Map) is { } r ? (Region)r.GetRegion<TownRegion>() ?? r.GetRegion<GuardedRegion>() : null;

    /// <summary>Подбирает до <paramref name="want"/> подходов с разных сторон. Возвращает отчёт.</summary>
    public static string AutoPick(RaidSite site, int want = 3)
    {
        var map = site.Map;
        var town = TownOf(site);
        if (town == null)
        {
            return $"{site.City}: банк ({site.Bank}) не в городском регионе";
        }

        var candidates = new List<(int dir, RaidApproach approach, int cost)>();
        for (var dir = 0; dir < 8; dir++)
        {
            if (TryApproach(site, town, dir, out var approach, out var cost))
            {
                candidates.Add((dir, approach, cost));
            }
        }

        // Самые короткие пути первыми; соседние направления (ближе 90°) не берём дважды.
        candidates.Sort((a, b) => a.cost.CompareTo(b.cost));
        var picked = new List<(int dir, RaidApproach approach, int cost)>();
        foreach (var c in candidates)
        {
            if (picked.Count >= want)
            {
                break;
            }

            var apart = true;
            foreach (var p in picked)
            {
                var diff = Math.Abs(p.dir - c.dir) % 8;
                if (Math.Min(diff, 8 - diff) < 2)
                {
                    apart = false;
                    break;
                }
            }

            if (apart)
            {
                picked.Add(c);
            }
        }

        site.Approaches.Clear();
        foreach (var p in picked)
        {
            site.Approaches.Add(p.approach);
        }

        var lines = new List<string>();
        foreach (var p in picked)
        {
            lines.Add($"{DirName(p.dir)}: лагерь {p.approach.Spawn}, ворота {p.approach.Gate}, путь ~{p.cost / 10}");
        }

        return $"{site.City}: {picked.Count} подход(а) из {candidates.Count} годных — " + string.Join("; ", lines);
    }

    private static readonly (int dx, int dy)[] Dirs = [(0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1)];

    public static string DirName(int dir) =>
        dir switch { 0 => "север", 1 => "северо-восток", 2 => "восток", 3 => "юго-восток", 4 => "юг", 5 => "юго-запад", 6 => "запад", _ => "северо-запад" };

    /// <summary>С какой стороны света от банка лежит точка — для вести дозорных.</summary>
    public static string SideOf(Point3D bank, Point3D p)
    {
        var angle = Math.Atan2(p.X - bank.X, -(p.Y - bank.Y)); // 0 — север, по часовой
        var dir = (int)Math.Round(angle / (Math.PI / 4));
        return DirName((dir + 8) % 8);
    }

    private static bool TryApproach(RaidSite site, Region town, int dir, out RaidApproach approach, out int cost)
    {
        approach = null;
        cost = 0;
        var map = site.Map;
        var (dx, dy) = Dirs[dir];

        // Идём лучом от банка до выхода из города.
        var r = 1;
        while (r < MaxEdge && town.Contains(new Point3D(site.Bank.X + dx * r, site.Bank.Y + dy * r, site.Bank.Z)))
        {
            r++;
        }

        var insideTown = r >= MaxEdge;

        // Не вышло на намеченном расстоянии (море, скалы) — ближе к городу.
        foreach (var outside in new[] { SpawnOutside, SpawnOutside * 2 / 3, SpawnOutside / 3 })
        {
            var cx = site.Bank.X + dx * (r + outside);
            var cy = site.Bank.Y + dy * (r + outside);

            for (var ring = 0; ring <= 6; ring++)
            {
                for (var ox = -ring; ox <= ring; ox++)
                {
                    for (var oy = -ring; oy <= ring; oy++)
                    {
                        if (Math.Max(Math.Abs(ox), Math.Abs(oy)) != ring ||
                            !IsCampSpot(map, cx + ox, cy + oy, insideTown, out var spot))
                        {
                            continue;
                        }

                        var route = NavPathfinder.Find(map, spot, map, site.Bank);
                        if (route == null)
                        {
                            continue;
                        }

                        // Путь в обход полкарты (другой берег, остров) — не подход к этому городу.
                        var straight = spot.GetDistanceToSqrt(site.Bank);
                        if (route.Cost / 10.0 > straight * 2.5 + 40)
                        {
                            continue;
                        }

                        approach = new RaidApproach { Spawn = spot, Gate = GateOn(site, spot, route) };
                        cost = route.Cost;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>Клетка годна под лагерь: на ней можно стоять, она не в воде, не в городе и не во дворе дома.</summary>
    public static bool IsCampSpot(Map map, int x, int y, bool townAllowed, out Point3D spot)
    {
        var z = map.GetAverageZ(x, y);
        spot = new Point3D(x, y, z);

        if (!map.CanSpawnMobile(spot))
        {
            return false;
        }

        var region = Region.Find(spot, map);
        if (!townAllowed && region.IsPartOf<GuardedRegion>() || region.IsPartOf<HouseRegion>() ||
            BaseHouse.FindHouseAt(spot, map, 16) != null)
        {
            return false;
        }

        var land = map.Tiles.GetLandTile(x, y);
        if (TileData.LandTable[land.ID & TileData.MaxLandValue].Flags.HasFlag(TileFlag.Wet))
        {
            return false;
        }

        // Под открытым небом: ни крыши, ни этажа, ни кроны над головой.
        foreach (var tile in map.Tiles.GetStaticAndMultiTiles(x, y))
        {
            if (tile.Z > z + 2)
            {
                return false;
            }
        }

        return true;
    }

    // Ворота — точка на пути лагерь→банк за GateToBank клеток пути до банка: бой у края
    // улиц, прорыв — короткий. Путь короче — ворота на полпути.
    private static Point3D GateOn(RaidSite site, Point3D spawn, NavRoute route)
    {
        var points = new List<Point3D> { spawn };
        foreach (var w in route.Waypoints)
        {
            if (w.Map == site.Map)
            {
                points.Add(w.Location);
            }
        }

        if (points[^1] != site.Bank)
        {
            points.Add(site.Bank);
        }

        var total = 0.0;
        for (var i = 1; i < points.Count; i++)
        {
            total += points[i - 1].GetDistanceToSqrt(points[i]);
        }

        var want = Math.Min(GateToBank, total / 2);

        // От банка назад по пути, пока не наберётся want.
        var walked = 0.0;
        for (var i = points.Count - 1; i > 0; i--)
        {
            var a = points[i];
            var b = points[i - 1];
            var leg = a.GetDistanceToSqrt(b);
            if (walked + leg >= want && leg > 0)
            {
                var t = (want - walked) / leg;
                var x = (int)Math.Round(a.X + (b.X - a.X) * t);
                var y = (int)Math.Round(a.Y + (b.Y - a.Y) * t);
                return Standable(site.Map, new Point3D(x, y, site.Map.GetAverageZ(x, y)));
            }

            walked += leg;
        }

        return Standable(site.Map, spawn);
    }

    private static Point3D Standable(Map map, Point3D p)
    {
        for (var ring = 0; ring <= 3; ring++)
        {
            for (var ox = -ring; ox <= ring; ox++)
            {
                for (var oy = -ring; oy <= ring; oy++)
                {
                    var q = new Point3D(p.X + ox, p.Y + oy, map.GetAverageZ(p.X + ox, p.Y + oy));
                    if (map.CanSpawnMobile(q))
                    {
                        return q;
                    }
                }
            }
        }

        return p;
    }

    // ---- команда --------------------------------------------------------------------------

    [Usage("RaidSite list | auto <город|all> | bank <город> | spawn <город> | gate <город> <n> | clear <город> | check <город>")]
    [Description("Точки набегов: банк города, лагеря налётчиков и ворота, где их встречает стража.")]
    private static void RaidSite_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var verb = e.Length > 0 ? e.GetString(0).ToLowerInvariant() : "list";
        var site = e.Length > 1 ? Get(e.GetString(1)) : null;

        switch (verb)
        {
            case "list":
                {
                    foreach (var s in _sites.Values)
                    {
                        from.SendMessage($"{s.City}: банк {s.Bank}, подходов {s.Approaches.Count}");
                        for (var i = 0; i < s.Approaches.Count; i++)
                        {
                            from.SendMessage($"  {i}: лагерь {s.Approaches[i].Spawn} ({SideOf(s.Bank, s.Approaches[i].Spawn)}), ворота {s.Approaches[i].Gate}");
                        }
                    }

                    return;
                }
            case "auto":
                {
                    var targets = new List<RaidSite>();
                    if (e.Length > 1 && e.GetString(1).Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        targets.AddRange(_sites.Values);
                    }
                    else if (site != null)
                    {
                        targets.Add(site);
                    }

                    if (targets.Count == 0)
                    {
                        from.SendMessage("Какой город? [RaidSite auto <город|all>");
                        return;
                    }

                    foreach (var s in targets)
                    {
                        var report = AutoPick(s);
                        from.SendMessage(0x59, report);
                        logger.Information("Raid sites: {Report}", report);
                    }

                    Save();
                    return;
                }
        }

        if (site == null)
        {
            from.SendMessage($"Город? Есть: {string.Join(", ", _sites.Keys)}");
            return;
        }

        switch (verb)
        {
            case "bank":
                {
                    site.Bank = from.Location;
                    site.Map = from.Map;
                    Save();
                    from.SendMessage(0x59, $"{site.City}: банк — здесь ({from.Location}).");
                    break;
                }
            case "spawn":
                {
                    var route = NavPathfinder.Find(from.Map, from.Location, site.Map, site.Bank);
                    if (route == null)
                    {
                        from.SendMessage(0x22, "Отсюда к банку нет пути (или банк вне города).");
                        return;
                    }

                    var approach = new RaidApproach { Spawn = from.Location, Gate = GateOn(site, from.Location, route) };
                    site.Approaches.Add(approach);
                    Save();
                    from.SendMessage(0x59, $"{site.City}: лагерь {site.Approaches.Count - 1} здесь, ворота {approach.Gate}.");
                    break;
                }
            case "gate":
                {
                    var n = e.Length > 2 ? e.GetInt32(2) : -1;
                    if (n < 0 || n >= site.Approaches.Count)
                    {
                        from.SendMessage("Номер подхода? [RaidSite list");
                        return;
                    }

                    site.Approaches[n].Gate = from.Location;
                    Save();
                    from.SendMessage(0x59, $"{site.City}: ворота подхода {n} — здесь.");
                    break;
                }
            case "clear":
                {
                    site.Approaches.Clear();
                    Save();
                    from.SendMessage(0x59, $"{site.City}: подходы сброшены.");
                    break;
                }
            case "check":
                {
                    for (var i = 0; i < site.Approaches.Count; i++)
                    {
                        var a = site.Approaches[i];
                        var toGate = NavPathfinder.Find(site.Map, a.Spawn, site.Map, a.Gate);
                        var toBank = NavPathfinder.Find(site.Map, a.Gate, site.Map, site.Bank);
                        from.SendMessage(
                            toGate != null && toBank != null ? 0x59 : 0x22,
                            $"{i}: лагерь→ворота {(toGate == null ? "НЕТ ПУТИ" : $"~{toGate.Cost / 10}")}, " +
                            $"ворота→банк {(toBank == null ? "НЕТ ПУТИ" : $"~{toBank.Cost / 10}")}"
                        );
                    }

                    break;
                }
            default:
                {
                    from.SendMessage("[RaidSite list | auto | bank | spawn | gate | clear | check");
                    break;
                }
        }
    }
}
