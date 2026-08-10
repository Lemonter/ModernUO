using Server.Spells;
using Server.Spells.Necromancy;

namespace Server.Mobiles;

/// <summary>
///     MageAI doesn't cast Animate Dead at all (it already handles some necromancy —
///     PainSpikeSpell, StrangleSpell — for any creature with Necromancy > 50 via
///     IsNecromancer, but summon-undead was never part of that). This just wraps
///     ChooseSpell: with some chance, and if this creature isn't already accompanied by
///     summoned undead, cast Animate Dead first — otherwise defers entirely to the normal
///     MageAI spell selection (Necromancy combo spells, Magery, healing, etc. all still
///     work exactly as before).
/// </summary>
public class MahaonNecroSummonerAI : MageAI
{
    private const double SummonChance = 0.12; // checked once per spell-choice, not per combat round
    private const int MaxSummons = 2;

    public MahaonNecroSummonerAI(BaseCreature m) : base(m)
    {
    }

    public override Spell ChooseSpell(Mobile c)
    {
        if (Core.AOS
            && Mobile.Skills.Necromancy.Value > 50
            && Mobile.Followers < Mobile.FollowersMax
            && CountMySummons() < MaxSummons
            && SummonChance >= Utility.RandomDouble())
        {
            return new AnimateDeadSpell(Mobile);
        }

        return base.ChooseSpell(c);
    }

    private int CountMySummons()
    {
        var count = 0;

        foreach (var mobile in Mobile.GetMobilesInRange(8))
        {
            if (mobile is BaseCreature bc && bc.Controlled && bc.ControlMaster == Mobile)
            {
                count++;
            }
        }

        return count;
    }
}
