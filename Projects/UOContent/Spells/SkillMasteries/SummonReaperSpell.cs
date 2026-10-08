using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Spellweaving;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/SummonReaper.cs) —
// Spellweaving mastery: summons a temporary powerful reaper ally. The epic-arcanist-quest
// gate is dropped (see ManaShieldSpell for why). The reaper's periodic poison-nova aura
// (ServUO's DoAura, built on AcquireIndirectTargets — not present here) is simplified to a
// direct-range poison tick against nearby enemies via GetMobilesInRange.
public class SummonReaperSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Summon Reaper", "Lartarisstree", 204, 9061);

    public override double RequiredSkill => 90;
    public override double UpKeep => 0;
    public override int RequiredMana => 50;
    public override bool PartyEffects => false;

    public override SkillName CastSkill => SkillName.Spellweaving;

    public SummonReaperSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (!base.CheckCast())
        {
            return false;
        }

        if (Caster.Followers + 5 > Caster.FollowersMax)
        {
            Caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
            return false;
        }

        return true;
    }

    public override void OnCast() => Caster.Target = new MasteryTarget(this, 10, true, Targeting.TargetFlags.None);

    protected override void OnTarget(object o)
    {
        if (o is not IPoint3D p)
        {
            return;
        }

        var map = Caster.Map;
        var loc = new Point3D(p.X, p.Y, map?.GetAverageZ(p.X, p.Y) ?? p.Z);

        if (map == null || !map.CanSpawnMobile(loc))
        {
            Caster.SendLocalizedMessage(501942); // That location is blocked.
            return;
        }

        if (!CheckSequence())
        {
            return;
        }

        var duration = TimeSpan.FromSeconds((Caster.Skills[CastSkill].Value + ArcanistSpell.GetFocusLevel(Caster) * 20) / 240 * 75);
        BaseCreature.Summon(new SummonedReaper(Caster, this), false, Caster, loc, 442, duration);
    }
}

[CorpseName("останки жнеца")]
[SerializationGenerator(0, false)]
public partial class SummonedReaper : BaseCreature
{
    public override double DispelDifficulty { get; }
    public override double DispelFocus => 45.0;

    private long _nextAura;

    [Constructible]
    public SummonedReaper(Mobile caster, SkillMasterySpell spell) : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "жнец";
        Body = 47;
        BaseSoundID = 442;

        ActiveSpeed = 0.2;
        PassiveSpeed = 0.4;
        CurrentSpeed = ActiveSpeed;

        var scale = 1.0 + (caster.Skills[spell.CastSkill].Value + spell.GetMasteryLevel() * 40 + ArcanistSpell.GetFocusLevel(caster) * 20) / 1000.0;

        SetStr((int)(450 * scale), (int)(500 * scale));
        SetDex((int)(130 * scale));
        SetInt((int)(247 * scale));

        SetHits((int)(450 * scale));
        SetDamage(16, 20);

        SetDamageType(ResistanceType.Physical, 80);
        SetDamageType(ResistanceType.Poison, 20);

        SetResistance(ResistanceType.Physical, 70);
        SetResistance(ResistanceType.Fire, 15);
        SetResistance(ResistanceType.Cold, 18);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 69);

        SetSkill(SkillName.Spellweaving, Math.Max(100, 75 * scale));
        SetSkill(SkillName.Anatomy, Math.Max(100, 75 * scale));
        SetSkill(SkillName.MagicResist, Math.Max(100, 75 * scale));
        SetSkill(SkillName.Tactics, Math.Max(100, 75 * scale));
        SetSkill(SkillName.Wrestling, Math.Max(100, 75 * scale));

        ControlSlots = 5;

        DispelDifficulty = 91 + caster.Skills[SkillName.Spellweaving].Base * 83 / 5.2;
        _nextAura = Core.TickCount + 3000;
    }

    public override Poison PoisonImmune => Poison.Greater;
    public override bool DisallowAllMoves => true;
    public override bool AlwaysMurderer => true;

    public override void OnThink()
    {
        base.OnThink();

        if (_nextAura >= Core.TickCount)
        {
            return;
        }

        DoAura();
        _nextAura = Core.TickCount + 2000;
    }

    private void DoAura()
    {
        Effects.SendLocationEffect(Location, Map, 0x3709, 0x14, 0x1, 0x8AF, 4);

        foreach (var m in Map.GetMobilesInRange(Location, 4))
        {
            if (m == this || !CanBeHarmful(m, false))
            {
                continue;
            }

            var damage = Utility.RandomMinMax(10, 20);
            AOS.Damage(m, this, damage, 0, 0, 0, 100, 0);
            m.RevealingAction();
        }
    }
}
