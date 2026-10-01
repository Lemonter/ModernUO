using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>The ten void creatures, ported from ServUO (Scripts/Mobiles/Void Creatures/).
/// Korpre is what everything starts as; the other nine are the three evolution lines, three
/// stages each. See BaseVoidCreature.cs for how they turn into one another.
///
/// Every SetWeaponAbility registration in the originals is ServUO's pet-training table, which
/// this codebase does not have; where a creature had one it comes back through
/// GetWeaponAbility, which is the hook that actually drives creature abilities here.
///
/// Two of the originals' resistance blocks are visibly broken and are ported as their effective
/// result rather than as dead lines: Betballem sets Fire four times (the last wins, at 100) and
/// never sets Cold, Poison or Energy at all; Anzuanord sets Physical three times and Poison
/// twice, ending at Physical 100 and Poison 0-20. Both are noted where they occur.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a korpre corpse")]
public partial class Korpre : BaseVoidCreature
{
    [Constructible]
    public Korpre() : base(AIType.AI_Melee)
    {
        Body = 51;
        BaseSoundID = 456;
        Hue = 2071;

        SetStr(22, 34);
        SetDex(16, 21);
        SetInt(16, 20);

        SetHits(50, 60);

        SetDamage(1, 5);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 5, 10);
        SetResistance(ResistanceType.Poison, 15, 20);

        SetSkill(SkillName.Poisoning, 36.0, 49.1);
        SetSkill(SkillName.Anatomy, 0);
        SetSkill(SkillName.MagicResist, 15.9, 18.9);
        SetSkill(SkillName.Tactics, 24.6, 26.1);
        SetSkill(SkillName.Wrestling, 24.9, 26.1);

        Fame = 300;
        Karma = -300;

        VirtualArmor = 8;
    }

    public override string DefaultName => "Корпре";

    public override Poison PoisonImmune => Poison.Regular;
    public override Poison HitPoison => Poison.Regular;
    public override FoodType FavoriteFood => FoodType.Fish;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Poor);
        AddLoot(LootPack.Gems);
    }
}

#region Killing line — Betballem, Ballem, Usagralem Ballem

[SerializationGenerator(0, false)]
[CorpseName("a betballem corpse")]
public partial class Betballem : BaseVoidCreature
{
    [Constructible]
    public Betballem() : base(AIType.AI_Melee)
    {
        Body = 776;
        Hue = 2071;
        BaseSoundID = 357;

        SetStr(270);
        SetDex(890);
        SetInt(80);

        SetHits(90, 100);

        SetDamage(5, 10);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        // The original writes Fire four times over and never writes Cold, Poison or Energy;
        // this is where those writes actually land.
        SetResistance(ResistanceType.Physical, 30, 40);
        SetResistance(ResistanceType.Fire, 100);

        SetSkill(SkillName.MagicResist, 40.0, 50.0);
        SetSkill(SkillName.Tactics, 20.1, 30.0);
        SetSkill(SkillName.Wrestling, 30.1, 40.0);
        SetSkill(SkillName.Anatomy, 0.0, 10.0);

        Fame = 500;
        Karma = -500;

        VirtualArmor = 38;

        AddItem(new LightSource());

        PackItem(new FertileDirt(Utility.RandomMinMax(1, 4)));
        PackItem(new DaemonBone(5));
    }

    public override string DefaultName => "бетбаллем";

    public override VoidEvolution Evolution => VoidEvolution.Killing;
    public override int Stage => 1;

    public override bool Unprovokable => true;
    public override bool BardImmune => true;
    public override bool CanRummageCorpses => true;
    public override bool BleedImmune => true;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
        AddLoot(LootPack.Meager);
        AddLoot(LootPack.Gems);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Utility.RandomDouble() < 0.10)
        {
            c.DropItem(new AncientPotteryFragments());
        }
    }

    public override int GetIdleSound() => 338;
    public override int GetAngerSound() => 338;
    public override int GetDeathSound() => 338;
    public override int GetAttackSound() => 406;
    public override int GetHurtSound() => 194;
}

