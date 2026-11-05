using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped Dungeons/BlackthornDungeon/Items/CloakOfSilenceBase).
/// ReforgedSuffix dropped: the ReforgedSuffix type does not exist in this codebase. ServUO's
/// RequiredRace/CanBeWornByGargoyles overrides become RequiredRaces = Race.AllowGargoylesOnly. IRepairable and
/// its RepairSystem member are dropped: IRepairable does not exist in this codebase. GargishFancyRobe has no
/// counterpart here; re-based on BaseOuterTorso with its ServUO art (0x4002). GargishRobe has no counterpart
/// here; re-based on BaseOuterTorso with its ServUO art (0x4000).</summary>
[SerializationGenerator(0, false)]
public partial class EpauletteBearingTheCrestOfBlackthorn3 : Cloak
{
    public override int LabelNumber => 1123325; // Epaulette

    [Constructible]
    public EpauletteBearingTheCrestOfBlackthorn3()
    {
        ItemID = 0x9985;
        SkillBonuses.SetValues(0, SkillName.Stealth, 10.0);
        Hue = 2130;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class FancyDressBearingTheCrestOfBlackthorn3 : FancyDress
{
    [Constructible]
    public FancyDressBearingTheCrestOfBlackthorn3()
    {
        SkillBonuses.SetValues(0, SkillName.Stealth, 10.0);
        Hue = 2130;
    }
}

[SerializationGenerator(0, false)]
public partial class FemaleKimonoBearingTheCrestOfBlackthorn3 : FemaleKimono
{
    [Constructible]
    public FemaleKimonoBearingTheCrestOfBlackthorn3()
    {
        SkillBonuses.SetValues(0, SkillName.Stealth, 10.0);
        Hue = 2130;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishEpauletteBearingTheCrestOfBlackthorn3 : Cloak
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override int LabelNumber => 1123326; // Gargish Epaulette

    [Constructible]
    public GargishEpauletteBearingTheCrestOfBlackthorn3()
    {
        ItemID = 0x9986;
        SkillBonuses.SetValues(0, SkillName.Stealth, 10.0);
        Hue = 2130;
        Layer = Layer.OuterTorso;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishFancyBearingTheCrestOfBlackthorn3 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishFancyBearingTheCrestOfBlackthorn3() : base(0x4002)
    {
        SkillBonuses.SetValues(0, SkillName.Stealth, 10.0);
        Hue = 2130;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishRobeBearingTheCrestOfBlackthorn3 : BaseOuterTorso
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishRobeBearingTheCrestOfBlackthorn3() : base(0x4000)
    {
        SkillBonuses.SetValues(0, SkillName.Stealth, 10.0);
        Hue = 2130;
    }
}

[SerializationGenerator(0, false)]
public partial class GildedDressBearingTheCrestOfBlackthorn3 : GildedDress
{
    [Constructible]
    public GildedDressBearingTheCrestOfBlackthorn3()
    {
        SkillBonuses.SetValues(0, SkillName.Stealth, 10.0);
        Hue = 2130;
    }
}

[Flippable(0x2684, 0x2683)]
[SerializationGenerator(0, false)]
public partial class HoodedRobeBearingTheCrestOfBlackthorn3 : BaseOuterTorso
{
    public override int LabelNumber => 1029863; // Hooded Robe

    [Constructible]
    public HoodedRobeBearingTheCrestOfBlackthorn3() : base(0x2683)
    {
        SkillBonuses.SetValues(0, SkillName.Stealth, 10.0);
        Hue = 2130;
    }
}

[SerializationGenerator(0, false)]
public partial class MaleKimonoBearingTheCrestOfBlackthorn3 : MaleKimono
{
    [Constructible]
    public MaleKimonoBearingTheCrestOfBlackthorn3()
    {
        SkillBonuses.SetValues(0, SkillName.Stealth, 10.0);
        Hue = 2130;
    }
}

[SerializationGenerator(0, false)]
public partial class PlainDressBearingTheCrestOfBlackthorn3 : PlainDress
{
    [Constructible]
    public PlainDressBearingTheCrestOfBlackthorn3()
    {
        SkillBonuses.SetValues(0, SkillName.Stealth, 10.0);
        Hue = 2130;
    }
}

[SerializationGenerator(0, false)]
public partial class RobeBearingTheCrestOfBlackthorn3 : Robe
{
    [Constructible]
    public RobeBearingTheCrestOfBlackthorn3()
    {
        SkillBonuses.SetValues(0, SkillName.Stealth, 10.0);
        Hue = 2130;
    }
}
