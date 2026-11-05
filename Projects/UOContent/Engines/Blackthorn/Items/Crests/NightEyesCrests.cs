using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped Dungeons/BlackthornDungeon/Items/NightEyesBase).
/// ReforgedSuffix dropped: the ReforgedSuffix type does not exist in this codebase. ServUO's GargishEarrings is
/// armor (BaseArmor, art 0x4213); this codebase redefined it as a BaseEarrings jewel with no
/// Base*Resistance/StrRequirement members, so these are re-based on BaseArmor with the ServUO art and chainmail
/// material. GargishGlasses has no counterpart here; re-based on BaseArmor with its ServUO art (0x4644),
/// leather material and gargoyle-only race.</summary>
[SerializationGenerator(0, false)]
public partial class BandanaBearingTheCrestOfBlackthorn1 : Bandana
{
    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public BandanaBearingTheCrestOfBlackthorn1()
    {
        Attributes.NightSight = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 3;
        Hue = 1233;
    }
}

[SerializationGenerator(0, false)]
public partial class BascinetBearingTheCrestOfBlackthorn1 : Bascinet
{
    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public BascinetBearingTheCrestOfBlackthorn1()
    {
        Attributes.NightSight = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 3;
        Hue = 1233;
    }
}

[SerializationGenerator(0, false)]
public partial class CircletBearingTheCrestOfBlackthorn1 : Circlet
{
    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public CircletBearingTheCrestOfBlackthorn1()
    {
        Attributes.NightSight = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 3;
        Hue = 1233;
    }
}

[SerializationGenerator(0, false)]
public partial class FeatheredHatBearingTheCrestOfBlackthorn1 : FeatheredHat
{
    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public FeatheredHatBearingTheCrestOfBlackthorn1()
    {
        Attributes.NightSight = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 3;
        Hue = 1233;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishEarringsBearingTheCrestOfBlackthorn1 : BaseArmor
{
    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Chainmail;

    [Constructible]
    public GargishEarringsBearingTheCrestOfBlackthorn1() : base(0x4213)
    {
        Attributes.NightSight = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 3;
        Hue = 1233;
        Layer = Layer.Earrings;
    }
}

[Flippable(0x4644, 0x4645)]
[SerializationGenerator(0, false)]
public partial class GargishGlassesBearingTheCrestOfBlackthorn1 : BaseArmor
{
    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;

    [Constructible]
    public GargishGlassesBearingTheCrestOfBlackthorn1() : base(0x4644)
    {
        Attributes.NightSight = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 3;
        Hue = 1233;
        Layer = Layer.Earrings;
    }
}

[SerializationGenerator(0, false)]
public partial class JesterHatBearingTheCrestOfBlackthorn1 : JesterHat
{
    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public JesterHatBearingTheCrestOfBlackthorn1()
    {
        Attributes.NightSight = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 3;
        Hue = 1233;
    }
}

[SerializationGenerator(0, false)]
public partial class NorseHelmBearingTheCrestOfBlackthorn1 : NorseHelm
{
    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    [Constructible]
    public NorseHelmBearingTheCrestOfBlackthorn1()
    {
        Attributes.NightSight = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 3;
        Hue = 1233;
    }
}

[SerializationGenerator(0, false)]
public partial class PlateHelmBearingTheCrestOfBlackthorn1 : PlateHelm
{
    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    [Constructible]
    public PlateHelmBearingTheCrestOfBlackthorn1()
    {
        Attributes.NightSight = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 3;
        Hue = 1233;
    }
}

[SerializationGenerator(0, false)]
public partial class RoyalCircletBearingTheCrestOfBlackthorn1 : RoyalCirclet
{
    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public RoyalCircletBearingTheCrestOfBlackthorn1()
    {
        Attributes.NightSight = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 3;
        Hue = 1233;
    }
}

[SerializationGenerator(0, false)]
public partial class SkullcapBearingTheCrestOfBlackthorn1 : SkullCap
{
    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public SkullcapBearingTheCrestOfBlackthorn1()
    {
        Attributes.NightSight = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 3;
        Hue = 1233;
    }
}

[SerializationGenerator(0, false)]
public partial class TricorneHatBearingTheCrestOfBlackthorn1 : TricorneHat
{
    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public TricorneHatBearingTheCrestOfBlackthorn1()
    {
        Attributes.NightSight = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 3;
        Hue = 1233;
    }
}

[SerializationGenerator(0, false)]
public partial class WizardsHatBearingTheCrestOfBlackthorn1 : WizardsHat
{
    public override int BasePhysicalResistance => 10;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public WizardsHatBearingTheCrestOfBlackthorn1()
    {
        Attributes.NightSight = 1;
        Attributes.DefendChance = 10;
        Attributes.CastRecovery = 3;
        Hue = 1233;
    }
}
