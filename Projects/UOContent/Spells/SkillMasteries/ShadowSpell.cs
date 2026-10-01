using System;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Shadow.cs) — Ninjitsu
// mastery: toggled stealth buff, harder to detect/unhide while active.
public class ShadowSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Shadow", "", -1, 9002);

    public override double UpKeep => 4;
    public override int RequiredMana => 10;
    public override bool RevealOnTick => false;
    public override bool RevealOnCast => false;

    public override SkillName CastSkill => SkillName.Ninjitsu;
    public override SkillName DamageSkill => SkillName.Stealth;

    public ShadowSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        var spell = GetSpell(Caster, GetType());

        if (spell != null)
        {
            spell.Expire();

            if (Caster is PlayerMobile pm)
            {
                pm.RemoveBuff(BuffIcon.Shadow);
            }

            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            Caster.FixedParticles(0x3709, 10, 30, 5052, 2050, 7, EffectLayer.LeftFoot, 0);
            Caster.PlaySound(0x22F);

            var skill = (Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value + GetMasteryLevel() * 40) / 3;
            var duration = (int)(skill / 3.4);

            Expires = Core.Now + TimeSpan.FromSeconds(duration);
            BeginTimer();

            if (Caster is PlayerMobile pm)
            {
                // Increases difficulty to be detected while hidden / to unhide from taking damage.
                pm.AddBuff(new BuffInfo(BuffIcon.Shadow, 1155910, 1156059, TimeSpan.FromSeconds(duration)));
            }
        }

        FinishSequence();
    }

    public static double GetDifficultyFactor(Mobile m)
    {
        if (GetSpell(m, typeof(ShadowSpell)) is not ShadowSpell spell)
        {
            return 0.0;
        }

        var skill = (spell.Caster.Skills[spell.CastSkill].Value + spell.Caster.Skills[spell.DamageSkill].Value + spell.GetMasteryLevel() * 40) / 3;
        return skill / 150;
    }

    protected override void DoEffects() => Caster.FixedParticles(0x376A, 9, 32, 5005, 2123, 0, EffectLayer.Waist, 0);
}
