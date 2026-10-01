namespace Server.Systems.MahaonBots;

/// <summary>
///     A dungeon a bot party can travel to. Coordinates are the real per-shard Entrance
///     points from Distribution/Data/regions.json's DungeonRegion entries (not guesses —
///     the previous 5-entry list here was placeholder coordinates that didn't match this
///     shard's actual data at all). Felucca only for now — bots have no facet-crossing
///     travel logic (moongate use, etc.), so Ilshenar (Rock/Spider/Spectre/Blood/Ankh/
///     Wisp/Exodus/Sorcerer's Dungeon/Ancient Lair), Malas (Doom/Doom Gauntlet/Labyrinth/
///     Orc Fortress) and TerMur (Sanctuary/Painted Caves/Prism of Light/Blighted Grove)
///     dungeons are left out until that exists — adding them here without it would just
///     make bots walk in place at a facet boundary forever.
/// </summary>
public readonly struct DungeonTarget
{
    public readonly string Name;
    public readonly Point3D Entrance;
    public readonly Map Map;

    public DungeonTarget(string name, Point3D entrance, Map map)
    {
        Name = name;
        Entrance = entrance;
        Map = map;
    }

    public static readonly DungeonTarget[] Known =
    [
        new DungeonTarget("Despise", new Point3D(1296, 1082, 0), Map.Felucca),
        new DungeonTarget("Deceit", new Point3D(4111, 429, 0), Map.Felucca),
        new DungeonTarget("Destard", new Point3D(1176, 2635, 0), Map.Felucca),
        new DungeonTarget("Covetous", new Point3D(2499, 916, 0), Map.Felucca),
        new DungeonTarget("Shame", new Point3D(512, 1559, 0), Map.Felucca),
        new DungeonTarget("Hythloth", new Point3D(4722, 3814, 0), Map.Felucca),
        new DungeonTarget("Khaldun", new Point3D(5882, 3819, 0), Map.Felucca),
        new DungeonTarget("Wrong", new Point3D(2042, 226, 0), Map.Felucca),
        new DungeonTarget("Terathan Keep", new Point3D(5426, 3120, 0), Map.Felucca),
        new DungeonTarget("Fire", new Point3D(2922, 3402, 0), Map.Felucca),
        new DungeonTarget("Ice", new Point3D(1996, 80, 0), Map.Felucca),
        new DungeonTarget("Orc Cave", new Point3D(1014, 1434, 0), Map.Felucca)
    ];
}
