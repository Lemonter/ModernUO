using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped Dungeons/BlackthornDungeon/Items/FoldedSteelBase).
/// ReforgedSuffix dropped: the ReforgedSuffix type does not exist in this codebase. ServUO's GargishEarrings is
/// armor (BaseArmor, art 0x4213); this codebase redefined it as a BaseEarrings jewel with no
/// Base*Resistance/StrRequirement members, so these are re-based on BaseArmor with the ServUO art and chainmail
/// material. GargishGlasses has no counterpart here; re-based on BaseArmor with its ServUO art (0x4644),
/// leather material and gargoyle-only race.</summary>
[SerializationGenerator(0, false)]
public partial class BandanaBearingTheCrestOfBlackthorn3 : Bandana
{
    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public BandanaBearingTheCrestOfBlackthorn3()
    {
        Attributes.NightSight = 1;
        Attributes.BonusStr = 8;
        Attributes.DefendChance = 15;
        StrRequirement = 45;
        Hue = 1150;
    }
}

[SerializationGenerator(0, false)]
public partial class BascinetBearingTheCrestOfBlackthorn3 : Bascinet
{
    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public BascinetBearingTheCrestOfBlackthorn3()
    {
        Attributes.NightSight = 1;
        Attributes.BonusStr = 8;
        Attributes.DefendChance = 15;
        StrRequirement = 45;
        Hue = 1150;
    }
}

[SerializationGenerator(0, false)]
public partial class CircletBearingTheCrestOfBlackthorn3 : Circlet
{
    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public CircletBearingTheCrestOfBlackthorn3()
    {
        Attributes.NightSight = 1;
        Attributes.BonusStr = 8;
        Attributes.DefendChance = 15;
        StrRequirement = 45;
        Hue = 1150;
    }
}

[SerializationGenerator(0, false)]
public partial class FeatheredHatBearingTheCrestOfBlackthorn3 : FeatheredHat
{
    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public FeatheredHatBearingTheCrestOfBlackthorn3()
    {
        Attributes.NightSight = 1;
        Attributes.BonusStr = 8;
        Attributes.DefendChance = 15;
        StrRequirement = 45;
        Hue = 1150;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishEarringsBearingTheCrestOfBlackthorn3 : BaseArmor
{
    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Chainmail;

    [Constructible]
    public GargishEarringsBearingTheCrestOfBlackthorn3() : base(0x4213)
    {
        Attributes.NightSight = 1;
        Attributes.BonusStr = 8;
        Attributes.DefendChance = 15;
        StrRequirement = 45;
        Hue = 1150;
        Layer = Layer.Earrings;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishGlassesBearingTheCrestOfBlackthorn3 : BaseArmor
{
    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;

    [Constructible]
    public GargishGlassesBearingTheCrestOfBlackthorn3() : base(0x4644)
    {
        Attributes.NightSight = 1;
        Attributes.BonusStr = 8;
        Attributes.DefendChance = 15;
        StrRequirement = 45;
        Hue = 1150;
        Layer = Layer.Earrings;
    }
}

[SerializationGenerator(0, false)]
public partial class JesterHatBearingTheCrestOfBlackthorn3 : JesterHat
{
    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public JesterHatBearingTheCrestOfBlackthorn3()
    {
        Attributes.NightSight = 1;
        Attributes.BonusStr = 8;
        Attributes.DefendChance = 15;
        StrRequirement = 45;
        Hue = 1150;
    }
}

[SerializationGenerator(0, false)]
public partial class NorseHelmBearingTheCrestOfBlackthorn3 : NorseHelm
{
    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public NorseHelmBearingTheCrestOfBlackthorn3()
    {
        Attributes.NightSight = 1;
        Attributes.BonusStr = 8;
        Attributes.DefendChance = 15;
        StrRequirement = 45;
        Hue = 1150;
    }
}

[SerializationGenerator(0, false)]
public partial class PlateHelmBearingTheCrestOfBlackthorn3 : PlateHelm
{
    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public PlateHelmBearingTheCrestOfBlackthorn3()
    {
        Attributes.NightSight = 1;
        Attributes.BonusStr = 8;
        Attributes.DefendChance = 15;
        StrRequirement = 45;
        Hue = 1150;
    }
}

[SerializationGenerator(0, false)]
public partial class RoyalCircletBearingTheCrestOfBlackthorn3 : RoyalCirclet
{
    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public RoyalCircletBearingTheCrestOfBlackthorn3()
    {
        Attributes.NightSight = 1;
        Attributes.BonusStr = 8;
        Attributes.DefendChance = 15;
        StrRequirement = 45;
        Hue = 1150;
    }
}

[SerializationGenerator(0, false)]
public partial class SkullcapBearingTheCrestOfBlackthorn3 : SkullCap
{
    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public SkullcapBearingTheCrestOfBlackthorn3()
    {
        Attributes.NightSight = 1;
        Attributes.BonusStr = 8;
        Attributes.DefendChance = 15;
        StrRequirement = 45;
        Hue = 1150;
    }
}

[SerializationGenerator(0, false)]
public partial class TricorneHatBearingTheCrestOfBlackthorn3 : TricorneHat
{
    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public TricorneHatBearingTheCrestOfBlackthorn3()
    {
        Attributes.NightSight = 1;
        Attributes.BonusStr = 8;
        Attributes.DefendChance = 15;
        StrRequirement = 45;
        Hue = 1150;
    }
}

[SerializationGenerator(0, false)]
public partial class WizardsHatBearingTheCrestOfBlackthorn3 : WizardsHat
{
    public override int BasePhysicalResistance => 20;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public WizardsHatBearingTheCrestOfBlackthorn3()
    {
        Attributes.NightSight = 1;
        Attributes.BonusStr = 8;
        Attributes.DefendChance = 15;
        StrRequirement = 45;
        Hue = 1150;
    }
}
