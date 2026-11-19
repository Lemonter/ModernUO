using System.Collections.Generic;
using System.Linq;
using Server.Engines.Pathing.Nav;
using Server.Items;
using Server.Regions;

namespace Server.Systems.MahaonDungeons;

/// <summary>
/// Обследование подземелья без единого шага ходока.
///
/// Правда о проходимости — заливка настоящими шагами (<see cref="RealFlood"/>): та же проверка
/// движения, что у игрока, двери не мешают, телепорты любого вида переносят. От входов она
/// показывает, что доступно человеку; из глубины недоступного — что там за кусок: запечатанная
/// комната на тысячу клеток или три клетки на верхушке скалы.
///
/// Граф навигации ботов сверяется с ней: каждая клетка, куда дошёл человек, должна лежать в
/// области графа, достижимой от того же входа. Иначе бот туда дороги не найдёт.
///
/// Конец подземелья — самая дальняя по пути ботов точка, концы уровней — самые дальние точки
/// каждого острова графа. Туда и пускаются ходоки.
/// </summary>
public static class DungeonSurvey
{
    public sealed class Entrance
    {
        public Map SourceMap;
        public Point3D Source;
        public Point3D Destination;
        public string Kind;
    }

    public sealed class Piece
    {
        public Point3D Location;
        public int Cells;
        public string Note;
    }

    public sealed class Report
    {
        public string Name;
        public Map Map;
        public DungeonRegion Region;
        public readonly List<Entrance> Entrances = [];
        public readonly List<Entrance> GatedEntrances = [];
        public int TeleportersInside;

        /// <summary>Телепорты, на которых никто не окажется на нужной высоте: шаг на плитку их не включает.</summary>
        public readonly List<string> DeadTeleporters = [];
        public bool HasGraph;

        /// <summary>Клеток, куда дойдёт человек от обычных входов обычными телепортами; из них таких,
        /// куда боты дороги не знают; и сколько добавят особые входы и телепорты (ключи, квесты).</summary>
        public int RealCells, GraphHoleCells, GatedCells;

        /// <summary>Шаги человека, которых нет в графе ботов: откуда, куда, что в графе.</summary>
        public readonly List<string> Breaks = [];

        /// <summary>Проходимые куски, до которых от входа не дойти: запечатанные или с особым входом.</summary>
        public readonly List<Piece> CutOff = [];

        public Point3D? Far;
        public int FarCost;
        public readonly List<(Point3D loc, int cost)> LevelEnds = [];

        public Point3D Stone;
        public bool StoneOnGround, StoneReached;

        public Map StartMap;
        public Point3D? Start;

        public bool HasEntrance => Entrances.Count > 0 || GatedEntrances.Count > 0;
    }

    public static IEnumerable<DungeonRegion> Dungeons(Map only = null) =>
        Region.Regions.OfType<DungeonRegion>()
            .Where(r => r.Map != null && r.Map != Map.Internal && r.Parent is not DungeonRegion && (only == null || r.Map == only))
            .OrderBy(r => r.Map.MapIndex)
            .ThenBy(r => r.Name);

