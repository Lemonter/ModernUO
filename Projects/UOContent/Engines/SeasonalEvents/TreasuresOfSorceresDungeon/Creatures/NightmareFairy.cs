using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Creatures/NightmareFairy.cs).
///     ServUO's <c>SetSpecialAbility(LifeLeech)</c> and <c>SetSpecialAbility(LifeDrain)</c> are
///     <see cref="MonsterAbilities.DrainLifeAttack" /> (single-target leech) and
///     <see cref="MonsterAbilities.DrainLifeAreaAttack" /> (the two-tile area drain).
/// </summary>
[SerializationGenerator(0, false)]
public partial class NightmareFairy : BaseCreature
{
    public override string CorpseName => "труп кошмарной феи";

    [Constructible]
    public NightmareFairy() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Name = "кошмарная фея";

        Body = 176;
        BaseSoundID = 0x467;
        Hue = 1910;

        SetStr(400);
        SetDex(150);
        SetInt(1200);

        SetHits(8000);

        SetDamage(21, 27);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 60, 70);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 80, 90);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 100);

        SetSkill(SkillName.EvalInt, 120);
        SetSkill(SkillName.Magery, 120);
        SetSkill(SkillName.Meditation, 100);
        SetSkill(SkillName.MagicResist, 200);
        SetSkill(SkillName.Tactics, 100.0);
        SetSkill(SkillName.Wrestling, 120);

        Fame = 12000;
        Karma = -12000;
    }

    public override bool AlwaysMurderer => true;

    private static readonly MonsterAbility[] _abilities =
    [
        MonsterAbilities.DrainLifeAttack,
        MonsterAbilities.DrainLifeAreaAttack
    ];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override Poison PoisonImmune => Poison.Deadly;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
    }
}