[SerializationGenerator(0, false)]
[CorpseName("a Ballem corpse")]
public partial class Ballem : BaseVoidCreature
{
    [Constructible]
    public Ballem() : base(AIType.AI_Melee)
    {
        Body = 304;
        Hue = 2071;
        BaseSoundID = 684;

        SetStr(991);
        SetDex(1001);
        SetInt(243);

        SetHits(500, 600);

        SetDamage(10, 15);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 30, 50);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 20, 30);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 30, 40);

        SetSkill(SkillName.MagicResist, 70.0, 80.0);
        SetSkill(SkillName.Tactics, 50.1, 60.0);
        SetSkill(SkillName.Wrestling, 70.1, 80.0);
        SetSkill(SkillName.Anatomy, 0.0, 10.0);

        Fame = 1800;
        Karma = -1800;

        VirtualArmor = 54;

        PackItem(new DaemonBone(15));
    }

    public override string DefaultName => "баллем";

    public override VoidEvolution Evolution => VoidEvolution.Killing;
    public override int Stage => 2;

    public override Poison PoisonImmune => Poison.Lethal;
    public override bool Unprovokable => true;
    public override bool BardImmune => true;
    public override bool CanRummageCorpses => true;
    public override bool BleedImmune => true;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.CrushingBlow;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
        AddLoot(LootPack.Average);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Utility.RandomDouble() < 0.10)
        {
            c.DropItem(new AncientPotteryFragments());
        }
    }
}

[SerializationGenerator(0, false)]
[CorpseName("an usagralem ballem corpse")]
public partial class UsagralemBallem : BaseVoidCreature
{
    [Constructible]
    public UsagralemBallem() : base(AIType.AI_Melee)
    {
        Body = 318;
        Hue = 2071;
        BaseSoundID = 0x165;

        SetStr(900, 1000);
        SetDex(1028);
        SetInt(1000, 1100);

        SetHits(2000, 2200);
        SetMana(5000);

        SetDamage(17, 21);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 30, 40);
        SetResistance(ResistanceType.Fire, 40, 60);
        SetResistance(ResistanceType.Cold, 40, 60);
        SetResistance(ResistanceType.Poison, 40, 60);
        SetResistance(ResistanceType.Energy, 40, 60);

        SetSkill(SkillName.MagicResist, 80.0, 90.0);
        SetSkill(SkillName.Tactics, 80.0, 90.0);
        SetSkill(SkillName.Wrestling, 80.0, 90.0);

        Fame = 18000;
        Karma = -18000;

        VirtualArmor = 64;

        PackItem(new DaemonBone(30));
    }

    public override string DefaultName => "усагралем баллем";

    public override VoidEvolution Evolution => VoidEvolution.Killing;
    public override int Stage => 3;

    public override bool IgnoreYoungProtection => Core.ML;
    public override bool BardImmune => !Core.SE;
    public override bool Unprovokable => Core.SE;
    public override bool AreaPeaceImmune => Core.SE;
    public override Poison PoisonImmune => Poison.Lethal;

    public override WeaponAbility GetWeaponAbility() =>
        Utility.Random(3) switch
        {
            0 => WeaponAbility.DoubleStrike,
            1 => WeaponAbility.WhirlwindAttack,
            _ => WeaponAbility.CrushingBlow
        };

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 1);
        AddLoot(LootPack.FilthyRich, 2);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Utility.RandomDouble() < 0.30)
        {
            c.DropItem(new AncientPotteryFragments());
        }
    }
}

#endregion

#region Grouping line — Anlorzen, Anlorlem, Anlorvaglem

[SerializationGenerator(0, false)]
[CorpseName("an anlorzen corpse")]
public partial class Anlorzen : BaseVoidCreature
{
    [Constructible]
    public Anlorzen() : base(AIType.AI_Melee)
    {
        Body = 11;
        BaseSoundID = 1170;

        SetStr(600, 750);
        SetDex(666, 800);
        SetInt(850, 1000);

        SetHits(300, 400);

        SetDamage(15, 18);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 40, 50);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 30, 50);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 40, 60);

        SetSkill(SkillName.MagicResist, 30.1, 60.0);
        SetSkill(SkillName.Tactics, 30.1, 70.0);
        SetSkill(SkillName.Wrestling, 50.1, 70.0);

        Fame = 5000;
        Karma = -5000;

        VirtualArmor = 56;

        PackItem(new DaemonBone(5));
    }

    public override string DefaultName => "анлорзен";

    public override VoidEvolution Evolution => VoidEvolution.Grouping;
    public override int Stage => 1;

    public override Poison PoisonImmune => Poison.Lethal;
    public override Poison HitPoison => Poison.Lethal;
    public override bool BardImmune => true;

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich);
}

