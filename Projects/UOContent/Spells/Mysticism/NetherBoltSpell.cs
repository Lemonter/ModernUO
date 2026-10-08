using System;
using Server.Targeting;

namespace Server.Spells.Mysticism;

/// <summary>Mysticism 677, the school's first-circle attack. Ported from ServUO
/// (Scripts/Spells/Mysticism/SpellDefinitions/NetherBoltSpell.cs). The registration was
/// already in Spells/Initializer.cs, commented out — without it the school had no first
/// circle at all, so a mystic literally could not start casting.
///
/// Structure mirrors EagleStrikeSpell.cs next door (this codebase's ITargetingSpell/
/// SpellTarget idiom in place of ServUO's InternalTarget, and a Mobile target rather than
/// ServUO's IDamageable, which doesn't exist here). Reflect handling is the local
/// EagleStrike pattern. Damage is 100% chaos, matching the original's argument positions.</summary>
public class NetherBoltSpell : MysticSpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new(
        "Nether Bolt",
        "In Corp Ylem",
        230,
        9022,
        Reagent.BlackPearl,
        Reagent.SulfurousAsh
    );

    public NetherBoltSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.First;

    public void Target(Mobile m)
    {
        if (CheckHSequence(m))
        {
            SpellHelper.Turn(Caster, m);

            if (Core.SA && HasDelayedDamageContext(m))
            {
                DoHurtFizzle();
                return;
            }

            var source = Caster;

            if (SpellHelper.CheckReflect(1, ref source, ref m))
            {
                Timer.StartTimer(TimeSpan.FromSeconds(0.5), () =>
                {
                    source.MovingParticles(m, 0x36D4, 7, 0, false, true, 0x49A, 0, 9502, 4019, 0x160, 0);
                    source.PlaySound(0x211);
                });
            }

            /* Damages the Target with a bolt of nether energy. */
            Caster.MovingParticles(m, 0x36D4, 7, 0, false, true, 0x49A, 0, 9502, 4019, 0x160, 0);
            Caster.PlaySound(0x211);

            Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => Damage(m));
        }
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
    }

    private void Damage(Mobile to)
    {
        if (to == null)
        {
            return;
        }

        double damage = GetNewAosDamage(10, 1, 4, to);

        SpellHelper.Damage(this, to, damage, 0, 0, 0, 0, 0, 100);
    }
}
