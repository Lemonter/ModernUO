using System;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Tolerance.cs) —
// Poisoning mastery: toggle that reduces incoming poison strength for a stamina cost.
public class ToleranceSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Tolerance", "", -1, 9002);

    public override int RequiredMana => 20;
    public override SkillName CastSkill => SkillName.Poisoning;

    public ToleranceSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (GetSpell(Caster, typeof(ToleranceSpell)) is ToleranceSpell spell)
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
            Caster.SendSound(0xF6);
            Effects.SendTargetParticles(Caster, 0x3709, 10, 30, 1166, 0, 9907, EffectLayer.LeftFoot, 0);

            BeginTimer();

            if (Caster is PlayerMobile pm)
            {
                // Reduces poison strength when poisoned at the cost of stamina.
                pm.AddBuff(new BuffInfo(BuffIcon.Tolerance, 1155926, 1156063));
            }
        }

        FinishSequence();
    }

    public override void EndEffects()
    {
        if (Caster is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.Tolerance);
        }
    }

    /// <summary>Called when poison would be applied to a mastery-holder — reduces the
    /// effective poison level at a stamina cost, or blocks nothing if stamina's too low
    /// (real hook point would be Mobile.ApplyPoison; not wired in this pass — see the
    /// session writeup for why the mastery combat pipeline is largely additive/non-
    /// invasive rather than patched into every poison/damage call site).</summary>
    public static bool OnPoisonApplied(Mobile m)
    {
        if (GetSpell(m, typeof(ToleranceSpell)) is not ToleranceSpell spell)
        {
            return false;
        }

        var stamCost = (m.Skills[spell.CastSkill].Base + MasteryInfo.GetMasteryLevel(m, SkillName.Poisoning) * 30 + 10) / 2;
        stamCost /= 4;
        stamCost = Math.Max(18, 25 - stamCost + 18);

        if (m.Stam < (int)stamCost)
        {
            spell.Caster.SendLocalizedMessage(1156036, ((int)stamCost).ToString()); // You must have at least ~1_STAM_REQUIREMENT~ Stamina to use this ability.
            return false;
        }

        spell.Caster.Stam -= (int)stamCost;
        return true;
    }
}
