using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Toughness.cs) —
// Macing mastery: toggled hit point buff. `SendCastEffect` (a ServUO Spell virtual) doesn't
// exist on this codebase's Spell — the cast-sound logic moved inline into OnCast.
public class ToughnessSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Toughness", "", -1, 9002);

    public override double UpKeep => 20;
    public override int RequiredMana => 20;

    public override SkillName CastSkill => SkillName.Macing;
    public override SkillName DamageSkill => SkillName.Tactics;

    private int _hpBonus;

    public ToughnessSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (!CheckWeapon())
        {
            Caster.SendLocalizedMessage(1155983); // You must have a mace weapon equipped to use this ability!
            return false;
        }

        if (GetSpell(Caster, GetType()) is ToughnessSpell spell)
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
            Effects.SendTargetParticles(Caster, 0x37CC, 1, 40, 1953, 0, 9907, EffectLayer.LeftFoot, 0);

            if (Caster.Player)
            {
                Caster.PlaySound(Caster.Female ? 0x338 : 0x44A);
            }
            else if (Caster is BaseCreature bc)
            {
                Caster.PlaySound(bc.GetAngerSound());
            }

            Caster.PlaySound(0x1EE);

            _hpBonus = (int)(BaseSkillBonus / 4);

            BeginTimer();

            if (Caster is PlayerMobile pm)
            {
                // Hit Point Increase: ~1_VAL~. Mana Upkeep Cost: ~2_VAL~.
                pm.AddBuff(new BuffInfo(BuffIcon.Toughness, 1155985, 1155986, default, $"{_hpBonus}\t{ScaleMana((int)UpKeep)}"));
            }
        }

        FinishSequence();
    }

    public override void EndEffects()
    {
        if (Caster is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.Toughness);
        }
    }

    public static int GetHPBonus(Mobile m) => GetSpell(m, typeof(ToughnessSpell)) is ToughnessSpell spell ? spell._hpBonus : 0;
}
