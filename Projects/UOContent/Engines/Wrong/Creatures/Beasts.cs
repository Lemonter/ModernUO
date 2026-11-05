using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Mobile/CaveTrollWrong.cs, Mobile/PrisonRat.cs and
///     Mobile/SavagePackWolf.cs) — the dungeon's non-humanoid beasts.
///
///     Only <c>[CorpseName]</c> had to change: it became a <c>CorpseName</c> override because nothing in
///     this codebase reads the attribute. The wolf also keeps its <c>[TypeAlias]</c> for the legacy
///     serialized spelling of its name.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CaveTrollWrong : BaseCreature
{
    [Constructible]
    public CaveTrollWrong() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "пещерный тролль";
        Body = Utility.RandomList(53, 54);
        BaseSoundID = 461;
        Hue = 674;

        SetStr(118, 118);
        SetDex(58, 58);
        SetInt(65, 65);

        SetHits(2136, 2136);

        SetDamage(8, 14);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 76, 76);
        SetResistance(ResistanceType.Fire, 55, 55);
        SetResistance(ResistanceType.Cold, 72, 72);
        SetResistance(ResistanceType.Poison, 75, 75);
        SetResistance(ResistanceType.Energy, 50, 50);

        SetSkill(SkillName.MagicResist, 87.4, 87.4);
        SetSkill(SkillName.Tactics, 125.8, 125.8);
        SetSkill(SkillName.Wrestling, 137.5, 137.5);

        Fame = 3500;
        Karma = -3500;

        VirtualArmor = 40;
    }

    public override string CorpseName => "труп пещерного тролля";

    public override bool CanRummageCorpses => true;
    public override int TreasureMapLevel => 2;
    public override int Meat => 2;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
    }
}

/// <summary>
///     The prison rat; see <see cref="CaveTrollWrong" /> for the corpse-name change.
/// </summary>
[SerializationGenerator(0, false)]
public partial class PrisonRat : BaseCreature
{
    [Constructible]
    public PrisonRat() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "тюремная крыса";
        Body = 0xEE;
        Hue = 443;
        BaseSoundID = 0xCC;

        SetStr(9);
        SetDex(35);
        SetInt(7, 10);

        SetHits(50);
        SetStam(25);
        SetMana(7, 10);

        SetDamage(5, 8);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 5, 10);
        SetResistance(ResistanceType.Poison, 15, 25);
        SetResistance(ResistanceType.Energy, 5, 10);

        SetSkill(SkillName.MagicResist, 3.7, 20.7);
        SetSkill(SkillName.Tactics, 6.7, 17.0);
        SetSkill(SkillName.Wrestling, 9.1, 19.5);

        Fame = 150;
        Karma = -150;

        VirtualArmor = 6;
    }

    public override string CorpseName => "труп крысы";

    public override int Meat => 1;
    public override FoodType FavoriteFood => FoodType.Fish;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Poor);
    }
}

/// <summary>
///     The savage pack wolf; see <see cref="CaveTrollWrong" /> for the corpse-name change.
/// </summary>
[TypeAlias("Server.Mobiles.SavagePackwolf")]
[SerializationGenerator(0, false)]
public partial class SavagePackWolf : BaseCreature
{
    [Constructible]
    public SavagePackWolf() : base(AIType.AI_Melee, FightMode.Weakest, 10, 1)
    {
        Name = "волк дикой стаи";
        Body = 0xE1;
        BaseSoundID = 0xE5;

        SetStr(100, 116);
        SetDex(51, 61);
        SetInt(11, 21);

        SetHits(650, 671);
        SetMana(0);

        SetDamage(9, 12);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 50, 60);

        SetSkill(SkillName.MagicResist, 60.7, 74.0);
        SetSkill(SkillName.Tactics, 80.9, 94.4);
        SetSkill(SkillName.Wrestling, 89.0, 97.1);

        Fame = 450;
        Karma = -450;

        VirtualArmor = 26;
        Tamable = false;
    }

    public override string CorpseName => "труп волка";

    public override bool AlwaysMurderer => true;
    public override int Meat => 1;
    public override int Hides => 5;
    public override FoodType FavoriteFood => FoodType.Meat;
    public override PackInstinct PackInstinct => PackInstinct.Canine;
}
