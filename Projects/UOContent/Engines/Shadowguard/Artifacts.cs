using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Mobiles;

namespace Server.Items;

// SetProtection (creature-specific damage reduction) is only wired up on BaseTalisman in this
// codebase — ServUO also allows it on shields/earrings. GrugorsShield(Gargoyle) and
// WamapsBoneEarrings(Gargoyle) below drop that one sub-effect rather than extending the mechanic
// to two more item bases for a handful of Eodon-creature match-ups; every other stat stays intact.
// None of these artifacts carry pre-codegen save data (brand new to this codebase), so the
// version-conditional legacy-migration branches in a few ServUO Deserialize() overrides
// (HalawasHuntingBow, LereisHuntingSpear, and their gargoyle variants) are dropped — the
// constructors below just set the current (post-migration) values directly.

[SerializationGenerator(0, false)]
public partial class AnonsBoots : Boots
{
    public override int LabelNumber => 1156295; // Anon's Boots
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public AnonsBoots()
    {
        Hue = 1325;

        Attributes.AttackChance = -5;
        Attributes.DefendChance = 10;
    }
}

[SerializationGenerator(0, false)]
public partial class AnonsBootsGargoyle : LeatherTalons
{
    public override int LabelNumber => 1156295; // Anon's Boots
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public AnonsBootsGargoyle()
    {
        Hue = 1325;

        Attributes.AttackChance = -5;
        Attributes.DefendChance = 10;
    }
}

[SerializationGenerator(0, false)]
public partial class AnonsSpellbook : Spellbook
{
    public override int LabelNumber => 1156344;

    [Constructible]
    public AnonsSpellbook()
    {
        LootType = LootType.Blessed;
        SkillBonuses.SetValues(0, SkillName.Magery, 15.0);
        Attributes.BonusInt = 8;
        Attributes.SpellDamage = 15;
        Attributes.LowerManaCost = 10;
        Attributes.LowerRegCost = 10;

        Slayer = SlayerName.None;
    }
}

[SerializationGenerator(0, false)]
public partial class BalakaisShamanStaff : WildStaff
{
    public override int LabelNumber => 1156125;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public BalakaisShamanStaff()
    {
        SkillBonuses.SetValues(0, SkillName.Meditation, 10.0);
        WeaponAttributes.MageWeapon = 30;
        Attributes.SpellChanneling = 1;
        Attributes.EnhancePotions = 25;
    }
}

[SerializationGenerator(0, false)]
public partial class BalakaisShamanStaffGargoyle : BaseWand
{
    public override int LabelNumber => 1156125;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public BalakaisShamanStaffGargoyle() : base(WandEffect.Clumsiness, 0, 0)
    {
        SkillBonuses.SetValues(0, SkillName.Meditation, 10.0);
        WeaponAttributes.MageWeapon = 30;
        Attributes.SpellChanneling = 1;
        Attributes.EnhancePotions = 25;
    }
}

[SerializationGenerator(0, false)]
public partial class EnchantressCameo : BaseTalisman
{
    public override int LabelNumber => 1156301;

    [Constructible]
    public EnchantressCameo() : base(0x2F5B)
    {
        Hue = 1645;
        Attributes.BonusStr = 1;
        Attributes.RegenHits = 2;
        Attributes.AttackChance = 10;
        Attributes.WeaponSpeed = 5;
        Attributes.WeaponDamage = 20;

        Slayer = (TalismanSlayerName)Utility.RandomList(11, 13, 14, 15, 16, 17);
    }
}

[SerializationGenerator(0, false)]
public partial class GrugorsShield : WoodenShield
{
    public override int LabelNumber => 1156129;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public GrugorsShield()
    {
        SkillBonuses.SetValues(0, SkillName.Parry, 10.0);
        Attributes.BonusStr = 10;
        Attributes.BonusStam = 10;
        Attributes.RegenHits = 5;
        Attributes.WeaponSpeed = 10;

        PhysicalBonus = 4;
        FireBonus = 4;
        ColdBonus = 4;
        PoisonBonus = 4;
        EnergyBonus = 3;
    }
}

