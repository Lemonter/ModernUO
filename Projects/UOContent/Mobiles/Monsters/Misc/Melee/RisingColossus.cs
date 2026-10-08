using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>The Mysticism 8th-circle summon. Ported from ServUO
/// (Scripts/Mobiles/Summons/RisingColossus.cs) to go with RisingColossusSpell.
///
/// One adaptation: local BaseCreature's constructor takes no speed arguments, so ServUO's
/// 0.4/0.5 are assigned to ActiveSpeed/PassiveSpeed in the body instead.
///
/// Everything else — AI_Mystic, the statbonus/hitsbonus/skillvalue arithmetic, resistances,
/// 5 control slots, immunities — is the original's.</summary>
[SerializationGenerator(0, false)]
public partial class RisingColossus : BaseCreature
{
    [Constructible]
    public RisingColossus(Mobile caster = null, double baseSkill = 0.0, double boostSkill = 0.0)
        : base(AIType.AI_Mystic, FightMode.Closest, 10, 1)
    {
        ActiveSpeed = 0.4;
        PassiveSpeed = 0.5;

        Body = 829;

        var level = baseSkill + boostSkill;
        var statBonus = (baseSkill - 83) / 1.3 + (boostSkill - 30) / 1.3 + 6;
        var hitsBonus = (baseSkill - 83) * 1.14 + (boostSkill - 30) * 1.03 + 20;
        var skillValue = boostSkill != 0 ? (baseSkill + boostSkill) / 2 : (baseSkill + 20) / 2;

        SetStr((int)(677 + statBonus));
        SetDex((int)(107 + statBonus));
        SetInt((int)(127 + statBonus));

        SetHits((int)(315 + hitsBonus));

        SetDamage((int)(level / 12), (int)(level / 10));

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 65, 70);
        SetResistance(ResistanceType.Fire, 50, 55);
        SetResistance(ResistanceType.Cold, 50, 55);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 65, 70);

        SetSkill(SkillName.MagicResist, skillValue);
        SetSkill(SkillName.Tactics, skillValue);
        SetSkill(SkillName.Wrestling, skillValue);
        SetSkill(SkillName.Anatomy, skillValue);
        SetSkill(SkillName.EvalInt, skillValue);
        SetSkill(SkillName.DetectHidden, 70.0);

        if (caster != null)
        {
            SetSkill(SkillName.Mysticism, caster.Skills.Mysticism.Value);
            SetSkill(SkillName.Focus, caster.Skills.Focus.Value);
        }

        ControlSlots = 5;
    }

    public override string DefaultName => "восстающий колосс";

    public override bool AlwaysMurderer => true;
    public override bool BleedImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;
}
