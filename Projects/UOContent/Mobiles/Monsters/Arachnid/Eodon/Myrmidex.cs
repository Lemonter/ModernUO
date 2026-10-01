using ModernUO.Serialization;

namespace Server.Mobiles;

// Ported from real OSI/ServUO content (Scripts/Mobiles/Normal/Myrmidex*.cs) — see
// Mobiles/Monsters/Reptile/Eodon/Dinosaurs.cs header for why this replaced an earlier
// from-scratch bestiary. Simplification: the "Myrmidex Invasion" event system
// (Server.Engines.MyrmidexInvasion, controls alliance-based IsEnemy overrides and the
// MyrmidexBattleground region-gated rare drops) doesn't exist in this codebase, so IsEnemy
// falls back to default aggressor behavior and the MyrmidexEggsac/MoonstoneCrystalShard rare
// drops are dropped (both item types don't exist here either).
[SerializationGenerator(0, false)]
public partial class MyrmidexLarvae : BaseCreature
{
    [Constructible]
    public MyrmidexLarvae() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = "личинка мирмидекса";
        Body = 1293;
        BaseSoundID = 959;

        SetStr(79, 100);
        SetDex(82, 95);
        SetInt(38, 75);

        SetDamage(5, 13);

        SetHits(446, 588);
        SetMana(0);

        SetResistance(ResistanceType.Physical, 20);
        SetResistance(ResistanceType.Fire, 10, 20);
        SetResistance(ResistanceType.Cold, 10, 20);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Poison, 50);

        SetSkill(SkillName.MagicResist, 30.1, 43.5);
        SetSkill(SkillName.Tactics, 30.1, 49.0);
        SetSkill(SkillName.Wrestling, 40, 50);

        Fame = 2500;
        Karma = -2500;
    }

    public override string CorpseName => "труп мирмидекса";

    public override Poison HitPoison => Poison.Lesser;
    public override Poison PoisonImmune => Poison.Lesser;
    public override int TreasureMapLevel => 1;

    public override void GenerateLoot() => PackGold(20, 40);
}

[SerializationGenerator(0, false)]
public partial class MyrmidexDrone : BaseCreature
{
    [Constructible]
    public MyrmidexDrone() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = "мирмидекс-рабочий";
        Body = 1402;
        BaseSoundID = 959;

        SetStr(76, 105);
        SetDex(96, 136);
        SetInt(25, 44);

        SetDamage(6, 12);

        SetHits(460, 597);
        SetMana(0);

        SetResistance(ResistanceType.Physical, 1, 5);
        SetResistance(ResistanceType.Fire, 1, 5);
        SetResistance(ResistanceType.Cold, 1, 5);
        SetResistance(ResistanceType.Poison, 1, 5);
        SetResistance(ResistanceType.Energy, 1, 5);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Poison, 50);

        SetSkill(SkillName.MagicResist, 30.1, 43.5);
        SetSkill(SkillName.Tactics, 30.1, 49.0);
        SetSkill(SkillName.Wrestling, 41.1, 49.8);

        Fame = 2500;
        Karma = -2500;
    }

    public override string CorpseName => "труп мирмидекса";

    public override int Meat => 4;
    public override Poison HitPoison => Poison.Regular;
    public override Poison PoisonImmune => Poison.Regular;
    public override int TreasureMapLevel => 1;

    public override void GenerateLoot() => PackGold(50, 70);
}

[SerializationGenerator(0, false)]
public partial class MyrmidexWarrior : BaseCreature
{
    [Constructible]
    public MyrmidexWarrior() : base(AIType.AI_Mage, FightMode.Closest)
    {
        Name = "мирмидекс-воин";
        Body = 1403;
        BaseSoundID = 959;

        SetStr(500, 600);
        SetDex(82, 95);
        SetInt(130, 140);

        SetDamage(18, 22);

        SetHits(2800, 3000);
        SetMana(40, 50);

        SetResistance(ResistanceType.Physical, 1, 10);
        SetResistance(ResistanceType.Fire, 1, 10);
        SetResistance(ResistanceType.Cold, 1, 10);
        SetResistance(ResistanceType.Poison, 1, 10);
        SetResistance(ResistanceType.Energy, 1, 10);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Poison, 50);

        SetSkill(SkillName.Wrestling, 90, 100);
        SetSkill(SkillName.Tactics, 90, 100);
        SetSkill(SkillName.MagicResist, 70, 80);
        SetSkill(SkillName.Poisoning, 70, 80);
        SetSkill(SkillName.Magery, 80, 90);
        SetSkill(SkillName.EvalInt, 70, 80);

        Fame = 8000;
        Karma = -8000;
    }

    public override string CorpseName => "труп мирмидекса-воина";

    public override Poison HitPoison => Poison.Deadly;
    public override Poison PoisonImmune => Poison.Deadly;
    public override int TreasureMapLevel => 2;

    public override void GenerateLoot() => AddLoot(LootPack.Rich, 2);
}