[SerializationGenerator(0, false)]
public partial class GrugorsShieldGargoyle : GargishWoodenShield
{
    public override int LabelNumber => 1156129;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public GrugorsShieldGargoyle()
    {
        SkillBonuses.SetValues(0, SkillName.Parry, 10.0);
        Attributes.BonusStr = 10;
        Attributes.BonusStam = 10;
        Attributes.RegenHits = 5;
        Attributes.WeaponSpeed = 10;

        PhysicalBonus = 4;
        FireBonus = 4;
        ColdBonus = 4;
        PoisonBonus = 4;
        EnergyBonus = 3;
    }
}

[SerializationGenerator(0, false)]
public partial class HalawasHuntingBow : Yumi
{
    public override int LabelNumber => 1156127;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public HalawasHuntingBow()
    {
        Slayer = SlayerName.None;
        WeaponAttributes.HitLeechMana = 50;
        Velocity = 60;
        Attributes.AttackChance = 20;
        Attributes.WeaponSpeed = 45;
    }
}

[SerializationGenerator(0, false)]
public partial class HalawasHuntingBowGargoyle : Cyclone
{
    public override int LabelNumber => 1156127;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public HalawasHuntingBowGargoyle()
    {
        Slayer = SlayerName.None;
        WeaponAttributes.HitLeechMana = 50;
        Velocity = 60;
        Attributes.AttackChance = 20;
        Attributes.WeaponSpeed = 45;
    }
}

[SerializationGenerator(0, false)]
public partial class HawkwindsRobe : BaseOuterTorso
{
    public override int LabelNumber => 1156299;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public HawkwindsRobe() : base(0x7816, 0)
    {
        Attributes.RegenMana = 2;
        Attributes.SpellDamage = 5;
        Attributes.LowerManaCost = 10;
        Attributes.LowerRegCost = 10;
    }
}

[SerializationGenerator(0, false)]
public partial class JumusSacredHide : FurCape
{
    public override int LabelNumber => 1156130;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public JumusSacredHide()
    {
        Attributes.SpellDamage = 5;
        Attributes.CastRecovery = 1;
        Attributes.WeaponDamage = 20;

        Resistances.Fire = 5;
    }
}

[SerializationGenerator(0, false)]
public partial class JumusSacredHideGargoyle : GargishLeatherWingArmor
{
    public override int LabelNumber => 1156130;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override int FireResistance => 5;

    [Constructible]
    public JumusSacredHideGargoyle()
    {
        Attributes.SpellDamage = 5;
        Attributes.CastRecovery = 1;
        Attributes.WeaponDamage = 20;

    }
}

[SerializationGenerator(0, false)]
public partial class JuonarsGrimoire : NecromancerSpellbook
{
    public override int LabelNumber => 1156300;

    [Constructible]
    public JuonarsGrimoire()
    {
        Hue = 2500;

        SkillBonuses.SetValues(0, SkillName.Necromancy, 15.0);
        Slayer = SlayerName.None;

        Attributes.BonusInt = 8;
        Attributes.SpellDamage = 15;
        Attributes.LowerManaCost = 10;
        Attributes.LowerRegCost = 10;
    }
}

[SerializationGenerator(0, false)]
public partial class LereisHuntingSpear : Spear
{
    public override int LabelNumber => 1156128;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public LereisHuntingSpear()
    {
        Slayer = SlayerName.ReptilianDeath;
        WeaponAttributes.HitLeechMana = 50;
        Attributes.AttackChance = 20;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 60;

        AosElementDamages.Poison = 100;
    }
}

[SerializationGenerator(0, false)]
public partial class LereisHuntingSpearGargoyle : DualPointedSpear
{
    public override int LabelNumber => 1156128;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public LereisHuntingSpearGargoyle()
    {
        Slayer = SlayerName.ReptilianDeath;
        WeaponAttributes.HitLeechMana = 50;
        Attributes.AttackChance = 20;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 60;

        AosElementDamages.Poison = 100;
    }
}

