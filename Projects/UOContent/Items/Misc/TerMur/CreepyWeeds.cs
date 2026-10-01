using System;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Items/Quest/CreepyWeeds.cs). Ballem and FNPitchfork
/// don't exist anywhere in this codebase (both tied to a specific ML "Farmer Nash" quest
/// chain never ported here) — dropped those switch cases and the pitchfork reward, keeping
/// the other four monster-in-the-weeds outcomes.</summary>
[SerializationGenerator(0, false)]
public partial class CreepyWeeds : Item
{
    [Constructible]
    public CreepyWeeds() : base(0x0CB8)
    {
        Weight = 1;
        Movable = false;

        Timer.DelayCall(TimeSpan.FromMinutes(10.0), Delete);
    }

    public override string DefaultName => "жуткие сорняки";

    public override void OnDoubleClick(Mobile from)
    {
        if (!CheckUse(from))
        {
            return;
        }

        var map = Map;
        var loc = Location;

        if (map != null && map != Map.Internal && from.InRange(loc, 1) && from.InLOS(this))
        {
            Delete();

            switch (Utility.Random(5))
            {
                case 0:
                    new Snake().MoveToWorld(loc, map);
                    break;
                case 1:
                    new Mongbat().MoveToWorld(loc, map);
                    break;
                case 2:
                    new SilverSerpent().MoveToWorld(loc, map);
                    break;
                case 3:
                    new Raptor().MoveToWorld(loc, map);
                    break;
            }
        }
    }

    private bool CheckUse(Mobile from)
    {
        if (Deleted || !IsAccessibleTo(from))
        {
            return false;
        }

        if (from.Map != Map || !from.InRange(GetWorldLocation(), 1))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return false;
        }

        return true;
    }
}
