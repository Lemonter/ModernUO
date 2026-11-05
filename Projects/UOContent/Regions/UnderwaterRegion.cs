using System.Text.Json.Serialization;
using Server.Engines.ExploringTheDeep;
using Server.Mobiles;
using Server.Spells;

namespace Server.Regions;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Regions.cs, <c>Underwater</c>). Gravewater
///     Lake's seabed: no mounts, no flying, no followers (bar the paralithode that lives down there), the
///     whole Salvager's Suit worn, and the suit-assembly quest finished. Leaving kills any paralithode that
///     followed the player out.
///
///     Upstream bug fixed: ServUO's check chain was a single <c>else if</c> ladder whose follower branch was
///     "reject only if no follower is a paralithode" — so bringing a paralithode let a player skip the quest
///     and equipment checks entirely and swim down naked. The checks are independent here; a paralithode
///     only excuses the follower rule.
/// </summary>
public class UnderwaterRegion : BaseRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public UnderwaterRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public UnderwaterRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }

    public UnderwaterRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public override bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
    {
        if (!base.OnMoveInto(m, d, newLocation, oldLocation))
        {
            return false;
        }

        if (m is PlayerMobile pm)
        {
            if (m.AccessLevel == AccessLevel.Player)
            {
                if (m.Mounted || m.Flying)
                {
                    // You cannot proceed while mounted or flying!
                    m.SendLocalizedMessage(1154411);
                    return false;
                }

                if (HasNonParalithodeFollowers(pm))
                {
                    // You cannot proceed while pets are under your control!
                    pm.SendLocalizedMessage(1154412);
                    return false;
                }

                if (!ExploringDeepChain.SuitComplete(pm))
                {
                    // You feel as though by doing this you are missing out on an important part of your journey...
                    m.SendLocalizedMessage(1154325);
                    return false;
                }

                if (!ExploringDeepChain.WearingSuit(pm))
                {
                    // You couldn't hope to survive proceeding without the proper equipment...
                    m.SendLocalizedMessage(1154413);
                    return false;
                }
            }
        }
        else if (m is BaseCreature and not Paralithode)
        {
            return false;
        }

        return true;
    }

    private static bool HasNonParalithodeFollowers(PlayerMobile pm)
    {
        var followers = pm.AllFollowers;

        if (followers.Count == 0)
        {
            return false;
        }

        foreach (var follower in followers)
        {
            if (follower is not Paralithode)
            {
                return true;
            }
        }

        return false;
    }

    public override void OnExit(Mobile m)
    {
        if (m is Paralithode)
        {
            m.Delete();
        }

        base.OnExit(m);
    }

    public override bool AllowHousing(Mobile from, Point3D p) => false;

    public override bool CheckTravel(Mobile m, Point3D newLocation, TravelCheckType travelType, out TextDefinition message)
    {
        message = null;
        return false;
    }
}
