using System;

namespace Server.Systems.MahaonMetals;

/// <summary>Doom "увеличивает регенерацию" — тот же приём оборачивания
/// Mobile.HitsRegenRateHandler, что уже использует GemRegenHooks (Gem Socketing). Порядок
/// вызова между Initialize()-методами разных систем не гарантирован, но это не важно:
/// каждая система захватывает ТЕКУЩИЙ обработчик перед тем, как обернуть его, так что
/// цепочка корректно строится вне зависимости от того, кто запустился первым.</summary>
public static class MahaonMetalRegenHooks
{
    private const double SpeedupPerItem = 0.10; // -10% времени тика за каждую надетую вещь Doom, не накопительно свыше предела
    private const double MaxSpeedup = 0.5; // не быстрее чем в 2 раза, даже с кучей вещей Doom

    public static void Initialize()
    {
        var baseHits = Mobile.HitsRegenRateHandler;
        Mobile.HitsRegenRateHandler = m => Scale(baseHits?.Invoke(m) ?? TimeSpan.FromSeconds(10), m);
    }

    private static TimeSpan Scale(TimeSpan baseRate, Mobile m)
    {
        var doomCount = MahaonMetalTracker.CountWorn(m, MahaonMetal.Doom);

        if (doomCount <= 0)
        {
            return baseRate;
        }

        var reduction = Math.Min(MaxSpeedup, doomCount * SpeedupPerItem);
        return baseRate * (1.0 - reduction);
    }
}
