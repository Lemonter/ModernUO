using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO's Despise Revamped dungeon (Scripts/Items/Artifacts/
/// DespiseArtifacts.cs) — the 7 boss-drop artifacts (see DespiseBoss.Artifacts). Two have a
/// human/Gargoyle variant pair (HailstormHuman/HailstormGargoyle) since a WarFork can't be
/// worn by both races' equip slots the same way. Dropped the original's `IsArtifact => true`
/// override on each — no Item in this codebase declares that as virtual, so there's nothing
/// to override (purely a cosmetic/informational flag in ServUO, not load-bearing here).</summary>
[SerializationGenerator(0, false)]
public partial class CompassionsEye : GoldRing
{
    public override int LabelNumber => 1153288; // Compassion's Eye

    [Constructible]
    public CompassionsEye()
    {
        Hue = 1174;

        Attributes.BonusInt = 10;
        Attributes.BonusMana = 10;
        Attributes.RegenMana = 2;
        Attributes.Luck = 250;
        Attributes.SpellDamage = 20;
        Attributes.LowerRegCost = 20;
    }
}

[SerializationGenerator(0, false)]
public partial class UnicornManeWovenSandals : Sandals
{
    public override int LabelNumber => 1153289; // Unicorn Mane Woven Sandals

    [Constructible]
    public UnicornManeWovenSandals()
    {
        Hue = 1154;

        // Was a random SAAbsorptionAttributes.Eater* roll — no clothing item in this
        // codebase exposes that attribute container (grepped BaseClothing, nothing);
        // dropped rather than force-add a whole new attribute set for one artifact.
        Attributes.NightSight = 1;
    }
}

[SerializationGenerator(0, false)]
public partial class UnicornManeWovenTalons : LeatherTalons
{
    public override int LabelNumber => 1153314; // Unicorn Mane Woven Talons

    [Constructible]
    public UnicornManeWovenTalons()
    {
        Hue = 1154;

        // Same drop as UnicornManeWovenSandals — see its constructor comment.
        Attributes.NightSight = 1;
    }
}

[SerializationGenerator(0, false)]
public partial class DespicableQuiver : BaseQuiver
{
    public override int LabelNumber => 1153290; // Despicable Quiver

    [Constructible]
    public DespicableQuiver() : base(0x2B02)
    {
        Hue = 2671;

        DamageIncrease = 10;
        WeightReduction = 30;
        Attributes.BonusDex = 5;
        Attributes.ReflectPhysical = 5;
        Attributes.AttackChance = 5;
        LowerAmmoCost = 30;

        // Was SkillBonuses.SetValues(...) and a random Resistances.X roll — BaseQuiver in
        // this codebase has Attributes/LowerAmmoCost/WeightReduction/DamageIncrease (all
        // used above) but no SkillBonuses or Resistances container at all; dropped rather
        // than bolt those onto BaseQuiver for one artifact.
    }
}

[SerializationGenerator(0, false)]
public partial class UnforgivenVeil : GargishLeatherWingArmor
{
    public override int LabelNumber => 1153291; // Unforgiven Veil

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override int PhysicalResistance => PhysicalBonus;
    public override int FireResistance => FireBonus;
    public override int ColdResistance => ColdBonus;
    public override int PoisonResistance => PoisonBonus;
    public override int EnergyResistance => EnergyBonus;

    [Constructible]
    public UnforgivenVeil()
    {
        Hue = 2671;

        Attributes.BonusDex = 5;
        SkillBonuses.SetValues(0, SkillName.Throwing, 5.0);
        Attributes.ReflectPhysical = 5;
        Attributes.AttackChance = 5;

        switch (Utility.Random(5))
        {
            case 0: PhysicalBonus = 10; break;
            case 1: FireBonus = 10; break;
            case 2: ColdBonus = 10; break;
            case 3: PoisonBonus = 10; break;
            case 4: EnergyBonus = 10; break;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class HailstormHuman : WarFork
{
    public override int LabelNumber => 1153292; // Hailstorm

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public HailstormHuman()
    {
        Hue = 2714;

        WeaponAttributes.HitLightning = 15;
        WeaponAttributes.HitColdArea = 100;
        WeaponAttributes.HitLeechMana = 30;
        Attributes.AttackChance = 20;
        Attributes.WeaponSpeed = 25;
        Attributes.WeaponDamage = 50;
        AosElementDamages.Cold = 100;
    }
}

[SerializationGenerator(0, false)]
public partial class HailstormGargoyle : GargishWarFork
{
    public override int LabelNumber => 1153292; // Hailstorm

    [Constructible]
    public HailstormGargoyle()
    {
        Hue = 2714;

        WeaponAttributes.HitLightning = 15;
        WeaponAttributes.HitColdArea = 100;
        WeaponAttributes.HitLeechMana = 30;
        Attributes.AttackChance = 20;
        Attributes.WeaponSpeed = 25;
        Attributes.WeaponDamage = 50;
        AosElementDamages.Cold = 100;
    }
}
