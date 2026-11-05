using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/Lion.cs). HealChance and
/// <c>SetMagicalAbility(MagicalAbility.Piercing)</c> are pet-training profile flags with no counterpart in
/// this codebase's monster-ability framework, so they stay dropped. The original's three
/// <c>SetWeaponAbility</c> calls live only in its version-0 <c>Deserialize</c> migration path, so a fresh
/// port registers none of them; <see cref="WeaponAbilityChance" /> is kept.</summary>
[SerializationGenerator(0, false)]
public partial class Lion : BaseCreature
{
    [Constructible]
    public Lion() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 0x592;
        Female = true;
        BaseSoundID = 0x3EF;

        SetStr(710, 720);
        SetDex(200, 220);
        SetInt(120, 140);

        SetHits(350, 370);

        SetDamage(16, 22);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40, 50);
        SetResistance(ResistanceType.Fire, 35, 45);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 20, 40);

        SetSkill(SkillName.Parry, 90.0, 100.0);
        SetSkill(SkillName.Tactics, 100.0, 110.0);
        SetSkill(SkillName.Wrestling, 100.0, 110.0);
        SetSkill(SkillName.DetectHidden, 80.0);

        Fame = 11000;
        Karma = -11000;

        Tamable = true;
        ControlSlots = 2;
        MinTameSkill = 96.0;
    }

    public override string CorpseName => "труп льва";
    public override string DefaultName => "лев";

    public override int GetIdleSound() => 0x673;
    public override int GetAngerSound() => 0x670;
    public override int GetHurtSound() => 0x672;
    public override int GetDeathSound() => 0x671;

    public override double WeaponAbilityChance => 0.5;

    public override int Hides => 11;
    public override HideType HideType => HideType.Regular;
    public override int Meat => 5;
    public override FoodType FavoriteFood => FoodType.Meat;

    public override bool CanAngerOnTame => true;
    public override bool StatLossAfterTame => true;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 1);
    }
}
