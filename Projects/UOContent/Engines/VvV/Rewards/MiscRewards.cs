using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Items/Rewards/*.cs) —
// smaller reward items grouped into one file. `VvVEpaulette`/`VvVGargishEpaulette`/
// `VvVGargishPlateArms`/`VvVGargishStoneChest` aren't ported — their base classes
// (`Epaulette`, `GargishEpaulette`, `GargishPlateArms`, `GargishStoneChest`) don't exist in
// this codebase. `ConfirmCallbackGump` (RunUO-era) doesn't exist either — ForgedRoyalPardon
// uses the real `WarningGump` here instead.
[SerializationGenerator(0, false)]
public partial class VvVRobe : BaseOuterTorso
{
    public override int LabelNumber =>
        Hue == ViceVsVirtueSystem.VirtueHue ? 1155532 : Hue == ViceVsVirtueSystem.ViceHue ? 1155533 : base.LabelNumber;

    [Constructible]
    public VvVRobe() : this(0)
    {
    }

    [Constructible]
    public VvVRobe(int hue) : base(0x2684, hue)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class VvVHairDye : Item
{
    public override int LabelNumber => Hue == ViceVsVirtueSystem.VirtueHue ? 1155538 : 1155539;

    [Constructible]
    public VvVHairDye() : this(0)
    {
    }

    [Constructible]
    public VvVHairDye(int hue) : base(3838) => Hue = hue;

    public override void OnDoubleClick(Mobile m)
    {
        if (!IsChildOf(m.Backpack))
        {
            return;
        }

        if (!ViceVsVirtueSystem.IsVvV(m))
        {
            m.SendLocalizedMessage(1155496); // This item can only be used by VvV participants!
            return;
        }

        m.HairHue = Hue;
        m.FacialHairHue = Hue;

        Delete();
        m.PlaySound(0x4E);
        m.SendLocalizedMessage(501199); // You dye your hair
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1154937); // vvv item
    }
}

[SerializationGenerator(0, false)]
public partial class VvVStuddedChest : StuddedChest
{
    public override int BasePhysicalResistance => 17;
    public override int BaseFireResistance => 19;
    public override int BaseColdResistance => 18;
    public override int BasePoisonResistance => 3;
    public override int BaseEnergyResistance => 6;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public VvVStuddedChest()
    {
        Attributes.BonusStr = 3;
        Attributes.BonusStam = 10;
        Attributes.RegenStam = 3;
    }
}

[SerializationGenerator(0, false)]
public partial class VvVWizardsHat : WizardsHat
{
    public override int BasePhysicalResistance => 6;
    public override int BaseFireResistance => 6;
    public override int BaseColdResistance => 6;
    public override int BasePoisonResistance => 6;
    public override int BaseEnergyResistance => 25;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public VvVWizardsHat()
    {
        Attributes.BonusHits = 5;
        Attributes.RegenMana = 3;
        Attributes.DefendChance = 4;
        Attributes.SpellDamage = 10;
        Attributes.LowerRegCost = 20;
    }
}

[SerializationGenerator(0, false)]
public partial class VvVGargishEarrings : GargishEarrings
{
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public VvVGargishEarrings()
    {
        Attributes.BonusHits = 5;
        Attributes.RegenMana = 3;
        Attributes.DefendChance = 4;
        Attributes.SpellDamage = 10;
        Attributes.LowerRegCost = 20;
    }
}

[SerializationGenerator(0, false)]
public partial class VvVWoodlandArms : WoodlandArms
{
    public override int BasePhysicalResistance => 15;
    public override int BaseFireResistance => 6;
    public override int BaseColdResistance => 17;
    public override int BasePoisonResistance => 18;
    public override int BaseEnergyResistance => 18;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public VvVWoodlandArms()
    {
        Attributes.BonusDex = 4;
        Attributes.BonusHits = 5;
        Attributes.BonusStam = 10;
        Attributes.RegenStam = 3;
    }
}

[SerializationGenerator(0, false)]
public partial class VvVDragonArms : DragonArms
{
    public override int BasePhysicalResistance => 15;
    public override int BaseFireResistance => 6;
    public override int BaseColdResistance => 17;
    public override int BasePoisonResistance => 18;
    public override int BaseEnergyResistance => 18;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public VvVDragonArms()
    {
        Attributes.BonusDex = 4;
        Attributes.BonusHits = 5;
        Attributes.BonusStam = 10;
        Attributes.RegenStam = 3;
    }
}

[SerializationGenerator(0, false)]
public partial class VvVPlateArms : PlateArms
{
    public override int BasePhysicalResistance => 15;
    public override int BaseFireResistance => 6;
    public override int BaseColdResistance => 17;
    public override int BasePoisonResistance => 18;
    public override int BaseEnergyResistance => 18;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public VvVPlateArms()
    {
        Resource = CraftResource.None;

        Attributes.BonusDex = 4;
        Attributes.BonusHits = 5;
        Attributes.BonusStam = 10;
        Attributes.RegenStam = 3;
    }
}

[SerializationGenerator(0, false)]
public partial class EssenceOfCourage : Item
{
    public override int LabelNumber => 1155554; // Essence of Courage

    [Constructible]
    public EssenceOfCourage() : base(3838) => Hue = 2718;

    public override void OnDoubleClick(Mobile m)
    {
        if (IsChildOf(m.Backpack))
        {
            m.SendLocalizedMessage(1155555); // Feed this to VvV War Steeds to maintain their battle readiness!
        }
    }
}

[SerializationGenerator(0, false)]
public partial class ForgedRoyalPardon : Item
{
    public override int LabelNumber => 1155524; // Forged Royal Pardon

    [Constructible]
    public ForgedRoyalPardon() : base(18098) => Hue = 0x21;

    public override void OnDoubleClick(Mobile m)
    {
        if (!IsChildOf(m.Backpack))
        {
            m.SendLocalizedMessage(1042004); // That must be in your pack for you to use it.
            return;
        }

        if (m is not PlayerMobile || !ViceVsVirtueSystem.IsVvV(m))
        {
            m.SendLocalizedMessage(1155496); // This item can only be used by VvV participants!
            return;
        }

        if (m.Kills <= 0)
        {
            m.SendMessage("Тебе эта вещь ни к чему.");
        }
        else if (Spells.SpellHelper.CheckCombat(m))
        {
            m.SendLocalizedMessage(1116588); // You cannot use a forged pardon while in combat.
        }
        else
        {
            m.SendGump(
                new WarningGump(
                    1155525,
                    400,
                    250,
                    okay =>
                    {
                        if (!okay)
                        {
                            return;
                        }

                        m.Kills = 0;
                        m.Delta(MobileDelta.Noto);

                        m.SendMessage("Все убийства тебе прощены.");
                        Delete();
                    }
                )
            );
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1154937); // vvv item
    }
}

[SerializationGenerator(0, false)]
public partial class MorphEarrings : GoldEarrings
{
    public override int LabelNumber => 1094746; // Morph Earrings

    [Constructible]
    public MorphEarrings()
    {
    }

    // ServUO drops any now-race-incompatible equipment on removal (e.g. gargoyle-only gear
    // when reverting to human) via `Race.ValidateEquipment(Item)`, which doesn't exist in this
    // codebase's Race class — dropped; players keep wearing whatever they had on.
}

[SerializationGenerator(0, false)]
public partial class VvVWand1 : BaseWand
{
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
    public override int LabelNumber => 1023570; // Wand

    [Constructible]
    public VvVWand1() : base(WandEffect.Clumsiness, 0, 0)
    {
        ItemID = 3571;

        Attributes.SpellChanneling = 1;
        WeaponAttributes.UseBestSkill = 1;
    }
}

[SerializationGenerator(0, false)]
public partial class VvVWand2 : BaseWand
{
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
    public override int LabelNumber => 1023570; // Wand

    [Constructible]
    public VvVWand2() : base(WandEffect.Clumsiness, 0, 0)
    {
        ItemID = 3571;

        Attributes.SpellChanneling = 1;
        WeaponAttributes.MageWeapon = 15;
    }
}
