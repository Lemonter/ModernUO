using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Covetous void pool (Scripts/Services/Revamped Dungeons/Covetous Void Spawn/
///     Items/*.cs) — artifact rewards handed out by the wave controller.
///
///     Three things the original declares have no counterpart here and are dropped rather than
///     bolted on for a handful of items: <c>IsArtifact</c> (not virtual on Item), the antique
///     variant (<c>NegativeAttributes.Antique</c> — see VvV, where the same concept was dropped),
///     and <c>WeaponAttributes.SplinteringWeapon</c>. The Gargoyle variants of Brightblade and
///     Hephaestus are left out too: this codebase has neither GargishKatana nor LargePlateShield,
///     and inventing new weapon bases for two rewards is a bigger change than the dungeon itself.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BraceletOfProtection : GoldBracelet
{
    public override int LabelNumber => 1152730; // Bracelet of Protection

    [Constructible]
    public BraceletOfProtection()
    {
        Hue = 1157;
        Attributes.BonusHits = 5;
        Attributes.RegenHits = 10;
        Attributes.DefendChance = 5;

        MaxHitPoints = 255;
        HitPoints = MaxHitPoints;
    }
}

[SerializationGenerator(0, false)]
public partial class Brightblade : Katana
{
    public override int LabelNumber => 1152732; // Brightblade

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public Brightblade()
    {
        Hue = 1756;

        WeaponAttributes.HitLeechStam = 100;
        Attributes.RegenStam = 3;
        Attributes.AttackChance = 10;
        Attributes.CastSpeed = 1;
        Attributes.WeaponSpeed = 40;
        Attributes.WeaponDamage = 50;
        AosElementDamages.Fire = 100;
    }
}

[SerializationGenerator(0, false)]
public partial class BlightOfTheTundra : CompositeBow
{
    public override int LabelNumber => 1152727; // Blight of the Tundra

    [Constructible]
    public BlightOfTheTundra()
    {
        Hue = 1165;

        Attributes.BonusStr = 5;
        Attributes.RegenStam = 10;
        Attributes.AttackChance = 15;
        Attributes.WeaponSpeed = 45;
        Attributes.WeaponDamage = 50;

        Slayer = BaseRunicTool.GetRandomSlayer();

        WeaponAttributes.ResistColdBonus = 15;
        AosElementDamages.Fire = 100;

        MaxHitPoints = 255;
        HitPoints = MaxHitPoints;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishBlightOfTheTundra : SoulGlaive
{
    public override int LabelNumber => 1152727; // Blight of the Tundra

    [Constructible]
    public GargishBlightOfTheTundra()
    {
        Hue = 1165;

        Attributes.BonusStr = 5;
        Attributes.RegenStam = 10;
        Attributes.AttackChance = 15;
        Attributes.WeaponSpeed = 45;
        Attributes.WeaponDamage = 50;

        Slayer = BaseRunicTool.GetRandomSlayer();

        WeaponAttributes.ResistColdBonus = 15;
        AosElementDamages.Fire = 100;

        MaxHitPoints = 255;
        HitPoints = MaxHitPoints;
    }
}

[SerializationGenerator(0, false)]
public partial class Hephaestus : HeaterShield
{
    public override int LabelNumber => 1152909; // Hephaestus

    [Constructible]
    public Hephaestus()
    {
        Hue = 1910;

        Attributes.SpellChanneling = 1;
        Attributes.ReflectPhysical = 15;
        Attributes.DefendChance = 15;
        Attributes.CastSpeed = 1;
        Attributes.LowerManaCost = 8;

        PhysicalBonus = 15;
        ArmorAttributes.SelfRepair = 5;

        SkillBonuses.SetValues(0, SkillName.Parry, 10.0);

        MaxHitPoints = 255;
        HitPoints = MaxHitPoints;
    }
}
