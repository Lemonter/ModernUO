using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped Dungeons/BlackthornDungeon/Items/CloakOfLifeBase).
/// ReforgedSuffix dropped: the ReforgedSuffix type does not exist in this codebase. ServUO's
/// RequiredRace/CanBeWornByGargoyles overrides become RequiredRaces = Race.AllowGargoylesOnly. IRepairable and
/// its RepairSystem member are dropped: IRepairable does not exist in this codebase. GargishFancyRobe has no
/// counterpart here; re-based on BaseOuterTorso with its ServUO art (0x4002). GargishRobe has no counterpart
/// here; re-based on BaseOuterTorso with its ServUO art (0x4000).</summary>
[SerializationGenerator(0, false)]
public partial class EpauletteBearingTheCrestOfBlackthorn5 : Cloak
{
    public override int LabelNumber => 1123325; // Epaulette

    [Constructible]
    public EpauletteBearingTheCrestOfBlackthorn5()
    {
        ItemID = 0x9985;
        Attributes.BonusHits = 3;
        Attributes.RegenHits = 1;
        Hue = 132;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class FancyDressBearingTheCrestOfBlackthorn5 : FancyDress
{
    [Constructible]
    public FancyDressBearingTheCrestOfBlackthorn5()
    {
        Attributes.BonusHits = 3;
        Attributes.RegenHits = 1;
        Hue = 132;
    }
}

[SerializationGenerator(0, false)]
public partial class FemaleKimonoBearingTheCrestOfBlackthorn5 : FemaleKimono
{
    [Constructible]
    public FemaleKimonoBearingTheCrestOfBlackthorn5()
    {
        Attributes.BonusHits = 3;
        Attributes.RegenHits = 1;
        Hue = 132;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishEpauletteBearingTheCrestOfBlackthorn5 : Cloak
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1123326; // Gargish Epaulette

    [Constructible]
    public GargishEpauletteBearingTheCrestOfBlackthorn5()
    {
        ItemID = 0x9986;
        Attributes.BonusHits = 3;
        Attributes.RegenHits = 1;
        Hue = 132;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishFancyBearingTheCrestOfBlackthorn5 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishFancyBearingTheCrestOfBlackthorn5() : base(0x4002)
    {
        Attributes.BonusHits = 3;
        Attributes.RegenHits = 1;
        Hue = 132;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishRobeBearingTheCrestOfBlackthorn5 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishRobeBearingTheCrestOfBlackthorn5() : base(0x4000)
    {
        Attributes.BonusHits = 3;
        Attributes.RegenHits = 1;
        Hue = 132;
    }
}

[SerializationGenerator(0, false)]
public partial class GildedDressBearingTheCrestOfBlackthorn5 : GildedDress
{
    [Constructible]
    public GildedDressBearingTheCrestOfBlackthorn5()
    {
        Attributes.BonusHits = 3;
        Attributes.RegenHits = 1;
        Hue = 132;
    }
}

[Flippable(0x2684, 0x2683)]
[SerializationGenerator(0, false)]
public partial class HoodedRobeBearingTheCrestOfBlackthorn5 : BaseOuterTorso
{
    public override int LabelNumber => 1029863; // Hooded Robe

    [Constructible]
    public HoodedRobeBearingTheCrestOfBlackthorn5() : base(0x2683)
    {
        Attributes.BonusHits = 3;
        Attributes.RegenHits = 1;
        Hue = 132;
    }
}

[SerializationGenerator(0, false)]
public partial class MaleKimonoBearingTheCrestOfBlackthorn5 : MaleKimono
{
    [Constructible]
    public MaleKimonoBearingTheCrestOfBlackthorn5()
    {
        Attributes.BonusHits = 3;
        Attributes.RegenHits = 1;
        Hue = 132;
    }
}

[SerializationGenerator(0, false)]
public partial class PlainDressBearingTheCrestOfBlackthorn5 : PlainDress
{
    [Constructible]
    public PlainDressBearingTheCrestOfBlackthorn5()
    {
        Attributes.BonusHits = 3;
        Attributes.RegenHits = 1;
        Hue = 132;
    }
}

[SerializationGenerator(0, false)]
public partial class RobeBearingTheCrestOfBlackthorn5 : Robe
{
    [Constructible]
    public RobeBearingTheCrestOfBlackthorn5()
    {
        Attributes.BonusHits = 3;
        Attributes.RegenHits = 1;
        Hue = 132;
    }
}
