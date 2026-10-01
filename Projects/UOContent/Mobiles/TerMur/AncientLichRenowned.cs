using System;
using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Renowned/AncientLichRenowned.cs). Extends the
/// local BaseRenowned base. AIType.AI_NecroMage has since been ported
/// (Mobiles/AI/NecroMageAI.cs) and is restored here.
/// UniqueSAList/SharedSAList: SpinedBloodwormBracers/DefenderOfTheMagus/SummonersKilt don't
/// exist anywhere in this codebase — dropped. LootPack.NecroRegs doesn't exist — dropped.</summary>
[SerializationGenerator(0, false)]
[CorpseName("Ancient Lich [Renowned] corpse")]
public partial class AncientLichRenowned : BaseRenowned
{
    [Constructible]
    public AncientLichRenowned() : base(AIType.AI_NecroMage)
    {
        Title = "[Renowned]";
        Body = 78;
        BaseSoundID = 412;

        SetStr(250, 305);
        SetDex(96, 115);
        SetInt(966, 1045);

        SetHits(2000, 2500);

        SetDamage(15, 27);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Cold, 40);
        SetDamageType(ResistanceType.Energy, 40);

        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 25, 30);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 25, 30);

        SetSkill(SkillName.EvalInt, 120.1, 130.0);
        SetSkill(SkillName.Magery, 120.1, 130.0);
        SetSkill(SkillName.Meditation, 100.1, 101.0);
        SetSkill(SkillName.MagicResist, 175.2, 200.0);
        SetSkill(SkillName.Tactics, 90.1, 100.0);
        SetSkill(SkillName.Wrestling, 75.1, 100.0);

        Fame = 23000;
        Karma = -23000;
    }

    public override string DefaultName => "древний лич";

    public override Type[] UniqueSAList => Array.Empty<Type>();
    public override Type[] SharedSAList => Array.Empty<Type>();

    public override bool Unprovokable => true;
    public override bool BleedImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;

    public override int GetIdleSound() => 0x19D;
    public override int GetAngerSound() => 0x175;
    public override int GetDeathSound() => 0x108;
    public override int GetAttackSound() => 0xE2;
    public override int GetHurtSound() => 0x28B;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
    }
}
