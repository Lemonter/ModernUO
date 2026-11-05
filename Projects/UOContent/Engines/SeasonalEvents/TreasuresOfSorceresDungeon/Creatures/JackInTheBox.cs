using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Creatures/JackInTheBox.cs).
///     ServUO's two <c>SetWeaponAbility</c> registrations come back through <see cref="GetWeaponAbility" />
///     as a random pick per swing, which is what ServUO's ability profile did.
/// </summary>
[SerializationGenerator(0, false)]
public partial class JackInTheBox : BaseCreature
{
    public override string CorpseName => "труп чёртика из коробки";

    [Constructible]
    public JackInTheBox() : base(AIType.AI_NecroMage, FightMode.Closest, 10, 1)
    {
        Name = "чёртик из коробки";
        Body = 1428;

        SetStr(200, 300);
        SetDex(150, 200);
        SetInt(700, 800);

        SetHits(8000);

        SetDamage(21, 27);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 70, 80);
        SetResistance(ResistanceType.Fire, 70, 80);
        SetResistance(ResistanceType.Cold, 70, 80);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.Anatomy, 25);
        SetSkill(SkillName.MagicResist, 120);
        SetSkill(SkillName.Tactics, 100, 110);
        SetSkill(SkillName.Wrestling, 130, 140);
        SetSkill(SkillName.Parry, 20, 30);
        SetSkill(SkillName.Magery, 120);
        SetSkill(SkillName.EvalInt, 120);
        SetSkill(SkillName.Necromancy, 120);
        SetSkill(SkillName.SpiritSpeak, 120);
        SetSkill(SkillName.Meditation, 120);
        SetSkill(SkillName.Focus, 70, 80);

        Fame = 12000;
        Karma = -12000;
    }

    public override bool AlwaysMurderer => true;

    public override WeaponAbility GetWeaponAbility() => Utility.Random(2) switch
    {
        0 => WeaponAbility.ArmorIgnore,
        _ => WeaponAbility.BleedAttack
    };

    public override Poison PoisonImmune => Poison.Deadly;

    public override int GetIdleSound() => 0x218;

    public override int GetAngerSound() => 0x26C;

    public override int GetDeathSound() => 0x211;

    public override int GetAttackSound() => 0x232;

    public override int GetHurtSound() => 0x140;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
    }
}
