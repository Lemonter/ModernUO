using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Mobiles/
/// DescicatedMyrmidexLarvae.cs). The misspelling of "desiccated" is the original's, kept so the
/// spawn data that names this type keeps resolving.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a myrmidex corpse")]
public partial class DescicatedMyrmidexLarvae : BaseCreature
{
    [Constructible]
    public DescicatedMyrmidexLarvae() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 1293;
        Hue = 2949;
        BaseSoundID = 959;

        SetStr(350, 450);
        SetDex(80, 95);
        SetInt(15, 25);

        SetHits(446, 588);
        SetMana(20, 50);

        SetDamage(5, 10);

        SetDamageType(ResistanceType.Physical, 60);
        SetDamageType(ResistanceType.Poison, 40);

        SetResistance(ResistanceType.Physical, 20, 25);
        SetResistance(ResistanceType.Fire, 10, 20);
        SetResistance(ResistanceType.Cold, 15, 25);
        SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.MagicResist, 30.1, 43.5);
        SetSkill(SkillName.Tactics, 60.0, 70.0);
        SetSkill(SkillName.Wrestling, 55.0, 60.0);
        SetSkill(SkillName.Poisoning, 80.0, 100.0);
        SetSkill(SkillName.DetectHidden, 30.0, 40.0);

        PackGold(20, 40);

        Fame = 2500;
        Karma = -2500;
    }

    public override string DefaultName => "иссохшая личинка мирмидекса";

    public override Poison HitPoison => Poison.Lesser;
    public override Poison PoisonImmune => Poison.Lesser;
}
