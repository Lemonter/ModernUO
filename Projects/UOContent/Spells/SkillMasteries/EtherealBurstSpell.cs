using System;
using Server.Items;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/EtherealBurst.cs) —
// Magery mastery: full mana refill on a long cooldown. Reagents converted from ServUO's
// Reagent enum to this codebase's real Item types (Type[], not an enum) — same conversion
// every reagent-consuming spell in this codebase already uses.
public class EtherealBurstSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new(
        "Ethereal Blast", "Uus Ort Grav", -1, 9002,
        typeof(Bloodmoss), typeof(Ginseng), typeof(MandrakeRoot)
    );

    public override double RequiredSkill => 90;
    public override double UpKeep => 0;
    public override int RequiredMana => 0;
    public override bool PartyEffects => false;

    public override SkillName CastSkill => SkillName.Magery;
    public override SkillName DamageSkill => SkillName.EvalInt;

    public EtherealBurstSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            Caster.Mana = Caster.ManaMax;

            var skill = (Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value) / 2.1 + GetMasteryLevel() * 2;

            var duration = skill switch
            {
                >= 120 => 30,
                >= 100 => 60,
                >= 60  => 90,
                _      => 120
            };

            AddToCooldown(TimeSpan.FromMinutes(duration));

            Caster.PlaySound(0x102);
            Effects.SendTargetParticles(Caster, 0x376A, 35, 90, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);
            Caster.SendLocalizedMessage(1155789); // You feel completely rejuvinated!
        }

        FinishSequence();
    }
}
