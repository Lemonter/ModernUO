using System;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Spellweaving;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/ManaShield.cs) —
// Spellweaving mastery: chance to absorb half of any incoming hit as mana instead of
// health, for 10 minutes.
public class ManaShieldSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Mana Shield", "Faerkulggen", -1, 9061);

    public override double RequiredSkill => 90;
    public override double UpKeep => 0;
    public override int RequiredMana => 40;
    public override bool PartyEffects => false;
    public override bool RevealOnTick => false;

    public override SkillName CastSkill => SkillName.Spellweaving;
    public override SkillName DamageSkill => SkillName.Meditation;

    public double Chance { get; set; }

    public ManaShieldSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        // ServUO gates this behind having completed the epic arcanist quest
        // (PlayerMobile.Spellweaving) — no such flag exists on this codebase's
        // PlayerMobile; the RequiredSkill >= 90 Spellweaving gate above is the real
        // remaining barrier.
        if (GetSpell(Caster, GetType()) is { } spell)
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
            var skill = (Caster.Skills[CastSkill].Value + ArcanistSpell.GetFocusLevel(Caster) * 20) / 2 + GetMasteryLevel() * 20 + 20;
            Chance = skill / 13.0 / 100.0;

            Expires = Core.Now + TimeSpan.FromSeconds(600);
            BeginTimer();

            Caster.PlaySound(0x29);
            Caster.FixedParticles(0x4B8F, 0x1, 0xF, 9502, 0x811, 0, EffectLayer.Waist);

            if (Caster is PlayerMobile pm)
            {
                // ~1_CHANCE~% chance to reduce incoming damage by 50%. Costs 50% of the
                // absorbed damage in mana.
                pm.AddBuff(new BuffInfo(BuffIcon.ManaShield, 1155902, 1156056, TimeSpan.FromSeconds(600), $"{(int)(Chance * 100)}\t50\t50"));
            }
        }

        FinishSequence();
    }

    public override void EndEffects()
    {
        Caster.SendLocalizedMessage(1156087); // Your Mana Shield has expired.

        if (Caster is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.ManaShield);
        }
    }

    public override void OnDamaged(Mobile attacker, Mobile defender, DamageType type, ref int damage)
    {
        if (Chance < Utility.RandomDouble())
        {
            return;
        }

        var toShield = damage / 2;

        if (defender.Mana >= toShield)
        {
            defender.Mana -= toShield;
            damage -= toShield;
        }
        else
        {
            damage -= defender.Mana;
            defender.Mana = 0;
        }
    }
}
