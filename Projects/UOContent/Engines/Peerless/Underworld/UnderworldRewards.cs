using System;
using ModernUO.Serialization;
using Server.Regions;
using Server.Targeting;

namespace Server.Items;

/// <summary>The rewards the Underworld quest line pays out, ported from ServUO. Each was found
/// in a different corner of that tree — Functional, Artifacts/Tools,
/// Artifacts/Equipment/Jewelry, Containers, Equipment/Talismans — which is why an earlier pass
/// concluded, wrongly, that they didn't exist.</summary>
[SerializationGenerator(0, false)]
public partial class ArielHavenWritofMembership : Item
{
    [Constructible]
    public ArielHavenWritofMembership() : base(0x14ED) => LootType = LootType.Blessed;

    public override int LabelNumber => 1094998; // Ariel Haven Writ of Membership
}

/// <summary>Elder Dugan's reward for "Missing". Ported from ServUO
/// (Scripts/Items/Artifacts/Equipment/Armor/CandlewoodTorch.cs) — a shield-slot torch that can
/// be lit and snuffed, and whose light drives off the swarms and the Meer illusions.</summary>
[SerializationGenerator(0, false)]
public partial class CandlewoodTorch : BaseShield
{
    [Constructible]
    public CandlewoodTorch() : base(0xF6B)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;

        Attributes.SpellChanneling = 1;
        Attributes.CastSpeed = -1;
    }

    public override int LabelNumber => 1094957; // Candlewood Torch

    public bool Burning => ItemID == 0xA12;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        ItemID = ItemID == 0xF6B ? 0xA12 : 0xF6B;

        if (Parent == from && Burning)
        {
            Mobiles.MeerMage.StopEffect(from, true);
        }
    }

    public override void OnAdded(IEntity parent)
    {
        base.OnAdded(parent);

        if (parent is Mobile m && Burning)
        {
            Mobiles.MeerMage.StopEffect(m, true);
        }
    }
}

/// <summary>Neville's reward for seeing him safely to Dugan. Ported from ServUO
/// (Scripts/Items/Equipment/Talismans/TalismanofGoblinSlaying.cs).</summary>
[SerializationGenerator(0, false)]
public partial class TalismanofGoblinSlaying : BaseTalisman
{
    [Constructible]
    public TalismanofGoblinSlaying() : base(0x2F58)
    {
        Slayer = TalismanSlayerName.Goblin;
        MaxChargeTime = 1200;
    }

    public override int LabelNumber => 1095011; // Talisman of Goblin Slaying
    public override bool ForceShowName => true;
}

/// <summary>Jaacar's reward box. Ported from ServUO
/// (Scripts/Items/Containers/JaacarBox.cs) — it carries the Bowl of Rotworm Stew recipe.</summary>
[SerializationGenerator(0, false)]
public partial class JaacarBox : WoodenBox
{
    [Constructible]
    public JaacarBox()
    {
        Movable = true;
        Hue = 1266;

        DropItem(new RecipeScroll(500));
    }

    public override string DefaultName => "Jaacar Reward Box";
}

/// <summary>Xenrr's reward. Ported from ServUO
/// (Scripts/Items/Artifacts/Tools/XenrrFishingPole.cs).
///
/// Wearing it turns you into a goblin — that's the joke, and the reason it refuses to equip
/// while mounted, flying, or already polymorphed.</summary>
[SerializationGenerator(0, false)]
public partial class XenrrFishingPole : FishingPole
{
    [Constructible]
    public XenrrFishingPole()
    {
        LootType = LootType.Blessed;

        Attributes.SpellChanneling = 1;
        Attributes.CastSpeed = -1;
    }

    public override int LabelNumber => 1095066; // Xenrr's fishing pole

    public override bool OnEquip(Mobile from)
    {
        if (!base.OnEquip(from))
        {
            return false;
        }

        if (from.Mounted)
        {
            from.SendLocalizedMessage(1010097); // You cannot use this while mounted.
            return false;
        }

        if (from.IsBodyMod)
        {
            from.SendLocalizedMessage(1111896); // You may only change forms while in your original body.
            return false;
        }

        return true;
    }

    public override void OnAdded(IEntity parent)
    {
        base.OnAdded(parent);

        if (parent is Mobile m)
        {
            m.FixedParticles(0x3728, 1, 13, 5042, EffectLayer.Waist);

            m.BodyMod = 723;
            m.HueMod = 0;
        }
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile m && !Deleted)
        {
            m.BodyMod = 0;
            m.HueMod = -1;
            m.FixedParticles(0x3728, 1, 13, 5042, EffectLayer.Waist);
        }
    }
}

/// <summary>Barreraak's reward. Ported from ServUO
/// (Scripts/Items/Artifacts/Equipment/Jewelry/BarreraakRing.cs) — the same goblin-shape joke as
/// Xenrr's pole, in a ring.</summary>
[SerializationGenerator(0, false)]
public partial class BarreraaksRing : GoldRing
{
    [Constructible]
    public BarreraaksRing() => LootType = LootType.Blessed;