[SerializationGenerator(0, false)]
[CorpseName("an Anlorlem corpse")]
public partial class Anlorlem : BaseVoidCreature
{
    [Constructible]
    public Anlorlem() : base(AIType.AI_Mage)
    {
        Body = 72;
        Hue = 2071;
        BaseSoundID = 644;

        SetStr(900, 1000);
        SetDex(1000, 1200);
        SetInt(900, 950);

        SetHits(500, 650);

        SetDamage(18, 22);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Poison, 50);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 35, 45);
        SetResistance(ResistanceType.Poison, 90, 100);
        SetResistance(ResistanceType.Energy, 35, 45);

        SetSkill(SkillName.MagicResist, 40.1, 70.0);
        SetSkill(SkillName.Tactics, 90.1, 100.0);
        SetSkill(SkillName.Wrestling, 90.1, 100.0);

        Fame = 16000;
        Karma = -16000;

        VirtualArmor = 50;

        PackItem(new DaemonBone(15));
    }

    public override string DefaultName => "анлорлем";

    public override VoidEvolution Evolution => VoidEvolution.Grouping;
    public override int Stage => 2;

    public override int TreasureMapLevel => 3;
    public override bool BardImmune => !Core.AOS;
    public override bool Unprovokable => true;
    public override bool ReacquireOnMovement => true;
    public override Poison PoisonImmune => Poison.Greater;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich);
        AddLoot(LootPack.Average, 2);
        AddLoot(LootPack.MedScrolls, 2);
    }
}

[SerializationGenerator(0, false)]
[CorpseName("an anlorvaglem corpse")]
public partial class Anlorvaglem : BaseVoidCreature
{
    [Constructible]
    public Anlorvaglem() : base(AIType.AI_Melee)
    {
        Body = 152;
        Hue = 2071;

        SetStr(1000, 1200);
        SetDex(1000, 1200);
        SetInt(100, 1200);

        SetHits(3205);

        SetDamage(11, 13);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 20, 50);
        SetResistance(ResistanceType.Fire, 20, 60);
        SetResistance(ResistanceType.Cold, 20, 58);
        SetResistance(ResistanceType.Poison, 80, 100);
        SetResistance(ResistanceType.Energy, 30, 50);

        SetSkill(SkillName.Wrestling, 75.8, 100.0);
        SetSkill(SkillName.Tactics, 50.0, 100.0);
        SetSkill(SkillName.MagicResist, 50.9, 90.0);

        Fame = 8000;
        Karma = -8000;

        VirtualArmor = 48;

        PackItem(new DaemonBone(30));
    }

    public override string DefaultName => "анлорваглем";

    public override VoidEvolution Evolution => VoidEvolution.Grouping;
    public override int Stage => 3;

    public override Poison PoisonImmune => Poison.Lethal;
    public override bool Unprovokable => true;
    public override bool BardImmune => true;
    public override bool ReacquireOnMovement => true;

    public override void GenerateLoot() => AddLoot(LootPack.UltraRich);
}

#endregion

#region Survival line — Anzuanord, Relanord, Vasanord

[SerializationGenerator(0, false)]
[CorpseName("an anzuanord corpse")]
public partial class Anzuanord : BaseVoidCreature
{
    [Constructible]
    public Anzuanord() : base(AIType.AI_Mage)
    {
        Body = 74;
        Hue = 2071;
        BaseSoundID = 422;

        SetStr(705);
        SetDex(900, 910);
        SetInt(900, 1000);

        SetHits(180);

        SetDamage(8, 10);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        // The original writes Physical three times and Poison twice; these are the values that
        // survive, and it never writes Fire, Cold or Energy at all.
        SetResistance(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Poison, 0, 20);

        SetSkill(SkillName.Anatomy, 5.0, 10.0);
        SetSkill(SkillName.MagicResist, 40.0, 50.0);
        SetSkill(SkillName.Tactics, 40.0, 50.0);
        SetSkill(SkillName.Wrestling, 40.0, 50.0);
        SetSkill(SkillName.Magery, 70.0, 80.0);
        SetSkill(SkillName.EvalInt, 80.0, 90.0);
        SetSkill(SkillName.Meditation, 50.0, 60.0);

        Fame = 2500;
        Karma = -2500;

        VirtualArmor = 50;

        PackItem(new DaemonBone(5));
    }

