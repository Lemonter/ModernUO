using System;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Targeting;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/BardSpells/
// Despair.cs) — Discordance mastery: Strength drain + periodic physical damage tick.
public class DespairSpell : BardSpell
{
    private const string ModName = "MasteryDespair";

    private static readonly SpellInfo Info = new("Despair", "Kal Des Mani Tym", -1, 9002);

    public override double RequiredSkill => 90;
    public override double UpKeep => 10;
    public override int RequiredMana => 26;
    public override bool PartyEffects => false;

    public override SkillName CastSkill => SkillName.Discordance;

    private int _statMod;
    private int _damage;
    private int _rounds;

    public DespairSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

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
        else if (!m.Alive)
        {
            Caster.SendLocalizedMessage(1115773); // Your target is dead.
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

            _statMod = (int)(BaseSkillBonus * 2);
            _damage = (int)(BaseSkillBonus * 4.5);
            _rounds = 5 + (int)(BaseSkillBonus * .75);

            var args = $"{_statMod}\t{_damage}";

            if (m is Mobiles.PlayerMobile targetPm)
            {
                targetPm.AddBuff(new BuffInfo(BuffIcon.DespairTarget, 1115741, 1115743, default, args));
            }

            if (Caster is Mobiles.PlayerMobile casterPm)
            {
                casterPm.AddBuff(new BuffInfo(BuffIcon.DespairCaster, 1115741, 1115743, default, args));
            }

            BeginTimer();
        }

        FinishSequence();
    }

    public override void EndEffects()
    {
        if (Target is Mobiles.PlayerMobile targetPm)
        {
            targetPm.RemoveBuff(BuffIcon.DespairTarget);
        }

        if (Caster is Mobiles.PlayerMobile casterPm)
        {
            casterPm.RemoveBuff(BuffIcon.DespairCaster);
        }
    }

    public override void AddStatMods() => Target?.AddStatMod(new StatMod(StatType.Str, ModName, -_statMod, TimeSpan.Zero));

    public override void RemoveStatMods() => Target?.RemoveStatMod(ModName);

    public override bool OnTick()
    {
        var tick = base.OnTick();

        if (Target == null || !Caster.InRange(Target.Location, PartyRange))
        {
            return false;
        }

        var damage = _damage;

        if (!Target.Player)
        {
            damage += AOS.Scale(damage, 50); // pvm = 1.5x
        }

        damage = (int)(damage * GetSlayerBonus());
        damage -= (int)(damage * DamageModifier(Target));

        AOS.Damage(Target, Caster, damage, 100, 0, 0, 0, 0, 0, DamageType.Spell);

        if (Target is { Alive: true, Map: not null })
        {
            Target.FixedEffect(0x376A, 1, 32);
        }

        if (_rounds-- == 0)
        {
            Expire();
        }

        return tick;
    }

    private class InternalTarget : Target
    {
        private readonly DespairSpell _owner;

        public InternalTarget(DespairSpell spell) : base(10, false, TargetFlags.Harmful) => _owner = spell;

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
