using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/BloodFox.cs). SpecialAbility.GraspingClaw
/// maps to the MonsterAbility of the same name; the weapon ability becomes GetWeaponAbility.</summary>
[SerializationGenerator(0, false)]
public partial class BloodFox : BaseCreature
{
    [Constructible]
    public BloodFox() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 0x58F;
        Female = true;

        SetStr(300, 320);
        SetDex(190, 200);
        SetInt(170, 210);

        SetHits(190, 200);

        SetDamage(16, 22);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 55, 65);
        SetResistance(ResistanceType.Poison, 25, 35);
        SetResistance(ResistanceType.Energy, 35);

        SetSkill(SkillName.MagicResist, 40.0, 50.0);
        SetSkill(SkillName.Tactics, 50.0, 70.0);
        SetSkill(SkillName.Wrestling, 75.0, 90.0);
        SetSkill(SkillName.DetectHidden, 50.0, 60.0);

        Tamable = true;
        ControlSlots = 2;
        MinTameSkill = 72.0;
    }

    public override string CorpseName => "труп кровавой лисы";
    public override string DefaultName => "кровавая лиса";

    public override int Meat => 5;
    public override FoodType FavoriteFood => FoodType.Meat;
    public override bool CanAngerOnTame => true;
    public override bool StatLossAfterTame => true;

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.GraspingClaw];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.BleedAttack;
}
