using System;
using System.Collections.Generic;
using System.Globalization;
using Server.Items;
using Server.Systems.MahaonMasteries;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Core/
// SkillMasteryMove.cs) — base for the mastery abilities that are on-hit special moves
// (Stagger, Thrust, Pierce, Rampage, Fists of Fury, Called Shot, Injected Strike, Shield
// Bash, Elemental Fury) rather than direct-cast spells. Straight port: `SpecialMove`
// (Spells/Base/SpecialMove.cs) is essentially identical between this codebase and real
// ServUO/RunUO — same OnHit/OnMiss/OnBeforeSwing/Validate/SetContext shape — so these moves
// get full, real BaseWeapon.OnHit integration for free, no engine surgery needed.
public abstract class SkillMasteryMove : SpecialMove
{
    private Dictionary<Mobile, DateTime> _cooldown;

    public virtual TimeSpan CooldownPeriod => TimeSpan.MinValue;
    public override bool ValidatesDuringHit => false;

    // Real ServUO has a separate virtual SendAbilityMessage(Mobile) hook — this codebase's
    // SpecialMove.SetCurrentMove already sends AbilityMessage.SendMessageTo(m) directly, so
    // there's nothing extra to override here; each concrete move just sets AbilityMessage.

    public override bool Validate(Mobile from)
    {
        var move = GetCurrentMove(from) as SkillMasteryMove;

        if ((move == null || move.GetType() != GetType()) && !CheckCooldown(from))
        {
            return false;
        }

        if (from.Player && MasteryState.GetCurrentMastery(from) != MoveSkill)
        {
            from.SendLocalizedMessage(1115664); // You are not on the correct path for using this mastery ability.
            return false;
        }

        return base.Validate(from);
    }

    public bool CheckCooldown(Mobile from)
    {
        if (CooldownPeriod > TimeSpan.MinValue && IsInCooldown(from))
        {
            var left = (_cooldown[from] - Core.Now).TotalMinutes;

            if (left > 1)
            {
                from.SendLocalizedMessage(1155787, ((int)left).ToString()); // You must wait ~1_minutes~ minutes before you can use this ability.
            }
            else
            {
                left = (_cooldown[from] - Core.Now).TotalSeconds;
                from.SendLocalizedMessage(1079335, left.ToString("F", CultureInfo.InvariantCulture)); // You must wait ~1_seconds~ seconds before you can use this ability again.
            }

            return false;
        }

        return true;
    }

    public bool CheckWeapon(Mobile from)
    {
        if (!from.Player)
        {
            return true;
        }

        return from.Weapon is BaseWeapon weapon && weapon.DefSkill == MoveSkill;
    }

    public virtual bool IsInCooldown(Mobile m) => _cooldown != null && _cooldown.ContainsKey(m);

    public virtual void AddToCooldown(Mobile m)
    {
        if (CooldownPeriod <= TimeSpan.MinValue)
        {
            return;
        }

        _cooldown ??= new Dictionary<Mobile, DateTime>();
        _cooldown[m] = Core.Now + CooldownPeriod;
        Timer.DelayCall(CooldownPeriod, () => _cooldown.Remove(m));
    }

    public virtual void OnGotHit(Mobile attacker, Mobile defender, ref int damage)
    {
    }

    public virtual void OnDamaged(Mobile attacker, Mobile defender, DamageType type, ref int damage)
    {
    }
}
