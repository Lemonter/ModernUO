using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.Mysticism;

/// <summary>Mysticism 681. Ported from ServUO
/// (Scripts/Spells/Mysticism/SpellDefinitions/SleepSpell.cs); registration was already in
/// Spells/Initializer.cs, commented out.
///
/// Sleep doesn't freeze — it drops the victim to a crawl until it expires or it takes damage.
/// The original does that with target.SendSpeedControl(SpeedControlType.WalkSpeed), which is
/// the same client packet ModernUO exposes as SpeedControlSetting.Walk; on a mobile with no
/// NetState it's a no-op in both codebases (ns?.Send here, the same null check there).
///
/// The wake-on-damage hook lives in AOS.Damage, next to SkillMasterySpell.OnDamage, which is
/// the same kind of hook for the same reason.</summary>
public class SleepSpell : MysticSpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new(
        "Sleep",
        "In Zu",
        230,
        9022,
        Reagent.Nightshade,
        Reagent.SpidersSilk,
        Reagent.BlackPearl
    );

    private static readonly Dictionary<Mobile, SleepTimer> _table = new();
    private static readonly HashSet<Mobile> _immunity = new();

    public SleepSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Third;

    public void Target(Mobile m)
    {
        if (m.Paralyzed || m.Frozen)
        {
            // Your target is already immobilized and cannot be slept.
            Caster.SendLocalizedMessage(1080134);
            return;
        }

        if (IsImmune(m))
        {
            Caster.SendLocalizedMessage(1080135); // Your target cannot be put to sleep.
            return;
        }

        if (!CheckHSequence(m))
        {
            return;
        }

        SpellHelper.Turn(Caster, m);

        var source = Caster;
        var target = m;

        SpellHelper.CheckReflect(3, ref source, ref target);

        var duration = (Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value) / 20 + 2;
        duration -= GetResistSkill(target) / 10;

        if (duration <= 0 || StoneFormSpell.UnderEffect(target))
        {
            Caster.SendLocalizedMessage(1080136); // Your target resists sleep.
            target.SendLocalizedMessage(1080137); // You resist sleep.
            return;
        }

        DoSleep(Caster, target, TimeSpan.FromSeconds(duration));
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
    }

    public static bool IsUnderSleepEffects(Mobile m) => _table.ContainsKey(m);

    public static bool IsImmune(Mobile m) => _immunity.Contains(m);

    public static void DoSleep(Mobile caster, Mobile target, TimeSpan duration)
    {
        if (_table.ContainsKey(target))
        {
            return;
        }

        target.Combatant = null;
        target.NetState.SendSpeedControl(SpeedControlSetting.Walk);

        var timer = new SleepTimer(target, duration);
        timer.Start();

        _table[target] = timer;

        (target as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.Sleep, 1080139, 1080140, duration));

        target.Delta(MobileDelta.WeaponDamage);
    }

    /// <summary>Called from AOS.Damage — any damage wakes the sleeper.</summary>
    public static void OnDamage(Mobile m)
    {
        if (_table.ContainsKey(m))
        {
            EndSleep(m);
        }
    }

    public static void EndSleep(Mobile target)
    {
        if (!_table.Remove(target, out var timer))
        {
            return;
        }

        target.NetState.SendSpeedControl(SpeedControlSetting.Disable);

        timer.Stop();

        (target as PlayerMobile)?.RemoveBuff(BuffIcon.Sleep);

        // Immunity is what stops a mystic from perma-locking one target.
        _immunity.Add(target);
        Timer.StartTimer(
            TimeSpan.FromSeconds(target.Skills.MagicResist.Value / 10),
            () => _immunity.Remove(target)
        );

        target.Delta(MobileDelta.WeaponDamage);
    }

    /// <summary>The original's SleepTimer ticks on a short interval, re-emitting the head
    /// particles each time and ending the sleep once its end time has passed — it is not a
    /// one-shot timer with a separate effect timer alongside it.</summary>
    private class SleepTimer : Timer
    {
        private readonly DateTime _endTime;
        private readonly Mobile _target;

        internal SleepTimer(Mobile target, TimeSpan duration)
            : base(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5))
        {
            _target = target;
            _endTime = Core.Now + duration;
        }

        protected override void OnTick()
        {
            if (_endTime < Core.Now)
            {
                EndSleep(_target);
                Stop();
            }
            else
            {
                Effects.SendTargetParticles(_target, 0x3779, 1, 32, 0x13BA, EffectLayer.Head);
            }
        }
    }
}
