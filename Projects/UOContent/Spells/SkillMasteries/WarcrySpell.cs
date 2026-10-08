using System;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Warcry.cs) — Bushido
// mastery: an AoE damage-reduction debuff against the caster's attackers for 10 seconds.
public class WarcrySpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Warcry", "", -1, 9002);

    public override int RequiredMana => 40;
    public override SkillName CastSkill => SkillName.Bushido;

    private int _damageMalus;
    private int _radius;

    public WarcrySpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            var skill = (int)(Caster.Skills[CastSkill].Value + GetWeaponSkill() + GetMasteryLevel() * 40) / 3;

            _radius = skill / 40;
            _damageMalus = (int)(skill / 2.4);

            Caster.PublicOverheadMessage(MessageType.Regular, Caster.SpeechHue, false, "Приготовься!");

            if (Caster.Player)
            {
                Caster.PlaySound(Caster.Female ? 0x338 : 0x44A);
            }
            else if (Caster is BaseCreature bc)
            {
                Caster.PlaySound(bc.GetAngerSound());
            }

            var cooldown = Caster.AccessLevel == AccessLevel.Player ? TimeSpan.FromMinutes(20) : TimeSpan.FromSeconds(10);
            AddToCooldown(cooldown);

            Expires = Core.Now + TimeSpan.FromSeconds(10);
            BeginTimer();

            if (Caster is PlayerMobile pm)
            {
                // Reduces all incoming attack damage from opponents who hear the war cry
                // within ~1_RANGE~ tiles by ~2_val~%.
                pm.AddBuff(new BuffInfo(BuffIcon.Warcry, 1155906, 1156058, TimeSpan.FromSeconds(10), $"{_radius}\t{_damageMalus}"));
            }
        }

        FinishSequence();
    }

    public override void OnDamaged(Mobile attacker, Mobile victim, DamageType type, ref int damage)
    {
        if (!attacker.InRange(Caster, _radius))
        {
            return;
        }

        damage -= (int)(damage * (_damageMalus / 100.0));

        if (Caster.Player)
        {
            Caster.PlaySound(attacker.Female ? 0x338 : 0x44A);
        }
        else if (Caster is BaseCreature bc)
        {
            Caster.PlaySound(bc.GetAngerSound());
        }

        Caster.FixedEffect(0x3779, 10, 20, 1372, 4);
    }
}
