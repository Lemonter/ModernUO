using ModernUO.Serialization;
using System;
using System.Linq;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Epiphany/VillainousEpiphany.cs).
///     KiltOfVillainousEpiphany: GargishPlateKilt has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x30C`).
///     GargishBreastplateOfVillainousEpiphany: GargishPlateChest has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x30A`).
///     GargishArmsOfVillainousEpiphany: GargishPlateArms has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x308`).
///     NecklaceOfVillainousEpiphany: GargishNecklace has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x4210`).
///     GargishLegsOfVillainousEpiphany: GargishPlateLegs has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x30E`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class HelmOfVillainousEpiphany : DragonHelm, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Evil;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150253; // Helm of Villainous Epiphany

    [Constructible]
    public HelmOfVillainousEpiphany()
    {
        Resource = CraftResource.None;

        Hue = 1778;
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
public partial class GorgetOfVillainousEpiphany : PlateGorget, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Evil;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150254; // Gorget of Villainous Epiphany

    [Constructible]
    public GorgetOfVillainousEpiphany()
    {
        Hue = 1778;
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
public partial class BreastplateOfVillainousEpiphany : DragonChest, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Evil;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150255; // Breastplate of Villainous Epiphany

    [Constructible]
    public BreastplateOfVillainousEpiphany()
    {
        Resource = CraftResource.None;

        Hue = 1778;
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
public partial class ArmsOfVillainousEpiphany : DragonArms, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Evil;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150256; // Arms of Villainous Epiphany

    [Constructible]
    public ArmsOfVillainousEpiphany()
    {
        Resource = CraftResource.None;

        Hue = 1778;
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
public partial class GauntletsOfVillainousEpiphany : DragonGloves, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Evil;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150257; // Gauntlets of Villainous Epiphany

    [Constructible]
    public GauntletsOfVillainousEpiphany()
    {
        Resource = CraftResource.None;

        Hue = 1778;
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
public partial class LegsOfVillainousEpiphany : DragonLegs, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Evil;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150258; // Leggings of Villainous Epiphany

    [Constructible]
    public LegsOfVillainousEpiphany()
    {
        Resource = CraftResource.None;

        Hue = 1778;
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
public partial class KiltOfVillainousEpiphany : BaseArmor, IEpiphanyArmor
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

    public Alignment Alignment => Alignment.Evil;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150263; // Kilt of Villainous Epiphany

    [Constructible]
    public KiltOfVillainousEpiphany() : base(0x30C)
    {
        Hue = 1778;
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
public partial class EarringsOfVillainousEpiphany : GargishEarrings, IEpiphanyArmor
{
    public Alignment Alignment => Alignment.Evil;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150260; // Earrings of Villainous Epiphany

    [Constructible]
    public EarringsOfVillainousEpiphany()
    {
        Hue = 1778;
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
public partial class GargishBreastplateOfVillainousEpiphany : BaseArmor, IEpiphanyArmor
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

    public Alignment Alignment => Alignment.Evil;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150255; // Breastplate of Villainous Epiphany

    [Constructible]
    public GargishBreastplateOfVillainousEpiphany() : base(0x30A)
    {
        Hue = 1778;
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
public partial class GargishArmsOfVillainousEpiphany : BaseArmor, IEpiphanyArmor
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

    public Alignment Alignment => Alignment.Evil;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150256; // Arms of Villainous Epiphany

    [Constructible]
    public GargishArmsOfVillainousEpiphany() : base(0x308)
    {
        Hue = 1778;
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
public partial class NecklaceOfVillainousEpiphany : BaseArmor, IEpiphanyArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 11;

    public override int InitMaxHits => 40;

    public override int InitMinHits => 30;

    public override int BasePhysicalResistance => 1;

    public override Layer Layer => Layer.Neck;

    public override double DefaultWeight => 1.0;

    public Alignment Alignment => Alignment.Evil;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150264; // Necklace of Villainous Epiphany

    [Constructible]
    public NecklaceOfVillainousEpiphany() : base(0x4210)
    {
        Hue = 1778;
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
public partial class GargishLegsOfVillainousEpiphany : BaseArmor, IEpiphanyArmor
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

    public Alignment Alignment => Alignment.Evil;

    public SurgeType Type => SurgeType.Mana;

    public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);

    public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

    public override int LabelNumber => 1150258; // Legs of Villainous Epiphany

    [Constructible]
    public GargishLegsOfVillainousEpiphany() : base(0x30E)
    {
        Hue = 1778;
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
