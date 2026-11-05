using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped
/// Dungeons/BlackthornDungeon/Items/RoyalBritannianBase). ReforgedSuffix dropped: the ReforgedSuffix type does
/// not exist in this codebase. GargishSash has no counterpart here; re-based on BaseMiddleTorso with its ServUO
/// art (0x46B4).</summary>
[SerializationGenerator(0, false)]
public partial class DoubletBearingTheCrestOfBlackthorn : Doublet
{
    public override int InitMinHits => 150;

    public override int InitMaxHits => 150;

    [Constructible]
    public DoubletBearingTheCrestOfBlackthorn()
    {
        Attributes.BonusInt = 5;
        Attributes.RegenMana = 2;
        Attributes.LowerRegCost = 10;
        StrRequirement = 10;
        Hue = 0xe8;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishSashBearingTheCrestOfBlackthorn : BaseMiddleTorso
{
    public override int InitMinHits => 150;

    public override int InitMaxHits => 150;

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishSashBearingTheCrestOfBlackthorn() : base(0x46B4)
    {
        Attributes.BonusInt = 5;
        Attributes.RegenMana = 2;
        Attributes.LowerRegCost = 10;
        StrRequirement = 10;
        Hue = 0xe8;
    }
}

[SerializationGenerator(0, false)]
public partial class SurcoatBearingTheCrestOfBlackthorn : Surcoat
{
    public override int InitMinHits => 150;

    public override int InitMaxHits => 150;

    [Constructible]
    public SurcoatBearingTheCrestOfBlackthorn()
    {
        Attributes.BonusInt = 5;
        Attributes.RegenMana = 2;
        Attributes.LowerRegCost = 10;
        StrRequirement = 10;
        Hue = 0xe8;
    }
}

[SerializationGenerator(0, false)]
public partial class TunicBearingTheCrestOfBlackthorn : Tunic
{
    public override int InitMinHits => 150;

    public override int InitMaxHits => 150;

    [Constructible]
    public TunicBearingTheCrestOfBlackthorn()
    {
        Attributes.BonusInt = 5;
        Attributes.RegenMana = 2;
        Attributes.LowerRegCost = 10;
        StrRequirement = 10;
        Hue = 0xe8;
    }
}
