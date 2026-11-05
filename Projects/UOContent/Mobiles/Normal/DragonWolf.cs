using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/DragonWolf.cs). Dragon breath is a
/// MonsterAbility here (FireBreath).</summary>
[SerializationGenerator(0, false)]
public partial class DragonWolf : BaseCreature
{
    [Constructible]
    public DragonWolf() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 719;
        BaseSoundID = 0x5ED;

        SetStr(750, 850);
        SetDex(60, 75);
        SetInt(50, 55);

        SetHits(800, 860);

        SetDamage(20, 25);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.Anatomy, 60.0, 70.0);
        SetSkill(SkillName.MagicResist, 125.0, 140.0);
        SetSkill(SkillName.Tactics, 95.0, 110.0);
        SetSkill(SkillName.Wrestling, 90.0, 105.0);
        SetSkill(SkillName.DetectHidden, 60.0);

        Fame = 8500;
        Karma = -8500;

        Tamable = true;
        ControlSlots = 4;
        MinTameSkill = 102.0;
    }

    public override string CorpseName => "труп драконоволка";
    public override string DefaultName => "драконоволк";

    public override bool CanAngerOnTame => true;

    public override int Meat => 4;
    public override int Hides => 25;
    public override FoodType FavoriteFood => FoodType.Meat;

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.FireBreath];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Rich);
    }
}
