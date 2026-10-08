using Server.Engines.BuffIcons;
using Server.Items;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/BardSpells/
// inspire.cs) — Provocation mastery: Hit Chance/Spell Damage/weapon-damage buff.
// Party-wide sharing dropped (self-only) — see BardSpell.cs.
public class InspireSpell : BardSpell
{
    private static readonly SpellInfo Info = new("Inspire", "Unus Por", -1, 9002);

    public override double RequiredSkill => 90;
    public override double UpKeep => 4;
    public override int RequiredMana => 16;
    public override SkillName CastSkill => SkillName.Provocation;

    private int _propertyBonus;
    private int _damageBonus;
    private int _damageModifier;

    public InspireSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
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
            _damageBonus = (int)(BaseSkillBonus * 5);
            _damageModifier = (int)(BaseSkillBonus + 1);

            Caster.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
            Caster.SendLocalizedMessage(1115736); // You feel inspired by the bard's spellsong.

            if (Caster is Mobiles.PlayerMobile pm)
            {
                pm.AddBuff(new BuffInfo(BuffIcon.Inspire, 1115683, 1151951, default, $"{_propertyBonus}\t{_propertyBonus}\t{_damageBonus}\t{_damageModifier}"));
            }

            BeginTimer();
        }

        FinishSequence();
    }

    public override void EndEffects()
    {
        if (Caster is Mobiles.PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.Inspire);
        }
    }

    public override int PropertyBonus() => _propertyBonus;   // HCI/SDI Bonus, read from AOS.cs
    public override int DamageBonus() => _damageBonus;       // Weapon damage bonus, read from AOS.cs

    public void DoDamage(ref int damageTaken) => damageTaken += AOS.Scale(damageTaken, _damageModifier);
}
