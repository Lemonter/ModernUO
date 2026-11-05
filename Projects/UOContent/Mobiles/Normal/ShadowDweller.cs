using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/ShadowDweller.cs). SpecialAbility.LifeLeech
/// maps to the DrainLifeAttack monster ability.</summary>
[SerializationGenerator(0, false)]
public partial class ShadowDweller : BaseCreature
{
    [Constructible]
    public ShadowDweller() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 740;
        Hue = 1;
        BaseSoundID = 0x5F1;

        SetStr(171, 200);
        SetDex(126, 145);
        SetInt(276, 305);

        SetHits(103, 120);

        SetDamage(24, 26);

        SetDamageType(ResistanceType.Physical, 10);
        SetDamageType(ResistanceType.Cold, 40);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 40, 60);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 55, 65);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.EvalInt, 100.0);
        SetSkill(SkillName.Magery, 70.1, 80.0);
        SetSkill(SkillName.Meditation, 85.1, 95.0);
        SetSkill(SkillName.MagicResist, 80.1, 100.0);
        SetSkill(SkillName.Tactics, 70.1, 90.0);

        Fame = 8000;
        Karma = -8000;

        VirtualArmor = 50;

        PackNecroReg(17, 24);
    }

    public override string CorpseName => "труп теневого жителя";
    public override string DefaultName => "теневой житель";

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.DrainLifeAttack];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;
    public override bool CanRummageCorpses => true;
    public override bool BleedImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override int TreasureMapLevel => 3;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 3);
        AddLoot(LootPack.MedScrolls, 2);
    }
}
