using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Systems.MahaonBots;

/// <summary>
///     A notably good sale (high price relative to what that kind of item usually goes
///     for) at the auction house makes that CITY look attractive — spreads via
///     BotRumors's existing infrastructure (a bot has to be nearby to hear it, same as
///     any other rumor), and while it's active, PickTravelDestination leans toward that
///     city for traders specifically. Not a live economy simulation, just enough organic
///     pull that "gold rush" behavior can emerge without every trader independently
///     recalculating optimal routes every cycle.
/// </summary>
public static class BotHotMarkets
{
    private static readonly TimeSpan HotMarketLifetime = TimeSpan.FromMinutes(15);

    // A sale needs to clear this to count as "notable" — otherwise every routine sale
    // would spam rumors and every city would constantly look hot.
    private const long NotableSaleThreshold = 500;

    private class HotMarket
    {
        public DateTime ExpiresAt;
    }

    private static readonly Dictionary<string, HotMarket> Active = new();

    public static void OnNotableSale(string city, string itemName, long price)
    {
        if (string.IsNullOrEmpty(city) || price < NotableSaleThreshold)
        {
            return;
        }

        Active[city] = new HotMarket { ExpiresAt = Core.Now + HotMarketLifetime };
        BotRumors.Spread($"В {city} продали {itemName} за {price} золота — вот это рынок!");
    }

    /// <summary>Currently-hot cities, freshest first. PickTravelDestination biases toward
    /// the top of this list for trader bots specifically.</summary>
    public static List<string> ActiveCities()
    {
        var now = Core.Now;

        foreach (var expired in Active.Where(kv => kv.Value.ExpiresAt <= now).Select(kv => kv.Key).ToList())
        {
            Active.Remove(expired);
        }

        return Active
            .OrderByDescending(kv => kv.Value.ExpiresAt)
            .Select(kv => kv.Key)
            .ToList();
    }
}
