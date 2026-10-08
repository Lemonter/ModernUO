using Server.Engines.Pathing.Nav;
using Server.Mobiles;

namespace Server.Systems.Bots;

public static class BotMovement
{
    // Player movement speeds (ms per step) — the same pacing the server enforces on clients.
    private const int WalkFoot = 400;
    private const int RunFoot = 200;
    private const int WalkMounted = 200;
    private const int RunMounted = 100;

    public static int StepDelay(Mobile m, bool run) =>
        m.Mounted ? run ? RunMounted : WalkMounted : run ? RunFoot : WalkFoot;

    /// <summary>
    /// A standable spot near <paramref name="center"/>: the representative cell of a nav region
    /// found around a random point. Always a place the walker can stand on, so a stroll never
    /// targets a wall or the inside of a rock.
    /// </summary>
    public static bool TryRandomSpot(Map map, Point3D center, int radius, out Point3D spot)
    {
        var graph = NavSystem.GetGraph(map);
        if (graph != null)
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var x = center.X + Utility.RandomMinMax(-radius, radius);
                var y = center.Y + Utility.RandomMinMax(-radius, radius);
                var region = graph.Locate(x, y, center.Z, 2);

                if (region >= 0)
                {
                    spot = new Point3D(graph.RegionX[region], graph.RegionY[region], graph.RegionZ[region]);
                    return true;
                }
            }
        }

        spot = center;
        return false;
    }
}
