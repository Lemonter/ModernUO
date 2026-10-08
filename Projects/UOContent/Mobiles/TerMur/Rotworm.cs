using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/Rotworm.cs). SetSpecialAbility
/// dropped — see Mobiles/Monsters/Reptile/Eodon/Dinosaurs.cs's own class doc comment
/// (an earlier session's port): this codebase has no RunUO-era creature special-move
/// framework at all, only the player-facing WeaponAbility system. The ML Quest tie-in
/// (Missing quest reward drop) dropped too — narrative flavor, not core to the creature.
/// LootPack.BodyPartsAndBones also dropped — no such member on this codebase's LootPack
/// class (verified: only Poor/Meager/Average/Rich/FilthyRich/UltraRich/SuperBoss/Gems/
/// Potions/LowScrolls/MedScrolls/HighScrolls exist; Parrot exists but is commented out dead
/// code in LootPack.cs). MeatType.Rotworm doesn't exist either — this codebase's MeatType
/// enum is just { Ribs, Bird, LambLeg } — dropped the MeatType override entirely.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a rotworm corpse")]
[TypeAlias("Server.Mobiles.RotWorm")]
public partial class Rotworm : BaseCreature
{
    [Constructible]
    public Rotworm() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 732;

        SetStr(200, 300);
        SetDex(80);
        SetInt(15, 20);

        SetHits(200, 250);
        SetStam(50);

        SetDamage(1, 5);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 35, 45);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 25, 35);
        SetResistance(ResistanceType.Poison, 65, 75);
        SetResistance(ResistanceType.Energy, 25, 35);

        SetSkill(SkillName.MagicResist, 25.0);
        SetSkill(SkillName.Tactics, 25.0);
        SetSkill(SkillName.Wrestling, 50.0);

        Fame = 500;
        Karma = -500;
    }

    public override string DefaultName => "гнилостный червь";

    public override int GetAngerSound() => 0x62D;
    public override int GetIdleSound() => 0x62D;
    public override int GetAttackSound() => 0x62A;
    public override int GetHurtSound() => 0x62C;
    public override int GetDeathSound() => 0x62B;

    public override int Meat => 2;
    public override FoodType FavoriteFood => FoodType.Fish;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }
}
