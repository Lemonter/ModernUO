using System;
using Server.Engines.BuffIcons;
using Server.Items;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/PlayingTheOdds.cs) —
// Archery mastery: a self Hit Chance/Swing Speed buff with reduced bow range. Simplified
// relative to the original in two ways: real ServUO shares this buff with the caster's
// whole party (this codebase's mastery port drops party-wide sharing entirely — see
// SkillMasterySpell.cs header) and its AoE "weaken everyone nearby" opener
// (AcquireIndirectTargets/HitLower.ApplyDefense — RunUO-era area-spell/weapon-ability
// machinery not present here) is dropped; the self-buff (the actual named effect) is real.
public class PlayingTheOddsSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Playing the Odds", "", -1, 9002);

    public override double RequiredSkill => 90;
    public override double UpKeep => 0;
    public override int RequiredMana => 25;

    public override SkillName CastSkill => SkillName.Archery;
    public override SkillName DamageSkill => SkillName.Tactics;

    private int _hciBonus;
    private int _ssiBonus;

    public PlayingTheOddsSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
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
            Caster.SendLocalizedMessage(1156000); // You must have an Archery weapon to use this ability!
            return false;
        }

        if (HasSpell(Caster, GetType()))
        {
            Caster.SendLocalizedMessage(1062945); // That ability is already in effect.
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast()
    {
        var weapon = GetWeapon();

        if (weapon != null && CheckSequence())
        {
            weapon.PlaySwingAnimation(Caster);

            var skill = (Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value) / 2;
            var duration = TimeSpan.FromMinutes(1);

            _hciBonus = (int)Math.Max(45, skill / 2.667);
            _ssiBonus = (int)Math.Max(30, skill / 4);

            if (Caster is Mobiles.PlayerMobile pm)
            {
                // Your bow range has been reduced as you play the odds.
                pm.AddBuff(new BuffInfo(BuffIcon.PlayingTheOddsDebuff, 1155913, 1156091, duration));
                pm.AddBuff(new BuffInfo(BuffIcon.PlayingTheOdds, 1155913, 1155998, duration, $"{Caster.Name}\t{_hciBonus}\t{_ssiBonus}"));
            }

            Caster.SendLocalizedMessage(1156091); // Your bow range has been reduced as you play the odds.

            Expires = Core.Now + duration;
            BeginTimer();

            AddToCooldown(TimeSpan.FromSeconds(90));

            weapon.InvalidateProperties();
        }

        FinishSequence();
    }

    public override void EndEffects()
    {
        GetWeapon()?.InvalidateProperties();

        if (Caster is Mobiles.PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.PlayingTheOdds);
            pm.RemoveBuff(BuffIcon.PlayingTheOddsDebuff);
        }

        Caster.SendLocalizedMessage(1156092); // Your bow range has returned to normal.
    }

    public static int HitChanceBonus(Mobile m) => GetSpell(m, typeof(PlayingTheOddsSpell)) is PlayingTheOddsSpell spell ? spell._hciBonus : 0;

    public static int SwingSpeedBonus(Mobile m) => GetSpell(m, typeof(PlayingTheOddsSpell)) is PlayingTheOddsSpell spell ? spell._ssiBonus : 0;

    public static int RangeModifier(BaseWeapon weapon)
    {
        if (weapon is BaseRanged and not BaseThrown && weapon.RootParent is Mobile m &&
            GetSpell(m, typeof(PlayingTheOddsSpell)) != null)
        {
            return weapon.DefMaxRange / 2;
        }

        return weapon.DefMaxRange;
    }
}
