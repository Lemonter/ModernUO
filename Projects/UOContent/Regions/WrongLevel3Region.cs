using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Server.Mobiles;

namespace Server.Regions;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Region.cs) — level 3 captures instead of killing: a
///     player who dies there is dragged back to the cell block and resurrected, with the "you are captured
///     by the jailor" message. A second death before leaving the level drops the capture flag instead, so
///     repeated deaths cannot farm the free resurrection.
/// </summary>
public class WrongLevel3Region : DungeonRegion
{
    private readonly List<Mobile> _captured = new();

    [JsonConstructor] // Don't include parent, since it is special
    public WrongLevel3Region(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public WrongLevel3Region(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public WrongLevel3Region(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public override void OnDeath(Mobile m)
    {
        base.OnDeath(m);

        if (m is not PlayerMobile player)
        {
            return;
        }

        player.MoveToWorld(new Point3D(5703, 639, 0), Map);

        if (!_captured.Contains(player))
        {
            player.Resurrect();
            _captured.Add(player);
        }
        else
        {
            _captured.Remove(player);
        }

        Timer.DelayCall(
            TimeSpan.FromSeconds(2),
            () => player.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1152076) // You are captured by the jailor and returned to your cell.
        );
    }

    public override void OnExit(Mobile m)
    {
        if (m is PlayerMobile)
        {
            _captured.Remove(m);
        }

        base.OnExit(m);
    }
}
