using ModernUO.Serialization;

namespace Server.Mobiles;

// Ported from real OSI/ServUO content (Scripts/Mobiles/Normal/WildTiger.cs,
// SilverbackGorilla.cs, DesertScorpion.cs, and Scripts/Quests/Eodon/Valley of One
// Quest/Creatures.cs for GreatApe) — see Mobiles/Monsters/Reptile/Eodon/Dinosaurs.cs
// header for why this replaced an earlier from-scratch bestiary.
// Simplifications: WildTiger's pelt-carving drop (TigerPelt/WhiteTigerPelt/BlackTigerPelt)
// and rare "peculiar seed" loot table are dropped — neither the pelt item types nor
// LootPack.PeculiarSeed1-4 exist in this codebase. SilverbackGorilla's and GreatApe's
// elaborate OnThink specials (banana-throw, teleport ambush, player-toss, barrel-throw) rely
// on RunUO-era `IPooledEnumerable`/`ColUtility` helpers this codebase replaced with a
// different enumerable API, and on a "GreatApeLair" region that doesn't exist here — dropped
// rather than reworked; both remain fully playable melee bosses without the flavor mechanic.
[SerializationGenerator(0, false)]
public partial class WildTiger : BaseMount
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _canRide;

    [Constructible]
    public WildTiger() : this("дикий тигр")
    {
    }

    protected WildTiger(string name) : base(Utility.RandomList(1254, 1255), 16071, AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = name;

        if (Body == 1255)
        {
            ItemID = 16072;
        }

        SetStr(496, 554);
        SetDex(88, 124);
        SetInt(94, 163);

        SetHits(352, 450);

        SetDamage(18, 24);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 56, 75);
        SetResistance(ResistanceType.Fire, 21, 40);
        SetResistance(ResistanceType.Cold, 55, 64);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 25, 35);

        SetSkill(SkillName.MagicResist, 90.8, 97.5);
        SetSkill(SkillName.Anatomy, 0);
        SetSkill(SkillName.Tactics, 100.2, 102.5);
        SetSkill(SkillName.Wrestling, 90.1, 94.4);

        Fame = 11000;
        Karma = -11000;

        Tamable = true;
        ControlSlots = 2;
        MinTameSkill = 95.1;
    }

    public override string CorpseName => "труп тигра";

    public override void OnDoubleClick(Mobile from)
    {
        if (CanRide)
        {
            base.OnDoubleClick(from);
        }
        else if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            Backpack?.DisplayTo(from);
        }
    }

    public override int GetIdleSound() => 0x673;
    public override int GetAngerSound() => 0x670;
    public override int GetHurtSound() => 0x672;
    public override int GetDeathSound() => 0x671;

    public override int Meat => 2;
    public override FoodType FavoriteFood => FoodType.Meat;
    public override int TreasureMapLevel => 1;

    public override void GenerateLoot() => AddLoot(LootPack.Rich, 1);
}

[SerializationGenerator(0, false)]
public partial class WildWhiteTiger : WildTiger
{
    [Constructible]
    public WildWhiteTiger() : base("белый тигр") => Hue = 2500;
}

[SerializationGenerator(0, false)]
public partial class WildBlackTiger : WildTiger
{
    [Constructible]
    public WildBlackTiger() : base("чёрный тигр") => Hue = 1175;
}

[SerializationGenerator(0, false)]
public partial class SilverbackGorilla : BaseCreature
{
    [Constructible]
    public SilverbackGorilla() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = "серебристая горилла";
        Body = 0x1D;
        BaseSoundID = 0x9E;

        SetStr(79, 106);
        SetDex(77, 91);
        SetInt(16, 29);

        SetDamage(5, 10);

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

        Fame = 5000;
        Karma = -5000;
    }

    public override string CorpseName => "труп гориллы";

    public override int Meat => 1;
    public override int Hides => 7;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies;

    public override void GenerateLoot() => PackGold(60, 70);
}

[SerializationGenerator(0, false)]
public partial class GreatApe : BaseCreature
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _teleports;

    [Constructible]
    public GreatApe() : this(false)
    {
    }

    // Matches ServUO's `GreatApe(bool teleports = false)` — Eodon.xml's XmlSpawner entry
    // "greatape,true" passes this as a constructor argument via the "TypeName,arg" syntax.
    [Constructible]
    public GreatApe(bool teleports) : base(AIType.AI_Melee, FightMode.Closest)
    {
        _teleports = teleports;

        Name = "большая обезьяна";
        Body = 1308;
        BaseSoundID = 0x9E;

        SetStr(986, 1185);
        SetDex(177, 255);
        SetInt(151, 250);

        SetHits(12500);

        SetDamage(20, 33);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 65, 80);
        SetResistance(ResistanceType.Fire, 60, 80);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.Anatomy, 25.1, 50.0);
        SetSkill(SkillName.MagicResist, 150.0);
        SetSkill(SkillName.Tactics, 100);
        SetSkill(SkillName.Wrestling, 100);

        Fame = 35000;
        Karma = -35000;
    }

    public override string CorpseName => "труп гориллы";

    public override bool AutoDispel => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override int TreasureMapLevel => 7;

    public override void GenerateLoot() => AddLoot(LootPack.SuperBoss, 2);
}

[SerializationGenerator(0, false)]
public partial class DesertScorpion : BaseCreature
{
    [Constructible]
    public DesertScorpion() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = "пустынный скорпион";
        Body = 0x2CD;
        BaseSoundID = 397;

        SetStr(600, 700);
        SetDex(120, 128);
        SetInt(150, 200);

        SetDamage(15, 25);

        SetHits(350, 400);

        SetResistance(ResistanceType.Physical, 30, 40);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 10, 20);
        SetResistance(ResistanceType.Poison, 70, 80);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetDamageType(ResistanceType.Physical, 100);

        SetSkill(SkillName.MagicResist, 60, 70);
        SetSkill(SkillName.Tactics, 80, 90);
        SetSkill(SkillName.Wrestling, 50, 60);
        SetSkill(SkillName.Poisoning, 110, 120);

        Fame = 8100;
        Karma = -8100;
    }

    public override string CorpseName => "труп скорпиона";

    public override Poison HitPoison => Poison.Lethal;
    public override Poison PoisonImmune => Poison.Lethal;
    public override int Meat => 3;
    public override int TreasureMapLevel => 1;

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich, 1);
}
