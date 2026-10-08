using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/HolyFistSpell.cs) —
// Chivalry mastery: a direct energy strike, boosted vs undead and by karma. Simplified:
// targets Mobile directly (not IDamageable), drops the reflect-swap and the temporary
// walk-speed slow (SendSpeedControl — client speed-control packet API not used elsewhere
// in this codebase).
public class HolyFistSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Holy Fist", "Kal Vas Grav", -1, 9002);

    public override double RequiredSkill => 90;
    public override int RequiredMana => 50;

    public override SkillName CastSkill => SkillName.Chivalry;
    public override SkillName DamageSkill => SkillName.Chivalry;

    public int RequiredTithing => 100;
    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.5);

    public HolyFistSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (Caster.Player && Caster.TithingPoints < RequiredTithing)
        {
            Caster.SendLocalizedMessage(1060173, RequiredTithing.ToString()); // You must have at least ~1_TITHE_REQUIREMENT~ Tithing Points to use this ability,
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast() => Caster.Target = new MasteryTarget(this);

    protected override void OnTarget(object o)
    {
        if (o is not Mobile m || !CheckHSequence(m))
        {
            return;
        }

        SpellHelper.Turn(Caster, m);

        var skill = (Caster.Skills[CastSkill].Value + GetWeaponSkill() + GetMasteryLevel() * 40) / 3;
        double damage = skill + Caster.Karma / 1000.0 + Utility.RandomMinMax(0, 5);

        if (m is BaseCreature bc && IsUndead(bc))
        {
            damage *= 1.5;
        }
        else if (m is PlayerMobile)
        {
            damage = Math.Min(35, damage);
        }

        Caster.MovingParticles(m, 0x9BB5, 7, 0, false, true, 9502, 4019, 0x160);
        Caster.PlaySound(0x5CE);

        SpellHelper.Damage(this, m, damage, 0, 0, 0, 0, 100);
    }

    public override bool CheckSequence()
    {
        var requiredTithing = RequiredTithing;

        if (Caster.Player && Caster.TithingPoints < requiredTithing)
        {
            Caster.SendLocalizedMessage(1060173, RequiredTithing.ToString());
            return false;
        }

        if (Caster.Player)
        {
            Caster.TithingPoints -= requiredTithing;
        }

        return base.CheckSequence();
    }

    private static bool IsUndead(BaseCreature bc)
    {
        var entry = SlayerGroup.GetEntryByName(SlayerName.Silver);
        return entry != null && entry.Slays(bc);
    }
}
