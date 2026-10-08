using System;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Thrust.cs) —
// Fencing mastery: a stacking damage-increase/defense-debuff stance against one target,
// escalating with consecutive hits.
public class ThrustSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Thrust", "", -1, 9002);

    public override int RequiredMana => 30;
    public override SkillName CastSkill => SkillName.Fencing;
    public override SkillName DamageSkill => SkillName.Tactics;

    private int _defenseMod;

    public int AttackModifier => GetMasteryLevel() * 6 * Phase;

    public int DefenseModifier
    {
        get => _defenseMod;
        set => _defenseMod = Math.Min(GetMasteryLevel() * 6 * 3, value);
    }

    private int _phase;

    public int Phase
    {
        get => _phase;
        set => _phase = value > 3 ? 1 : value;
    }

    public ThrustSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override void EndEffects()
    {
        if (Caster is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.Thrust);
        }
    }

    public override bool CheckCast()
    {
        if (!CheckWeapon())
        {
            Caster.SendLocalizedMessage(1155992); // You must have a fencing weapon equipped to use this ability!
            return false;
        }

        if (GetSpell(Caster, GetType()) is ThrustSpell spell)
        {
            spell.Expire();
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast()
    {
        if (!CheckSequence())
        {
            return;
        }

        Phase = 1;
        DefenseModifier = GetMasteryLevel() * 6;

        Caster.PlaySound(0x101);
        Caster.FixedEffect(0x37C4, 0x1, 0x8, 0x4EB, 0);

        Caster.PrivateOverheadMessage(MessageType.Regular, 1150, 1155988, Caster.NetState); // *You enter a thrusting stance!*

        RefreshCasterBuff();

        FinishSequence();
        BeginTimer();
    }

    private void RefreshCasterBuff()
    {
        if (Caster is not PlayerMobile pm)
        {
            return;
        }

        // Your next physical attack will be increased by +~1_VAL~% damage while reducing
        // your victim's physical attack damage by ~2_VAL~%. Mana Upkeep Cost: ~3_VAL~.
        pm.AddBuff(new BuffInfo(BuffIcon.Thrust, 1155989, 1155990, default, $"{AttackModifier}\t{DefenseModifier}\t{ScaleMana(30)}"));
    }

    private InternalTimer _resetTimer;

    public override void OnHit(Mobile defender, ref int damage)
    {
        var currentMod = AttackModifier;

        if (Target != defender)
        {
            Phase = 1;
            DefenseModifier = GetMasteryLevel() * 6;
            Target = defender;

            // Was un-tracked (`_ = new InternalTimer(this)`) — switching targets before the
            // previous target's 8s window lapsed left the old timer running too. Its OWN
            // _expires (tied to the OLD target) would still fire and Reset() the stance
            // early, wiping out the still-valid new target's Target/DefenseModifier.
            _resetTimer?.Stop();
            _resetTimer = new InternalTimer(this);
        }
        else
        {
            DefenseModifier += GetMasteryLevel() * 6;
            Phase++;
        }

        damage = (int)(damage + damage * (currentMod / 100.0));
        defender.FixedEffect(0x36BD, 0x1, 0xE, 0x776, 0);

        if (defender is PlayerMobile defenderPm)
        {
            // All damage from your physical attacks have been reduced by ~1_val~%.
            defenderPm.AddBuff(new BuffInfo(BuffIcon.ThrustDebuff, 1155989, 1156234, TimeSpan.FromSeconds(8), DefenseModifier.ToString()));
        }

        RefreshCasterBuff();

        if (!CheckMana())
        {
            Reset();
            Expire();
        }
    }

    public override void OnGotHit(Mobile attacker, ref int damage)
    {
        if (Target == attacker && DefenseModifier > 0)
        {
            damage -= (int)(damage * (DefenseModifier / 100.0));
        }
    }

    private bool CheckMana()
    {
        var mana = ScaleMana(GetMana());

        if (Caster.Mana < mana)
        {
            return false;
        }

        Caster.Mana -= mana;
        return true;
    }

    private void Reset()
    {
        DefenseModifier = 0;

        if (Target is PlayerMobile targetPm)
        {
            targetPm.RemoveBuff(BuffIcon.ThrustDebuff);
        }

        Target = null;
        RefreshCasterBuff();
    }

    private class InternalTimer : Timer
    {
        private readonly ThrustSpell _spell;
        private readonly DateTime _expires;

        public InternalTimer(ThrustSpell spell) : base(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250))
        {
            _spell = spell;
            _expires = Core.Now + TimeSpan.FromSeconds(8);
            Start();
        }

        protected override void OnTick()
        {
            if (_expires < Core.Now)
            {
                _spell.Reset();
                Stop();
            }
        }
    }
}
