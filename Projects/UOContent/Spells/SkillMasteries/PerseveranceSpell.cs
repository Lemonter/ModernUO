using Server.Engines.BuffIcons;
using Server.Items;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/BardSpells/
// Perseverance.cs) — Peacemaking mastery: defense chance/damage reduction buff.
// Party-wide sharing dropped (self-only) — see BardSpell.cs.
public class PerseveranceSpell : BardSpell
{
    private static readonly SpellInfo Info = new("Perseverance", "Unus Jux Sanct", -1, 9002);

    public override double RequiredSkill => 90;
    public override double UpKeep => 5;
    public override int RequiredMana => 18;
    public override SkillName CastSkill => SkillName.Peacemaking;

    private int _propertyBonus;
    private int _propertyBonus2;
    private int _damageMod;

    public PerseveranceSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
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
            _propertyBonus = (int)(BaseSkillBonus * 3);
            _propertyBonus2 = (int)(BaseSkillBonus / 2);
            _damageMod = (int)(BaseSkillBonus * 3);

            Caster.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
            Caster.SendLocalizedMessage(1115739); // The bard's spellsong fills you with a feeling of invincibility.

            if (Caster is Mobiles.PlayerMobile pm)
            {
                pm.AddBuff(new BuffInfo(BuffIcon.Perseverance, 1115615, 1115732, default, $"{_propertyBonus}\t-{_damageMod}\t{_propertyBonus2}"));
            }

            BeginTimer();
        }

        FinishSequence();
    }

    public override void EndEffects()
    {
        if (Caster is Mobiles.PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.Perseverance);
        }
    }

    public override int PropertyBonus() => _propertyBonus;   // Defense Chance Bonus
    public override int PropertyBonus2() => _propertyBonus2; // Casting Focus

    public void AbsorbDamage(ref int damage) => damage -= AOS.Scale(damage, _damageMod);
}
