using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/FocusedEye.cs) —
// Swords mastery: toggled Hit Chance Increase buff.
public class FocusedEyeSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Focused Eye", "", -1, 9002);

    public override double RequiredSkill => 90;
    public override double UpKeep => 20;
    public override int RequiredMana => 20;

    public override SkillName CastSkill => SkillName.Swords;
    public override SkillName DamageSkill => SkillName.Tactics;

    private int _propertyBonus;

    public FocusedEyeSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (GetSpell(Caster, GetType()) is { } spell)
        {
            spell.Expire();
            return false;
        }

        if (!CheckWeapon())
        {
            Caster.SendLocalizedMessage(1156006); // You must have a swordsmanship weapon equipped to use this ability.
            return false;
        }

        return base.CheckCast();
    }

    public override void OnBeginCast()
    {
        base.OnBeginCast();
        Caster.PlaySound(0x1FD);
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            _propertyBonus = (int)((Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value + GetMasteryLevel() * 40) / 12);

            Caster.PrivateOverheadMessage(MessageType.Regular, 1150, 1156002, Caster.NetState); // *You focus your eye on your opponents!*

            if (Caster is PlayerMobile pm)
            {
                // +~1_VAL~% Hit Chance Increase. Mana Upkeep Cost: ~2_VAL~.
                pm.AddBuff(new BuffInfo(BuffIcon.FocusedEye, 1156003, 1156004, default, $"{_propertyBonus}\t{ScaleUpkeep()}"));
            }

            Caster.PlaySound(0x101);
            Effects.SendTargetParticles(Caster, 0x3789, 1, 40, 2726, 5, 9907, EffectLayer.RightFoot, 0);

            BeginTimer();
        }

        FinishSequence();
    }

    public override void OnExpire()
    {
        if (Caster is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.FocusedEye);
        }
    }

    public static int HitChanceBonus(Mobile attacker) =>
        GetSpell(attacker, typeof(FocusedEyeSpell)) is FocusedEyeSpell spell ? spell._propertyBonus : 0;
}
