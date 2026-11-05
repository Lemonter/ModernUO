using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped Dungeons/BlackthornDungeon/Items/ConjurersGarbBase).
/// ReforgedSuffix dropped: the ReforgedSuffix type does not exist in this codebase. ServUO's
/// RequiredRace/CanBeWornByGargoyles overrides become RequiredRaces = Race.AllowGargoylesOnly. IRepairable and
/// its RepairSystem member are dropped: IRepairable does not exist in this codebase. GargishFancyRobe has no
/// counterpart here; re-based on BaseOuterTorso with its ServUO art (0x4002). GargishRobe has no counterpart
/// here; re-based on BaseOuterTorso with its ServUO art (0x4000).</summary>
[SerializationGenerator(0, false)]
public partial class EpauletteBearingTheCrestOfBlackthorn7 : Cloak
{
    public override int LabelNumber => 1123325; // Epaulette

    [Constructible]
    public EpauletteBearingTheCrestOfBlackthorn7()
    {
        ItemID = 0x9985;
        Attributes.RegenMana = 2;
        Attributes.DefendChance = 5;
        Attributes.Luck = 140;
        Hue = 1194;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class FancyDressBearingTheCrestOfBlackthorn7 : FancyDress
{
    [Constructible]
    public FancyDressBearingTheCrestOfBlackthorn7()
    {
        Attributes.RegenMana = 2;
        Attributes.DefendChance = 5;
        Attributes.Luck = 140;
        Hue = 1194;
    }
}

[SerializationGenerator(0, false)]
public partial class FemaleKimonoBearingTheCrestOfBlackthorn7 : FemaleKimono
{
    [Constructible]
    public FemaleKimonoBearingTheCrestOfBlackthorn7()
    {
        Attributes.RegenMana = 2;
        Attributes.DefendChance = 5;
        Attributes.Luck = 140;
        Hue = 1194;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishEpauletteBearingTheCrestOfBlackthorn7 : Cloak
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1123326; // Gargish Epaulette

    [Constructible]
    public GargishEpauletteBearingTheCrestOfBlackthorn7()
    {
        ItemID = 0x9986;
        Attributes.RegenMana = 2;
        Attributes.DefendChance = 5;
        Attributes.Luck = 140;
        Hue = 1194;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishFancyBearingTheCrestOfBlackthorn7 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishFancyBearingTheCrestOfBlackthorn7() : base(0x4002)
    {
        Attributes.RegenMana = 2;
        Attributes.DefendChance = 5;
        Attributes.Luck = 140;
        Hue = 1194;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishRobeBearingTheCrestOfBlackthorn7 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishRobeBearingTheCrestOfBlackthorn7() : base(0x4000)
    {
        Attributes.RegenMana = 2;
        Attributes.DefendChance = 5;
        Attributes.Luck = 140;
        Hue = 1194;
    }
}

[SerializationGenerator(0, false)]
public partial class GildedDressBearingTheCrestOfBlackthorn7 : GildedDress
{
    [Constructible]
    public GildedDressBearingTheCrestOfBlackthorn7()
    {
        Attributes.RegenMana = 2;
        Attributes.DefendChance = 5;
        Attributes.Luck = 140;
        Hue = 1194;
    }
}

[Flippable(0x2684, 0x2683)]
[SerializationGenerator(0, false)]
public partial class HoodedRobeBearingTheCrestOfBlackthorn7 : BaseOuterTorso
{
    public override int LabelNumber => 1029863; // Hooded Robe

    [Constructible]
    public HoodedRobeBearingTheCrestOfBlackthorn7() : base(0x2683)
    {
        Attributes.RegenMana = 2;
        Attributes.DefendChance = 5;
        Attributes.Luck = 140;
        Hue = 1194;
    }
}

[SerializationGenerator(0, false)]
public partial class MaleKimonoBearingTheCrestOfBlackthorn7 : MaleKimono
{
    [Constructible]
    public MaleKimonoBearingTheCrestOfBlackthorn7()
    {
        Attributes.RegenMana = 2;
        Attributes.DefendChance = 5;
        Attributes.Luck = 140;
        Hue = 1194;
    }
}

[SerializationGenerator(0, false)]
public partial class PlainDressBearingTheCrestOfBlackthorn7 : PlainDress
{
    [Constructible]
    public PlainDressBearingTheCrestOfBlackthorn7()
    {
        Attributes.RegenMana = 2;
        Attributes.DefendChance = 5;
        Attributes.Luck = 140;
        Hue = 1194;
    }
}

[SerializationGenerator(0, false)]
public partial class RobeBearingTheCrestOfBlackthorn7 : Robe
{
    [Constructible]
    public RobeBearingTheCrestOfBlackthorn7()
    {
        Attributes.RegenMana = 2;
        Attributes.DefendChance = 5;
        Attributes.Luck = 140;
        Hue = 1194;
    }
}
