using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonPoison;

/// <summary>
///     Mahaon poison tolerance: getting poisoned repeatedly at the same level in a short
///     window builds resistance — the next hit at that level (or lower) gets knocked down
///     a level instead of applying at full strength. Tolerance itself decays if you go a
///     while without being poisoned at that level again.
/// </summary>
public class PoisonToleranceSystem : GenericPersistence
{
    private static PoisonToleranceSystem _instance;

    private static readonly TimeSpan ToleranceDecay = TimeSpan.FromMinutes(30);

    // Mobile -> (poison level -> last exposure time). Tolerance to a level is "active"
    // as long as the last hit at that level was within ToleranceDecay.
    private static readonly Dictionary<Mobile, Dictionary<int, DateTime>> Exposures = new();

    public PoisonToleranceSystem() : base("MahaonPoisonTolerance", 1)
    {
    }

    public static void Configure()
    {
        _instance = new PoisonToleranceSystem();
    }

    /// <summary>
    ///     Call before applying poison. Returns the poison to actually apply — knocked down
    ///     a level if the target has recent tolerance built up at this level, otherwise the
    ///     original. Also records this exposure for future tolerance checks.
    /// </summary>
    public static Poison GetEffectivePoison(Mobile target, Poison incoming)
    {
        if (incoming == null)
        {
            return null;
        }

        if (!Exposures.TryGetValue(target, out var levels))
        {
            Exposures[target] = levels = new Dictionary<int, DateTime>();
        }

        var hasTolerance = levels.TryGetValue(incoming.Level, out var lastHit) &&
                            Core.Now - lastHit < ToleranceDecay;

        levels[incoming.Level] = Core.Now;

        if (!hasTolerance)
        {
            return incoming;
        }

        // Knock it down a level (never below the weakest registered standard poison).
        var weaker = Poison.GetPoisonByIndex(incoming.Index - 1);
        return weaker != null && weaker.Family == incoming.Family ? weaker : incoming;
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        // Tolerance is short-lived (30 min) and purely a combat-flavor mechanic — not worth
        // persisting across a restart, it'll just start fresh, which is fine.
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt();
    }
}
