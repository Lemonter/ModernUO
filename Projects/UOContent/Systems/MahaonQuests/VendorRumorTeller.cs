using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.MahaonQuests;

/// <summary>
///     City vendors occasionally repeat whatever rumor is currently circulating (see
///     Systems.MahaonBots.BotRumors) out loud — a small, low-frequency background timer,
///     deliberately NOT hooked into BaseVendor.OnDoubleClick (that's reserved for the shop
///     gump) or OnThink (runs far too often for this). Doesn't filter by which city the
///     rumor is actually ABOUT — BotRumors stores free text, not a structured city field —
///     so this is "a vendor mentions news" rather than strictly "a vendor only knows their
///     own city's news"; close enough to the spirit of it without needing a bigger rumor
///     data model rework.
/// </summary>
public static class VendorRumorTeller
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(45);
    private const double ChancePerVendor = 0.08; // per check, per currently-loaded vendor

    public static void Initialize()
    {
        Timer.DelayCall(CheckInterval, CheckInterval, Tick);
    }

    private static void Tick()
    {
        foreach (var mobile in new List<Mobile>(World.Mobiles.Values))
        {
            if (mobile is not BaseVendor vendor || vendor.Deleted || !vendor.Alive || vendor.Map == null)
            {
                continue;
            }

            if (Utility.RandomDouble() > ChancePerVendor)
            {
                continue;
            }

            // Only worth speaking if someone's actually nearby to hear it — same
            // "has to be physically near" spirit as BotRumors.TryHear for bots.
            var hasListener = false;

            foreach (var nearby in vendor.Map.GetMobilesInRange<PlayerMobile>(vendor.Location, 8))
            {
                hasListener = true;
                break;
            }

            if (hasListener)
            {
                Systems.MahaonBots.BotRumors.TryHear(vendor);
            }
        }
    }
}
