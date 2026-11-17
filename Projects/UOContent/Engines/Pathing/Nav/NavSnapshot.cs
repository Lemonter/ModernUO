using System.Collections.Generic;

namespace Server.Engines.Pathing.Nav;

/// <summary>
/// Everything a route search reads, frozen: the graph of each map, its islands, and the links.
/// Rebuilt on the loop whenever a graph or the links change and published behind one volatile
/// reference, so a search running on the route worker sees one consistent world while the loop
/// moves on.
///
/// Off-loop readers touch only the graphs' baked arrays — never <see cref="NavMapGraph.Locate"/>,
/// whose cluster cache is loop-only state.
/// </summary>
public sealed class NavSnapshot
{
    public static readonly NavSnapshot Empty = new(new NavMapGraph[0x100], new int[0x100][], [], new Dictionary<long, NavLink[]>());

    public NavSnapshot(NavMapGraph[] graphs, int[][] islands, NavLink[] links, Dictionary<long, NavLink[]> linksFrom)
    {
        Graphs = graphs;
        Islands = islands;
        Links = links;
        LinksFrom = linksFrom;
        _world = BuildWorldIslands(islands, links);
    }

    // Острова всех карт, сшитые телепортами и вратами (в обе стороны — оценка сверху): если у
    // начала и цели они разные, дороги нет наверняка, и искать её незачем. Без этого каждый поиск
    // к недостижимой цели перебирал до двухсот тысяч узлов, и такие поиски забивали очередь.
    private readonly Dictionary<long, int> _world;

    private static long IslandKey(int mapId, int island) => ((long)mapId << 32) | (uint)island;

    private static Dictionary<long, int> BuildWorldIslands(int[][] islands, NavLink[] links)
    {
        var parent = new Dictionary<long, long>();

        long Find(long x)
        {
            while (parent.TryGetValue(x, out var p) && p != x)
            {
                if (parent.TryGetValue(p, out var gp))
                {
                    parent[x] = gp;
                }

                x = p;
            }

            return x;
        }

        foreach (var link in links)
        {
            var a = IslandOf(islands, link.SourceMapId, link.SourceRegion);
            var b = IslandOf(islands, link.DestMapId, link.DestRegion);
            if (a < 0 || b < 0)
            {
                continue;
            }

            var ka = IslandKey(link.SourceMapId, a);
            var kb = IslandKey(link.DestMapId, b);
            parent.TryAdd(ka, ka);
            parent.TryAdd(kb, kb);
            var ra = Find(ka);
            var rb = Find(kb);
            if (ra != rb)
            {
                parent[ra] = rb;
            }
        }

        var world = new Dictionary<long, int>(parent.Count);
        var ids = new Dictionary<long, int>();
        foreach (var key in parent.Keys)
        {
            var root = Find(key);
            if (!ids.TryGetValue(root, out var id))
            {
                ids[root] = id = ids.Count + 1;
            }

            world[key] = id;
        }

        return world;
    }

    private static int IslandOf(int[][] islands, int mapId, int region) =>
        mapId >= 0 && mapId < islands.Length && islands[mapId] is { } map && region >= 0 && region < map.Length ? map[region] : -1;

    /// <summary>
    /// Дороги между узлами точно нет: разные острова, и никакие телепорты их не соединяют. False —
    /// дорога, может быть, есть (решит поиск).
    /// </summary>
    public bool Unreachable(long startNode, long goalNode)
    {
        int startMap = (int)(startNode >> 32), goalMap = (int)(goalNode >> 32);
        var a = IslandOf(Islands, startMap, (int)(startNode & 0xFFFFFFFF));
        var b = IslandOf(Islands, goalMap, (int)(goalNode & 0xFFFFFFFF));
        if (a < 0 || b < 0 || startMap >= Graphs.Length || goalMap >= Graphs.Length || Graphs[startMap] == null || Graphs[goalMap] == null)
        {
            return false;
        }

        if (startMap == goalMap && a == b)
        {
            return false;
        }

        // Остров, у которого нет ни одного телепорта, ни с кем не сшит.
        var wa = _world.GetValueOrDefault(IslandKey(startMap, a));
        var wb = _world.GetValueOrDefault(IslandKey(goalMap, b));
        return wa == 0 || wb == 0 || wa != wb;
    }

    /// <summary>Indexed by map id.</summary>
    public NavMapGraph[] Graphs { get; }

    /// <summary>Indexed by map id, then region: undirected connected component.</summary>
    public int[][] Islands { get; }

    public NavLink[] Links { get; }

    public Dictionary<long, NavLink[]> LinksFrom { get; }

    public static NavSnapshot Build(NavMapGraph[] graphs, int[][] islands, IReadOnlyList<NavLink> links)
    {
        var grouped = new Dictionary<long, List<NavLink>>();
        foreach (var link in links)
        {
            var key = NavLinks.NodeKey(link.SourceMapId, link.SourceRegion);
            if (!grouped.TryGetValue(key, out var list))
            {
                grouped[key] = list = [];
            }

            list.Add(link);
        }

        var linksFrom = new Dictionary<long, NavLink[]>(grouped.Count);
        foreach (var (key, list) in grouped)
        {
            linksFrom[key] = list.ToArray();
        }

        var all = new NavLink[links.Count];
        for (var i = 0; i < all.Length; i++)
        {
            all[i] = links[i];
        }

        return new NavSnapshot((NavMapGraph[])graphs.Clone(), (int[][])islands.Clone(), all, linksFrom);
    }
}
