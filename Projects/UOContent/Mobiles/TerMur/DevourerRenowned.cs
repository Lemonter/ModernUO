using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Renowned/DevourerRenowned.cs). Extends the
/// local BaseRenowned base (ported earlier for the Citadel cluster). AIType.AI_NecroMage has
/// since been ported (Mobiles/AI/NecroMageAI.cs) and is restored here. SharedSAList: AnimatedLegsoftheInsaneTinker
/// and PillarOfStrength don't exist anywhere in this codebase — dropped, kept StormCaller
/// (confirmed to exist). LootPack.NecroRegs doesn't exist — dropped.</summary>
[SerializationGenerator(0, false)]
[CorpseName("Devourer of Souls [Renowned] corpse")]
public partial class DevourerRenowned : BaseRenowned
{
    [Constructible]
    public DevourerRenowned() : base(AIType.AI_NecroMage)
    {
        Title = "[Renowned]";
        Body = 303;
        BaseSoundID = 357;

        SetStr(801, 950);
        SetDex(126, 175);
        SetInt(201, 250);

        SetHits(2000);

        SetDamage(22, 26);

        SetDamageType(ResistanceType.Physical, 60);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 25, 35);
        SetResistance(ResistanceType.Cold, 15, 25);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.Necromancy, 90.1, 100.0);
        SetSkill(SkillName.SpiritSpeak, 90.1, 100.0);
        SetSkill(SkillName.EvalInt, 90.1, 100.0);
        SetSkill(SkillName.Magery, 90.1, 100.0);
        SetSkill(SkillName.Meditation, 90.1, 100.0);
        SetSkill(SkillName.MagicResist, 90.1, 105.0);
        SetSkill(SkillName.Tactics, 75.1, 85.0);
        SetSkill(SkillName.Wrestling, 80.1, 100.0);

        Fame = 9500;
        Karma = -9500;
    }

    public override string DefaultName => "пожиратель душ";

    public override Type[] UniqueSAList => Array.Empty<Type>();
    public override Type[] SharedSAList => new[] { typeof(StormCaller) };
    public override Poison PoisonImmune => Poison.Lethal;
    public override int Meat => 3;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
    }
}
