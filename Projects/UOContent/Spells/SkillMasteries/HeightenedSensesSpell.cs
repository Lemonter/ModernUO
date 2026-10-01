using System;
using Server.Engines.BuffIcons;
using Server.Items;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/HeightenSenses.cs) —
// Parry mastery: toggled parry-chance buff, requires a shield or weapon equipped.
public class HeightenedSensesSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Heightened Senses", "", -1, 9002);

    public override double UpKeep => 10;
    public override int RequiredMana => 10;
    public override double TickTime => 3;
    public override bool BlocksMovement => false;
    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

    public override SkillName CastSkill => SkillName.Parry;

    public HeightenedSensesSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (GetSpell(Caster, GetType()) is HeightenedSensesSpell spell)
        {
            spell.Expire();
            return false;
        }

        return HasShieldOrWeapon() && base.CheckCast();
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            Caster.FixedParticles(0x376A, 9, 32, 5030, 1168, 0, EffectLayer.Waist, 0);
            Caster.PlaySound(0x5BC);

            Caster.SendLocalizedMessage(1156023); // Your senses heighten!

            BeginTimer();

            if (Caster is Mobiles.PlayerMobile pm)
            {
                // +~1_ARG~% Parry Bonus. Mana Upkeep Cost: ~2_VAL~.
                pm.AddBuff(new BuffInfo(BuffIcon.HeightenedSenses, 1155925, 1156062, default, $"{GetPropertyBonus()}\t{ScaleUpkeep()}"));
            }
        }

        FinishSequence();
    }

    public override bool OnTick()
    {
        if (!HasShieldOrWeapon())
        {
            Expire();
            return false;
        }

        return base.OnTick();
    }

    public bool HasShieldOrWeapon()
    {
        if (!Caster.Player)
        {
            return true;
        }

        if (Caster.FindItemOnLayer(Layer.TwoHanded) is BaseShield)
        {
            return true;
        }

        if (Caster.Weapon is BaseWeapon and not Fists)
        {
            return true;
        }

        Caster.SendLocalizedMessage(1156096); // You must be wielding a shield to use this ability!
        return false;
    }

    protected override void DoEffects() => Caster.FixedParticles(0x376A, 9, 32, 5005, 1167, 0, EffectLayer.Waist, 0);

    public override void EndEffects()
    {
        if (Caster is Mobiles.PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.HeightenedSenses);
        }
    }

    private int GetPropertyBonus() => (int)((Caster.Skills[CastSkill].Value + GetWeaponSkill() + GetMasteryLevel() * 40) / 3) / 10;

    public static double GetParryBonus(Mobile m) =>
        GetSpell(m, typeof(HeightenedSensesSpell)) is HeightenedSensesSpell spell ? spell.GetPropertyBonus() / 100.0 : 0;
}
