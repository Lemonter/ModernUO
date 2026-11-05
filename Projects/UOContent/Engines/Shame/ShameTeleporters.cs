using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.ShameRevamped;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (ShameTeleporter.cs) — the altar's one-way exit and
///     the pass-through teleporter that only works while the rubble wall next to it is still standing.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShameTeleporter : Teleporter
{
    [Constructible]
    public ShameTeleporter(Point3D dest, Map map) : base(dest, map, true)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ShameWallTeleporter : Teleporter
{
    public ShameWallTeleporter(Point3D dest, Map map) : base(dest, map, true)
    {
    }

    public override bool CanTeleport(Mobile m)
    {
        if (Deleted || Map == null || Map == Map.Internal)
        {
            return false;
        }

        foreach (var item in Map.GetItemsInRange(Location, 1))
        {
            if (item is AddonComponent { Addon: ShameWall { Visible: true } })
            {
                return true;
            }
        }

        return false;
    }

    public override void DoTeleport(Mobile m)
    {
        m.SendLocalizedMessage(1072790); // The wall becomes transparent, and you push your way through it.

        base.DoTeleport(m);
    }
}
