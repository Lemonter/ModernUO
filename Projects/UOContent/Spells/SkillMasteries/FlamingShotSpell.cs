using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/FlamingShot.cs) —
// Archery mastery: a flaming volley. Simplified from ServUO's multi-target AoE
// (AcquireIndirectTargets/SpellHelper.Damage — RunUO-era AOS spell-area machinery this
// codebase's Spell class doesn't carry) down to a single real target hit via AOS.Damage,
// same damage-application call every other bot/mastery ability in this session already
// uses. Real fire damage, real hit-chance check against the target — just one target
// instead of a splash.
public class FlamingShotSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Flaming Shot", "", -1, 9002);

    public override int RequiredMana => 30;
    public override SkillName CastSkill => SkillName.Archery;
    public override SkillName DamageSkill => SkillName.Tactics;

    public FlamingShotSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (!CheckWeapon())
        {
            Caster.SendLocalizedMessage(1156000); // You must have an Archery weapon to use this ability!
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast()
    {
        Caster.PrivateOverheadMessage(MessageType.Regular, 1150, 1155999, Caster.NetState); // You ready a volley of flaming arrows!
        Effects.SendTargetParticles(Caster, 0x3709, 10, 30, 2724, 0, 9907, EffectLayer.LeftFoot, 0);
        Caster.PlaySound(0x5CF);

        Caster.Target = new FlamingShotTarget(this);
    }

    private class FlamingShotTarget : Target
    {
        private readonly FlamingShotSpell _spell;

        public FlamingShotTarget(FlamingShotSpell spell) : base(10, false, TargetFlags.Harmful) => _spell = spell;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Mobile mob || _spell.GetWeapon() is not BaseRanged weapon || !_spell.CheckSequence())
            {
                return;
            }

            from.MovingEffect(mob, weapon.EffectID, 18, 1, false, false);

            if (!weapon.CheckHit(from, mob))
            {
                return;
            }

            var damage = (int)(_spell.BaseSkillBonus / 1.5);

            from.DoHarmful(mob);
            AOS.Damage(mob, from, damage, 0, 100, 0, 0, 0);

            mob.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
            mob.PlaySound(0x1DD);

            weapon.PlaySwingAnimation(from);
            from.PlaySound(0x101);
        }
    }
}
