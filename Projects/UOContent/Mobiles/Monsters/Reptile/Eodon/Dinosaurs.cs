using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

// Ported from real OSI/ServUO content (github.com/ServUO/ServUO, Scripts/Mobiles/Normal/*.cs
// and Scripts/Quests/Eodon/Valley of One Quest/Creatures.cs).
// ServUO's per-creature weapon abilities come back through GetWeaponAbility; where the original
// registered several, one is picked at random per swing, which is exactly what ServUO's ability
// profile did. Its SetSpecialAbility/SetAreaEffect registrations map onto the monster-ability
// framework: GraspingClaw and TailSwipe keep their names, LifeLeech becomes DrainLifeAttack, and
// AreaEffect.PoisonBreath becomes PoisonGasAreaAttack. `AttacksFocus`,
// `IsChampionSpawn`/`SetToChampionSpawn`, `DragonBlood`, and TRex's elaborate "freeze wall"
// OnThink spectacle (MovementPath-based, RunUO-era `IPooledEnumerable`) are dropped —
// all flavor/rare-mechanic loss, not core combat stats.
[SerializationGenerator(0, false)]
public partial class Anchisaur : BaseCreature
{
    [Constructible]
    public Anchisaur() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = "анхизавр";
        Body = 1292;
        BaseSoundID = 422;

        SetStr(441, 511);
        SetDex(166, 185);
        SetInt(362, 431);

        SetDamage(16, 19);

        SetHits(2663, 3718);

        SetResistance(ResistanceType.Physical, 3, 4);
        SetResistance(ResistanceType.Fire, 3, 4);
        SetResistance(ResistanceType.Cold, 1);
        SetResistance(ResistanceType.Poison, 2, 3);
        SetResistance(ResistanceType.Energy, 2, 3);

        SetDamageType(ResistanceType.Physical, 100);

        SetSkill(SkillName.MagicResist, 105.0, 115.0);
        SetSkill(SkillName.Tactics, 95.0, 105.0);
        SetSkill(SkillName.Wrestling, 100.0, 110.0);
        SetSkill(SkillName.Anatomy, 95.0, 105.0);
        SetSkill(SkillName.DetectHidden, 75.0, 85.0);
        SetSkill(SkillName.Parry, 75.0, 85.0);

        Fame = 8000;
        Karma = -8000;
    }

    public override string CorpseName => "труп анхизавра";

    public override WeaponAbility GetWeaponAbility() => Utility.Random(2) switch
    {
        0 => WeaponAbility.Disarm,
        _ => WeaponAbility.ParalyzingBlow
    };
    public override int Meat => 6;
    public override MeatType MeatType => MeatType.Ribs;
    public override int Hides => 11;
    public override int TreasureMapLevel => 1;

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich, 1);
}

[SerializationGenerator(0, false)]
public partial class Archaeosaurus : BaseCreature
{
    [Constructible]
    public Archaeosaurus() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = "археозавр";
        Body = 1287;
        BaseSoundID = 422;

        SetStr(405, 421);
        SetDex(301, 320);
        SetInt(201, 224);

        SetDamage(14, 16);

        SetHits(1818, 2500);

        SetResistance(ResistanceType.Physical, 2, 3);
        SetResistance(ResistanceType.Fire, 4, 5);
        SetResistance(ResistanceType.Cold, 2, 3);
        SetResistance(ResistanceType.Poison, 3, 4);
        SetResistance(ResistanceType.Energy, 3);

        SetDamageType(ResistanceType.Poison, 50);
        SetDamageType(ResistanceType.Fire, 50);

        SetSkill(SkillName.MagicResist, 100.0, 115.0);
        SetSkill(SkillName.Tactics, 90.0, 110.0);
        SetSkill(SkillName.Wrestling, 90.0, 110.0);
        SetSkill(SkillName.DetectHidden, 60.0, 70.0);
        SetSkill(SkillName.EvalInt, 95.0, 105.0);
        SetSkill(SkillName.Ninjitsu, 120.0);

        Fame = 8100;
        Karma = -8100;
    }

    public override string CorpseName => "труп археозавра";

    public override WeaponAbility GetWeaponAbility() => Utility.Random(2) switch
    {
        0 => WeaponAbility.BleedAttack,
        _ => WeaponAbility.TalonStrike
    };
    public override int Meat => 1;
    public override MeatType MeatType => MeatType.Ribs;
    public override int Hides => 7;
    public override int TreasureMapLevel => 1;

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich, 2);
}

[SerializationGenerator(0, false)]
public partial class Dimetrosaur : BaseCreature
{
    [Constructible]
    public Dimetrosaur() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = "диметрозавр";
        Body = 1285;

