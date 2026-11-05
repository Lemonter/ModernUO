using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped Dungeons/BlackthornDungeon/Items/MysticsGarbBase).
/// ReforgedSuffix dropped: the ReforgedSuffix type does not exist in this codebase. ServUO's
/// RequiredRace/CanBeWornByGargoyles overrides become RequiredRaces = Race.AllowGargoylesOnly. IRepairable and
/// its RepairSystem member are dropped: IRepairable does not exist in this codebase. GargishFancyRobe has no
/// counterpart here; re-based on BaseOuterTorso with its ServUO art (0x4002). GargishRobe has no counterpart
/// here; re-based on BaseOuterTorso with its ServUO art (0x4000).</summary>
[SerializationGenerator(0, false)]
public partial class EpauletteBearingTheCrestOfBlackthorn2 : Cloak
{
    public override int LabelNumber => 1123325; // Epaulette

    [Constructible]
    public EpauletteBearingTheCrestOfBlackthorn2()
    {
        ItemID = 0x9985;
        Attributes.LowerManaCost = 1;
        Attributes.BonusMana = 5;
        Hue = 1306;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class FancyDressBearingTheCrestOfBlackthorn2 : FancyDress
{
    [Constructible]
    public FancyDressBearingTheCrestOfBlackthorn2()
    {
        Attributes.LowerManaCost = 1;
        Attributes.BonusMana = 5;
        Hue = 1306;
    }
}

[SerializationGenerator(0, false)]
public partial class FemaleKimonoBearingTheCrestOfBlackthorn2 : FemaleKimono
{
    [Constructible]
    public FemaleKimonoBearingTheCrestOfBlackthorn2()
    {
        Attributes.LowerManaCost = 1;
        Attributes.BonusMana = 5;
        Hue = 1306;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishEpauletteBearingTheCrestOfBlackthorn2 : Cloak
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1123326; // Gargish Epaulette

    [Constructible]
    public GargishEpauletteBearingTheCrestOfBlackthorn2()
    {
        ItemID = 0x9986;
        Attributes.LowerManaCost = 1;
        Attributes.BonusMana = 5;
        Hue = 1306;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishFancyBearingTheCrestOfBlackthorn2 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishFancyBearingTheCrestOfBlackthorn2() : base(0x4002)
    {
        Attributes.LowerManaCost = 1;
        Attributes.BonusMana = 5;
        Hue = 1306;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishRobeBearingTheCrestOfBlackthorn2 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishRobeBearingTheCrestOfBlackthorn2() : base(0x4000)
    {
        Attributes.LowerManaCost = 1;
        Attributes.BonusMana = 5;
        Hue = 1306;
    }
}

[SerializationGenerator(0, false)]
public partial class GildedDressBearingTheCrestOfBlackthorn2 : GildedDress
{
    [Constructible]
    public GildedDressBearingTheCrestOfBlackthorn2()
    {
        Attributes.LowerManaCost = 1;
        Attributes.BonusMana = 5;
        Hue = 1306;
    }
}

[Flippable(0x2684, 0x2683)]
[SerializationGenerator(0, false)]
public partial class HoodedRobeBearingTheCrestOfBlackthorn2 : BaseOuterTorso
{
    public override int LabelNumber => 1029863; // Hooded Robe

    [Constructible]
    public HoodedRobeBearingTheCrestOfBlackthorn2() : base(0x2683)
    {
        Attributes.LowerManaCost = 1;
        Attributes.BonusMana = 5;
        Hue = 1306;
    }
}

[SerializationGenerator(0, false)]
public partial class MaleKimonoBearingTheCrestOfBlackthorn2 : MaleKimono
{
    [Constructible]
    public MaleKimonoBearingTheCrestOfBlackthorn2()
    {
        Attributes.LowerManaCost = 1;
        Attributes.BonusMana = 5;
        Hue = 1306;
    }
}

[SerializationGenerator(0, false)]
public partial class PlainDressBearingTheCrestOfBlackthorn2 : PlainDress
{
    [Constructible]
    public PlainDressBearingTheCrestOfBlackthorn2()
    {
        Attributes.LowerManaCost = 1;
        Attributes.BonusMana = 5;
        Hue = 2075;
    }
}

[SerializationGenerator(0, false)]
public partial class RobeBearingTheCrestOfBlackthorn2 : Robe
{
    [Constructible]
    public RobeBearingTheCrestOfBlackthorn2()
    {
        Attributes.LowerManaCost = 1;
        Attributes.BonusMana = 5;
        Hue = 1306;
    }
}
