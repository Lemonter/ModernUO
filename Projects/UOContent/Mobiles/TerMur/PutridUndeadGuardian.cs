using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/PutridUndeadGuardian.cs). Its reagent
/// pack is a guess in the original too — its own comment says Stratics never listed one.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a putrid undead guardian corpse")]
public partial class PutridUndeadGuardian : BaseCreature
{
    [Constructible]
    public PutridUndeadGuardian() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 722;

        SetStr(79);
        SetDex(63);
        SetInt(187);

        SetHits(553);

        SetDamage(3, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40);
        SetResistance(ResistanceType.Fire, 23);
        SetResistance(ResistanceType.Cold, 57);
        SetResistance(ResistanceType.Poison, 29);
        SetResistance(ResistanceType.Energy, 39);

        SetSkill(SkillName.MagicResist, 62.7);
        SetSkill(SkillName.Tactics, 45.4);
        SetSkill(SkillName.Wrestling, 50.7);

        Fame = 3000;
        Karma = -3000;

        PackNecroReg(10, 15);
    }

    public override string DefaultName => "гнилой страж-нежить";

    public override int Meat => 1;

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich, 3);

    public override int GetIdleSound() => 1609;
    public override int GetAngerSound() => 1606;
    public override int GetHurtSound() => 1608;
    public override int GetDeathSound() => 1607;
}
