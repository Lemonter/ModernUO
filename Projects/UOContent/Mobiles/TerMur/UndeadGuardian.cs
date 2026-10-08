using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/UndeadGuardian.cs).
/// LootPack.NecroRegs doesn't exist here (verified) — dropped.</summary>
[SerializationGenerator(0, false)]
[CorpseName("an undead guardian corpse")]
public partial class UndeadGuardian : BaseCreature
{
    [Constructible]
    public UndeadGuardian() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 722;

        SetStr(212);
        SetDex(76);
        SetInt(56);

        SetHits(138);

        SetDamage(8, 18);

        SetDamageType(ResistanceType.Physical, 40);
        SetDamageType(ResistanceType.Cold, 60);

        SetResistance(ResistanceType.Physical, 38);
        SetResistance(ResistanceType.Fire, 24);
        SetResistance(ResistanceType.Cold, 58);
        SetResistance(ResistanceType.Poison, 28);
        SetResistance(ResistanceType.Energy, 38);

        SetSkill(SkillName.MagicResist, 66.6);
        SetSkill(SkillName.Tactics, 86.2);
        SetSkill(SkillName.Wrestling, 86.9);
    }

    public override string DefaultName => "страж-нежить";

    public override int Meat => 1;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 3);
    }

    public override int GetIdleSound() => 1609;
    public override int GetAngerSound() => 1606;
    public override int GetHurtSound() => 1608;
    public override int GetDeathSound() => 1607;
}
