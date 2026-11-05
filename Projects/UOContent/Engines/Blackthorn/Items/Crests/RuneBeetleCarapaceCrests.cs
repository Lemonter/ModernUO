using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped
/// Dungeons/BlackthornDungeon/Items/RuneBeetleCarapaceBase). ReforgedSuffix dropped: the ReforgedSuffix type
/// does not exist in this codebase. GargishPlateChest has no counterpart here (only the
/// stone/leather/studded/cloth gargish chests exist); re-based on BaseArmor with its ServUO art (0x30A) and
/// plate material. GargishStoneChest survived the Type1/Type2 split as GargishStoneChestType1
/// ([TypeAlias("Server.Items.GargishStoneChest")]).</summary>
[SerializationGenerator(0, false)]
public partial class ChainmailTunicBearingTheCrestOfBlackthorn : ChainChest
{
    public override int BasePhysicalResistance => 5;

    public override int BaseFireResistance => 4;

    public override int BaseColdResistance => 14;

    public override int BasePoisonResistance => 3;

    public override int BaseEnergyResistance => 14;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public ChainmailTunicBearingTheCrestOfBlackthorn()
    {
        Hue = 1773;
        Attributes.BonusMana = 10;
        Attributes.RegenMana = 3;
        Attributes.LowerManaCost = 15;
        ArmorAttributes.LowerStatReq = 100;
        ArmorAttributes.MageArmor = 1;
    }
}

[SerializationGenerator(0, false)]
public partial class DragonBreastplateBearingTheCrestOfBlackthorn : DragonChest
{
    public override int BasePhysicalResistance => 5;

    public override int BaseFireResistance => 4;

    public override int BaseColdResistance => 14;

    public override int BasePoisonResistance => 3;

    public override int BaseEnergyResistance => 14;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public DragonBreastplateBearingTheCrestOfBlackthorn()
    {
        Hue = 1773;
        Attributes.BonusMana = 10;
        Attributes.RegenMana = 3;
        Attributes.LowerManaCost = 15;
        ArmorAttributes.LowerStatReq = 100;
        ArmorAttributes.MageArmor = 1;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishPlatemailChestBearingTheCrestOfBlackthorn : BaseArmor
{
    public override int BasePhysicalResistance => 5;

    public override int BaseFireResistance => 4;

    public override int BaseColdResistance => 14;

    public override int BasePoisonResistance => 3;

    public override int BaseEnergyResistance => 14;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    [Constructible]
    public GargishPlatemailChestBearingTheCrestOfBlackthorn() : base(0x30A)
    {
        Hue = 1773;
        Attributes.BonusMana = 10;
        Attributes.RegenMana = 3;
        Attributes.LowerManaCost = 15;
        ArmorAttributes.LowerStatReq = 100;
        ArmorAttributes.MageArmor = 1;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishStoneChestBearingTheCrestOfBlackthorn : GargishStoneChestType1
{
    public override int BasePhysicalResistance => 5;

    public override int BaseFireResistance => 4;

    public override int BaseColdResistance => 14;

    public override int BasePoisonResistance => 3;

    public override int BaseEnergyResistance => 14;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public GargishStoneChestBearingTheCrestOfBlackthorn()
    {
        Hue = 1773;
        Attributes.BonusMana = 10;
        Attributes.RegenMana = 3;
        Attributes.LowerManaCost = 15;
        ArmorAttributes.LowerStatReq = 100;
        ArmorAttributes.MageArmor = 1;
    }
}

[SerializationGenerator(0, false)]
public partial class PlatemailTunicBearingTheCrestOfBlackthorn : PlateChest
{
    public override int BasePhysicalResistance => 5;

    public override int BaseFireResistance => 4;

    public override int BaseColdResistance => 14;

    public override int BasePoisonResistance => 3;

    public override int BaseEnergyResistance => 14;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public PlatemailTunicBearingTheCrestOfBlackthorn()
    {
        Hue = 1773;
        Attributes.BonusMana = 10;
        Attributes.RegenMana = 3;
        Attributes.LowerManaCost = 15;
        ArmorAttributes.LowerStatReq = 100;
        ArmorAttributes.MageArmor = 1;
    }
}

[SerializationGenerator(0, false)]
public partial class RingmailTunicBearingTheCrestOfBlackthorn : RingmailChest
{
    public override int BasePhysicalResistance => 5;

    public override int BaseFireResistance => 4;

    public override int BaseColdResistance => 14;

    public override int BasePoisonResistance => 3;

    public override int BaseEnergyResistance => 14;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public RingmailTunicBearingTheCrestOfBlackthorn()
    {
        Hue = 1773;
        Attributes.BonusMana = 10;
        Attributes.RegenMana = 3;
        Attributes.LowerManaCost = 15;
        ArmorAttributes.LowerStatReq = 100;
        ArmorAttributes.MageArmor = 1;
    }
}
