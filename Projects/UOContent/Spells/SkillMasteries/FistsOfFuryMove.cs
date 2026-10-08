using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Items;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/FistsOfFury.cs) —
// Wrestling mastery on-hit move: land three unarmed hits in a row on whoever damages you
// within 2 tiles, the third dealing bonus direct damage.
public class FistsOfFuryMove : SkillMasteryMove
{
    public override int BaseMana => 20;
    public override double RequiredSkill => 90.0;

    public override SkillName MoveSkill => SkillName.Wrestling;
    public override TextDefinition AbilityMessage => 1155895; // You ready yourself to unleash your fists of fury!
    public override TimeSpan CooldownPeriod => TimeSpan.FromSeconds(20);

    private Dictionary<Mobile, FistsOfFuryContext> _table;

    public override bool Validate(Mobile from)
    {
        if (!CheckWeapon(from))
        {
            from.SendLocalizedMessage(1155979); // You may not wield a weapon and use this ability.
            return false;
        }

        return base.Validate(from) && CheckMana(from, true);
    }

    public override void OnUse(Mobile from)
    {
        var damageSkill = from.Skills[SkillName.Anatomy].Value > from.Skills[SkillName.EvalInt].Value ? SkillName.Anatomy : SkillName.EvalInt;
        var duration = (from.Skills[MoveSkill].Value + from.Skills[damageSkill].Value) / 24;

        AddToCooldown(from);

        Timer.DelayCall(TimeSpan.FromMilliseconds(400), () =>
        {
            Effects.SendTargetParticles(from, 0x376A, 1, 40, 2724, 5, 9907, EffectLayer.Waist, 0);
            Effects.SendTargetParticles(from, 0x37CC, 1, 40, 2724, 5, 9907, EffectLayer.Waist, 0);
            from.PlaySound(0x101);
            from.PlaySound(0x056D);

            Timer.DelayCall(TimeSpan.FromSeconds(duration), () => Expire(from));

            if (from is Mobiles.PlayerMobile pm)
            {
                // You prepare yourself to attempt to land three hits in rapid succession
                // to the next target that damages you within a 2 tile radius.
                pm.AddBuff(new BuffInfo(BuffIcon.FistsOfFury, 1155930, 1156256, TimeSpan.FromSeconds(duration)));
            }
        });
    }

    public override void OnDamaged(Mobile attacker, Mobile defender, DamageType type, ref int damage)
    {
        if (defender == null || attacker == null)
        {
            return;
        }

        if (defender.Weapon is not Fists wep)
        {
            defender.SendLocalizedMessage(1155979); // You may not wield a weapon and use this ability.
            Expire(defender);
            return;
        }

        if (!defender.InRange(attacker.Location, 2) || UnderEffects(defender))
        {
            return;
        }

        _table ??= new Dictionary<Mobile, FistsOfFuryContext>();
        _table[defender] = new FistsOfFuryContext(attacker);

        defender.NextCombatTime = Core.TickCount + (int)wep.GetDelay(defender).TotalMilliseconds;

        for (var i = 0; i < 3; i++)
        {
            wep.OnSwing(defender, attacker);
        }

        if (_table[defender].Hit > 0)
        {
            attacker.FixedParticles(0x36BD, 20, 10, 5044, 2724, 0, EffectLayer.Head, 0);
            attacker.PlaySound(0x3B3);
        }

        _table.Remove(defender);

        if (_table.Count == 0)
        {
            _table = null;
        }
    }

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
        if (!UnderEffects(attacker) || _table[attacker].Target != defender)
        {
            return;
        }

        _table[attacker].Hit++;

        if (_table[attacker].Hit < 3)
        {
            return;
        }

        var level = MasteryInfo.GetMasteryLevel(attacker, MoveSkill);
        AOS.Damage(defender, attacker, Utility.RandomMinMax(level + 1, level * 7 - 1), 100, 0, 0, 0, 0, 0, DamageType.Spell);
    }

    public void Expire(Mobile from) => ClearCurrentMove(from);

    public override void OnClearMove(Mobile from)
    {
        if (from is Mobiles.PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.FistsOfFury);
        }
    }

    private bool UnderEffects(Mobile from) => _table != null && _table.ContainsKey(from);

    private class FistsOfFuryContext
    {
        public Mobile Target { get; }
        public int Hit { get; set; }

        public FistsOfFuryContext(Mobile target) => Target = target;
    }
}
