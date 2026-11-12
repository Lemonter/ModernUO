using Server.Mobiles;

namespace Server.Systems.MahaonRaids;

/// <summary>Marker for every town-defender variant (melee MahaonTownDefender, caster
/// MahaonTownDefenderMage, the guards of a held city, and any future variant) — lets
/// MahaonRaidCombat recognize all of them without checking a concrete class, the same way
/// IRaidSpawn covers every raid mob variant on the other side.</summary>
public interface IMahaonTownDefender
{
}

/// <summary>
///     Драка налётчиков (IRaidSpawn и всё, что они призвали) со стражей (IMahaonTownDefender)
///     — зрелище, а не бой: удар в ней снимает ровно единицу и никого не добивает. Пять минут
///     у ворот обе стороны держатся, исход решают игроки, бьющие налётчиков в полную силу.
/// </summary>
public static class MahaonRaidCombat
{
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

    /// <summary>Call from Damage() overrides on both sides: in a raider-vs-guard fight a blow
    /// takes exactly one hit point, and none at all from someone already down to one.</summary>
    public static int CapIfSpectacleFight(Mobile attacker, Mobile defender, int amount)
    {
        if (attacker == null || defender == null || amount <= 0)
        {
            return amount;
        }

        var isSpectacle = IsRaidRelated(attacker) && defender is IMahaonTownDefender ||
                          attacker is IMahaonTownDefender && IsRaidRelated(defender);

        if (!isSpectacle)
        {
            return amount;
        }

        return defender.Hits > 1 ? 1 : 0;
    }

    /// <summary>Враг ли налётчику этот мобиль: стража — да, пока не начался прорыв к банку; на
    /// прорыве — только тот, кто сам напал; null — решает обычная логика существа.</summary>
    public static bool? RaiderIsEnemy(Mobile raider, Mobile m)
    {
        var breaking = RaidDirector.IsBreakingThrough(raider);

        if (m is IMahaonTownDefender)
        {
            return m.Alive && !breaking;
        }

        // На прорыве к банку налётчик ни на кого сам не бросается — отвечает только тому, кто ударил.
        if (breaking)
        {
            foreach (var info in raider.Aggressors)
            {
                if (info.Attacker == m)
                {
                    return m.Alive;
                }
            }

            return false;
        }

        return null;
    }

    /// <summary>Перемирие: налётчик прорывается к банку, и стража ему уже не противник (и наоборот).</summary>
    public static bool IsTruce(Mobile a, Mobile b) =>
        a is IMahaonTownDefender && IsRaidRelated(b) && RaidDirector.IsBreakingThrough(LeaderOf(b)) ||
        b is IMahaonTownDefender && IsRaidRelated(a) && RaidDirector.IsBreakingThrough(LeaderOf(a));

    // Призванный налётчиком (скелеты лича) — по хозяину.
    private static Mobile LeaderOf(Mobile m) => m is BaseCreature { ControlMaster: { } master } ? master : m;

    /// <summary>Враг ли стражнику налётчик: да, пока тот не прорывается к банку.</summary>
    public static bool IsRaiderForGuard(Mobile m) =>
        m.Alive && IsRaidRelated(m) && !RaidDirector.IsBreakingThrough(LeaderOf(m));
}
