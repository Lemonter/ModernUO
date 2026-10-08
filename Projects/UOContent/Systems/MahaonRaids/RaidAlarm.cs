using System.Collections.Generic;

namespace Server.Systems.MahaonRaids;

/// <summary>A town under attack: where the raiders were sent and which town it is.</summary>
public sealed class RaidAlert
{
    public Map Map;
    public Point3D Location;
    public string City;
    public long RaisedAt;
}

/// <summary>
/// The towns raided right now, for whoever wants to answer the call — the bots go to defend or
/// get out of the way. A raid is over once no raider is left standing near where it struck,
/// or after a while regardless.
/// </summary>
public static class RaidAlarm
{
    private const long MaxDurationMs = 60 * 60_000;
    private const int RaiderSearchRange = 40;

    private static readonly List<RaidAlert> _alerts = [];

    public static void Raise(Map map, Point3D location, string city)
    {
        if (map == null || map == Map.Internal)
        {
            return;
        }

        _alerts.Add(new RaidAlert { Map = map, Location = location, City = city, RaisedAt = Core.TickCount });
        MahaonCities.CityGuardChatter.OnRaid(MahaonCities.CityControlSystem.Find(city));
    }

    /// <summary>The raid in progress nearest a point within <paramref name="range"/>, or null.</summary>
    public static RaidAlert Nearest(Map map, Point3D from, int range)
    {
        var now = Core.TickCount;
        _alerts.RemoveAll(a => now - a.RaisedAt >= MaxDurationMs);

        RaidAlert best = null;
        var bestDist = (double)range;

        for (var i = _alerts.Count - 1; i >= 0; i--)
        {
            var alert = _alerts[i];
            if (alert.Map != map)
            {
                continue;
            }

            var dist = alert.Location.GetDistanceToSqrt(from);
            if (dist >= bestDist)
            {
                continue;
            }

            if (RaiderNear(alert) == null)
            {
                _alerts.RemoveAt(i);
                continue;
            }

            bestDist = dist;
            best = alert;
        }

        return best;
    }

    /// <summary>A raider still standing where the raid struck.</summary>
    public static Mobile RaiderNear(RaidAlert alert)
    {
        foreach (var m in alert.Map.GetMobilesInRange(alert.Location, RaiderSearchRange))
        {
            if (m is IRaidSpawn && m.Alive && !m.Deleted)
            {
                return m;
            }
        }

        return null;
    }
}