    public override string DefaultName => "анзуанорд";

    public override VoidEvolution Evolution => VoidEvolution.Survival;
    public override int Stage => 1;

    public override bool BardImmune => true;
    public override int Meat => 1;
    public override int Hides => 7;
    public override HideType HideType => HideType.Spined;
    public override FoodType FavoriteFood => FoodType.Meat;
    public override PackInstinct PackInstinct => PackInstinct.Daemon;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
        AddLoot(LootPack.MedScrolls, 2);
    }
}

[SerializationGenerator(0, false)]
[CorpseName("a relanord corpse")]
public partial class Relanord : BaseVoidCreature
{
    [Constructible]
    public Relanord() : base(AIType.AI_Melee)
    {
        Body = 0x2F4;
        Hue = 2071;

        SetStr(700, 800);
        SetDex(60, 100);
        SetInt(60, 100);

        SetHits(400, 500);

        SetDamage(10, 15);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 45, 60);
        SetResistance(ResistanceType.Fire, 40, 60);
        SetResistance(ResistanceType.Cold, 25, 35);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 30, 60);

        SetSkill(SkillName.MagicResist, 30.2, 50.0);
        SetSkill(SkillName.Tactics, 40.2, 60.0);
        SetSkill(SkillName.Wrestling, 50.2, 70.0);

        Fame = 10000;
        Karma = -10000;

        VirtualArmor = 50;

        PackItem(new DaemonBone(15));
    }

    public override string DefaultName => "реланорд";

    public override VoidEvolution Evolution => VoidEvolution.Survival;
    public override int Stage => 2;

    public override bool AutoDispel => true;
    public override bool BardImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich, 1);

    public override int GetIdleSound() => 0xFD;
    public override int GetAngerSound() => 0x26C;
    public override int GetDeathSound() => 0x211;
    public override int GetAttackSound() => 0x23B;
    public override int GetHurtSound() => 0x140;
}

[SerializationGenerator(0, false)]
[CorpseName("a plant corpse")]
public partial class Vasanord : BaseVoidCreature
{
    [Constructible]
    public Vasanord() : base(AIType.AI_Mage)
    {
        Body = 780;

        SetStr(805, 869);
        SetDex(51, 64);
        SetInt(38, 48);

        SetHits(5000, 5200);
        SetMana(40, 70);
        SetStam(50, 80);

        SetDamage(10, 23);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 30, 50);
        SetResistance(ResistanceType.Fire, 20, 50);
        SetResistance(ResistanceType.Cold, 20, 40);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 20, 50);

        SetSkill(SkillName.MagicResist, 72.8, 77.7);
        SetSkill(SkillName.Tactics, 50.7, 110.0);
        SetSkill(SkillName.EvalInt, 99.5, 120.0);
        SetSkill(SkillName.Magery, 95.5, 106.9);
        SetSkill(SkillName.Wrestling, 53.6, 98.6);

        Fame = 15000;
        Karma = -15000;

        VirtualArmor = 28;

        PackItem(new DaemonBone(30));
    }

    public override string DefaultName => "васанорд";

    public override VoidEvolution Evolution => VoidEvolution.Survival;
    public override int Stage => 3;

    public override bool BardImmune => !Core.AOS;
    public override Poison PoisonImmune => Poison.Lethal;

    public override void GenerateLoot() => AddLoot(LootPack.UltraRich, 2);

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        // The original writes `new TaintedSeeds(2)`, but that item is not stackable and takes
        // no amount — in ServUO the 2 binds to the deserialization constructor's Serial, so it
        // drops one broken item. Two seeds is plainly what was meant.
        if (Utility.RandomDouble() < 0.6)
        {
            c.DropItem(new TaintedSeeds());
            c.DropItem(new TaintedSeeds());
        }
    }
}

#endregion
