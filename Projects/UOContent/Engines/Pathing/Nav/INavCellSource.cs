namespace Server.Engines.Pathing.Nav;

/// <summary>
/// Static walkability, one cluster at a time. The live implementation reads the pathfinding step
/// cache; tests feed synthetic grids.
/// </summary>
public interface INavCellSource
{
    int Width { get; }
    int Height { get; }

    /// <summary>Refills <paramref name="cells"/> with every standable surface in the cluster.
    /// Cells outside the map stay empty.</summary>
    void FillCluster(int clusterX, int clusterY, NavClusterCells cells);
}
