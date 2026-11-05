using System;
using System.Text.Json.Serialization;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Bushido;
using Server.Spells.Chivalry;
using Server.Spells.Ninjitsu;

namespace Server.Regions;

/// <summary>
///     Ported from ServUO's Blackthorn Revamped dungeon (Region.cs) — the castle half: no paladin, ninja
///     or samurai abilities inside, no mounts (they are dismounted and sent to the stables), and an escort
///     trigger for the "visit the castle" quest step.
///
///     Adapted: ServUO's <c>QuestHelper</c> hook (<c>AVisitToCastleBlackthornQuest.CheckLocation</c>) has no
///     counterpart here — that quest is not ported — so the castle-crossing check is gone. The
///     <c>Allured</c> pet flag does not exist either; every other stabling rule is unchanged.
/// </summary>
public class BlackthornCastleRegion : GuardedRegion
{
    public static readonly Point3D[] StableLocs =
    {
        new(1510, 1543, 25),
        new(1516, 1542, 25),
        new(1520, 1542, 25),
        new(1525, 1542, 25)
    };

    [JsonConstructor] // Don't include parent, since it is special
    public BlackthornCastleRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public BlackthornCastleRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public BlackthornCastleRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public override bool OnBeginSpellCast(Mobile m, ISpell s)
    {
        if (m.AccessLevel > AccessLevel.Player)
        {
            return base.OnBeginSpellCast(m, s);
        }

        int loc;

        if (s is PaladinSpell)
        {
            loc = 1062075; // You cannot use a Paladin ability here.
        }
        else if (s is NinjaMove or NinjaSpell or SamuraiSpell or SamuraiMove)
        {
            loc = 1062938; // That ability does not seem to work here.
        }
        else
        {
            return base.OnBeginSpellCast(m, s);
        }

        m.SendLocalizedMessage(loc);
        return false;
    }

    public override void OnLocationChanged(Mobile m, Point3D oldLocation)
    {
        base.OnLocationChanged(m, oldLocation);

        if (m.AccessLevel > AccessLevel.Player)
        {
            return;
        }

        if (m.Mount != null)
        {
            if (m is PlayerMobile pm)
            {
                pm.SetMountBlock(BlockMountType.DismountRecovery, TimeSpan.FromSeconds(30), true);
            }
            else
            {
                m.Mount.Rider = null;
            }

            m.SendLocalizedMessage(1153052); // Mounts and flying are not permitted in this area.

            if (m.Mount is BaseCreature { Controlled: true } mount)
            {
                TryAutoStable(mount);
            }
        }

        if (m is BaseCreature { Controlled: true } pet)
        {
            TryAutoStable(pet);
        }
    }

    public void TryAutoStable(BaseCreature pet)
    {
        if (pet == null)
        {
            return;
        }

        var owner = pet.GetMaster();

        if (!pet.Controlled || owner is not PlayerMobile ownerPm)
        {
            if (owner != null)
            {
                SendToStables(pet, owner);
            }

            return;
        }

        if (pet.Body.IsHuman || pet.IsDeadPet || ownerPm.Stabled.Count >= AnimalTrainer.GetMaxStabled(owner) ||
            pet is PackLlama or PackHorse or Beetle && pet.Backpack?.Items.Count > 0)
        {
            SendToStables(pet, owner);
            return;
        }

        pet.ControlTarget = null;
        pet.ControlOrder = OrderType.Stay;
        pet.Internalize();

        pet.SetControlMaster(null);
        pet.SummonMaster = null;

        pet.IsStabled = true;
        pet.StabledBy = owner;

        if (Core.SE)
        {
            pet.Loyalty = BaseCreature.MaxLoyalty; // Wonderfully happy
        }

        ownerPm.Stabled.Add(pet);

        // Pets are not permitted in this location. Your pet named ~1_NAME~ has been sent to the stables.
        owner.SendLocalizedMessage(1153050, pet.Name);
    }

    public void SendToStables(BaseCreature bc, Mobile m = null)
    {
        var p = StableLocs[Utility.Random(StableLocs.Length)];
        bc.MoveToWorld(p, Map);

        // Pets are not permitted in this area. Your pet named ~1_NAME~ could not be sent to the stables...
        m?.SendLocalizedMessage(1153053, bc.Name);
    }

    public override bool CheckTravel(Mobile traveller, Point3D p, TravelCheckType type, out TextDefinition message)
    {
        message = null;

        return traveller.AccessLevel > AccessLevel.Player || type > TravelCheckType.Mark;
    }
}
