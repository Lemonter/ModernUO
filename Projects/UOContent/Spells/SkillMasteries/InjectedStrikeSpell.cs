using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/InjectedStrike.cs) —
// Poisoning mastery: coat the weapon from a targeted poison potion, then the next hit
// poisons the target and applies a temporary poison-resist debuff.
public class InjectedStrikeSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Injected Strike", "", -1, 9002);

    public override int RequiredMana => 30;

    public override SkillName CastSkill => SkillName.Poisoning;
    public override SkillName DamageSkill => SkillName.Anatomy;

    public override bool CancelsWeaponAbility => true;
    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

    public override void GetCastSkills(out double min, out double max)
    {
        min = RequiredSkill;
        max = RequiredSkill + 10.0;
    }

    public InjectedStrikeSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override void OnCast()
    {
        var weapon = GetWeapon();

        if (!CheckWeapon())
        {
            Caster.SendLocalizedMessage(1060179); // You must be wielding a weapon to use this ability!
            FinishSequence();
            return;
        }

        if (weapon.Poison == null || weapon.PoisonCharges == 0)
        {
            Caster.SendLocalizedMessage(502137); // Select the poison you wish to use.
            Caster.Target = new MasteryTarget(this, 2, false, Targeting.TargetFlags.None);
            return;
        }

        if (HasSpell(Caster, GetType()))
        {
            Caster.SendLocalizedMessage(501775); // This spell is already in effect.
            FinishSequence();
            return;
        }

        if (CheckSequence())
        {
            BeginTimer();
            Caster.SendLocalizedMessage(1156138); // You ready your weapon to unleash an injected strike!

            const int bonus = 30;

            if (Caster is PlayerMobile pm)
            {
                // Your next successful attack will poison your target and reduce its
                // poison resist by: ~1_VAL~% PvM / ~2_VAL~% PvP
                pm.AddBuff(new BuffInfo(BuffIcon.InjectedStrike, 1155927, 1156163, default, $"{bonus}\t{bonus / 2}"));
            }

            Caster.FixedParticles(0x3728, 0x1, 0xA, 0x251E, 0x4F7, 7, (EffectLayer)2, 0);
            weapon.InvalidateProperties();
        }

        FinishSequence();
    }

    protected override void OnTarget(object o)
    {
        var weapon = GetWeapon();

        if (o is not BasePoisonPotion potion)
        {
            Caster.SendLocalizedMessage(502143); // The poison vial not usable.
            return;
        }

        if (!potion.IsChildOf(Caster.Backpack))
        {
            Caster.SendLocalizedMessage(1080058); // This must be in your backpack to use it.
            return;
        }

        if (!CheckSequence())
        {
            return;
        }

        if (!Caster.CheckTargetSkill(CastSkill, potion, potion.MinPoisoningSkill, potion.MaxPoisoningSkill))
        {
            Caster.SendLocalizedMessage(1010518); // You fail to apply a sufficient dose of poison
            return;
        }

        ApplyPoison(weapon, potion);
    }

    private void ApplyPoison(BaseWeapon weapon, BasePoisonPotion potion)
    {
        if (!Caster.InRange(potion.GetWorldLocation(), 2) || !Caster.InLOS(potion))
        {
            Caster.SendLocalizedMessage(502138); // That is too far away for you to use.
            return;
        }

        weapon.Poison = potion.Poison;
        weapon.PoisonCharges = 18 - potion.Poison.Level * 2;

        Caster.SendLocalizedMessage(1010517); // You apply the poison
        Caster.PlaySound(0x246);

        potion.Consume();
        Caster.Backpack.DropItem(new Bottle());

        OnCast();
    }

    public override void EndEffects()
    {
        if (Caster is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.InjectedStrike);
        }
    }

    public override void OnHit(Mobile defender, ref int damage)
    {
        var weapon = GetWeapon();

        if (!CheckWeapon())
        {
            return;
        }

        var p = weapon.Poison;

        if (p == null || weapon.PoisonCharges <= 0)
        {
            Caster.SendLocalizedMessage(1061141); // Your weapon must have a dose of poison to perform an infectious strike!
            return;
        }

        var noChargeChance = MasteryInfo.NonPoisonConsumeChance(Caster);

        if (noChargeChance == 0 || noChargeChance < Utility.Random(100))
        {
            --weapon.PoisonCharges;
        }
        else
        {
            Caster.SendLocalizedMessage(1156095); // Your mastery of poisoning allows you to use your poison charge without consuming it.
        }

        if ((Caster.Skills[SkillName.Poisoning].Value / 100.0) > Utility.RandomDouble() && p.Level < 3)
        {
            var newPoison = Poison.GetPoison(p.Level + 1);

            if (newPoison != null)
            {
                p = newPoison;
                Caster.SendLocalizedMessage(1060080); // Your precise strike has increased the level of the poison by 1
                defender.SendLocalizedMessage(1060081); // The poison seems extra effective!
            }
        }

        defender.PlaySound(0xDD);
        defender.FixedParticles(0x3728, 244, 25, 9941, 1266, 0, EffectLayer.Waist);

        if (defender.ApplyPoison(Caster, p) != ApplyPoisonResult.Immune)
        {
            Caster.SendLocalizedMessage(1008096, true, defender.Name);  // You have poisoned your target :
            defender.SendLocalizedMessage(1008097, false, Caster.Name); //  : poisoned you!
        }

        var malus = 30;

        if (defender is PlayerMobile)
        {
            malus /= 2;
        }

        if (weapon is BaseRanged)
        {
            malus /= 2;
        }

        var mod = new ResistanceMod(ResistanceType.Poison, "MasteryInjectedStrike", -malus, defender);
        defender.AddResistanceMod(mod);

        if (defender is PlayerMobile defenderPm)
        {
            // ~2_NAME~ reduces your poison resistance by ~1_VAL~.
            defenderPm.AddBuff(new BuffInfo(BuffIcon.InjectedStrikeDebuff, 1155927, 1156133, TimeSpan.FromSeconds(7), $"{malus}\t{Caster.Name}"));
        }

        Server.Timer.DelayCall(TimeSpan.FromSeconds(7), () => defender.RemoveResistanceMod(mod));

        Expire();
    }

    public override void OnWeaponRemoved(BaseWeapon weapon) => Expire();
}
