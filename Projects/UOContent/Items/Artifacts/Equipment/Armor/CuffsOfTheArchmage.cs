using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/CuffsOfTheArchmage.cs).
///     GargishCuffsOfTheArchmage: GargishStoneArms has no counterpart here; re-based on GargishStoneArmsType1.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CuffsOfTheArchmage : BoneArms
{
    public override int LabelNumber => 1157348; // cuffs of the archmage

    [Constructible]
    public CuffsOfTheArchmage()
    {
        SkillBonuses.SetValues(0, SkillName.MagicResist, 15.0);
        Attributes.BonusMana = 5;
        Attributes.RegenMana = 4;
        Attributes.SpellDamage = 20;
        ArmorAttributes.MageArmor = 1;
    }

    public override int BasePhysicalResistance => 15;

    public override int BaseFireResistance => 15;

    public override int BaseColdResistance => 15;

    public override int BasePoisonResistance => 15;

    public override int BaseEnergyResistance => 15;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class GargishCuffsOfTheArchmage : GargishStoneArmsType1
{
    public override int LabelNumber => 1157348; // cuffs of the archmage

    [Constructible]
    public GargishCuffsOfTheArchmage()
    {
        SkillBonuses.SetValues(0, SkillName.MagicResist, 15.0);
        Attributes.BonusMana = 5;
        Attributes.RegenMana = 4;
        Attributes.SpellDamage = 20;
        ArmorAttributes.MageArmor = 1;
    }

    public override int BasePhysicalResistance => 15;

    public override int BaseFireResistance => 15;

    public override int BaseColdResistance => 15;

    public override int BasePoisonResistance => 15;

    public override int BaseEnergyResistance => 15;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
