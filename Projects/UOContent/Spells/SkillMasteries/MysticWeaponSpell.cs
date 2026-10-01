using System;
using Server.Engines.BuffIcons;
using Server.Items;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/MysticWeapon.cs) —
// Mysticism mastery: enchants the caster's weapon with elemental procs for a duration.
// Real ServUO applies this through its "Enhancement"/ExtendedWeaponAttributes framework
// (weapon.AddMysticMod/RemoveMysticMod), which doesn't exist in this codebase — the real
// effect here (a temporary hit-effect proc) is instead applied directly through the
// weapon's own real WeaponAttributes indexer (the same AosWeaponAttribute channel every
// Imbuing property already writes through), removed again on Expire.
public class MysticWeaponSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new(
        "Mystic Weapon", "Vas Ylem Wis", -1, 9002,
        typeof(FertileDirt), typeof(Bone)
    );

    public override double RequiredSkill => 90;
    public override int RequiredMana => 40;
    public override bool PartyEffects => false;

    public override SkillName CastSkill => SkillName.Mysticism;

    public override SkillName DamageSkill =>
        Caster.Skills[SkillName.Focus].Value > Caster.Skills[SkillName.Imbuing].Value ? SkillName.Focus : SkillName.Imbuing;

    private BaseWeapon _weapon;
    private const int ProcMagnitude = 25;

    public MysticWeaponSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        var weapon = GetWeapon();

        if (weapon == null || weapon is Fists)
        {
            Caster.SendLocalizedMessage(1060179); // You must be wielding a weapon to use this ability!
            return false;
        }

        if (weapon.WeaponAttributes[AosWeaponAttribute.HitEnergyArea] >= ProcMagnitude)
        {
            Caster.SendMessage("Оружие уже под этими эффектами.");
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast()
    {
        var weapon = GetWeapon();

        if (weapon == null || weapon is Fists)
        {
            Caster.SendLocalizedMessage(1060179); // You must be wielding a weapon to use this ability!
        }
        else if (CheckSequence())
        {
            var skill = Caster.Skills[CastSkill].Value * 1.5 + Caster.Skills[DamageSkill].Value;
            var duration = (skill + GetMasteryLevel() * 50) * 2;

            weapon.WeaponAttributes[AosWeaponAttribute.HitEnergyArea] = ProcMagnitude;
            weapon.InvalidateProperties();

            if (Caster is Mobiles.PlayerMobile pm)
            {
                pm.AddBuff(new BuffInfo(BuffIcon.MysticWeapon, 1155899, 1156055, TimeSpan.FromSeconds(duration)));
            }

            Effects.SendLocationParticles(EffectItem.Create(Caster.Location, Caster.Map, EffectItem.DefaultDuration), 0x36CB, 1, 14, 0x55C, 7, 9915, 0);
            Caster.PlaySound(0x64E);

            Expires = Core.Now + TimeSpan.FromSeconds(duration);
            BeginTimer();

            _weapon = weapon;
        }

        FinishSequence();
    }

    public override void OnWeaponRemoved(BaseWeapon weapon) => Expire();

    public override void EndEffects()
    {
        if (Caster is Mobiles.PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.MysticWeapon);
        }

        if (_weapon is { Deleted: false })
        {
            _weapon.WeaponAttributes[AosWeaponAttribute.HitEnergyArea] = 0;
            _weapon.InvalidateProperties();
        }

        Caster.SendLocalizedMessage(1115273); // The enchantment on your weapon has expired.
        Caster.PlaySound(0x1ED);
    }
}
