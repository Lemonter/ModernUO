using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped Dungeons/BlackthornDungeon/Items/MaceAndShieldBase).
/// ReforgedSuffix dropped: the ReforgedSuffix type does not exist in this codebase.
/// WeaponAttributes.HitLowerDefend is dropped: BaseArmor/BaseClothing here expose no AosWeaponAttributes (only
/// BaseWeapon and ElvenGlasses carry one). ServUO's GargishEarrings is armor (BaseArmor, art 0x4213); this
/// codebase redefined it as a BaseEarrings jewel with no Base*Resistance/StrRequirement members, so these are
/// re-based on BaseArmor with the ServUO art and chainmail material. GargishGlasses has no counterpart here;
/// re-based on BaseArmor with its ServUO art (0x4644), leather material and gargoyle-only race.</summary>
[SerializationGenerator(0, false)]
public partial class BandanaBearingTheCrestOfBlackthorn2 : Bandana
{
    public override int BasePhysicalResistance => 25;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public BandanaBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
        StrRequirement = 45;
        Hue = 66;
    }
}

[SerializationGenerator(0, false)]
public partial class BascinetBearingTheCrestOfBlackthorn2 : Bascinet
{
    public override int BasePhysicalResistance => 25;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public BascinetBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
        StrRequirement = 45;
        Hue = 66;
    }
}

[SerializationGenerator(0, false)]
public partial class CircletBearingTheCrestOfBlackthorn2 : Circlet
{
    public override int BasePhysicalResistance => 25;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public CircletBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
        StrRequirement = 45;
        Hue = 66;
    }
}

[SerializationGenerator(0, false)]
public partial class FeatheredHatBearingTheCrestOfBlackthorn2 : FeatheredHat
{
    public override int BasePhysicalResistance => 25;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public FeatheredHatBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
        StrRequirement = 45;
        Hue = 66;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishEarringsBearingTheCrestOfBlackthorn2 : BaseArmor
{
    public override int BasePhysicalResistance => 25;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Chainmail;

    [Constructible]
    public GargishEarringsBearingTheCrestOfBlackthorn2() : base(0x4213)
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
        StrRequirement = 45;
        Hue = 66;
        Layer = Layer.Earrings;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishGlassesBearingTheCrestOfBlackthorn2 : BaseArmor
{
    public override int BasePhysicalResistance => 25;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;

    [Constructible]
    public GargishGlassesBearingTheCrestOfBlackthorn2() : base(0x4644)
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
        StrRequirement = 45;
        Hue = 66;
        Layer = Layer.Earrings;
    }
}

[SerializationGenerator(0, false)]
public partial class JesterHatBearingTheCrestOfBlackthorn2 : JesterHat
{
    public override int BasePhysicalResistance => 25;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public JesterHatBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
        StrRequirement = 45;
        Hue = 66;
    }
}

[SerializationGenerator(0, false)]
public partial class NorseHelmBearingTheCrestOfBlackthorn2 : NorseHelm
{
    public override int BasePhysicalResistance => 25;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public NorseHelmBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
        StrRequirement = 45;
        Hue = 66;
    }
}

[SerializationGenerator(0, false)]
public partial class PlateHelmBearingTheCrestOfBlackthorn2 : PlateHelm
{
    public override int BasePhysicalResistance => 25;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public PlateHelmBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
        StrRequirement = 45;
        Hue = 66;
    }
}

[SerializationGenerator(0, false)]
public partial class RoyalCircletBearingTheCrestOfBlackthorn2 : RoyalCirclet
{
    public override int BasePhysicalResistance => 25;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public RoyalCircletBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
        StrRequirement = 45;
        Hue = 66;
    }
}

[SerializationGenerator(0, false)]
public partial class SkullcapBearingTheCrestOfBlackthorn2 : SkullCap
{
    public override int BasePhysicalResistance => 25;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public SkullcapBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
        StrRequirement = 45;
        Hue = 66;
    }
}

[SerializationGenerator(0, false)]
public partial class TricorneHatBearingTheCrestOfBlackthorn2 : TricorneHat
{
    public override int BasePhysicalResistance => 25;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public TricorneHatBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
        StrRequirement = 45;
        Hue = 66;
    }
}

[SerializationGenerator(0, false)]
public partial class WizardsHatBearingTheCrestOfBlackthorn2 : WizardsHat
{
    public override int BasePhysicalResistance => 25;

    public override int BaseFireResistance => 10;

    public override int BaseColdResistance => 10;

    public override int BasePoisonResistance => 10;

    public override int BaseEnergyResistance => 10;

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public WizardsHatBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusStr = 10;
        Attributes.BonusDex = 5;
        StrRequirement = 45;
        Hue = 66;
    }
}
