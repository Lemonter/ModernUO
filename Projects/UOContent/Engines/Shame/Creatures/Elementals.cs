using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (Mobiles/Creatures.cs) — the air, flame and water
///     elementals.
///
///     ServUO's <c>SetSpecialAbility(DragonBreath)</c> on the flame elemental comes through
///     <see cref="GetMonsterAbilities" /> as <see cref="MonsterAbilities.FireBreath" />, and its
///     <c>SetAreaEffect(AreaEffect.AuraDamage)</c> / <c>IAuraCreature</c> pair is BaseCreature's own
///     built-in aura (HasAura / Aura*Damage / AuraEffect) — the same conversion FireDaemon uses, which
///     keeps the "the intense heat is damaging you" message. Everything else — bodies, hues, stats,
///     pack reagents — is unchanged.
/// </summary>
[SerializationGenerator(0, false)]
public partial class GreaterAirElemental : AirElemental
{
    [Constructible]
    public GreaterAirElemental()
    {
        SetStr(250, 315);
        SetHits(800, 900);
        SetDamage(15, 17);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Cold, 40);
        SetDamageType(ResistanceType.Energy, 40);

        SetResistance(ResistanceType.Physical, 75, 85);
        SetResistance(ResistanceType.Fire, 55, 65);
        SetResistance(ResistanceType.Cold, 55, 65);
        SetResistance(ResistanceType.Poison, 55, 65);
        SetResistance(ResistanceType.Energy, 45, 55);

        SetSkill(SkillName.MagicResist, 100, 120);
        SetSkill(SkillName.Tactics, 100, 120);
        SetSkill(SkillName.Wrestling, 100, 120);
        SetSkill(SkillName.Magery, 100, 120);
        SetSkill(SkillName.EvalInt, 100, 120);

        Fame = 4500;
        Karma = -4500;
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.10 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal());
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }
}

[SerializationGenerator(0, false)]
public partial class LesserWindElemental : BaseCreature
{
    public override string CorpseName => "труп воздушного элементаля";

    [Constructible]
    public LesserWindElemental() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Name = "воздушный элементаль";
        Body = 13;
        BaseSoundID = 655;
        Hue = 33765;

        SetStr(370, 460);
        SetDex(160, 250);
        SetInt(150, 220);

        SetHits(2500, 2600);
        SetMana(1000, 1300);

        SetDamage(15, 17);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Cold, 40);
        SetDamageType(ResistanceType.Energy, 40);

        SetResistance(ResistanceType.Physical, 65, 75);
        SetResistance(ResistanceType.Fire, 55, 65);
        SetResistance(ResistanceType.Cold, 55, 65);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 60, 75);

        SetSkill(SkillName.MagicResist, 60, 80);
        SetSkill(SkillName.Tactics, 60, 80.0);
        SetSkill(SkillName.Wrestling, 60, 80);
        SetSkill(SkillName.Magery, 60, 80);
        SetSkill(SkillName.EvalInt, 60, 80);

        Fame = 3500;
        Karma = -3500;
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.10 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal());
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }
}

[SerializationGenerator(0, false)]
public partial class LesserFlameElemental : BaseCreature
{
    public override string CorpseName => "труп элементаля пламени";

    [Constructible]
    public LesserFlameElemental() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Name = "элементаль пламени";
        Body = 15;
        BaseSoundID = 838;
        Hue = 1161;

        SetStr(420, 460);
        SetDex(160, 210);
        SetInt(120, 190);

        SetHits(700, 800);
        SetMana(1000, 1200);

        SetDamage(13, 15);

        SetDamageType(ResistanceType.Physical, 25);
        SetDamageType(ResistanceType.Fire, 75);

        SetResistance(ResistanceType.Physical, 40, 60);
        SetResistance(ResistanceType.Fire, 100);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetSkill(SkillName.MagicResist, 90, 140);
        SetSkill(SkillName.Tactics, 90, 130.0);
        SetSkill(SkillName.Wrestling, 90, 120);
        SetSkill(SkillName.Magery, 100, 145);
        SetSkill(SkillName.EvalInt, 90, 140);
        SetSkill(SkillName.Meditation, 80, 120);
        SetSkill(SkillName.Parry, 100, 120);

        Fame = 3500;
        Karma = -3500;

        PackItem(new SulfurousAsh(5));
    }

    public override int TreasureMapLevel => 2;

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.FireBreath];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override bool HasAura => true;
    public override int AuraBaseDamage => 5;
    public override int AuraRange => 4;

    public override void AuraEffect(Mobile m)
    {
        m.SendLocalizedMessage(1008112); // The intense heat is damaging you!
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.10 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal());
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }
}

[SerializationGenerator(0, false)]
public partial class GreaterWaterElemental : WaterElemental
{
    [Constructible]
    public GreaterWaterElemental()
    {
        SetStr(400, 500);
        SetDex(150, 160);
        SetInt(120, 140);

        SetHits(500, 600);
        SetMana(600, 700);

        SetDamage(14, 16);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Cold, 50);

        SetResistance(ResistanceType.Physical, 60, 70);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 70, 80);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.MagicResist, 100, 110);
        SetSkill(SkillName.Tactics, 90, 110);
        SetSkill(SkillName.Wrestling, 90, 110);
        SetSkill(SkillName.Magery, 90, 110);
        SetSkill(SkillName.EvalInt, 90, 100);

        Fame = 3500;
        Karma = -3500;
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 1);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.10 > Utility.RandomDouble() && Region.Find(c.Location, c.Map)?.IsPartOf("Shame") == true)
        {
            c.DropItem(new ShameCrystal());
        }
    }
}

[SerializationGenerator(0, false)]
public partial class ShameGreaterPoisonElemental : PoisonElemental
{
    [Constructible]
    public ShameGreaterPoisonElemental()
    {
        Hue = 32854;

        SetStr(400, 500);
        SetDex(170, 175);
        SetInt(400, 450);

        SetHits(950, 1050);

        SetDamage(16, 19);

        SetDamageType(ResistanceType.Physical, 10);
        SetDamageType(ResistanceType.Poison, 90);

        SetResistance(ResistanceType.Physical, 60, 70);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.MagicResist, 110, 120);
        SetSkill(SkillName.Tactics, 90, 120);
        SetSkill(SkillName.Wrestling, 100, 115);
        SetSkill(SkillName.Magery, 90, 110);
        SetSkill(SkillName.EvalInt, 90, 100);
        SetSkill(SkillName.Meditation, 100, 120);
        SetSkill(SkillName.DetectHidden, 85.1);
        SetSkill(SkillName.Parry, 80, 100);
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
