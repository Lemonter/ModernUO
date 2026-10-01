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
