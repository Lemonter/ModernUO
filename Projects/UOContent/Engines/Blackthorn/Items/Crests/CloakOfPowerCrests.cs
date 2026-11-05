using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped Dungeons/BlackthornDungeon/Items/CloakOfPowerBase).
/// ReforgedSuffix dropped: the ReforgedSuffix type does not exist in this codebase. ServUO's
/// RequiredRace/CanBeWornByGargoyles overrides become RequiredRaces = Race.AllowGargoylesOnly. IRepairable and
/// its RepairSystem member are dropped: IRepairable does not exist in this codebase. GargishFancyRobe has no
/// counterpart here; re-based on BaseOuterTorso with its ServUO art (0x4002). GargishRobe has no counterpart
/// here; re-based on BaseOuterTorso with its ServUO art (0x4000).</summary>
[SerializationGenerator(0, false)]
public partial class EpauletteBearingTheCrestOfBlackthorn4 : Cloak
{
    public override int LabelNumber => 1123325; // Epaulette

    [Constructible]
    public EpauletteBearingTheCrestOfBlackthorn4()
    {
        ItemID = 0x9985;
        Attributes.BonusStr = 2;
        Attributes.BonusDex = 2;
        Attributes.BonusInt = 2;
        Hue = 2107;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class FancyDressBearingTheCrestOfBlackthorn4 : FancyDress
{
    [Constructible]
    public FancyDressBearingTheCrestOfBlackthorn4()
    {
        Attributes.BonusStr = 2;
        Attributes.BonusDex = 2;
        Attributes.BonusInt = 2;
        Hue = 2107;
    }
}

[SerializationGenerator(0, false)]
public partial class FemaleKimonoBearingTheCrestOfBlackthorn4 : FemaleKimono
{
    [Constructible]
    public FemaleKimonoBearingTheCrestOfBlackthorn4()
    {
        Attributes.BonusStr = 2;
        Attributes.BonusDex = 2;
        Attributes.BonusInt = 2;
        Hue = 2107;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishEpauletteBearingTheCrestOfBlackthorn4 : Cloak
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1123326; // Gargish Epaulette

    [Constructible]
    public GargishEpauletteBearingTheCrestOfBlackthorn4()
    {
        ItemID = 0x9986;
        Attributes.BonusStr = 2;
        Attributes.BonusDex = 2;
        Attributes.BonusInt = 2;
        Hue = 2107;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishFancyBearingTheCrestOfBlackthorn4 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishFancyBearingTheCrestOfBlackthorn4() : base(0x4002)
    {
        Attributes.BonusStr = 2;
        Attributes.BonusDex = 2;
        Attributes.BonusInt = 2;
        Hue = 2107;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishRobeBearingTheCrestOfBlackthorn4 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishRobeBearingTheCrestOfBlackthorn4() : base(0x4000)
    {
        Attributes.BonusStr = 2;
        Attributes.BonusDex = 2;
        Attributes.BonusInt = 2;
        Hue = 2107;
    }
}

[SerializationGenerator(0, false)]
public partial class GildedDressBearingTheCrestOfBlackthorn4 : GildedDress
{
    [Constructible]
    public GildedDressBearingTheCrestOfBlackthorn4()
    {
        Attributes.BonusStr = 2;
        Attributes.BonusDex = 2;
        Attributes.BonusInt = 2;
        Hue = 2107;
    }
}

[Flippable(0x2684, 0x2683)]
[SerializationGenerator(0, false)]
public partial class HoodedRobeBearingTheCrestOfBlackthorn4 : BaseOuterTorso
{
    public override int LabelNumber => 1029863; // Hooded Robe

    [Constructible]
    public HoodedRobeBearingTheCrestOfBlackthorn4() : base(0x2683)
    {
        Attributes.BonusStr = 2;
        Attributes.BonusDex = 2;
        Attributes.BonusInt = 2;
        Hue = 2107;
    }
}

[SerializationGenerator(0, false)]
public partial class MaleKimonoBearingTheCrestOfBlackthorn4 : MaleKimono
{
    [Constructible]
    public MaleKimonoBearingTheCrestOfBlackthorn4()
    {
        Attributes.BonusStr = 2;
        Attributes.BonusDex = 2;
        Attributes.BonusInt = 2;
        Hue = 2107;
    }
}

[SerializationGenerator(0, false)]
public partial class PlainDressBearingTheCrestOfBlackthorn4 : PlainDress
{
    [Constructible]
    public PlainDressBearingTheCrestOfBlackthorn4()
    {
        Attributes.BonusStr = 2;
        Attributes.BonusDex = 2;
        Attributes.BonusInt = 2;
        Hue = 2107;
    }
}

[SerializationGenerator(0, false)]
public partial class RobeBearingTheCrestOfBlackthorn4 : Robe
{
    [Constructible]
    public RobeBearingTheCrestOfBlackthorn4()
    {
        Attributes.BonusStr = 2;
        Attributes.BonusDex = 2;
        Attributes.BonusInt = 2;
        Hue = 2107;
    }
}
