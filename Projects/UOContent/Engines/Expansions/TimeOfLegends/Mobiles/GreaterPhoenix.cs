using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Expansions/Time Of Legends/Mobiles/GreaterPhoenix.cs).
///
///     ServUO's <c>SetSpecialAbility(SpecialAbility.GraspingClaw)</c> becomes
///     <see cref="GetMonsterAbilities" /> with <see cref="MonsterAbilities.GraspingClaw" />, which is
///     the same ability under this codebase's monster-ability framework.
/// </summary>
[SerializationGenerator(0, false)]
public partial class GreaterPhoenix : BaseCreature
{
    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.GraspingClaw];

    [Constructible]
    public GreaterPhoenix() : base(AIType.AI_Mage, FightMode.Closest)
    {
        Name = "большой феникс";
        Body = 832;
        BaseSoundID = 0x8F;

        SetStr(332, 386);
        SetDex(97, 113);
        SetInt(182, 258);

        SetDamage(11, 14);

        SetHits(119, 240);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 70, 80);
        SetResistance(ResistanceType.Cold, 20, 30);
        SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 35, 45);

        SetDamageType(ResistanceType.Physical, 40);
        SetDamageType(ResistanceType.Fire, 60);

        SetSkill(SkillName.Wrestling, 60, 77);
        SetSkill(SkillName.MagicResist, 90, 105);
        SetSkill(SkillName.Tactics, 50, 70);
        SetSkill(SkillName.Magery, 90, 100);
        SetSkill(SkillName.EvalInt, 90, 100);

        PackGold(500, 700);

        Fame = 10000;
        Karma = -10000;
    }

    public override string CorpseName => "труп большого феникса";

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;
}
