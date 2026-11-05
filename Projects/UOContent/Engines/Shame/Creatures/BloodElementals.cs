using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (Mobiles/Creatures.cs) — the dungeon's two blood
///     elementals.
///
///     <c>SetWeaponAbility(WeaponAbility.BleedAttack)</c> is the <see cref="GetWeaponAbility" /> override
///     and <c>SetSpecialAbility(SpecialAbility.LifeLeech)</c> is
///     <see cref="MonsterAbilities.DrainLifeAttack" /> on the diseased one. Still dropped:
///     <c>TreasureMapChance</c>
///     (not a BaseCreature member here), and a dead local (<c>int scrolls = ...</c>) that was never read.
///     ServUO's <c>Poison.Parasitic</c> is this codebase's <see cref="Poison.DeadlyParasitic" />.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DiseasedBloodElemental : BloodElemental
{
    public override string CorpseName => "заражённый кровавый элементаль";

    [Constructible]
    public DiseasedBloodElemental()
    {
        Name = "заражённый кровавый элементаль";
        Body = 0x9F;
        Hue = 1779;

        SetStr(650, 750);
        SetDex(70, 80);
        SetInt(300, 400);

        SetHits(2500, 2700);
        SetMana(1400, 1600);

        SetDamage(19, 27);

        SetDamageType(ResistanceType.Poison, 50);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 65, 75);
        SetResistance(ResistanceType.Fire, 55, 65);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 50, 60);

        SetSkill(SkillName.MagicResist, 110, 125);
        SetSkill(SkillName.Tactics, 130, 140);
        SetSkill(SkillName.Wrestling, 120, 140);
        SetSkill(SkillName.Poisoning, 100);
        SetSkill(SkillName.Magery, 110, 120);
        SetSkill(SkillName.EvalInt, 115, 130);
        SetSkill(SkillName.Meditation, 130, 155);
        SetSkill(SkillName.DetectHidden, 80.0);
        SetSkill(SkillName.Parry, 90.0, 100.0);

        PackReg(7, 11);

        Fame = 8500;
        Karma = -8500;
    }

    public override bool AutoDispel => true;
    public override double AutoDispelChance => 1.0;
    public override int TreasureMapLevel => 5;
    public override Poison HitPoison => Poison.Lethal;
    public override Poison PoisonImmune => Poison.DeadlyParasitic;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.BleedAttack;

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.DrainLifeAttack];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.33 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal(5));
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
        AddLoot(LootPack.HighScrolls, Utility.RandomMinMax(1, 8));
    }
}

[SerializationGenerator(0, false)]
public partial class GreaterBloodElemental : BloodElemental
{
    [Constructible]
    public GreaterBloodElemental()
    {
        SetStr(500, 600);
        SetDex(60, 90);
        SetInt(230, 350);

        // The original sets hits twice in a row (1350-1500, then 900-1000); only the second stands.
        SetHits(900, 1000);

        SetDamage(17, 27);

        SetDamageType(ResistanceType.Poison, 50);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 40, 50);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.MagicResist, 115, 120);
        SetSkill(SkillName.Tactics, 100, 120);
        SetSkill(SkillName.Wrestling, 110, 120);
        SetSkill(SkillName.Magery, 80, 100);
        SetSkill(SkillName.EvalInt, 110, 120);
        SetSkill(SkillName.Meditation, 120, 140);
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 1);
        AddLoot(LootPack.FilthyRich, 1);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.10 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal(5));
        }
    }
}
