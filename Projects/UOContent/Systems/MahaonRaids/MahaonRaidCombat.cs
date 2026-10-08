using Server.Mobiles;

namespace Server.Systems.MahaonRaids;

/// <summary>Marker for every town-defender variant (melee MahaonTownDefender, caster
/// MahaonTownDefenderMage, and any future variant) — lets MahaonRaidCombat recognize all
/// of them without checking a concrete class, the same way IRaidSpawn covers every raid
/// mob variant on the other side.</summary>
public interface IMahaonTownDefender
{
}

/// <summary>
///     Shared "this is just a spectacle, not a real fight" damage cap between raid mobs
///     (anything tagged IRaidSpawn, including creatures summoned BY a raid mob — a raid
///     Lich's Animate Dead skeletons, for instance) and town defenders spawned to fight
///     them (anything tagged IMahaonTownDefender). Both sides check this from their own
///     Damage() override — see MahaonTownDefender/MahaonTownDefenderMage and each RaidXxx
///     class in RaidMobiles.cs.
/// </summary>
public static class MahaonRaidCombat
{
    private const int MinDamage = 1;
    private const int MaxDamage = 2;

    /// <summary>True if this mobile is itself a raid spawn, or was summoned by one
    /// (checked via ControlMaster, so a raid Lich's Animate Dead skeletons count
    /// too).</summary>
    public static bool IsRaidRelated(Mobile m)
    {
        if (m is IRaidSpawn)
        {
            return true;
        }

        return m is BaseCreature { ControlMaster: IRaidSpawn };
    }

    /// <summary>Call from Damage() overrides on both sides — returns the original amount
    /// unchanged unless this specific attacker/defender pairing is a raid-vs-defender
    /// spectacle fight, in which case it's capped to 1-2.</summary>
    public static int CapIfSpectacleFight(Mobile attacker, Mobile defender, int amount)
    {
        if (attacker == null || defender == null)
        {
            return amount;
        }

        var attackerIsRaid = IsRaidRelated(attacker);
        var defenderIsRaid = IsRaidRelated(defender);
        var attackerIsDefender = attacker is IMahaonTownDefender;
        var defenderIsDefender = defender is IMahaonTownDefender;

        var isSpectacle = (attackerIsRaid && defenderIsDefender) || (attackerIsDefender && defenderIsRaid);

        return isSpectacle ? Utility.RandomMinMax(MinDamage, MaxDamage) : amount;
    }
}
