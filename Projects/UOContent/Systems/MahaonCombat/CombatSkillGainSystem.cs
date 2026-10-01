namespace Server.Systems.MahaonCombat;

/// <summary>
///     "Чем больше нанесено урона, тем больше бонус к приросту навыка" — scoped to combat/
///     damage-dealing skills only (per the shard owner's own scoping), NOT a change to
///     Server.Misc.SkillCheck.Gain itself (that function is shared by every skill in the
///     game, gathering/crafting/thieving included — touching it would rebalance everything
///     at once). Instead this is a small ADDITIONAL gain roll, on top of whatever the
///     normal hit-time/cast-time check already granted, fired once real damage is known
///     (weapon hits from BaseWeapon.OnHit, spell damage from SpellHelper.Damage) — bigger
///     hits mean a better shot at that extra roll.
/// </summary>
public static class CombatSkillGainSystem
{
    // 20 damage -> +20% chance, capped so one huge hit can't guarantee a free bonus point.
    private const double ChancePerDamage = 0.01;
    private const double MaxExtraChance = 0.5;

    public static void OnDamageDealt(Mobile attacker, SkillName skillName, int damage)
    {
        if (attacker?.Player != true || damage <= 0)
        {
            return;
        }

        var skill = attacker.Skills[skillName];

        if (skill == null || skill.Lock != SkillLock.Up || skill.Base >= skill.Cap)
        {
            return;
        }

        var extraChance = System.Math.Min(MaxExtraChance, damage * ChancePerDamage);

        if (Utility.RandomDouble() < extraChance)
        {
            Misc.SkillCheck.Gain(attacker, skill);
        }
    }
}
