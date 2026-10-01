using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/MaddeningHorror.cs).
/// AIType.AI_NecroMage has since been ported (Mobiles/AI/NecroMageAI.cs) and is restored
/// here. SetSpecialAbility dropped (see Rotworm.cs).
/// The VileTentacles rare drop dropped too — that item doesn't exist anywhere in this
/// codebase.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a maddening horror corpse")]
public partial class MaddeningHorror : BaseCreature
{
    [Constructible]
    public MaddeningHorror() : base(AIType.AI_NecroMage, FightMode.Closest, 10, 1)
    {
        Body = 721;

        SetStr(270, 290);
        SetDex(80, 100);
        SetInt(850);

        SetHits(660);

        SetDamage(15, 27);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Cold, 40);
        SetDamageType(ResistanceType.Energy, 40);

        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 50, 60);

        SetSkill(SkillName.EvalInt, 120.0, 130.0);
        SetSkill(SkillName.Magery, 120.0, 130.0);
        SetSkill(SkillName.Meditation, 100.0, 110.0);
        SetSkill(SkillName.MagicResist, 180.0, 195.0);
        SetSkill(SkillName.Tactics, 95.0, 100.0);
        SetSkill(SkillName.Wrestling, 80.0, 85.0);
        SetSkill(SkillName.Poisoning, 110.0);
        SetSkill(SkillName.DetectHidden, 100.0);
        SetSkill(SkillName.Necromancy, 120.0);
        SetSkill(SkillName.SpiritSpeak, 120.0);

        Fame = 23000;
        Karma = -23000;
    }

    public override string DefaultName => "сводящий с ума ужас";

    public override int GetIdleSound() => 1553;
    public override int GetAngerSound() => 1550;
    public override int GetHurtSound() => 1552;
    public override int GetDeathSound() => 1551;
}
