using System;
using System.Linq;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Whispering.cs) —
// AnimalTaming mastery: boosts nearby pets' skill gain chance for 10 minutes. ServUO's
// DespiseCreature exclusion filter dropped (that dungeon-specific pet type doesn't exist
// here).
public class WhisperingSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Whispering", "", -1, 9002);

    public override int RequiredMana => 40;

    public override SkillName CastSkill => SkillName.AnimalTaming;
    public override SkillName DamageSkill => SkillName.AnimalLore;
    public override bool RevealOnTick => false;

    private int _enhancedGainChance;
    public int EnhancedGainChance => _enhancedGainChance;

    public WhisperingSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (IsInCooldown(Caster, GetType()))
        {
            return false;
        }

        if (GetSpell(Caster, GetType()) != null)
        {
            Caster.SendLocalizedMessage(1155889); // You are already under the effect of this ability.
            return false;
        }

        if (Caster is PlayerMobile { AllFollowers.Count: 0 })
        {
            Caster.SendLocalizedMessage(1156112); // This ability requires you to have pets.
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            if (Caster is PlayerMobile pm)
            {
                foreach (var m in pm.AllFollowers.Where(m => m.Map == Caster.Map && Caster.InRange(m.Location, PartyRange)))
                {
                    Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration), 0, 0, 0, 0, 0, 5060, 0);
                    Effects.PlaySound(m.Location, m.Map, 0x243);
                    Effects.SendTargetParticles(m, 0x375A, 35, 90, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);
                }
            }

            Caster.SendSound(0x64E);

            var duration = TimeSpan.FromSeconds(600);
            Expires = Core.Now + duration;
            BeginTimer();

            _enhancedGainChance = (int)(BaseSkillBonus / 1.26);

            if (Caster is PlayerMobile pm2)
            {
                pm2.AddBuff(new BuffInfo(BuffIcon.Whispering, 1155932, 1156106, duration, _enhancedGainChance.ToString()));
            }

            AddToCooldown(TimeSpan.FromMinutes(30));
        }

        FinishSequence();
    }
}