        SetStr(526, 601);
        SetDex(166, 184);
        SetInt(373, 435);

        SetDamage(18, 21);
        SetHits(5300, 5400);

        SetResistance(ResistanceType.Physical, 80, 90);
        SetResistance(ResistanceType.Fire, 60, 70);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 65, 75);
        SetResistance(ResistanceType.Energy, 65, 75);

        SetDamageType(ResistanceType.Physical, 90);
        SetDamageType(ResistanceType.Poison, 10);

        SetSkill(SkillName.MagicResist, 120.0, 140.0);
        SetSkill(SkillName.Tactics, 100.0, 120.0);
        SetSkill(SkillName.Wrestling, 115.0, 125.0);
        SetSkill(SkillName.Anatomy, 70.0, 80.0);
        SetSkill(SkillName.Poisoning, 85.0, 95.0);
        SetSkill(SkillName.DetectHidden, 70.0, 80.0);
        SetSkill(SkillName.Parry, 95.0, 105.0);

        Fame = 17000;
        Karma = -17000;

        Tamable = true;
        ControlSlots = 3;
        MinTameSkill = 102.0;
    }

    public override string CorpseName => "труп диметрозавра";

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.PoisonGasAreaAttack];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override WeaponAbility GetWeaponAbility() => Utility.Random(2) switch
    {
        0 => WeaponAbility.MortalStrike,
        _ => WeaponAbility.Dismount
    };

    public override bool CanAngerOnTame => true;
    public override bool StatLossAfterTame => true;
    public override int Meat => 1;
    public override MeatType MeatType => MeatType.Ribs;
    public override int Hides => 11;
    public override HideType HideType => HideType.Spined;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies;
    public override int TreasureMapLevel => 6;

    public override void GenerateLoot() => AddLoot(LootPack.UltraRich, 2);
}

[SerializationGenerator(0, false)]
public partial class Gallusaurus : BaseCreature
{
    [Constructible]
    public Gallusaurus() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = "галлюзавр";
        Body = 1286;
        BaseSoundID = 0x275;

        SetStr(477, 511);
        SetDex(155, 168);
        SetInt(221, 274);

        SetDamage(11, 17);

        SetHits(700, 900);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 20, 30);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 20, 30);

        SetSkill(SkillName.MagicResist, 70.0, 80.0);
        SetSkill(SkillName.Tactics, 80.0, 90.0);
        SetSkill(SkillName.Wrestling, 80.0, 91.0);
        SetSkill(SkillName.Bushido, 110.0, 120.0);
        SetSkill(SkillName.DetectHidden, 25.0, 35.0);

        Fame = 8100;
        Karma = -8100;

        Tamable = true;
        ControlSlots = 3;
        MinTameSkill = 102.0;
    }

    public override string CorpseName => "труп галлюзавра";

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.GraspingClaw];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.Block;
    public override int Meat => 3;
    public override MeatType MeatType => MeatType.Ribs;
    public override bool CanAngerOnTame => true;
    public override int TreasureMapLevel => 1;

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich, 1);
}

[SerializationGenerator(0, false)]
public partial class Najasaurus : BaseCreature
{
    [Constructible]
    public Najasaurus() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = "наязавр";
        Body = 1289;
        BaseSoundID = 219;

        SetStr(162, 346);
        SetDex(151, 218);
        SetInt(21, 40);

        SetDamage(13, 24);
        SetHits(737, 854);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Poison, 50);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 45, 55);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 35, 45);

        SetSkill(SkillName.MagicResist, 150.0, 190.0);
        SetSkill(SkillName.Tactics, 80.0, 95.0);
        SetSkill(SkillName.Wrestling, 80.0, 100.0);
        SetSkill(SkillName.Poisoning, 90.0, 100.0);
        SetSkill(SkillName.DetectHidden, 45.0, 55.0);

        Fame = 17000;
        Karma = -17000;

        Tamable = true;
        ControlSlots = 2;
        MinTameSkill = 102.0;
    }

    public override string CorpseName => "труп наязавра";

    public override Poison HitPoison => Poison.Lethal;
    public override Poison PoisonImmune => Poison.Lethal;
    public override bool CanAngerOnTame => true;
    public override int TreasureMapLevel => 2;
    public override int Meat => 15;
    public override MeatType MeatType => MeatType.Ribs;

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich);
}

