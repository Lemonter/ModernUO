using System;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/ShieldBash.cs) —
// Parry mastery: next hit or parry bashes with the shield for bonus damage and a brief
// paralyze. Low-level packet effects (Effects.SendPacket/HuedEffect/ParticleEffect) replaced
// with the higher-level Caster.FixedEffect/Effects.SendTargetParticles helpers already used
// throughout this session. `OnParried`'s real ServUO effect (recursively triggering
// weapon.OnHit) isn't attempted against this codebase's heavily-customized BaseWeapon
// pipeline — a direct AOS.Damage hit stands in for it instead, same net effect (damage +
// paralyze) without risking a feedback loop through MahaonCombat's own hooks.
public class ShieldBashSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Shield Bash", "", -1, 9002);

    public override int RequiredMana => 40;
    public override bool BlocksMovement => false;
    public override bool CancelsWeaponAbility => true;
    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

    public override SkillName CastSkill => SkillName.Parry;
    public override int ExpireMessage => 1063119; // You return to your normal stance.

    public ShieldBashSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (!HasShield())
        {
            return false;
        }

        if (HasSpell(Caster, GetType()))
        {
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            Caster.FixedEffect(0x37C4, 10, 7);
            Server.Timer.DelayCall(TimeSpan.FromMilliseconds(250), () => Effects.SendTargetParticles(Caster, 0x375A, 1, 17, 0, 0, 9502, EffectLayer.Waist, 0));
            Caster.PlaySound(0x51A);

            var duration = TimeSpan.FromSeconds(3);
            Caster.SendLocalizedMessage(1156022); // You ready your shield.

            Expires = Core.Now + duration;
            BeginTimer();

            if (Caster is PlayerMobile pm)
            {
                // Places you in an offensive stance which allows you to strike your target
                // with your shield on your next successful attack or parry.
                pm.AddBuff(new BuffInfo(BuffIcon.ShieldBash, 1155923, 1156093, duration));
            }
        }

        FinishSequence();
    }

    public override bool OnTick()
    {
        if (!HasShield())
        {
            Expire();
            return false;
        }

        return base.OnTick();
    }

    private bool HasShield()
    {
        if (!Caster.Player)
        {
            return true;
        }

        if (Caster.FindItemOnLayer(Layer.TwoHanded) is BaseShield)
        {
            return true;
        }

        Caster.SendLocalizedMessage(1156096); // You must be wielding a shield to use this ability!
        return false;
    }

    public override void EndEffects()
    {
        if (Caster is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.ShieldBash);
        }
    }

    public override void OnHit(Mobile defender, ref int damage)
    {
        if (!HasShield())
        {
            Expire();
            return;
        }

        Caster.SendLocalizedMessage(1156027); // You bash you target with your shield!

        var pvp = Caster is PlayerMobile && defender is PlayerMobile;
        var dmg = GetDamage(pvp, GetMasteryLevel());

        if (pvp)
        {
            AOS.Damage(defender, Caster, dmg, 100, 0, 0, 0, 0, 0, DamageType.Spell);
            damage /= 10;
        }
        else
        {
            damage = dmg;
        }

        Server.Timer.DelayCall(TimeSpan.FromMilliseconds(100), () =>
        {
            if (defender.Alive)
            {
                CheckParalyze(defender, TimeSpan.FromSeconds(3));
            }
        });

        Expire();
    }

    private static int GetDamage(bool pvp, int level) =>
        pvp ? Math.Min(35, Utility.RandomMinMax(27, 35) * level) : Utility.RandomMinMax(45, 65) * level;

    private void CheckParalyze(Mobile defender, TimeSpan duration)
    {
        if (ParalyzingBlow.IsImmune(defender))
        {
            Caster.SendLocalizedMessage(1070804);  // Your target resists paralysis.
            defender.SendLocalizedMessage(1070813); // You resist paralysis.
            return;
        }

        defender.FixedEffect(0x376A, 9, 32);
        defender.PlaySound(0x204);

        Caster.SendLocalizedMessage(1060163);  // You deliver a paralyzing blow!
        defender.SendLocalizedMessage(1060164); // The attack has temporarily paralyzed you!

        defender.Paralyze(duration);
        ParalyzingBlow.BeginImmunity(defender, duration);
    }

    public override void OnParried(Mobile attacker)
    {
        var dmg = GetDamage(Caster is PlayerMobile && attacker is PlayerMobile, GetMasteryLevel());
        AOS.Damage(attacker, Caster, dmg, 100, 0, 0, 0, 0, 0, DamageType.Spell);
    }
}
