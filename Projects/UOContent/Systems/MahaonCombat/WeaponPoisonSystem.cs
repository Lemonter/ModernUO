using Server.Items;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Mahaon: gives poisoned weapons their pre-AOS behaviour back.
///
///     In AOS the poison you painstakingly applied to a blade does nothing on its own —
///     the only way to deliver it is the InfectiousStrike weapon ability, which exists on
///     a handful of weapon types (BaseKnife/fencing daggers/etc.) and costs 15 mana per
///     swing. Every other weapon could not even be poisoned (Poisoning.cs refused the
///     target), and the few that could never poisoned anybody unless the player armed the
///     special move. That is why "способность не срабатывает": nothing in the AOS code
///     path ever looks at Poison/PoisonCharges during a normal hit.
///
///     The old pre-AOS delivery lived in BaseSword/BaseSpear/BaseKnife.OnHit behind a
///     `!Core.AOS` gate — a flat 50% per hit, one charge burned per swing whether or not
///     it landed. This puts it back in BaseWeapon.OnHit (so it covers every weapon type,
///     Swordsmanship and Fencing included), scales the chance with the Poisoning skill,
///     and only burns a charge when the poison actually goes in, so a full dose is 18
///     real applications instead of 18 swings.
///
///     InfectiousStrike is left alone and takes precedence — when the attacker armed it,
///     this passive stays out of the way so the ability's own (stronger, skill-boosted)
///     application is the one that lands.
/// </summary>
public static class WeaponPoisonSystem
{
    private const double MinChance = 0.30;
    private const double MaxChance = 0.60;

    public static double GetApplyChance(Mobile attacker)
    {
        var skill = attacker?.Skills.Poisoning.Value ?? 0.0;

        var chance = MinChance + (MaxChance - MinChance) * (skill / 100.0);

        // «Ядовитый клинок» — обычный перк категории Ниндзя: яд с его клинка сходит
        // заметно охотнее.
        if (Systems.MahaonProfessions.ProfessionSystem.HasFullKit(
                attacker, Systems.MahaonProfessions.ProfessionCategory.Ninjitsu
            ))
        {
            chance += 0.25;
        }

        return System.Math.Min(chance, 0.95);
    }

    public static void OnWeaponHit(Mobile attacker, Mobile defender, BaseWeapon weapon, WeaponAbility ability)
    {
        if (attacker == null || defender == null || weapon == null)
        {
            return;
        }

        // InfectiousStrike delivers the dose itself a few lines later in OnHit — do not
        // spend a second charge on the same swing.
        if (ability is InfectiousStrike)
        {
            return;
        }

        var poison = weapon.Poison;

        if (poison == null || weapon.PoisonCharges <= 0)
        {
            return;
        }

        var skill = attacker.Skills[SkillName.Poisoning];

        if (skill == null)
        {
            return;
        }

        // Training in combat. Routed through the shared SkillCheck rather than rolling
        // GetApplyChance by hand, so a swing with a poisoned weapon goes through exactly
        // the same machinery as any other skill use: the standard gain curve, the
        // anti-macro guard (keyed on the defender, so beating one dummy forever does not
        // train), the region AllowGain check and the periodic stat gain. Its return value
        // IS the delivery roll — chance still comes from GetApplyChance, so the balance is
        // unchanged; this only adds the gain that combat use never had. The check runs
        // before ApplyPoison on purpose: swinging at a poison-immune target is still
        // practice, it just cannot land.
        if (!Misc.SkillCheck.CheckSkill(attacker, skill, defender, GetApplyChance(attacker)))
        {
            return;
        }

        var result = defender.ApplyPoison(attacker, poison);

        if (result == ApplyPoisonResult.Immune)
        {
            return;
        }

        --weapon.PoisonCharges;

        if (weapon.PoisonCharges <= 0)
        {
            weapon.Poison = null;
            attacker.SendMessage(0x3B2, "Яд на оружии закончился.");
        }

        defender.PlaySound(0xDD);
        defender.FixedParticles(0x374A, 10, 15, 5021, EffectLayer.Waist);

        attacker.SendLocalizedMessage(1008096, true, defender.Name);  // You have poisoned your target :
        defender.SendLocalizedMessage(1008097, false, attacker.Name); //  : poisoned you!

        // Delivering a dose in combat is still practising the craft. (The Poisoning skill
        // itself already gained above, through CheckSkill.)
        ThievingSpecializationSystem.Train(attacker, ThievingSpecialization.Poisoner);
    }
}