[SerializationGenerator(0, false)]
public partial class Saurosaurus : BaseCreature
{
    [Constructible]
    public Saurosaurus() : base(AIType.AI_Mage, FightMode.Closest)
    {
        Name = "заврозавр";
        Body = 1291;
        BaseSoundID = 362;

        SetStr(802, 824);
        SetDex(201, 220);
        SetInt(403, 440);

        SetDamage(21, 28);

        SetHits(1321, 1468);

        SetResistance(ResistanceType.Physical, 75, 85);
        SetResistance(ResistanceType.Fire, 80, 90);
        SetResistance(ResistanceType.Cold, 45, 55);
        SetResistance(ResistanceType.Poison, 35, 45);
        SetResistance(ResistanceType.Energy, 45, 55);

        SetDamageType(ResistanceType.Physical, 100);

        SetSkill(SkillName.MagicResist, 70.0, 90.0);
        SetSkill(SkillName.Tactics, 110.0, 120.0);
        SetSkill(SkillName.Wrestling, 110.0, 130.0);
        SetSkill(SkillName.Anatomy, 50.0, 60.0);
        SetSkill(SkillName.DetectHidden, 80.0);
        SetSkill(SkillName.Parry, 80.0, 90);
        SetSkill(SkillName.Focus, 115.0, 125.0);

        Fame = 11000;
        Karma = -11000;

        Tamable = true;
        ControlSlots = 3;
        MinTameSkill = 102.0;
    }

    public override string CorpseName => "труп заврозавра";

    private static readonly MonsterAbility[] _abilities =
    [
        MonsterAbilities.TailSwipe,
        MonsterAbilities.DrainLifeAttack
    ];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.ConcussionBlow;

    public override bool CanAngerOnTame => true;
    public override bool StatLossAfterTame => true;
    public override int Meat => 5;
    public override MeatType MeatType => MeatType.Ribs;
    public override int Hides => 11;
    public override int TreasureMapLevel => 2;

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich, 3);
}

[SerializationGenerator(0, false)]
public partial class Allosaurus : BaseCreature
{
    [Constructible]
    public Allosaurus() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = "аллозавр";
        Body = 1290;

        SetStr(699, 828);
        SetDex(200);
        SetInt(127, 150);

        SetDamage(21, 23);

        SetHits(18000);
        SetMana(48, 70);

        SetResistance(ResistanceType.Physical, 65, 75);
        SetResistance(ResistanceType.Fire, 55, 65);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 90, 100);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Fire, 50);

        SetSkill(SkillName.MagicResist, 100.0, 110.0);
        SetSkill(SkillName.Tactics, 120.0, 140.0);
        SetSkill(SkillName.Wrestling, 120.0, 150.0);
        SetSkill(SkillName.Poisoning, 50.0, 60.0);
        SetSkill(SkillName.Parry, 80.0, 90.0);
        SetSkill(SkillName.Magery, 70.0, 80.0);
        SetSkill(SkillName.EvalInt, 75.0, 85.0);

        Fame = 21000;
        Karma = -21000;
    }

    public override string CorpseName => "труп аллозавра";

    public override WeaponAbility GetWeaponAbility() => Utility.Random(3) switch
    {
        0 => WeaponAbility.ArmorPierce,
        1 => WeaponAbility.CrushingBlow,
        _ => WeaponAbility.Disarm
    };
    public override int Meat => 3;
    public override MeatType MeatType => MeatType.Ribs;
    public override int Hides => 11;
    public override HideType HideType => HideType.Horned;
    public override int TreasureMapLevel => 7;

    public override void GenerateLoot() => AddLoot(LootPack.UltraRich, 3);
}

[SerializationGenerator(0, false)]
public partial class TRex : BaseCreature
{
    [Constructible]
    public TRex() : base(AIType.AI_Melee, FightMode.Weakest)
    {
        Name = "тираннозавр";
        Body = 1400;
        BaseSoundID = 362;

        SetStr(500, 700);
        SetDex(500, 700);
        SetInt(100, 180);

        SetHits(15000);

        SetDamage(33, 55);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 80, 90);
        SetResistance(ResistanceType.Fire, 60, 80);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 80, 100);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetSkill(SkillName.Anatomy, 100.0);
        SetSkill(SkillName.MagicResist, 140.0, 150.0);
        SetSkill(SkillName.Tactics, 110.0, 130.0);
        SetSkill(SkillName.Wrestling, 130.0, 150.0);
        SetSkill(SkillName.Poisoning, 60.0, 70.0);
        SetSkill(SkillName.Parry, 100);

        Fame = 24000;
        Karma = -24000;

        CanSwim = true;
    }

    public override string CorpseName => "труп тираннозавра";

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.TailSwipe];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.ArmorIgnore;

    public override bool AutoDispel => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override bool CanFlee => false;
    public override int TreasureMapLevel => 7;

    public override void GenerateLoot() => AddLoot(LootPack.SuperBoss, 2);
}
