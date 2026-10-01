using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Items;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/WhiteTigerForm.cs) —
// Ninjitsu mastery: normally a real Animal Form transformation (BodyMod into a
// WildWhiteTiger, defense/bleed bonuses tied to that form's AnimalFormContext). Simplified
// here to the buff/combat effects alone (Defense Chance Increase + on-hit bleed chance) via
// a toggled buff icon, without the actual polymorph — this codebase's AnimalForm/
// AnimalFormContext/DisguiseTimers/TransformationSpellHelper machinery would need its own
// verification pass before safely hooking a real BodyMod transformation through it.
public class WhiteTigerFormSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("White Tiger Form", "", -1, 9002);

    public override int RequiredMana => 10;

    public override SkillName CastSkill => SkillName.Ninjitsu;
    public override SkillName DamageSkill => SkillName.Stealth;

    public override bool BlocksMovement => false;
    public override bool RevealOnCast => false;

    public WhiteTigerFormSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    // Was: a private static Dictionary<Mobile,int> never registered with the base class's
    // Table (AddToTable/BeginTimer). MasteryInfo.OnMasteryChanged expires every spell it
    // finds in that Table when the player switches mastery away from Ninjitsu — since this
    // spell was never in it, switching mastery could never clear the buff, and CheckCast's
    // "wrong mastery path" gate then permanently blocked recasting to toggle it back off.
    // Rewritten onto the same BeginTimer/Expire/EndEffects toggle shape every other
    // mastery-spell buff (e.g. RampageSpell) already uses.
    public override void OnCast()
    {
        if (GetSpell(Caster, GetType()) is WhiteTigerFormSpell spell)
        {
            spell.Expire();
            FinishSequence();
            return;
        }

        if (!CheckSequence())
        {
            FinishSequence();
            return;
        }

        Caster.FixedParticles(0x3728, 10, 13, 2023, EffectLayer.Waist);
        Caster.PlaySound(0x208);

        _bleedMod = (int)((Caster.Skills[SkillName.Ninjitsu].Value + Caster.Skills[SkillName.Stealth].Value + GetMasteryLevel() * 40) / 3 / 10);

        Expires = Core.Now + ExpirationPeriod;
        BeginTimer();

        if (Caster is Mobiles.PlayerMobile casterPm)
        {
            // +20 Defense Chance Increase. +5 Max Defense Chance Increase Cap. Chance to
            // evade attacks. Applies bleed to victim with a max damage of ~4_ARG~.
            casterPm.AddBuff(new BuffInfo(BuffIcon.WhiteTigerForm, 1155911, 1156060, default, $"20\t5\t\t{_bleedMod}"));
        }

        Caster.Delta(MobileDelta.WeaponDamage);

        FinishSequence();
    }

    public override void EndEffects()
    {
        if (Caster is Mobiles.PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.WhiteTigerForm);
        }

        _bleedCooldown.Remove(Caster);
        Caster.Delta(MobileDelta.WeaponDamage);
    }

    private int _bleedMod;

    public static bool IsActive(Mobile m) => HasSpell(m, typeof(WhiteTigerFormSpell));

    public static bool CheckEvasion(Mobile m) => IsActive(m) && MasteryInfo.GetMasteryLevel(m, SkillName.Ninjitsu) + 2 > Utility.Random(100);

    // +20 DCI / +5 max DCI cap, matching the buff-icon tooltip numbers exactly (not scaled
    // by mastery level like the bleed chance/magnitude below — those are the only two
    // numbers the real tooltip hardcodes).
    public static int GetDciBonus(Mobile m) => IsActive(m) ? 20 : 0;

    public static int GetDefenseCap(Mobile m) => IsActive(m) ? 5 : 0;

    private static readonly Dictionary<Mobile, DateTime> _bleedCooldown = new();

    // Real dispatcher override (base SkillMasterySpell.OnHit(Mobile, ref int)), called from
    // SkillMasterySpell.OnHit's static fan-out in BaseWeapon.OnHit — was previously a static
    // method with a different signature that nothing ever called (looked wired, wasn't).
    public override void OnHit(Mobile defender, ref int damage)
    {
        if (_bleedCooldown.TryGetValue(Caster, out var until) && until > Core.Now)
        {
            return;
        }

        var bleedChance = (Caster.Skills[SkillName.Ninjitsu].Value + Caster.Skills[SkillName.Stealth].Value + GetMasteryLevel() * 40) / 3.0 / 15.0;

        if (bleedChance <= Utility.RandomDouble())
        {
            return;
        }

        Systems.MahaonCombat.BleedingSystem.ApplyBleed(defender);
        _bleedCooldown[Caster] = Core.Now + TimeSpan.FromMinutes(1);
    }
}
