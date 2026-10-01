using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Stagger.cs) — Macing
// mastery on-hit special move: staggers the defender, boosting the caster's damage against
// them for 10 seconds. Real SpecialMove/BaseWeapon integration, no engine surgery needed
// (see SkillMasteryMove.cs).
public class StaggerMove : SkillMasteryMove
{
    public override int BaseMana => 20;
    public override double RequiredSkill => 90.0;

    public override SkillName MoveSkill => SkillName.Macing;
    public override TextDefinition AbilityMessage => 1155980; // *You ready yourself to stagger your opponent!*
    public override TimeSpan CooldownPeriod => TimeSpan.FromSeconds(2);

    private static Dictionary<Mobile, int> _table;
    private static Dictionary<Mobile, Timer> _removalTimers;

    public override bool Validate(Mobile from)
    {
        if (!CheckWeapon(from))
        {
            from.SendLocalizedMessage(1155983); // You must have a mace weapon equipped to use this ability!
            return false;
        }

        return base.Validate(from);
    }

    public override void OnUse(Mobile from)
    {
        if (from.Player)
        {
            from.PlaySound(from.Female ? 0x338 : 0x44A);
        }
        else if (from is BaseCreature bc)
        {
            from.PlaySound(bc.GetAngerSound());
        }

        from.FixedParticles(0x373A, 10, 15, 5018, 2719, 0, EffectLayer.Waist);
    }

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
        if (!Validate(attacker) || !CheckMana(attacker, true))
        {
            return;
        }

        ClearCurrentMove(attacker);

        attacker.PrivateOverheadMessage(MessageType.Regular, 1150, 1155984, attacker.NetState);

        defender.FixedEffect(0x3779, 20, 10, 2719, 0);

        var skills = (attacker.Skills[MoveSkill].Value + attacker.Skills[SkillName.Tactics].Value + Server.Spells.SkillMasteries.MasteryInfo.GetMasteryLevel(attacker, MoveSkill) * 40) / 3;

        AddToTable(defender, (int)(skills / 2));

        if (defender is PlayerMobile pm)
        {
            pm.AddBuff(new BuffInfo(BuffIcon.Stagger, 1155981, 1155982, TimeSpan.FromSeconds(10), ((int)skills / 2).ToString()));
        }

        defender.Delta(MobileDelta.WeaponDamage);

        AddToCooldown(attacker);
    }

    public override double GetDamageScalar(Mobile attacker, Mobile defender) => defender is PlayerMobile ? 1.25 : 1.5;

    public static void AddToTable(Mobile defender, int amount)
    {
        // Was a no-op on a repeat stagger within the window (early return before touching
        // _table at all) — but OnHit below always sends a fresh 10s buff icon regardless,
        // so the icon would show a full countdown while the real effect (and its magnitude)
        // stayed tied to whichever removal timer fired first. Now always refreshes both the
        // amount and the removal timer, canceling any still-pending one from an earlier hit.
        _table ??= new Dictionary<Mobile, int>();
        _table[defender] = amount;

        _removalTimers ??= new Dictionary<Mobile, Timer>();

        if (_removalTimers.TryGetValue(defender, out var existing))
        {
            existing.Stop();
        }

        _removalTimers[defender] = Timer.DelayCall(TimeSpan.FromSeconds(10), () =>
        {
            _table.Remove(defender);
            _removalTimers.Remove(defender);
        });
    }

    public static int GetStagger(Mobile from) => _table != null && _table.TryGetValue(from, out var amount) ? amount : 0;
}
