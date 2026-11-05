namespace Server.Engines.Blackthorn;

/// <summary>
///     Ported from ServUO's Blackthorn invasion spawner (InvasionSpawner/InvasionDefinition.cs) — one
///     city's invasion layout: the rectangles a wave's groups spawn in, and where Minax's beacon stands.
///     Nothing here is persisted; the table lives in <see cref="InvasionController" />.
/// </summary>
public class InvasionDefinition
{
    public InvasionDefinition(Rectangle2D[] spawn, Point3D beaconLoc)
    {
        SpawnRecs = spawn;
        BeaconLoc = beaconLoc;
    }

    public Rectangle2D[] SpawnRecs { get; set; }

    public Point3D BeaconLoc { get; set; }
}
