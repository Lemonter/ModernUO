using System;
using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Renowned/GreenGoblinAlchemistRenowned.cs).
/// Extends the local BaseRenowned base. UniqueSAList: ObsidianEarrings/TheImpalersPick don't
/// exist anywhere in this codebase — dropped, leaving an empty list. AllureImmune dropped (no
/// such virtual here).</summary>
[SerializationGenerator(0, false)]
[CorpseName("Green Goblin Alchemist [Renowned] corpse")]
public partial class GreenGoblinAlchemistRenowned : BaseRenowned
{
    [Constructible]
    public GreenGoblinAlchemistRenowned() : base(AIType.AI_Melee)
    {
        Title = "[Renowned]";
        Body = 723;
        BaseSoundID = 0x600;

        SetStr(600, 650);
        SetDex(50, 70);
        SetInt(100, 250);

        SetHits(1000, 1500);

        SetDamage(5, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 55);
        SetResistance(ResistanceType.Fire, 55, 60);
        SetResistance(ResistanceType.Cold, 40, 50);
        SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 20, 25);

        SetSkill(SkillName.MagicResist, 120.0, 125.0);
        SetSkill(SkillName.Tactics, 95.0, 100.0);
        SetSkill(SkillName.Wrestling, 100.0, 110.0);

        Fame = 1500;
        Karma = -1500;
    }

    public override string DefaultName => "зелёный гоблин-алхимик";

    public override Type[] UniqueSAList => Array.Empty<Type>();
    public override Type[] SharedSAList => Array.Empty<Type>();

    public override int GetAngerSound() => 0x600;
    public override int GetIdleSound() => 0x600;
    public override int GetAttackSound() => 0x5FD;
    public override int GetHurtSound() => 0x5FF;
    public override int GetDeathSound() => 0x5FE;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
    }
}
