using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped
/// Dungeons/BlackthornDungeon/Items/CrimsonCinctureBase). ReforgedSuffix dropped: the ReforgedSuffix type does
/// not exist in this codebase.</summary>
[SerializationGenerator(0, false)]
public partial class GargishHalfApronBearingTheCrestOfBlackthorn2 : GargoyleHalfApron
{
    [Constructible]
    public GargishHalfApronBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusDex = 5;
        Attributes.BonusHits = 10;
        Attributes.RegenHits = 2;
        StrRequirement = 10;
        Hue = 1157;
    }
}

[SerializationGenerator(0, false)]
public partial class LeatherNinjaBeltBearingTheCrestOfBlackthorn2 : LeatherNinjaBelt
{
    [Constructible]
    public LeatherNinjaBeltBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusDex = 5;
        Attributes.BonusHits = 10;
        Attributes.RegenHits = 2;
        StrRequirement = 10;
        Hue = 1157;
    }
}

[SerializationGenerator(0, false)]
public partial class ObiBearingTheCrestOfBlackthorn2 : Obi
{
    [Constructible]
    public ObiBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusDex = 5;
        Attributes.BonusHits = 10;
        Attributes.RegenHits = 2;
        StrRequirement = 10;
        Hue = 1157;
    }
}

[SerializationGenerator(0, false)]
public partial class WoodlandBeltBearingTheCrestOfBlackthorn2 : WoodlandBelt
{
    [Constructible]
    public WoodlandBeltBearingTheCrestOfBlackthorn2()
    {
        Attributes.BonusDex = 5;
        Attributes.BonusHits = 10;
        Attributes.RegenHits = 2;
        StrRequirement = 10;
        Hue = 1157;
    }
}
