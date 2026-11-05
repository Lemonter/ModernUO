using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped
/// Dungeons/BlackthornDungeon/Items/ShroudOfTheCondemnedBase). ReforgedSuffix dropped: the ReforgedSuffix type
/// does not exist in this codebase. ServUO's RequiredRace/CanBeWornByGargoyles overrides become RequiredRaces =
/// Race.AllowGargoylesOnly. IRepairable and its RepairSystem member are dropped: IRepairable does not exist in
/// this codebase. GargishFancyRobe has no counterpart here; re-based on BaseOuterTorso with its ServUO art
/// (0x4002). GargishRobe has no counterpart here; re-based on BaseOuterTorso with its ServUO art (0x4000).
/// </summary>
[SerializationGenerator(0, false)]
public partial class EpauletteBearingTheCrestOfBlackthorn1 : Cloak
{
    public override int LabelNumber => 1123325; // Epaulette

    [Constructible]
    public EpauletteBearingTheCrestOfBlackthorn1()
    {
        ItemID = 0x9985;
        Attributes.BonusHits = 3;
        Attributes.BonusInt = 5;
        Hue = 2075;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class FancyDressBearingTheCrestOfBlackthorn1 : FancyDress
{
    [Constructible]
    public FancyDressBearingTheCrestOfBlackthorn1()
    {
        Attributes.BonusHits = 3;
        Attributes.BonusInt = 5;
        Hue = 2075;
    }
}

[SerializationGenerator(0, false)]
public partial class FemaleKimonoBearingTheCrestOfBlackthorn1 : FemaleKimono
{
    [Constructible]
    public FemaleKimonoBearingTheCrestOfBlackthorn1()
    {
        Attributes.BonusHits = 3;
        Attributes.BonusInt = 5;
        Hue = 2075;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishEpauletteBearingTheCrestOfBlackthorn1 : Cloak
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1123326; // Gargish Epaulette

    [Constructible]
    public GargishEpauletteBearingTheCrestOfBlackthorn1()
    {
        ItemID = 0x9986;
        Attributes.BonusHits = 3;
        Attributes.BonusInt = 5;
        Hue = 2075;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishFancyBearingTheCrestOfBlackthorn1 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishFancyBearingTheCrestOfBlackthorn1() : base(0x4002)
    {
        Attributes.BonusHits = 3;
        Attributes.BonusInt = 5;
        Hue = 2075;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishRobeBearingTheCrestOfBlackthorn1 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishRobeBearingTheCrestOfBlackthorn1() : base(0x4000)
    {
        Attributes.BonusHits = 3;
        Attributes.BonusInt = 5;
        Hue = 2075;
    }
}

[SerializationGenerator(0, false)]
public partial class GildedDressBearingTheCrestOfBlackthorn1 : GildedDress
{
    [Constructible]
    public GildedDressBearingTheCrestOfBlackthorn1()
    {
        Attributes.BonusHits = 3;
        Attributes.BonusInt = 5;
        Hue = 2075;
    }
}

[Flippable(0x2684, 0x2683)]
[SerializationGenerator(0, false)]
public partial class HoodedRobeBearingTheCrestOfBlackthorn1 : BaseOuterTorso
{
    public override int LabelNumber => 1029863; // Hooded Robe

    [Constructible]
    public HoodedRobeBearingTheCrestOfBlackthorn1() : base(0x2683)
    {
        Attributes.BonusHits = 3;
        Attributes.BonusInt = 5;
        Hue = 2075;
    }
}

[SerializationGenerator(0, false)]
public partial class MaleKimonoBearingTheCrestOfBlackthorn1 : MaleKimono
{
    [Constructible]
    public MaleKimonoBearingTheCrestOfBlackthorn1()
    {
        Attributes.BonusHits = 3;
        Attributes.BonusInt = 5;
        Hue = 2075;
    }
}

[SerializationGenerator(0, false)]
public partial class PlainDressBearingTheCrestOfBlackthorn1 : PlainDress
{
    [Constructible]
    public PlainDressBearingTheCrestOfBlackthorn1()
    {
        Attributes.BonusHits = 3;
        Attributes.BonusInt = 5;
        Hue = 2075;
    }
}

[SerializationGenerator(0, false)]
public partial class RobeBearingTheCrestOfBlackthorn1 : Robe
{
    [Constructible]
    public RobeBearingTheCrestOfBlackthorn1()
    {
        Attributes.BonusHits = 3;
        Attributes.BonusInt = 5;
        Hue = 2075;
    }
}
