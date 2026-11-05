using System;
using System.Text.Json.Serialization;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Chivalry;
using Server.Spells.Fourth;
using Server.Spells.Sixth;
using Server.Spells.Seventh;
using Server.Spells.Third;

namespace Server.Regions;

/// <summary>
///     ServUO's <c>CusteauPerronHouseRegion</c> (Scripts/Services/ExploringTheDeep/Regions.cs): the frozen
///     tinker's house is warded against every travel spell, so the only way in is the door.
/// </summary>
public class CousteauPerronHouseRegion : GuardedRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public CousteauPerronHouseRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public CousteauPerronHouseRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public CousteauPerronHouseRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }

    public CousteauPerronHouseRegion(string name, Map map, Region parent, int priority, Type guardType, params Rectangle3D[] area) : base(name, map, parent, priority, guardType, area)
    {
    }

    public override bool OnBeginSpellCast(Mobile from, ISpell s)
    {
        if (from.Player &&
            s is TeleportSpell or GateTravelSpell or RecallSpell or MarkSpell or SacredJourneySpell)
        {
            from.SendLocalizedMessage(500015); // You do not have that spell!
            return false;
        }

        return base.OnBeginSpellCast(from, s);
    }
}
