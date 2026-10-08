using System;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/CalledShot.cs) —
// Throwing mastery: a timed Hit Chance/Damage Increase buff on the caster's next throws.
public class CalledShotSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Called Shot", "", -1, 9002);

    public override double RequiredSkill => 90;
    public override double UpKeep => 0;
    public override int RequiredMana => 40;

    public override SkillName CastSkill => SkillName.Throwing;
    public override SkillName DamageSkill => SkillName.Tactics;

    private int _hciBonus;
    private int _damageBonus;

    public CalledShotSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (IsInCooldown(Caster, GetType()))
        {
            return false;
        }

        if (!CheckWeapon())
        {
            Caster.SendLocalizedMessage(1156016); // You must have a throwing weapon equipped to use this ability.
            return false;
        }

        if (GetSpell(Caster, GetType()) is CalledShotSpell spell)
        {
            spell.Expire();
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            Caster.PlaySound(0x101);
            Caster.FixedEffect(0x37C4, 10, 40, 2720, 3);

            Caster.PrivateOverheadMessage(MessageType.Regular, 1150, 1156024, Caster.NetState); // *You call your next shot...*

            var duration = TimeSpan.FromSeconds(10);

            _hciBonus = (int)(Caster.Skills[DamageSkill].Value / 2.66);
            _damageBonus = (int)(Caster.Skills[CastSkill].Value / 1.6);

            Expires = Core.Now + duration;
            BeginTimer();

            AddToCooldown(TimeSpan.FromSeconds(60));

            if (Caster is PlayerMobile pm)
            {
                // Hit Chance Increase: ~1_VAL~% / Damage Increase: ~2_VAL~%
                pm.AddBuff(new BuffInfo(BuffIcon.CalledShot, 1156025, 1156026, duration, $"{_hciBonus}\t{_damageBonus}"));
            }
        }

        FinishSequence();
    }

    public override void EndEffects()
    {
        if (Caster is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.CalledShot);
        }
    }

    public override void OnHit(Mobile defender, ref int damage)
    {
        if (SpecialMove.GetCurrentMove(Caster) != null)
        {
            return;
        }

        damage += (int)(damage * (_damageBonus / 100.0));

        if (defender is PlayerMobile && damage > 100)
        {
            damage = 100;
        }
    }

    public static int GetHitChanceBonus(Mobile m) => GetSpell(m, typeof(CalledShotSpell)) is CalledShotSpell spell ? spell._hciBonus : 0;
}