    public static Report Survey(DungeonRegion region)
    {
        var report = new Report { Name = region.Name, Map = region.Map, Region = region, Stone = EntranceOf(region) ?? region.GoLocation };
        var map = region.Map;
        var graph = NavSystem.GetGraph(map);
        var snap = NavSystem.Snapshot;

        if (graph == null)
        {
            return report;
        }

        report.HasGraph = true;
        var mapId = map.MapIndex;

        // Области графа в подземелье.
        var inside = new HashSet<int>();
        foreach (var rect in region.Area)
        {
            var cx0 = System.Math.Max(0, rect.Start.X >> 4);
            var cy0 = System.Math.Max(0, rect.Start.Y >> 4);
            var cx1 = System.Math.Min(graph.ClusterCols - 1, (rect.End.X - 1) >> 4);
            var cy1 = System.Math.Min(graph.ClusterRows - 1, (rect.End.Y - 1) >> 4);

            for (var cy = cy0; cy <= cy1; cy++)
            {
                for (var cx = cx0; cx <= cx1; cx++)
                {
                    var c = graph.GetClusterIndex(cx, cy);
                    for (var r = graph.ClusterRegionStart[c]; r < graph.ClusterRegionStart[c + 1]; r++)
                    {
                        if (region.Contains(At(graph, r)))
                        {
                            inside.Add(r);
                        }
                    }
                }
            }
        }

        // Входы обычными телепортами: начало снаружи, конец внутри.
        var seeds = new List<int>();
        foreach (var link in snap.Links)
        {
            if (link.DestMapId != mapId || !inside.Contains(link.DestRegion) ||
                link.SourceMapId == mapId && inside.Contains(link.SourceRegion))
            {
                continue;
            }

            seeds.Add(link.DestRegion);

            if (link.Kind == NavLinkKind.Teleporter)
            {
                report.Entrances.Add(
                    new Entrance { SourceMap = Map.Maps[link.SourceMapId], Source = link.Source, Destination = link.Destination, Kind = "телепорт" }
                );
            }
        }

        // Особые телепорты (наследники Teleporter, выключенные) внутрь — игроцкие входы с условием.
        foreach (var srcMap in Map.AllMaps)
        {
            if (srcMap == null || srcMap == Map.Internal)
            {
                continue;
            }

            foreach (var tele in srcMap.GetItemsInBounds<Teleporter>(new Rectangle2D(0, 0, srcMap.Width, srcMap.Height)))
            {
                if (tele.Parent != null)
                {
                    continue;
                }

                var leadsIn = (tele.MapDest ?? srcMap) == map && tele.PointDest != Point3D.Zero && region.Contains(tele.PointDest);
                var isInside = srcMap == map && region.Contains(tele.Location);

                if ((isInside || leadsIn) && tele.Active && !Fires(srcMap, tele))
                {
                    report.DeadTeleporters.Add($"{tele.GetType().Name} {srcMap.Name} {tele.Location} -> {tele.PointDest}");
                }

                if (isInside)
                {
                    report.TeleportersInside++;
                    continue;
                }

                if (tele.GetType() == typeof(Teleporter) && tele.Active)
                {
                    continue;
                }

                if ((tele.MapDest ?? srcMap) == map && tele.PointDest != Point3D.Zero && region.Contains(tele.PointDest))
                {
                    report.GatedEntrances.Add(
                        new Entrance
                        {
                            SourceMap = srcMap, Source = tele.Location, Destination = tele.PointDest,
                            Kind = tele.Active ? tele.GetType().Name : $"{tele.GetType().Name} (выключен)"
                        }
                    );
                }
            }
        }

        var walkIn = new List<Point3D>();

        // Ни одного телепорта внутрь — входят пешком: рёбра графа снаружи внутрь.
        if (seeds.Count == 0)
        {
            for (var r = 0; r < graph.RegionCount; r++)
            {
                if (inside.Contains(r))
                {
                    continue;
                }

                for (var e = graph.EdgeStart[r]; e < graph.EdgeStart[r + 1]; e++)
                {
                    if (!inside.Contains(graph.EdgeTarget[e]))
                    {
                        continue;
                    }

                    seeds.Add(graph.EdgeTarget[e]);
                    if (walkIn.Count < 300)
                    {
                        walkIn.Add(At(graph, graph.EdgeTarget[e]));
                    }

                    if (report.Entrances.Count < 3)
                    {
                        report.Entrances.Add(
                            new Entrance
                            {
                                SourceMap = map,
                                Source = new Point3D(graph.PortalFromX[e], graph.PortalFromY[e], graph.PortalFromZ[e]),
                                Destination = new Point3D(graph.PortalToX[e], graph.PortalToY[e], graph.PortalToZ[e]),
                                Kind = "вход пешком"
                            }
                        );
                    }
                }
            }
        }

        // Путь ботов от обычных входов. Ходить ему можно и чуть за край региона: опорная клетка
        // области у края лежит снаружи, а дорога у открытых зон (Тер-Мур) и вовсе идёт вдоль них.
        var near = new HashSet<int>(inside);
        foreach (var rect in region.Area)
        {
            var cx0 = System.Math.Max(0, (rect.Start.X - 24) >> 4);
            var cy0 = System.Math.Max(0, (rect.Start.Y - 24) >> 4);
            var cx1 = System.Math.Min(graph.ClusterCols - 1, (rect.End.X + 24) >> 4);
            var cy1 = System.Math.Min(graph.ClusterRows - 1, (rect.End.Y + 24) >> 4);

            for (var cy = cy0; cy <= cy1; cy++)
            {
                for (var cx = cx0; cx <= cx1; cx++)
                {
                    var c = graph.GetClusterIndex(cx, cy);
                    for (var r = graph.ClusterRegionStart[c]; r < graph.ClusterRegionStart[c + 1]; r++)
                    {
                        near.Add(r);
                    }
                }
            }
        }

        var dist = Dijkstra(graph, snap, mapId, near, seeds);

        // Шаги человека: от обычных входов обычными телепортами — и со всеми особыми.
        var starts = report.Entrances.Where(e => e.Kind == "телепорт").Select(e => e.Destination).Concat(walkIn).ToList();
        var real = RealFlood(map, region, starts, 400_000, false);
        var full = RealFlood(map, region, starts.Concat(report.GatedEntrances.Select(g => g.Destination)).ToList(), 400_000, true);
        report.RealCells = real.Count;
        report.GatedCells = System.Math.Max(0, full.Count - real.Count);

        foreach (var key in real)
        {
            var p = FromKey(key);
            var reg = graph.Locate(p.X, p.Y, p.Z, 0);
            if (reg >= 0 && dist.ContainsKey(reg))
            {
                continue;
            }

            report.GraphHoleCells++;
            if (report.Breaks.Count < 4)
            {
                report.Breaks.Add($"{p}: {(reg < 0 ? "клетки нет в графе" : $"область {reg} не достижима ботом")}");
            }
        }

        // Концы: по пути ботов.
        var ends = new Dictionary<int, (int node, int cost)>();
        foreach (var (node, cost) in dist)
        {
            if (!inside.Contains(node))
            {
                continue;
            }

            var island = NavSystem.GetIsland(graph, node);
            if (!ends.TryGetValue(island, out var best) || cost > best.cost)
            {
                ends[island] = (node, cost);
            }
        }

        foreach (var (node, cost) in ends.Values.OrderByDescending(v => v.cost))
        {
            report.LevelEnds.Add((At(graph, node), cost));
        }

        if (report.LevelEnds.Count > 0)
        {
            report.Far = report.LevelEnds[0].loc;
            report.FarCost = report.LevelEnds[0].cost;
        }

        // Отрезанное: острова графа, куда человек не дошёл, — заливка изнутри покажет, что там.
        var gatedDest = report.GatedEntrances.Select(g => g.Destination).ToList();
        var probed = new HashSet<int>();
        var sealedSeen = new HashSet<long>();
        foreach (var node in inside)
        {
            if (dist.ContainsKey(node) || probed.Count >= 400 || !probed.Add(NavSystem.GetIsland(graph, node)))
            {
                continue;
            }

            var at = At(graph, node);
            if (Touched(full, at) || sealedSeen.Contains(Key(at.X, at.Y, at.Z)))
            {
                continue;
            }

            var piece = RealFlood(map, region, [at], 5000, true);
            sealedSeen.UnionWith(piece);
            if (piece.Count < 60 || piece.Overlaps(full))
            {
                continue;
            }

            var gated = gatedDest.Any(d => Touched(piece, d));
            report.CutOff.Add(new Piece { Location = at, Cells = piece.Count, Note = gated ? "особый вход" : null });
        }

        report.CutOff.Sort((a, b) => b.Cells.CompareTo(a.Cells));

        // Камень путешествий.
        report.StoneOnGround = graph.Locate(report.Stone.X, report.Stone.Y, report.Stone.Z, 3) >= 0;
        report.StoneReached = Touched(full, report.Stone);

        // Начало пачки: у наземного входа, на шаг в сторону от самого телепорта.
        var entrance = report.Entrances.FirstOrDefault(e => e.Kind == "телепорт") ?? report.Entrances.FirstOrDefault();
        if (entrance != null)
        {
            report.StartMap = entrance.SourceMap;
            report.Start = entrance.Kind == "телепорт" ? Beside(entrance.SourceMap, entrance.Source) : entrance.Source;
        }
        else if (report.GatedEntrances.Count > 0)
        {
            report.StartMap = map;
            report.Start = report.GatedEntrances[0].Destination;
        }

        return report;
    }

