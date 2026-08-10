using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Network;

namespace Server.Systems.MahaonWorld;

public readonly struct StaticOverride
{
    public readonly uint Id;
    public readonly Point3D Location;
    public readonly Map Map;
    public readonly ushort OriginalGraphic;
    public readonly ushort ReplacementGraphic;
    public readonly DateTime ExpireTime;

    public StaticOverride(uint id, Point3D location, Map map, ushort originalGraphic, ushort replacementGraphic, DateTime expireTime)
    {
        Id = id;
        Location = location;
        Map = map;
        OriginalGraphic = originalGraphic;
        ReplacementGraphic = replacementGraphic;
        ExpireTime = expireTime;
    }
}

/// <summary>
///     Dynamic Static Override System — tells our custom ClassicUO fork "draw this graphic
///     instead of that one, at this exact spot", entirely as a visual layer. Never touches
///     an Item, never touches the map. The server is the sole source of truth; state resets
///     on restart, which is fine since it's cosmetic (chopped trees just look normal again
///     until re-chopped, no gameplay impact).
/// </summary>
public static class StaticOverrideManager
{
    private static readonly Dictionary<uint, StaticOverride> Active = new();
    private static uint _nextId = 1;

    // How far around an override's location a player needs to be to receive it — matches
    // typical UO client view range with a little slack.
    private const int NotifyRange = 24;

    public static uint AddOverride(Point3D location, Map map, ushort originalGraphic, ushort replacementGraphic, TimeSpan duration)
    {
        var id = _nextId++;
        var expireTime = Core.Now + duration;
        var entry = new StaticOverride(id, location, map, originalGraphic, replacementGraphic, expireTime);

        Active[id] = entry;

        NotifyNearbyPlayers(entry, StaticOverrideAction.Add);

        Timer.DelayCall(duration, () => ExpireOverride(id));

        return id;
    }

    public static void RemoveOverride(uint id)
    {
        if (!Active.TryGetValue(id, out var entry))
        {
            return;
        }

        Active.Remove(id);
        NotifyNearbyPlayers(entry, StaticOverrideAction.Remove);
    }

    private static void ExpireOverride(uint id)
    {
        if (Active.TryGetValue(id, out var entry) && Core.Now >= entry.ExpireTime)
        {
            RemoveOverride(id);
        }
    }

    private static void NotifyNearbyPlayers(StaticOverride entry, StaticOverrideAction action)
    {
        if (entry.Map == null)
        {
            return;
        }

        foreach (var mobile in entry.Map.GetMobilesInRange(entry.Location, NotifyRange))
        {
            if (mobile is PlayerMobile { NetState: { } ns })
            {
                ns.SendStaticGraphicOverride(
                    entry.Id, entry.Location.X, entry.Location.Y, entry.Location.Z,
                    entry.OriginalGraphic, entry.ReplacementGraphic,
                    (uint)Math.Max(0, (entry.ExpireTime - Core.Now).TotalSeconds), action
                );
            }
        }
    }

    /// <summary>Sends every currently-active override near a player to them directly — call
    /// this on login and on map change, so a player who wasn't around when an override was
    /// first added still sees it correctly once they're back in range.</summary>
    public static void ResendActiveOverridesTo(PlayerMobile player)
    {
        if (player.NetState == null || player.Map == null)
        {
            return;
        }

        foreach (var entry in Active.Values)
        {
            if (entry.Map != player.Map || !player.InRange(entry.Location, NotifyRange))
            {
                continue;
            }

            player.NetState.SendStaticGraphicOverride(
                entry.Id, entry.Location.X, entry.Location.Y, entry.Location.Z,
                entry.OriginalGraphic, entry.ReplacementGraphic,
                (uint)Math.Max(0, (entry.ExpireTime - Core.Now).TotalSeconds), StaticOverrideAction.Add
            );
        }
    }
}