    public override int LabelNumber => 1095049; // Barreraak's Old Beat Up Ring

    public override bool CanEquip(Mobile from)
    {
        if (!base.CanEquip(from))
        {
            return false;
        }

        if (from.Mounted)
        {
            from.SendLocalizedMessage(1010097); // You cannot use this while mounted.
            return false;
        }

        if (from.IsBodyMod)
        {
            from.SendLocalizedMessage(1111896); // You may only change forms while in your original body.
            return false;
        }

        return true;
    }

    public override void OnAdded(IEntity parent)
    {
        base.OnAdded(parent);

        if (parent is Mobile m)
        {
            m.BodyMod = 334;
        }
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile m)
        {
            m.BodyMod = 0;
        }
    }
}

/// <summary>Tobin's reward — a kit that arms a hidden goblin trap wherever you point it. Ported
/// from ServUO (Scripts/Items/Functional/GoblinFloorTrap.cs).</summary>
[SerializationGenerator(0, false)]
public partial class GoblinFloorTrapKit : Item
{
    [Constructible]
    public GoblinFloorTrapKit() : base(16704)
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1054107); // This item must be in your backpack.
            return;
        }

        if (from.Skills.Tinkering.Value < 80)
        {
            from.SendLocalizedMessage(1113318); // You do not have enough skill to set the trap.
            return;
        }

        if (from.Mounted)
        {
            from.SendLocalizedMessage(1113319); // You cannot set the trap while riding or flying.
            return;
        }

        if (from.Region is GuardedRegion gr && !gr.IsDisabled())
        {
            from.SendMessage("You cannot place a trap in a guard region.");
            return;
        }

        from.Target = new InternalTarget(this);
    }

    private class InternalTarget : Target
    {
        private readonly GoblinFloorTrapKit _kit;

        public InternalTarget(GoblinFloorTrapKit kit) : base(-1, true, TargetFlags.None) => _kit = kit;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D point || _kit.Deleted)
            {
                return;
            }

            var p = new Point3D(point);

            if (from.Skills.Tinkering.Value < 80)
            {
                from.SendLocalizedMessage(1113318); // You do not have enough skill to set the trap.
                return;
            }

            if (from.Mounted)
            {
                from.SendLocalizedMessage(1113319); // You cannot set the trap while riding or flying.
                return;
            }

            if (Region.Find(p, from.Map) is GuardedRegion gr2 && !gr2.IsDisabled())
            {
                from.SendMessage("You cannot place a trap in a guard region.");
                return;
            }

            if (!from.InRange(p, 2))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            new GoblinFloorTrap(from).MoveToWorld(p, from.Map);

            from.SendLocalizedMessage(1113294); // You carefully arm the goblin trap.
            from.SendLocalizedMessage(1113297); // You hide the trap to the best of your ability.

            _kit.Consume();
        }
    }
}

/// <summary>The trap the kit arms. Ported from ServUO. Hidden until it goes off, then visible
/// for ten seconds before it settles back down.
///
/// ServUO's version also implements IRevealableItem so Detect Hidden can find it; that
/// interface exists here only inside the VvV engine and isn't wired to detection, so the trap
/// stays hidden until triggered. Wire it up if a general revealable-item system lands.</summary>
[SerializationGenerator(0, false)]
public partial class GoblinFloorTrap : BaseTrap
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [Constructible]
    public GoblinFloorTrap(Mobile from = null) : base(0x4004)
    {
        _owner = from;
        Visible = false;
    }

    public override int LabelNumber => 1113296; // Armed Floor Trap

    public override bool PassivelyTriggered => true;
    public override TimeSpan PassiveTriggerDelay => TimeSpan.FromSeconds(1.0);
    public override int PassiveTriggerRange => 1;
    public override TimeSpan ResetDelay => TimeSpan.FromSeconds(1.0);

    public override void OnTrigger(Mobile from)
    {
        if (from.AccessLevel > AccessLevel.Player || !from.Alive)
        {
            return;
        }

        if (_owner != null)
        {
            if (!_owner.CanBeHarmful(from) || _owner == from)
            {
                return;
            }

            if (_owner.Guild != null && _owner.Guild == from.Guild)
            {
                return;
            }
        }

        from.PlaySound(0x22B);
        from.SendLocalizedMessage(1095157); // You stepped onto a goblin trap!

        Spells.SpellHelper.Damage(
            TimeSpan.FromSeconds(0.30),
            from,
            from,
            Utility.RandomMinMax(50, 75),
            100,
            0,
            0,
            0,
            0
        );

        if (_owner != null)
        {
            from.DoHarmful(_owner);
        }

        Visible = true;
        Timer.DelayCall(TimeSpan.FromSeconds(10), Rehide);

        PublicOverheadMessage(MessageType.Regular, 0x65, 500813); // [Trapped]

        new Blood().MoveToWorld(from.Location, from.Map);
    }

    private void Rehide()
    {
        if (!Deleted)
        {
            Visible = false;
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _owner = null;
    }
}
