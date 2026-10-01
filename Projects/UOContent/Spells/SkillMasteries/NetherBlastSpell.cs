using System;
using Server.Items;
using Server.Targeting;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/NetherBlast.cs) —
// Mysticism mastery: a mana-draining nether strike. Heavily simplified from ServUO's
// original — five walking "nova" ground-effect waves along a direction, ticking AoE damage
// each second via AcquireIndirectTargets/SpellHelper.AdjustField (RunUO-era area-spell
// machinery this codebase's Spell class doesn't have) — down to one direct hit on a
// targeted Mobile. Real mana-drain and damage math kept.
public class NetherBlastSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new(
        "Nether Blast", "In Vas Por Grav", 204, 9061,
        typeof(NoxCrystal), typeof(DaemonBone)
    );

    public override double RequiredSkill => 90;
    public override double UpKeep => 0;
    public override int RequiredMana => 40;
    public override bool PartyEffects => false;
    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.0);

    public override SkillName CastSkill => SkillName.Mysticism;

    public override SkillName DamageSkill =>
        Caster.Skills[SkillName.Focus].Value > Caster.Skills[SkillName.Imbuing].Value ? SkillName.Focus : SkillName.Imbuing;

    public NetherBlastSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (HasSpell(Caster, GetType()))
        {
            Caster.SendMessage("Вы не можете использовать эту способность, пока не истечёт предыдущая.");
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast() => Caster.Target = new MasteryTarget(this, 10, false, TargetFlags.Harmful);

    protected override void OnTarget(object o)
    {
        if (o is not Mobile m || !CheckHSequence(m))
        {
            return;
        }

        SpellHelper.Turn(Caster, m);

        var skill = (Caster.Skills[SkillName.Mysticism].Value + Caster.Skills[DamageSkill].Value * 2) / 3;
        skill /= m.Player ? 3.5 : 2;

        var damage = (int)skill + Utility.RandomMinMax(-3, 3);

        Caster.DoHarmful(m);
        m.FixedParticles(0x374A, 1, 15, 9502, 97, 3, (EffectLayer)255);
        Caster.PlaySound(0x211);

        AOS.Damage(m, Caster, damage, 0, 0, 0, 0, 100, 0, DamageType.Spell);

        var manaRip = Math.Min(m.Mana, damage / 4);

        if (manaRip <= 0)
        {
            return;
        }

        m.Mana -= manaRip;
        Caster.Mana += manaRip;
    }
}
