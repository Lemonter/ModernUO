using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Rejuvinate.cs) —
// Chivalry mastery: a strong heal/mana/stam refill plus a cure and stat-curse cleanse.
// Simplified: ServUO also cleanses a long specific list of individual curse spells
// (EvilOmen/Strangle/CorpseSkin/Curse/MortalStrike/BloodOath/MindRot) by calling each
// spell's own removal method directly — kept here to just the cure/stat-offset/paralyze
// cleanse (the broadly-applicable part); the rest would need verifying each of those
// spell classes' exact static API in this codebase one at a time for a fairly minor
// flavor addition.
public class RejuvenateSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Rejuvenate", "In Vas Ort Grav Mani", 204, 9061);

    public override double RequiredSkill => 90;
    public override double UpKeep => 0;
    public override int RequiredMana => 10;

    public int RequiredTithing => 100;

    public override SkillName CastSkill => SkillName.Chivalry;
    public override SkillName DamageSkill => SkillName.Chivalry;

    public RejuvenateSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (Caster.Player && Caster.TithingPoints < RequiredTithing)
        {
            Caster.SendLocalizedMessage(1060173, RequiredTithing.ToString()); // You must have at least ~1_TITHE_REQUIREMENT~ Tithing Points to use this ability,
            return false;
        }

        if (GetWeapon() == null)
        {
            Caster.SendLocalizedMessage(1156006); // You must have a swordsmanship weapon equipped to use this ability.
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast() => Caster.Target = new MasteryTarget(this);

    protected override void OnTarget(object o)
    {
        if (o is not Mobile m)
        {
            Caster.SendLocalizedMessage(1046439); // That is not a valid target.
            return;
        }

        if (m.IsDeadBondedPet || (m is BaseCreature { IsAnimatedDead: true }) || m is Golem)
        {
            Caster.SendLocalizedMessage(1046439); // That is not a valid target.
            return;
        }

        if (m.Hits > m.HitsMax && m.Stam >= m.StamMax && m.Mana >= m.ManaMax)
        {
            Caster.SendLocalizedMessage(1155788); // Your target is already at full health, mana and stamina!
            return;
        }

        if (!CheckBSequence(m))
        {
            return;
        }

        var rejuv = Math.Min(1.0, GetMasteryLevel() * 33.3 / 100);

        var hitsNeeds = m.HitsMax - m.Hits;
        var stamNeeds = m.StamMax - m.Stam;
        var manaNeeds = m.ManaMax - m.Mana;

        if (hitsNeeds > 0)
        {
            var toRejuv = (int)Math.Ceiling(hitsNeeds * rejuv);
            if (toRejuv > 0)
            {
                SpellHelper.Heal(toRejuv, m, Caster, false);
            }
        }

        if (stamNeeds > 0)
        {
            var toRejuv = (int)Math.Ceiling(stamNeeds * rejuv);
            if (toRejuv > 0)
            {
                m.Stam += toRejuv;
            }
        }

        if (manaNeeds > 0)
        {
            var toRejuv = (int)Math.Ceiling(manaNeeds * rejuv);
            if (toRejuv > 0)
            {
                m.Mana += toRejuv;
            }
        }

        if (Caster.Karma > Utility.Random(5000))
        {
            if (m.Poisoned)
            {
                m.CurePoison(Caster);
            }

            m.RemoveStatMod("[Magic] Str Offset");
            m.RemoveStatMod("[Magic] Dex Offset");
            m.RemoveStatMod("[Magic] Int Offset");
            m.Paralyzed = false;
        }

        Caster.PlaySound(0x102);
        m.SendLocalizedMessage(1155789); // You feel completely rejuvenated!

        if (Caster != m)
        {
            m.PlaySound(0x102);
            Caster.SendLocalizedMessage(1155790); // Your target has been rejuvenated!
        }

        var skill = ((int)Caster.Skills[CastSkill].Value + GetWeaponSkill() + GetMasteryLevel() * 40) / 3;
        var duration = skill switch
        {
            >= 120 => 60,
            >= 110 => 120,
            _      => 180
        };

        var cooldown = Caster.AccessLevel == AccessLevel.Player ? TimeSpan.FromMinutes(duration) : TimeSpan.FromSeconds(10);
        AddToCooldown(cooldown);
    }

    public override bool CheckSequence()
    {
        var requiredTithing = RequiredTithing;

        if (Caster.Player && Caster.TithingPoints < requiredTithing)
        {
            Caster.SendLocalizedMessage(1060173, RequiredTithing.ToString());
            return false;
        }

        if (Caster.Player)
        {
            Caster.TithingPoints -= requiredTithing;
        }

        return base.CheckSequence();
    }
}
