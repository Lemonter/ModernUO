using Server.Mobiles;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Mahaon: shard-wide hit point multiplier for creatures.
///
///     Applied in BaseCreature.HitsMax rather than in SetHits, for three reasons: it is a
///     single place instead of ~500 call sites; it also covers the creatures that never
///     call SetHits at all and derive their hit points from Str (HitsMaxSeed &lt;= 0); and
///     because HitsMax is computed rather than stored, creatures already saved in the world
///     pick the new maximum up on load instead of needing a respawn.
///
///     One consequence of that last point: a creature loaded from an existing save keeps
///     its stored current Hits, so on the first world load after this lands, everything
///     alive will be sitting at a quarter of its new maximum until it regenerates or
///     respawns. Newly spawned creatures are at full — SetHits/SetStr both assign
///     Hits = HitsMax after setting the seed.
/// </summary>
public static class CreatureHitsSystem
{
    /// <summary>"Подними всем нпс хп х4".</summary>
    public const double Scalar = 4.0;

    /// <summary>
    ///     Deliberately applies to every creature, pets and player summons included — if
    ///     monsters get four times the hit points and tamed animals do not, taming and
    ///     summoning stop being viable overnight. Kept as a method rather than reading the
    ///     constant directly so a per-creature exception has one obvious place to go.
    /// </summary>
    public static double GetScalar(BaseCreature bc) => bc == null ? 1.0 : Scalar;
}
