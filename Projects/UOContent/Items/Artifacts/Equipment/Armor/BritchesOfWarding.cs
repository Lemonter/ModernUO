using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/BritchesOfWarding.cs).
///     GargishBritchesOfWarding: GargishPlateLegs has no counterpart here; re-based on BaseArmor with its ServUO art/stat line (`0x30E`).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BritchesOfWarding : ChainLegs
{
    public override int LabelNumber => 1157345; // britches of warding

    [Constructible]
    public BritchesOfWarding()
    {
        switch (Utility.Random(6))
        {
        }

        Attributes.BonusStam = 12;
        Attributes.AttackChance = 10;
        Attributes.LowerManaCost = 8;
    }

    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 20;

    public override int BaseColdResistance => 20;

    public override int BasePoisonResistance => 20;

    public override int BaseEnergyResistance => 20;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class GargishBritchesOfWarding : BaseArmor
{
    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override int ArmorBase => 34;

    public override int OldStrReq => 90;

    public override int AosStrReq => 90;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override double DefaultWeight => 7.0;

    public override int LabelNumber => 1157345; // britches of warding

    [Constructible]
    public GargishBritchesOfWarding() : base(0x30E)
    {
        switch (Utility.Random(6))
        {
        }

        Attributes.BonusStam = 12;
        Attributes.AttackChance = 10;
        Attributes.LowerManaCost = 8;
    }

    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 20;

    public override int BaseColdResistance => 20;

    public override int BasePoisonResistance => 20;

    public override int BaseEnergyResistance => 20;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