    /// <summary>
    /// Точка подземелья, куда ставить пришедшего камнем путешествий: конец первого наземного
    /// входа — то место, куда игрок попал бы, спустившись в подземелье сам.
    /// </summary>
    public static Point3D? EntranceOf(DungeonRegion region)
    {
        var snap = NavSystem.Snapshot;
        var graph = NavSystem.GetGraph(region.Map);
        if (graph == null)
        {
            return null;
        }

        var mapId = region.Map.MapIndex;
        foreach (var link in snap.Links)
        {
            if (link.Kind == NavLinkKind.Teleporter && link.DestMapId == mapId && region.Contains(link.Destination) &&
                !(link.SourceMapId == mapId && region.Contains(link.Source)))
            {
                return link.Destination;
            }
        }

        return null;
    }

    private static Point3D At(NavMapGraph graph, int node) => new(graph.RegionX[node], graph.RegionY[node], graph.RegionZ[node]);

    private static long Key(int x, int y, int z) => ((long)x << 24) | ((long)y << 8) | (byte)(sbyte)z;

    private static Point3D FromKey(long key) => new((int)(key >> 24), (int)((key >> 8) & 0xFFFF), (sbyte)(byte)(key & 0xFF));

    private static bool Touched(HashSet<long> cells, Point3D p)
    {
        for (var dz = -6; dz <= 6; dz++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (cells.Contains(Key(p.X + dx, p.Y + dy, p.Z + dz)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static DungeonWalker _probe;

    /// <summary>
    /// Заливка настоящими шагами: MovementImpl, как у игрока, только двери не мешают (тело ГМа),
    /// а телепорты переносят — обычные всегда, особые при <paramref name="gated"/>. Не выходит из
    /// региона подземелья.
    /// </summary>
    private static HashSet<long> RealFlood(Map map, DungeonRegion region, List<Point3D> starts, int limit, bool gated)
    {
        _probe ??= new DungeonWalker { Body = 0x3DB };
        var seen = new HashSet<long>();
        var queue = new Queue<Point3D>();

        void Add(Point3D p)
        {
            if (region.Contains(p) && seen.Add(Key(p.X, p.Y, p.Z)))
            {
                queue.Enqueue(p);
            }
        }

        foreach (var s in starts)
        {
            Add(s);
        }

        while (queue.Count > 0 && seen.Count < limit)
        {
            var p = queue.Dequeue();

            foreach (var tele in map.GetItemsAt<Teleporter>(new Point2D(p.X, p.Y)))
            {
                if (tele.Active && (gated || tele.GetType() == typeof(Teleporter)) && (tele.MapDest ?? map) == map && tele.PointDest != Point3D.Zero &&
                    FiresAt(tele, p.Z))
                {
                    Add(tele.PointDest);
                }
            }

            for (var d = 0; d < 8; d++)
            {
                if (Movement.Movement.CheckMovement(_probe, map, p, (Direction)d, out var newZ))
                {
                    int x = p.X, y = p.Y;
                    Movement.Movement.Offset((Direction)d, ref x, ref y);
                    Add(new Point3D(x, y, newZ));
                }
            }
        }

        return seen;
    }

    /// <summary>Сработает ли шаг на плитку на высоте z — то же условие, что в Mobile.Move.</summary>
    private static bool FiresAt(Item tele, int z) => tele.Z == z || tele.Z + tele.ItemData.Height > z && z + 15 > tele.Z;

    /// <summary>
    /// Чинит телепорты, лежащие не на высоте пола (Mobile.Move включает плитку, только если
    /// высоты совпали): ставит каждый на ближайшую поверхность в пределах 6 по высоте. Отвечает
    /// списком починенных.
    /// </summary>
    public static List<string> RepairTeleporters()
    {
        var fixedOnes = new List<string>();
        System.Span<sbyte> zs = stackalloc sbyte[8];
        foreach (var map in Map.AllMaps)
        {
            if (map == null || map == Map.Internal)
            {
                continue;
            }

            var all = new List<Teleporter>();
            foreach (var t in map.GetItemsInBounds<Teleporter>(new Rectangle2D(0, 0, map.Width, map.Height)))
            {
                all.Add(t);
            }

            foreach (var tele in all)
            {
                if (tele.Parent != null || !tele.Active || Fires(map, tele))
                {
                    continue;
                }

                var n = Engines.Pathing.Cache.StepProbe.ComputeStandableSurfaceZs(map, tele.X, tele.Y, zs);
                int? best = null;
                for (var i = 0; i < n; i++)
                {
                    if (System.Math.Abs(zs[i] - tele.Z) <= 6 && (best == null || System.Math.Abs(zs[i] - tele.Z) < System.Math.Abs(best.Value - tele.Z)))
                    {
                        best = zs[i];
                    }
                }

                if (best is { } z)
                {
                    fixedOnes.Add($"{map.Name} {tele.Location} -> z {z}");
                    tele.Z = z;
                }
            }
        }

        return fixedOnes;
    }

    /// <summary>Есть ли на плитке телепорта поверхность, встав на которую, его включишь.</summary>
    private static bool Fires(Map map, Item tele)
    {
        System.Span<sbyte> zs = stackalloc sbyte[8];
        var n = Engines.Pathing.Cache.StepProbe.ComputeStandableSurfaceZs(map, tele.X, tele.Y, zs);
        for (var i = 0; i < n; i++)
        {
            if (FiresAt(tele, zs[i]))
            {
                return true;
            }
        }

        // Пол может быть и предметом (мост, плиты): тогда стоят на его верхушке.
        foreach (var item in map.GetItemsAt(tele.X, tele.Y))
        {
            if (item != tele && item.ItemData.Surface && FiresAt(tele, item.Z + item.ItemData.CalcHeight))
            {
                return true;
            }
        }

        return n == 0; // земли под плиткой нет вовсе — судить не по чему
    }

    /// <summary>Свободная клетка рядом с телепортом: ходок встаёт на неё и сам шагает на телепорт.</summary>
    private static Point3D Beside(Map map, Point3D p)
    {
        for (var r = 1; r <= 3; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != r)
                    {
                        continue;
                    }

                    var x = p.X + dx;
                    var y = p.Y + dy;
                    if (HasTeleporter(map, x, y))
                    {
                        continue;
                    }

                    if (map.CanFit(x, y, p.Z, 16, false, false))
                    {
                        return new Point3D(x, y, p.Z);
                    }

                    var z = map.GetAverageZ(x, y);
                    if (System.Math.Abs(z - p.Z) <= 8 && map.CanFit(x, y, z, 16, false, false))
                    {
                        return new Point3D(x, y, z);
                    }
                }
            }
        }

        return p;
    }

    private static bool HasTeleporter(Map map, int x, int y)
    {
        foreach (var _ in map.GetItemsAt<Teleporter>(new Point2D(x, y)))
        {
            return true;
        }

        return false;
    }

    private static Dictionary<int, int> Dijkstra(NavMapGraph graph, NavSnapshot snap, int mapId, HashSet<int> inside, List<int> seeds)
    {
        var dist = new Dictionary<int, int>();
        var open = new PriorityQueue<int, int>();

        foreach (var s in seeds)
        {
            if (inside.Contains(s) && dist.TryAdd(s, 0))
            {
                open.Enqueue(s, 0);
            }
        }

        void Relax(int node, int cost)
        {
            if (!inside.Contains(node) || dist.TryGetValue(node, out var old) && old <= cost)
            {
                return;
            }

            dist[node] = cost;
            open.Enqueue(node, cost);
        }

        while (open.TryDequeue(out var node, out var cost))
        {
            if (dist[node] < cost)
            {
                continue;
            }

            for (var e = graph.EdgeStart[node]; e < graph.EdgeStart[node + 1]; e++)
            {
                Relax(graph.EdgeTarget[e], cost + graph.EdgeCost[e]);
            }

            if (snap.LinksFrom.TryGetValue(NavLinks.NodeKey(mapId, node), out var links))
            {
                foreach (var link in links)
                {
                    if (link.DestMapId == mapId)
                    {
                        Relax(link.DestRegion, cost + link.Cost);
                    }
                }
            }
        }

        return dist;
    }
}
