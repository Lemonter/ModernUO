using System.Collections.Generic;
using Server.Engines.Pathing.Nav;

namespace Server.Systems.Bots;

/// <summary>
/// Gathering spots every bot shares: where someone found ore, trees or water to stand next to.
/// Bots prospect around their town when the atlas has nothing near; a spot that turns out dry or
/// unreachable is benched for a while rather than forgotten, since veins and trees respawn.
///
/// In memory only: prospecting is cheap, and a restart rebuilds it within minutes.
/// </summary>
public static class ResourceAtlas
{
    private const int MaxSpotsPerKind = 4000;
    private const int ProspectSamples = 48;
    private const long BenchMs = 15 * 60_000;

    private sealed class Spot
    {
        public Point3D Location;
        public long BenchedUntil;
        public bool Benched;
    }

    private static readonly Dictionary<(Map, ResourceKind), List<Spot>> _spots = new();

    public static int Count(Map map, ResourceKind kind) => _spots.TryGetValue((map, kind), out var list) ? list.Count : 0;

    /// <summary>A known, unbenched spot within <paramref name="range"/>, nearest first among a few
    /// random picks so bots spread over the spots instead of queuing at one.</summary>
    public static bool TryPick(Map map, Point3D near, int range, ResourceKind kind, out Point3D spot)
    {
        spot = Point3D.Zero;
        if (!_spots.TryGetValue((map, kind), out var list) || list.Count == 0)
        {
            return false;
        }

        var now = Core.TickCount;
        var bestDist = int.MaxValue;

        for (var i = 0; i < 12; i++)
        {
            var s = list[Utility.Random(list.Count)];
            if (s.Benched && now - s.BenchedUntil < 0)
            {
                continue;
            }

            var dist = NavMath.Octile(near.X, near.Y, s.Location.X, s.Location.Y) / 10;
            if (dist <= range && dist < bestDist)
            {
                bestDist = dist;
                spot = s.Location;
            }
        }

        return bestDist != int.MaxValue;
    }

    /// <summary>
    /// Samples standable ground around <paramref name="center"/> for a place next to the resource.
    /// Each sample is a nav-graph lookup and a few tile reads; the sample count bounds the cost.
    /// </summary>
    public static bool TryProspect(Map map, Point3D center, int radius, ResourceKind kind, out Point3D spot)
    {
        spot = Point3D.Zero;
        var graph = NavSystem.GetGraph(map);
        if (graph == null)
        {
            return false;
        }

        for (var i = 0; i < ProspectSamples; i++)
        {
            var x = center.X + Utility.RandomMinMax(-radius, radius);
            var y = center.Y + Utility.RandomMinMax(-radius, radius);
            var region = graph.Locate(x, y, center.Z, 2);
            if (region < 0)
            {
                continue;
            }

            // The region's own representative cell is guaranteed standable; the sample cell
            // itself usually is too and sits closer to whatever made the sample interesting.
            var candidate = new Point3D(graph.RegionX[region], graph.RegionY[region], graph.RegionZ[region]);
            if (graph.Locate(x, y, candidate.Z, 0) == region)
            {
                candidate = new Point3D(x, y, candidate.Z);
            }

            if (ResourceProbe.HasTargetNear(map, candidate, kind))
            {
                Remember(map, candidate, kind);
                spot = candidate;
                return true;
            }
        }

        return false;
    }

    public static void Remember(Map map, Point3D location, ResourceKind kind)
    {
        if (!_spots.TryGetValue((map, kind), out var list))
        {
            _spots[(map, kind)] = list = [];
        }

        foreach (var s in list)
        {
            if (Utility.InRange(s.Location, location, 1))
            {
                s.Benched = false;
                return;
            }
        }

        if (list.Count >= MaxSpotsPerKind)
        {
            list[Utility.Random(list.Count)] = new Spot { Location = location };
            return;
        }

        list.Add(new Spot { Location = location });
    }

    /// <summary>Sets a spot aside for a while: dry, or the way there failed.</summary>
    public static void Bench(Map map, Point3D location, ResourceKind kind)
    {
        if (!_spots.TryGetValue((map, kind), out var list))
        {
            return;
        }

        foreach (var s in list)
        {
            if (Utility.InRange(s.Location, location, 2))
            {
                s.Benched = true;
                s.BenchedUntil = Core.TickCount + BenchMs;
            }
        }
    }
}
