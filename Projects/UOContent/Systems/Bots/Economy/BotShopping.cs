using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonAuction;

namespace Server.Systems.Bots;

/// <summary>Planning a purchase in town: the town's auction when a lot there is the better deal,
/// otherwise a vendor with the item in stock; fetch the money from the bank if the purse is short,
/// walk over, buy.</summary>
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

        // Аукцион — только когда товар узнаётся по типу: семена и саженцы различаются тем, с чем
        // созданы, а лот на аукционе — готовый предмет.
        if (match == null && PlanAuctionPurchase(bot, city, type, amount, stock) is { } auction)
        {
            return auction;
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

    // Сколько бот готов отдать за лот, которого нет у торговцев, — доля от всех денег.
    private const int NoVendorBudgetShare = 5;

    /// <summary>
    ///     Лот на аукционе города, который стоит купить вместо лавки: не дороже, чем то же
    ///     количество у торговца, а если торговцы этого не продают — по разумной цене и не
    ///     дороже пятой части всех денег. Свои лоты бот не выкупает.
    /// </summary>
    public static AuctionListing FindAuctionDeal(Mobile bot, BotCity city, Type type, int amount, GenericBuyInfo vendorStock)
    {
        AuctionListing best = null;
        var bestUnit = double.MaxValue;
        var funds = Funds(bot);

        foreach (var listing in AuctionHouseSystem.ActiveListings(city.Name))
        {
            if (listing.Seller == bot || listing.Item is not { Deleted: false } item || !type.IsAssignableFrom(item.GetType()) ||
                listing.Price is <= 0 or > int.MaxValue || listing.Price > funds)
            {
                continue;
            }

            var unit = (double)listing.Price / Math.Max(1, item.Amount);
            var acceptable = vendorStock != null
                ? listing.Price <= (long)vendorStock.Price * Math.Max(amount, item.Amount) && unit < vendorStock.Price
                : listing.Price <= funds / NoVendorBudgetShare &&
                  (BotGoods.BaseUnitPrice(item) is var basePrice && (basePrice == 0 || unit <= basePrice * 3));

            if (acceptable && unit < bestUnit)
            {
                best = listing;
                bestUnit = unit;
            }
        }

        return best;
    }

    private static List<BotAction> PlanAuctionPurchase(Mobile bot, BotCity city, Type type, int amount, GenericBuyInfo vendorStock)
    {
        if (bot is not PlayerMobile || FindAuctionDeal(bot, city, type, amount, vendorStock) is not { } deal)
        {
            return null;
        }

        var steps = new List<BotAction>();
        var purse = bot.Backpack?.GetAmount(typeof(Gold)) ?? 0;

        if (purse < deal.Price)
        {
            var banker = WorldCatalog.GetBanker(city);
            if (banker == null || Banker.GetBalance(bot) < deal.Price - purse)
            {
                return null;
            }

            steps.Add(new GoToAction(banker, 3, "в банк"));
            steps.Add(new WithdrawGoldAction(banker, (int)(deal.Price - purse)));
        }

        // Покупают у камня аукциона, как игрок; без отметки камня — где стоит (так же выставляет).
        if (MahaonCities.CityMarkers.TryGetMarker(city.Name, "auctionstone", out var stone, out var stoneMap) && stoneMap == bot.Map)
        {
            steps.Add(new GoToAction(stoneMap, stone, 2, "к аукциону"));
        }

        steps.Add(new BuyAtAuctionAction(deal.Id, deal.Price));
        return steps;
    }

    public static long Funds(Mobile bot) => (bot.Backpack?.GetAmount(typeof(Gold)) ?? 0) + Banker.GetBalance(bot);
}
