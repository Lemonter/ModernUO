using System;

namespace Server.Systems.MahaonGems;

/// <summary>
///     Mobile.HitsRegenRateHandler (and the Stam/Mana equivalents) are public static
///     delegates already used by Misc/RegenRates.cs to set the base rate. Wrapping them
///     here — captured at Initialize time, after RegenRates.Configure() has already run —
///     lets gem-socketed regen speed bonuses shorten the tick interval without touching
///     that core rate logic at all.
/// </summary>
public static class GemRegenHooks
{
    public static void Initialize()
    {
        var baseHits = Mobile.HitsRegenRateHandler;
        var baseStam = Mobile.StamRegenRateHandler;
        var baseMana = Mobile.ManaRegenRateHandler;

        Mobile.HitsRegenRateHandler = m => Scale(baseHits?.Invoke(m) ?? TimeSpan.FromSeconds(10), m, GemBonusType.HitsRegen);
        Mobile.StamRegenRateHandler = m => Scale(baseStam?.Invoke(m) ?? TimeSpan.FromSeconds(10), m, GemBonusType.StamRegen);
        Mobile.ManaRegenRateHandler = m => Scale(baseMana?.Invoke(m) ?? TimeSpan.FromSeconds(10), m, GemBonusType.ManaRegen);
    }

    private static TimeSpan Scale(TimeSpan baseRate, Mobile m, GemBonusType type)
    {
        var multiplier = GemSocketingSystem.GetRegenSpeedMultiplier(m, type);
        return multiplier >= 1.0 ? baseRate : baseRate * multiplier;
    }
}
