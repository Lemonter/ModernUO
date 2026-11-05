using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Suits/BaseCostume.cs).
/// </summary>
[Flippable(0x19BC, 0x19BD)]
[SerializationGenerator(0, false)]
public partial class BaseCostume : BaseShield
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _transformed;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _costumeBody;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _costumeHue = -1;

    public virtual string CreatureName { get; private set; }

    public BaseCostume()
        : base(0x19BC)
    {
        Resource = CraftResource.None;
        Attributes.SpellChanneling = 1;
        Layer = Layer.FirstValid;
        Weight = 4.0;
        StrRequirement = 10;
    }

    private bool EnMask(Mobile from)
    {
        if (from.Mounted || from.Flying) // You cannot use this while mounted or flying.
        {
            from.SendLocalizedMessage(1010097);
        }
        else if (from.IsBodyMod || from.HueMod > -1)
        {
            from.SendLocalizedMessage(1158010); // You cannot use that item in this form.
        }
        else
        {
            from.BodyMod = _costumeBody;
            from.HueMod = _costumeHue;
            Transformed = true;

            return true;
        }

        return false;
    }

    private void DeMask(Mobile from)
    {
        from.BodyMod = 0;
        from.HueMod = -1;
        Transformed = false;
    }

    public virtual bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
            return false;

        else if (RootParent is Mobile && from != RootParent)
            return false;

        Hue = sender.DyedHue;
        return true;
    }

    public override bool OnEquip(Mobile from)
    {
        if (!Transformed)
        {
            if (EnMask(from))
                return true;

            return false;
        }

        return base.OnEquip(from);
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile && Transformed)
        {
            DeMask((Mobile)parent);
        }

        base.OnRemoved(parent);
    }

    public static void OnDamaged(Mobile m)
    {
        BaseCostume costume = m.FindItemOnLayer(Layer.FirstValid) as BaseCostume;

        if (costume != null)
        {
            m.AddToBackpack(costume);
        }
    }
}
