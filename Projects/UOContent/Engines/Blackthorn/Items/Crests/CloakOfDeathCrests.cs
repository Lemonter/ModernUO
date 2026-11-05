using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped Dungeons/BlackthornDungeon/Items/CloakOfDeathBase).
/// ReforgedSuffix dropped: the ReforgedSuffix type does not exist in this codebase. ServUO's
/// RequiredRace/CanBeWornByGargoyles overrides become RequiredRaces = Race.AllowGargoylesOnly. IRepairable and
/// its RepairSystem member are dropped: IRepairable does not exist in this codebase. GargishFancyRobe has no
/// counterpart here; re-based on BaseOuterTorso with its ServUO art (0x4002). GargishRobe has no counterpart
/// here; re-based on BaseOuterTorso with its ServUO art (0x4000).</summary>
[SerializationGenerator(0, false)]
public partial class EpauletteBearingTheCrestOfBlackthorn6 : Cloak
{
    public override int LabelNumber => 1123325; // Epaulette

    [Constructible]
    public EpauletteBearingTheCrestOfBlackthorn6()
    {
        ItemID = 0x9985;
        Attributes.AttackChance = 3;
        Attributes.DefendChance = 3;
        Attributes.SpellDamage = 3;
        Hue = 2019;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class FancyDressBearingTheCrestOfBlackthorn6 : FancyDress
{
    [Constructible]
    public FancyDressBearingTheCrestOfBlackthorn6()
    {
        Attributes.AttackChance = 3;
        Attributes.DefendChance = 3;
        Attributes.SpellDamage = 3;
        Hue = 2019;
    }
}

[SerializationGenerator(0, false)]
public partial class FemaleKimonoBearingTheCrestOfBlackthorn6 : FemaleKimono
{
    [Constructible]
    public FemaleKimonoBearingTheCrestOfBlackthorn6()
    {
        Attributes.AttackChance = 3;
        Attributes.DefendChance = 3;
        Attributes.SpellDamage = 3;
        Hue = 2019;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishEpauletteBearingTheCrestOfBlackthorn6 : Cloak
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1123326; // Gargish Epaulette

    [Constructible]
    public GargishEpauletteBearingTheCrestOfBlackthorn6()
    {
        ItemID = 0x9986;
        Attributes.AttackChance = 3;
        Attributes.DefendChance = 3;
        Attributes.SpellDamage = 3;
        Hue = 2019;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishFancyBearingTheCrestOfBlackthorn6 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishFancyBearingTheCrestOfBlackthorn6() : base(0x4002)
    {
        Attributes.AttackChance = 3;
        Attributes.DefendChance = 3;
        Attributes.SpellDamage = 3;
        Hue = 2019;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishRobeBearingTheCrestOfBlackthorn6 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishRobeBearingTheCrestOfBlackthorn6() : base(0x4000)
    {
        Attributes.AttackChance = 3;
        Attributes.DefendChance = 3;
        Attributes.SpellDamage = 3;
        Hue = 2019;
    }
}

[SerializationGenerator(0, false)]
public partial class GildedDressBearingTheCrestOfBlackthorn6 : GildedDress
{
    [Constructible]
    public GildedDressBearingTheCrestOfBlackthorn6()
    {
        Attributes.AttackChance = 3;
        Attributes.DefendChance = 3;
        Attributes.SpellDamage = 3;
        Hue = 2019;
    }
}

[Flippable(0x2684, 0x2683)]
[SerializationGenerator(0, false)]
public partial class HoodedRobeBearingTheCrestOfBlackthorn6 : BaseOuterTorso
{
    public override int LabelNumber => 1029863; // Hooded Robe

    [Constructible]
    public HoodedRobeBearingTheCrestOfBlackthorn6() : base(0x2683)
    {
        Attributes.AttackChance = 3;
        Attributes.DefendChance = 3;
        Attributes.SpellDamage = 3;
        Hue = 2019;
    }
}

[SerializationGenerator(0, false)]
public partial class MaleKimonoBearingTheCrestOfBlackthorn6 : MaleKimono
{
    [Constructible]
    public MaleKimonoBearingTheCrestOfBlackthorn6()
    {
        Attributes.AttackChance = 3;
        Attributes.DefendChance = 3;
        Attributes.SpellDamage = 3;
        Hue = 2019;
    }
}

[SerializationGenerator(0, false)]
public partial class PlainDressBearingTheCrestOfBlackthorn6 : PlainDress
{
    [Constructible]
    public PlainDressBearingTheCrestOfBlackthorn6()
    {
        Attributes.AttackChance = 3;
        Attributes.DefendChance = 3;
        Attributes.SpellDamage = 3;
        Hue = 2019;
    }
}

[SerializationGenerator(0, false)]
public partial class RobeBearingTheCrestOfBlackthorn6 : Robe
{
    [Constructible]
    public RobeBearingTheCrestOfBlackthorn6()
    {
        Attributes.AttackChance = 3;
        Attributes.DefendChance = 3;
        Attributes.SpellDamage = 3;
        Hue = 2019;
    }
}
