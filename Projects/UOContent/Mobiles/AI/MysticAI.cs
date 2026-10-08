using Server.Spells;
using Server.Spells.Mysticism;
using Server.Targeting;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/AI/Magical AI/MysticAI.cs). A creature that
/// fights with Mysticism instead of Magery — the AI RisingColossus and the other mystic
/// casters are built on.
///
/// ServUO's version overrides GetHealSpell / GetCureSpell / GetRandomBuffSpell /
/// RandomCombatSpell, none of which exist under those names in this codebase's MageAI; the
/// same decisions live here in CheckCastHealingSpell and the two GetRandom* overrides, which
/// are the hooks ModernUO's MageAI actually exposes. Those two were private and have been
/// widened to protected virtual for this — no behaviour change to MageAI itself.
///
/// The spell pools, their mana thresholds, the 50%-fall-back-to-Magery rule and the
/// area-spell targeting are the original's.</summary>
public class MysticAI : MageAI
{
    public MysticAI(BaseCreature m) : base(m)
    {
    }

    /// <summary>A creature only falls back on Magery if it actually has some and isn't a pet.</summary>
    public virtual bool UsesMagery => Mobile.Skills.Magery.Base >= 20.0 && !Mobile.Controlled;

    public override Spell GetRandomDamageSpell()
    {
        if (UsesMagery && Utility.RandomBool())
        {
            return base.GetRandomDamageSpell();
        }

        var mana = Mobile.Mana;

        var pool = mana switch
        {
            >= 50 => 5,
            >= 20 => 3,
            >= 9  => 2,
            _     => 1
        };

        return Utility.Random(pool) switch
        {
            0 => new NetherBoltSpell(Mobile),
            1 => new EagleStrikeSpell(Mobile),
            2 => new BombardSpell(Mobile, null),
            3 => new HailStormSpell(Mobile),
            _ => new NetherCycloneSpell(Mobile)
        };
    }

    public override Spell GetRandomCurseSpell()
    {
        if (UsesMagery && Utility.RandomBool())
        {
            return base.GetRandomCurseSpell();
        }

        var mana = Mobile.Mana;

        var pool = mana switch
        {
            >= 40 => 4,
            >= 14 => 3,
            >= 8  => 2,
            _     => 1
        };

        return Utility.Random(pool) switch
        {
            0 => new PurgeMagicSpell(Mobile),
            1 => new SleepSpell(Mobile),
            2 => new MassSleepSpell(Mobile),
            _ => new SpellPlagueSpell(Mobile)
        };
    }

    protected override Spell CheckCastHealingSpell()
    {
        if (UsesMagery && Utility.RandomBool())
        {
            return base.CheckCastHealingSpell();
        }

        return Mobile.Mana >= 20 ? new CleansingWindsSpell(Mobile, null) : null;
    }

    /// <summary>Hail Storm and Nether Cyclone are ground-targeted, so the base AI — which only
    /// knows how to hand a spell a Mobile — can't aim them. Drop them on the combatant if it's
    /// close enough, otherwise on our own feet, which still catches anything in melee.</summary>
    protected override bool ProcessTarget()
    {
        var targ = Mobile.Target;

        if (targ is ISpellTarget<IPoint3D> pointTarg &&
            pointTarg.Spell is HailStormSpell or NetherCycloneSpell)
        {
            var combatant = Mobile.Combatant;

            IPoint3D p = combatant != null && Mobile.InRange(combatant, 8) ? combatant : Mobile;

            targ.Invoke(Mobile, p);
            return true;
        }

        return base.ProcessTarget();
    }
}