[SerializationGenerator(0, false)]
public partial class MinaxsSandles : Sandals
{
    public override int LabelNumber => 1156297; // Minax's Sandles
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public MinaxsSandles()
    {
        Hue = 1645;
        Attributes.Luck = 150;
        Attributes.LowerManaCost = 5;
        Attributes.LowerRegCost = 10;

        switch (Utility.Random(5))
        {
            case 0: Resistances.Physical = -3; break;
            case 1: Resistances.Fire = -3; break;
            case 2: Resistances.Cold = -3; break;
            case 3: Resistances.Poison = -3; break;
            case 4: Resistances.Energy = -3; break;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class MinaxsSandlesGargoyle : LeatherTalons
{
    public override int LabelNumber => 1156297; // Minax's Sandles
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public MinaxsSandlesGargoyle()
    {
        Hue = 1645;
        Attributes.Luck = 150;
        Attributes.LowerManaCost = 5;
        Attributes.LowerRegCost = 10;

        switch (Utility.Random(5))
        {
            case 0: Resistances.Physical = -3; break;
            case 1: Resistances.Fire = -3; break;
            case 2: Resistances.Cold = -3; break;
            case 3: Resistances.Poison = -3; break;
            case 4: Resistances.Energy = -3; break;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class OzymandiasObi : Obi
{
    public override int LabelNumber => 1156298;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public OzymandiasObi()
    {
        Hue = 2105;
        Attributes.BonusStr = 10;
        Attributes.BonusStam = 10;
        Attributes.RegenStam = 2;
    }
}

[SerializationGenerator(0, false)]
public partial class OzymandiasObiGargoyle : GargoyleHalfApron
{
    public override int LabelNumber => 1156298;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public OzymandiasObiGargoyle()
    {
        Hue = 2105;
        Attributes.BonusStr = 10;
        Attributes.BonusStam = 10;
        Attributes.RegenStam = 2;
    }
}

[SerializationGenerator(0, false)]
public partial class ShantysWaders : ThighBoots
{
    public override int LabelNumber => 1156296; // Shanty's Waders
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public ShantysWaders()
    {
        Attributes.AttackChance = 10;
        Attributes.DefendChance = -5;
    }
}

[SerializationGenerator(0, false)]
public partial class ShantysWadersGargoyle : LeatherTalons
{
    public override int LabelNumber => 1156296; // Shanty's Waders
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public ShantysWadersGargoyle()
    {
        Attributes.AttackChance = 10;
        Attributes.DefendChance = -5;
    }
}

[SerializationGenerator(0, false)]
public partial class TotemOfTheTribe : BaseTalisman
{
    public override int LabelNumber => 1156294;

    [Constructible]
    public TotemOfTheTribe() : base(0x2F5A)
    {
        Attributes.RegenHits = 2;
        Attributes.AttackChance = 5;
        Attributes.DefendChance = 5;
    }
}

[SerializationGenerator(0, false)]
public partial class WamapsBoneEarrings : GoldEarrings
{
    public override int LabelNumber => 1156132;

    [Constructible]
    public WamapsBoneEarrings() => Hue = 2955;
}

[SerializationGenerator(0, false)]
public partial class WamapsBoneEarringsGargoyle : GargishEarrings
{
    public override int LabelNumber => 1156132;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public WamapsBoneEarringsGargoyle() => Hue = 2955;
}

[SerializationGenerator(0, false)]
public partial class UnstableTimeRift : Item
{
    public override int LabelNumber => 1156320; // An Unstable Time Rift

    [Constructible]
    public UnstableTimeRift() : base(14068)
    {
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (m.InRange(GetWorldLocation(), 3))
        {
            LabelTo(m, 1156321); // *You peer into the Time Rift and see back to the very beginning of Time...*
        }
    }
}

[TypeAlias("Server.Items.MocapotilsObsidianSword")]
[SerializationGenerator(0, false)]
public partial class MocapotlsObsidianSword : PaladinSword
{
    public override int LabelNumber => 1156131; // Moctapotl's Obsidian Sword
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    [Constructible]
    public MocapotlsObsidianSword()
    {
        WeaponAttributes.HitHarm = 50;
        WeaponAttributes.HitPhysicalArea = 50;
        WeaponAttributes.HitLeechStam = 100;
        Attributes.WeaponSpeed = 40;
        Attributes.WeaponDamage = 75;

        Hue = 1932;
    }
}
