using ModernUO.Serialization;
using System;
using System.Linq;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Epiphany/VirtuousEpiphany.cs).
///     KiltOfVirtuousEpiphany: GargishPlateKilt has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x30C`).
///     GargishBreastplateOfVirtuousEpiphany: GargishPlateChest has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x30A`).
///     GargishArmsOfVirtuousEpiphany: GargishPlateArms has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x308`).
///     NecklaceOfVirtuousEpiphany: GargishNecklace has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x4210`).
///     GargishLegsOfVirtuousEpiphany: GargishPlateLegs has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x30E`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class HelmOfVirtuousEpiphany : PlateHelm, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Good;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150233; // Helm of Virtuous Epiphany

    [Constructible]
    public HelmOfVirtuousEpiphany()
    {
        Hue = 2076;
        ArmorAttributes.MageArmor = 1;
    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        EpiphanyHelper.AddProperties(this, list);
    }

    public override bool OnEquip(Mobile from)
    {
        bool canEquip = base.OnEquip(from);

        if (canEquip)
        {
            foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }

        return canEquip;
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile)
        {
            var m = (Mobile)parent;

            foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class GorgetOfVirtuousEpiphany : PlateGorget, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Good;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150234; // Gorget of Virtuous Epiphany

    [Constructible]
    public GorgetOfVirtuousEpiphany()
    {
        Hue = 2076;
        ArmorAttributes.MageArmor = 1;
    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        EpiphanyHelper.AddProperties(this, list);
    }

    public override bool OnEquip(Mobile from)
    {
        bool canEquip = base.OnEquip(from);

        if (canEquip)
        {
            foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }

        return canEquip;
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile)
        {
            var m = (Mobile)parent;

            foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class BreastplateOfVirtuousEpiphany : PlateChest, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Good;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150235; // Breastplate of Virtuous Epiphany

    [Constructible]
    public BreastplateOfVirtuousEpiphany()
    {
        Hue = 2076;
        ArmorAttributes.MageArmor = 1;
    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        EpiphanyHelper.AddProperties(this, list);
    }

    public override bool OnEquip(Mobile from)
    {
        bool canEquip = base.OnEquip(from);

        if (canEquip)
        {
            foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }

        return canEquip;
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile)
        {
            var m = (Mobile)parent;

            foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class ArmsOfVirtuousEpiphany : PlateArms, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Good;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150236; // Arms of Virtuous Epiphany

    [Constructible]
    public ArmsOfVirtuousEpiphany()
    {
        Hue = 2076;
        ArmorAttributes.MageArmor = 1;
    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        EpiphanyHelper.AddProperties(this, list);
    }

    public override bool OnEquip(Mobile from)
    {
        bool canEquip = base.OnEquip(from);

        if (canEquip)
        {
            foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }

        return canEquip;
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile)
        {
            var m = (Mobile)parent;

            foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class GauntletsOfVirtuousEpiphany : PlateGloves, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Good;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150237; // Gauntlets of Virtuous Epiphany

    [Constructible]
    public GauntletsOfVirtuousEpiphany()
    {
        Hue = 2076;
        ArmorAttributes.MageArmor = 1;
    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        EpiphanyHelper.AddProperties(this, list);
    }

    public override bool OnEquip(Mobile from)
    {
        bool canEquip = base.OnEquip(from);

        if (canEquip)
        {
            foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }

        return canEquip;
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile)
        {
            var m = (Mobile)parent;

            foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class LegsOfVirtuousEpiphany : PlateLegs, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Good;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150238; // Leggings of Virtuous Epiphany

    [Constructible]
    public LegsOfVirtuousEpiphany()
    {
        Hue = 2076;
        ArmorAttributes.MageArmor = 1;
    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        EpiphanyHelper.AddProperties(this, list);
    }

    public override bool OnEquip(Mobile from)
    {
        bool canEquip = base.OnEquip(from);

        if (canEquip)
        {
            foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }

        return canEquip;
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile)
        {
            var m = (Mobile)parent;

            foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class KiltOfVirtuousEpiphany : BaseArmor, IEpiphanyArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 32;

    public override int OldStrReq => 80;

    public override int AosStrReq => 80;

    public override int InitMaxHits => 65;

    public override int InitMinHits => 50;

    public override int BaseEnergyResistance => 5;

    public override int BasePoisonResistance => 6;

    public override int BaseColdResistance => 5;

    public override int BaseFireResistance => 6;

    public override int BasePhysicalResistance => 8;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override double DefaultWeight => 5.0;

    public Alignment Alignment => Alignment.Good;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150262; // Kilt of Virtuous Epiphany

    [Constructible]
    public KiltOfVirtuousEpiphany() : base(0x30C)
    {
        Hue = 2076;
        ArmorAttributes.MageArmor = 1;
    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        EpiphanyHelper.AddProperties(this, list);
    }

    public override bool OnEquip(Mobile from)
    {
        bool canEquip = base.OnEquip(from);

        if (canEquip)
        {
            foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }

        return canEquip;
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile)
        {
            var m = (Mobile)parent;

            foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class EarringsOfVirtuousEpiphany : GargishEarrings, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Good;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150262; // Earrings of Virtuous Epiphany

    [Constructible]
    public EarringsOfVirtuousEpiphany()
    {
        Hue = 2076;
    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        EpiphanyHelper.AddProperties(this, list);
    }

    public override bool OnEquip(Mobile from)
    {
        bool canEquip = base.OnEquip(from);

        if (canEquip)
        {
            foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }

        return canEquip;
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile)
        {
            var m = (Mobile)parent;

            foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class GargishBreastplateOfVirtuousEpiphany : BaseArmor, IEpiphanyArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 36;

    public override int OldStrReq => 95;

    public override int AosStrReq => 95;

    public override int InitMaxHits => 65;

    public override int InitMinHits => 50;

    public override int BaseEnergyResistance => 5;

    public override int BasePoisonResistance => 6;

    public override int BaseColdResistance => 5;

    public override int BaseFireResistance => 6;

    public override int BasePhysicalResistance => 8;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override double DefaultWeight => 10.0;

    public Alignment Alignment => Alignment.Good;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150235; // Breastplate of Virtuous Epiphany

    [Constructible]
    public GargishBreastplateOfVirtuousEpiphany() : base(0x30A)
    {
        Hue = 2076;
    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        EpiphanyHelper.AddProperties(this, list);
    }

    public override bool OnEquip(Mobile from)
    {
        bool canEquip = base.OnEquip(from);

        if (canEquip)
        {
            foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }

        return canEquip;
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile)
        {
            var m = (Mobile)parent;

            foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class GargishArmsOfVirtuousEpiphany : BaseArmor, IEpiphanyArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 32;

    public override int OldStrReq => 80;

    public override int AosStrReq => 80;

    public override int InitMaxHits => 65;

    public override int InitMinHits => 50;

    public override int BaseEnergyResistance => 5;

    public override int BasePoisonResistance => 6;

    public override int BaseColdResistance => 5;

    public override int BaseFireResistance => 6;

    public override int BasePhysicalResistance => 8;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override double DefaultWeight => 5.0;

    public Alignment Alignment => Alignment.Good;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150236; // Arms of Virtuous Epiphany

    [Constructible]
    public GargishArmsOfVirtuousEpiphany() : base(0x308)
    {
        Hue = 2076;
    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        EpiphanyHelper.AddProperties(this, list);
    }

    public override bool OnEquip(Mobile from)
    {
        bool canEquip = base.OnEquip(from);

        if (canEquip)
        {
            foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }

        return canEquip;
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile)
        {
            var m = (Mobile)parent;

            foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class NecklaceOfVirtuousEpiphany : BaseArmor, IEpiphanyArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 11;

    public override int InitMaxHits => 40;

    public override int InitMinHits => 30;

    public override int BasePhysicalResistance => 1;

    public override Layer Layer => Layer.Neck;

    public override double DefaultWeight => 1.0;

    public Alignment Alignment => Alignment.Good;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150261; // Necklace of Virtuous Epiphany

    [Constructible]
    public NecklaceOfVirtuousEpiphany() : base(0x4210)
    {
        Hue = 2076;
    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        EpiphanyHelper.AddProperties(this, list);
    }

    public override bool OnEquip(Mobile from)
    {
        bool canEquip = base.OnEquip(from);

        if (canEquip)
        {
            foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }

        return canEquip;
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile)
        {
            var m = (Mobile)parent;

            foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class GargishLegsOfVirtuousEpiphany : BaseArmor, IEpiphanyArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 34;

    public override int OldStrReq => 90;

    public override int AosStrReq => 90;

    public override int InitMaxHits => 65;

    public override int InitMinHits => 50;

    public override int BaseEnergyResistance => 5;

    public override int BasePoisonResistance => 6;

    public override int BaseColdResistance => 5;

    public override int BaseFireResistance => 6;

    public override int BasePhysicalResistance => 8;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override double DefaultWeight => 7.0;

    public Alignment Alignment => Alignment.Good;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150238; // Legs of Virtuous Epiphany

    [Constructible]
    public GargishLegsOfVirtuousEpiphany() : base(0x30E)
    {
        Hue = 2076;
    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        EpiphanyHelper.AddProperties(this, list);
    }

    public override bool OnEquip(Mobile from)
    {
        bool canEquip = base.OnEquip(from);

        if (canEquip)
        {
            foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }

        return canEquip;
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is Mobile)
        {
            var m = (Mobile)parent;

            foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor))
            {
                armor.InvalidateProperties();
            }
        }
    }
}
