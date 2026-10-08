using System;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Targeting;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/BardSpells/
// Tribulation.cs) — Discordance mastery: debuffs a target's hit chance/spell damage and
// gives damage taken a chance to trigger a bonus physical burst.
public class TribulationSpell : BardSpell
{
    private static readonly SpellInfo Info = new("Tribulation", "In Jux Hur Rel", -1, 9002);

    public override double RequiredSkill => 90;
    public override double UpKeep => 8;
    public override int RequiredMana => 24;
    public override bool PartyEffects => false;
    public override double TickTime => 2.0;

    public override SkillName CastSkill => SkillName.Discordance;

    private DateTime _nextDamage;
    private int _propertyBonus;
    private double _damageChance;
    private int _damageFactor;
    private int _rounds;

    public TribulationSpell(Mobile caster, Item scroll) : base(caster, scroll, Info) => _nextDamage = Core.Now;

    public override void OnCast()
    {
        if (GetSpell(Caster, GetType()) is { } spell)
        {
            spell.Expire();
            Caster.SendLocalizedMessage(1115774); // You halt your spellsong.
        }
        else
        {
            Caster.Target = new InternalTarget(this);
        }
    }

    private void OnTarget(Mobile m)
    {
        if (!Caster.CanSee(m))
        {
            Caster.SendLocalizedMessage(500237); // Target can not be seen.
        }
        else if (Caster == m)
        {
            Caster.SendMessage("Нельзя нацелить эту способность на самого себя!");
        }
        else if (HasHarmfulEffects(m, GetType()))
        {
            Caster.SendLocalizedMessage(1115772); // Your target is already under the effect of this spellsong.
        }
        else if (CheckHSequence(m))
        {
            SpellHelper.Turn(Caster, m);
            Target = m;
            HarmfulSpell(m);

            m.FixedParticles(0x374A, 10, 15, 5028, EffectLayer.Waist);

            _propertyBonus = (int)(BaseSkillBonus * 2.75);
            _damageChance = (int)(BaseSkillBonus * 7.5);
            _damageFactor = (int)(BaseSkillBonus * 4);
            _rounds = 5 + (int)(BaseSkillBonus * .75);

            if (m is Mobiles.PlayerMobile targetPm)
            {
                // ~1_HCI~% Hit Chance. ~2_SDI~% Spell Damage. Damage taken has a ~3_EXP~%
                // chance to cause an additional burst of physical damage.
                targetPm.AddBuff(new BuffInfo(BuffIcon.TribulationTarget, 1115740, 1115742, default, $"{_propertyBonus}\t{_propertyBonus}\t{(int)_damageChance}"));
            }

            if (Caster is Mobiles.PlayerMobile casterPm)
            {
                casterPm.AddBuff(new BuffInfo(BuffIcon.TribulationCaster, 1115740, 1151388, default, $"{m.Name}\t{_damageFactor}\t{(int)_damageChance}"));
            }

            BeginTimer();
        }

        FinishSequence();
    }

    public override bool OnTick()
    {
        if (Target is { Alive: true, Map: not null })
        {
            Target.FixedEffect(0x376A, 1, 32);
        }

        if (_rounds-- <= 0)
        {
            Expire();
            return false;
        }

        return base.OnTick();
    }

    public override void EndEffects()
    {
        if (Target is Mobiles.PlayerMobile targetPm)
        {
            targetPm.RemoveBuff(BuffIcon.TribulationTarget);
        }

        if (Caster is Mobiles.PlayerMobile casterPm)
        {
            casterPm.RemoveBuff(BuffIcon.TribulationCaster);
        }
    }

    public override void OnTargetDamaged(Mobile attacker, Mobile victim, DamageType type, ref int damageTaken)
    {
        if (_nextDamage > Core.Now || _damageChance / 100 <= Utility.RandomDouble())
        {
            return;
        }

        _nextDamage = Core.Now + TimeSpan.FromSeconds(1);

        var damage = AOS.Scale(damageTaken, _damageFactor);
        damage = (int)(damage * GetSlayerBonus());
        damage -= (int)(damage * DamageModifier(Target));

        AOS.Damage(victim, Caster, damage, 100, 0, 0, 0, 0, 0, DamageType.Spell);
        victim.FixedParticles(0x374A, 10, 15, 5038, 1181, 0, EffectLayer.Head);
    }

    public override int PropertyBonus() => _propertyBonus;

    private class InternalTarget : Target
    {
        private readonly TribulationSpell _owner;

        public InternalTarget(TribulationSpell spell) : base(10, false, TargetFlags.Harmful) => _owner = spell;

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is Mobile m)
            {
                _owner.OnTarget(m);
            }
        }

        protected override void OnTargetFinish(Mobile from) => _owner.FinishSequence();
    }
}
