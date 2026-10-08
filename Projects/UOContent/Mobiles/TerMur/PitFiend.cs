using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/PitFiend.cs).</summary>
[SerializationGenerator(0, false)]
[CorpseName("a pit fiend corpse")]
public partial class PitFiend : BaseCreature
{
    [Constructible]
    public PitFiend() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 43;
        Hue = 1863;
        BaseSoundID = 357;

        SetStr(376, 405);
        SetDex(176, 195);
        SetInt(201, 225);

        SetHits(226, 243);

        SetDamage(15, 20);

        SetSkill(SkillName.EvalInt, 80.1, 90.0);
        SetSkill(SkillName.Magery, 80.1, 90.0);
        SetSkill(SkillName.MagicResist, 75.1, 85.0);
        SetSkill(SkillName.Tactics, 80.1, 90.0);
        SetSkill(SkillName.Wrestling, 80.1, 100.0);

        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 10, 20);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 20, 30);
        SetResistance(ResistanceType.Energy, 30, 40);

        Fame = 18000;
        Karma = -18000;
    }

    public override string DefaultName => "бес из бездны";

    public override double DispelDifficulty => 125.0;
    public override double DispelFocus => 45.0;
    public override bool CanRummageCorpses => true;
    public override Poison PoisonImmune => Poison.Regular;
    public override int TreasureMapLevel => 4;
    public override int Meat => 1;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
        AddLoot(LootPack.Average, 2);
        AddLoot(LootPack.MedScrolls, 2);
    }
}
