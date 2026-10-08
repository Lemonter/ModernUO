using Server.Engines.BuffIcons;
using Server.Items;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/BardSpells/
// Resilience.cs) — Peacemaking mastery: regen buff. Party-wide sharing dropped (self-only)
// — see BardSpell.cs.
public class ResilienceSpell : BardSpell
{
    private static readonly SpellInfo Info = new("Resilience", "Kal Mani Tym", -1, 9002);

    public override double RequiredSkill => 90;
    public override double UpKeep => 4;
    public override int RequiredMana => 16;
    public override SkillName CastSkill => SkillName.Peacemaking;

    private int _propertyBonus;

    public ResilienceSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override void OnCast()
    {
        if (GetSpell(Caster, GetType()) is { } spell)
        {
            spell.Expire();
            Caster.SendLocalizedMessage(1115774); // You halt your spellsong.
        }
        else if (CheckSequence())
        {
            _propertyBonus = (int)(BaseSkillBonus * 2);

            Caster.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
            Caster.SendLocalizedMessage(1115738); // The bard's spellsong fills you with a feeling of resilience.

            if (Caster is Mobiles.PlayerMobile pm)
            {
                pm.AddBuff(new BuffInfo(BuffIcon.Resilience, 1115614, 1115731, default, $"{_propertyBonus}\t{_propertyBonus}\t{_propertyBonus}"));
            }

            BeginTimer();
        }

        FinishSequence();
    }

    public override void EndEffects()
    {
        if (Caster is Mobiles.PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.Resilience);
        }
    }

    public override int PropertyBonus() => _propertyBonus; // All 3 regen bonuses, read from AOS.cs

    public static bool UnderEffects(Mobile m) => HasSpell(m, typeof(ResilienceSpell));
}
