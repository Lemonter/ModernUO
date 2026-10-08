using System;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/DeathRay.cs) —
// Magery mastery: a channeled beam that ticks energy damage on a target as long as the
// caster stays in place, has mana, and doesn't act.
public class DeathRaySpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new(
        "Death Ray", "In Grav Corp", 204, 9061,
        typeof(BlackPearl), typeof(Bloodmoss), typeof(SpidersSilk)
    );

    private Point3D _location;
    private ResistanceMod _mod;

    public override double UpKeep => 35;
    public override int RequiredMana => 50;
    public override int DamageThreshold => 0;
    public override bool DamageCanDisrupt => true;
    public override double TickTime => 3;

    public override int UpkeepCancelMessage => 1155874; // You do not have enough mana to keep your death ray active.
    public override int DisruptMessage => 1155793;       // This action disturbs the focus necessary to keep your death ray active and it dissipates.

    public override TimeSpan ExpirationPeriod => TimeSpan.FromMinutes(360);

    public override SkillName CastSkill => SkillName.Magery;
    public override SkillName DamageSkill => SkillName.EvalInt;

    public DeathRaySpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override void OnCast() => Caster.Target = new MasteryTarget(this);

    protected override void OnTarget(object o)
    {
        if (o is not Mobile m)
        {
            return;
        }

        if (GetSpell<DeathRaySpell>(Caster, m) != null)
        {
            Caster.SendLocalizedMessage(1156094); // Your target is already under the effect of this ability.
            return;
        }

        if (!CheckHSequence(m))
        {
            return;
        }

        if (CheckResisted(m))
        {
            m.SendLocalizedMessage(1156101);      // You resist the effects of death ray.
            Caster.SendLocalizedMessage(1156102); // Your target resists the effects of death ray.
            return;
        }

        SpellHelper.CheckReflect(0, Caster, ref m);
        _location = Caster.Location;

        m.FixedParticles(0x374A, 1, 15, 5054, 0x7A2, 7, EffectLayer.Head);
        Caster.FixedParticles(0x0000, 10, 5, 2054, EffectLayer.Head);

        var damage = (Caster.Skills[CastSkill].Base + Caster.Skills[DamageSkill].Base) * (GetMasteryLevel() * .8);
        damage /= Target is PlayerMobile ? 5.15 : 2.5;

        var mod = (int)Caster.Skills[DamageSkill].Value / 12;
        _mod = new ResistanceMod(ResistanceType.Energy, "MasteryDeathRay", -mod, m);
        m.AddResistanceMod(_mod);

        if (Caster is PlayerMobile casterPm)
        {
            // Deals ~2_DAMAGE~ to ~1_NAME~ every 3 seconds while in range. Performing any action will end spell.
            casterPm.AddBuff(new BuffInfo(BuffIcon.DeathRay, 1155896, 1156085, default, $"{(int)damage}\t{m.Name}"));
        }

        if (m is PlayerMobile targetPm)
        {
            targetPm.AddBuff(new BuffInfo(BuffIcon.DeathRayDebuff, 1155896, 1156086, default, mod.ToString()));
        }

        Target = m;
        BeginTimer();
    }

    public override void EndEffects()
    {
        if (Target != null && _mod != null)
        {
            Target.RemoveResistanceMod(_mod);
        }

        if (Caster is PlayerMobile casterPm)
        {
            casterPm.RemoveBuff(BuffIcon.DeathRay);
        }

        if (Target is PlayerMobile targetPm)
        {
            targetPm.RemoveBuff(BuffIcon.DeathRayDebuff);
        }
    }

    public override bool OnTick()
    {
        if (!base.OnTick())
        {
            return false;
        }

        if (Target == Caster || !Target.Alive)
        {
            Expire();
            Caster.SendLocalizedMessage(1156097); // Your ability was interrupted.
        }
        else if (Caster.Location != _location)
        {
            Expire(true);
            return false;
        }
        else
        {
            var damage = (Caster.Skills[CastSkill].Base + Caster.Skills[DamageSkill].Base) * (GetMasteryLevel() * .8);
            damage /= Target is PlayerMobile ? 5.15 : 2.5;

            SpellHelper.Damage(this, Target, (int)damage, 0, 0, 0, 0, 100);
        }

        return true;
    }
}
