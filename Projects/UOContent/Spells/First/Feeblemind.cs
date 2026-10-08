using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.First
{
    public class FeeblemindSpell : MagerySpell, ITargetingSpell<Mobile>
    {
        private static readonly SpellInfo _info = new(
            "Feeblemind",
            "Rel Wis",
            212,
            9031,
            Reagent.Ginseng,
            Reagent.Nightshade
        );

        public FeeblemindSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.First;

        public void Target(Mobile m)
        {
            if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)Circle, Caster, ref m);

                if (CheckResisted(m))
                {
                    m.FixedParticles(0x3779, 10, 15, 5052, EffectLayer.Waist);
                    m.PlaySound(0x1F7); // resisted sound
                    HarmfulSpell(m);
                    return;
                }

                // TODO: StoneForm immunity

                var length = SpellHelper.GetHarmfulDuration(Caster, m);
                SpellHelper.AddStatCurse(Caster, m, StatType.Int, length, false);

                // Mahaon: no longer disturbs the target's own cast — enemy spells don't
                // fizzle casting anymore (see Spell.OnCasterHurt's doc comment).

                m.Paralyzed = false;

                m.FixedParticles(0x3779, 10, 15, 5004, EffectLayer.Head);
                m.PlaySound(0x1E4);

                var percentage = (int)(SpellHelper.GetOffsetScalar(Caster, m, true) * 100);

                (m as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.FeebleMind, 1075833, length, percentage.ToString()));

                HarmfulSpell(m);
            }
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
        }
    }
}
