using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/FairyDragon.cs). AIType.AI_Mystic
/// substituted with AI_Mage (see Skree.cs). The FairyDragonWing/DraconicOrb rare drops
/// dropped — neither item exists anywhere in this codebase. HitPoisonChance confirmed to
/// exist on BaseCreature.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a fairy dragon corpse")]
public partial class FairyDragon : BaseCreature
{
    public override bool AutoDispel => !Controlled;
    public override int TreasureMapLevel => 3;
    public override int Meat => 9;
    public override Poison HitPoison => Poison.Greater;
    public override double HitPoisonChance => 0.75;
    public override FoodType FavoriteFood => FoodType.Meat;

    [Constructible]
    public FairyDragon() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 718;
        BaseSoundID = 362;

        SetStr(512, 558);
        SetDex(95, 105);
        SetInt(455, 501);

        SetHits(398, 403);

        SetDamage(15, 18);

        SetDamageType(ResistanceType.Fire, 20, 25);
        SetDamageType(ResistanceType.Cold, 20, 25);
        SetDamageType(ResistanceType.Poison, 20, 25);
        SetDamageType(ResistanceType.Energy, 20, 25);

        SetResistance(ResistanceType.Physical, 16, 30);
        SetResistance(ResistanceType.Fire, 41, 44);
        SetResistance(ResistanceType.Cold, 40, 49);
        SetResistance(ResistanceType.Poison, 40, 49);
        SetResistance(ResistanceType.Energy, 45, 47);

        SetSkill(SkillName.MagicResist, 99.1, 100.0);
        SetSkill(SkillName.Tactics, 60.6, 68.2);
        SetSkill(SkillName.Wrestling, 90.1, 92.5);
        SetSkill(SkillName.Mysticism, 101.8, 108.3);

        Fame = 15000;
        Karma = -15000;
    }

    public override string DefaultName => "дракончик-фея";

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
        AddLoot(LootPack.MedScrolls, 2);
    }

    public override int GetAttackSound() => 1513;
    public override int GetAngerSound() => 1558;
    public override int GetDeathSound() => 1514;
    public override int GetHurtSound() => 1515;
    public override int GetIdleSound() => 1516;
}
