using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>Planning a purchase in town: find a vendor with the item in stock, fetch the money
/// from the bank if the purse is short, walk over, buy.</summary>
public static class BotShopping
{
    /// <summary>The steps to buy <paramref name="amount"/> of <paramref name="type"/> in
    /// <paramref name="city"/>, or null when nobody sells it or the bot can't pay.</summary>
    public static List<BotAction> PlanPurchase(
        Mobile bot, BotCity city, Type type, int amount, Func<GenericBuyInfo, bool> match = null
    )
    {
        if (city == null)
        {
            return null;
        }

        BaseVendor seller = null;
        GenericBuyInfo stock = null;

        foreach (var vendor in WorldCatalog.GetVendors(city))
        {
            if (BuyFromVendorAction.FindStock(vendor, type, match) is { } found)
            {
                (seller, stock) = (vendor, found);
                break;
            }
        }

        if (seller == null)
        {
            return null;
        }

        var steps = new List<BotAction>();
        var cost = stock.Price * Math.Min(amount, stock.Amount);
        var purse = bot.Backpack?.GetAmount(typeof(Gold)) ?? 0;

        if (purse < cost)
        {
            var banker = WorldCatalog.GetBanker(city);
            if (banker == null || Banker.GetBalance(bot) < cost - purse)
            {
                return null;
            }

            steps.Add(new GoToAction(banker, 3, "в банк"));
            steps.Add(new WithdrawGoldAction(banker, cost - purse));
        }

        steps.Add(new GoToAction(seller, 2, $"к торговцу {seller.Name}"));
        steps.Add(new BuyFromVendorAction(seller, type, amount, match));
        return steps;
    }

    public static long Funds(Mobile bot) => (bot.Backpack?.GetAmount(typeof(Gold)) ?? 0) + Banker.GetBalance(bot);
}
