using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonAuction;

public class AuctionListing
{
    public int Id;
    public Mobile Seller;
    public Item Item;
    public long Price;
    public DateTime Expires;
    public string City;
}

/// <summary>
///     Local, per-city auction houses — each city's stone only shows/lists that city's own
///     listings (not a global pool). Listed items are pulled out of the world (Internalize)
///     while active and either handed to the buyer or returned to the seller's bank when
///     the listing expires unsold.
/// </summary>
public class AuctionHouseSystem : GenericPersistence
{
    private static AuctionHouseSystem _instance;

    private static readonly Dictionary<int, AuctionListing> Listings = new();
    private static int _nextId = 1;

    private static readonly TimeSpan ListingDuration = TimeSpan.FromDays(30);
    private static readonly TimeSpan ExpiryCheckInterval = TimeSpan.FromMinutes(15);

    // A flat cut taken off the sale price when a listing sells — tune freely, 0 to disable.
    private const double AuctionHouseCutPercent = 0.05;

    // Warriors keep more of the sale — the one category perk not tied to combat/gathering.
    private const double WarriorAuctionHouseCutPercent = 0.02;

    public AuctionHouseSystem() : base("MahaonAuctionHouse", 1)
    {
    }

    public static void Configure()
    {
        _instance = new AuctionHouseSystem();
    }

    public static void Initialize()
    {
        Timer.DelayCall(ExpiryCheckInterval, ExpiryCheckInterval, CheckExpired);
    }

    public static IEnumerable<AuctionListing> ActiveListings(string city)
    {
        foreach (var listing in Listings.Values)
        {
            if (listing.City == city)
            {
                yield return listing;
            }
        }
    }

    public static int ActiveCount(string city)
    {
        var count = 0;
        foreach (var listing in Listings.Values)
        {
            if (listing.City == city)
            {
                count++;
            }
        }

        return count;
    }

    public static AuctionListing CreateListing(Mobile seller, Item item, long price, string city)
    {
        item.Internalize();

        var listing = new AuctionListing
        {
            Id = _nextId++,
            Seller = seller,
            Item = item,
            Price = price,
            Expires = Core.Now + ListingDuration,
            City = city
        };

        Listings[listing.Id] = listing;
        return listing;
    }

    /// <summary>Attempts to buy a listing. Returns a result code for the UI to message on.</summary>
    public static AuctionBuyResult TryBuy(PlayerMobile buyer, int listingId)
    {
        if (!Listings.TryGetValue(listingId, out var listing))
        {
            return AuctionBuyResult.NotFound;
        }

        if (listing.Seller == buyer)
        {
            return AuctionBuyResult.OwnListing;
        }

        // Defense in depth against a listing whose Price is outside int range (a bad price
        // shouldn't be creatable anymore — see AuctionPriceGump's own bound — but a stale
        // listing from before that check existed could still be sitting in a live save).
        // (int)listing.Price would silently wrap for a price outside int range, and a
        // wrapped non-positive amount makes ConsumeTotal treat the purchase as already paid
        // for regardless of the buyer's real gold.
        if (listing.Price is <= 0 or > int.MaxValue)
        {
            return AuctionBuyResult.CantAfford;
        }

        var backpack = buyer.Backpack;
        if (backpack == null || !backpack.ConsumeTotal(typeof(Gold), (int)listing.Price))
        {
            return AuctionBuyResult.CantAfford;
        }

        Listings.Remove(listingId);

        if (backpack.TryDropItem(buyer, listing.Item, false) != true)
        {
            listing.Item.MoveToWorld(buyer.Location, buyer.Map);
        }

        PaySeller(listing);

        return AuctionBuyResult.Success;
    }

    // The seller's share after the house's cut and the city's tax, to the bank; the tax to the
    // city's holder.
    private static void PaySeller(AuctionListing listing)
    {
        var cutPercent = AuctionHouseCutPercent;

        if (listing.Seller is PlayerMobile sellerCheckPm)
        {
            var profession = Systems.MahaonProfessions.ProfessionSystem.GetProfession(sellerCheckPm);
            if (profession != null &&
                Systems.MahaonProfessions.ProfessionData.All[profession.Value].Category ==
                Systems.MahaonProfessions.ProfessionCategory.Warrior)
            {
                cutPercent = WarriorAuctionHouseCutPercent;
            }
        }

        // A guild holding the city adds its tax to the house's cut and keeps that part.
        var city = Systems.MahaonCities.CityControlSystem.Find(listing.City);
        var taxPercent = Systems.MahaonCities.CityControlSystem.TaxIn(city) / 100.0;
        var tax = (long)(listing.Price * taxPercent);
        Systems.MahaonCities.CityControlSystem.CollectTax(city, tax);

        var sellerCut = (long)(listing.Price * (1.0 - cutPercent - taxPercent));

        if (listing.Seller is PlayerMobile sellerPm)
        {
            if (sellerPm.BankBox != null)
            {
                Banker.Deposit(sellerPm, (int)sellerCut);
                sellerPm.SendMessage(0x59, $"Твой лот продан за {listing.Price} золота (в банк зачислено {sellerCut} после комиссии аукциона и налога города).");
            }
        }

        Systems.MahaonBots.BotHotMarkets.OnNotableSale(listing.City, listing.Item.GetType().Name, listing.Price);
    }

    /// <summary>The cheapest listing, per unit, among those the predicate takes.</summary>
    public static AuctionListing Cheapest(Predicate<Item> match)
    {
        AuctionListing best = null;
        var bestUnit = double.MaxValue;
        foreach (var listing in Listings.Values)
        {
            if (listing.Item?.Deleted == false && listing.Price is > 0 and <= int.MaxValue && match(listing.Item))
            {
                var unit = (double)listing.Price / Math.Max(1, listing.Item.Amount);
                if (unit < bestUnit)
                {
                    bestUnit = unit;
                    best = listing;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Buys a listing for a guild: the guild bank pays, and the goods go into it. How a guild
    /// keeps its city guard stocked.
    /// </summary>
    public static bool TryBuyForGuild(string guildName, AuctionListing listing)
    {
        if (listing == null || !Listings.ContainsKey(listing.Id) || listing.Price is <= 0 or > int.MaxValue ||
            listing.Seller?.Guild?.Name == guildName ||
            !Systems.MahaonBots.GuildBank.TrySpend(guildName, listing.Price, Systems.MahaonMetals.MahaonMetal.Iron, 0))
        {
            return false;
        }

        Listings.Remove(listing.Id);
        Systems.MahaonBots.GuildBank.Deposit(guildName, listing.Item);
        PaySeller(listing);
        return true;
    }

    public static bool TryCancel(PlayerMobile seller, int listingId)
    {
        if (!Listings.TryGetValue(listingId, out var listing) || listing.Seller != seller)
        {
            return false;
        }

        Listings.Remove(listingId);
        ReturnItemToSeller(listing);
        return true;
    }

    private static void CheckExpired()
    {
        List<int> expired = null;

        foreach (var listing in Listings.Values)
        {
            if (Core.Now >= listing.Expires)
            {
                (expired ??= new List<int>()).Add(listing.Id);
            }
        }

        if (expired == null)
        {
            return;
        }

        foreach (var id in expired)
        {
            if (Listings.Remove(id, out var listing))
            {
                ReturnItemToSeller(listing);
            }
        }
    }

    private static void ReturnItemToSeller(AuctionListing listing)
    {
        if (listing.Item.Deleted)
        {
            return;
        }

        if (listing.Seller is PlayerMobile pm)
        {
            var bank = pm.BankBox;

            if (bank?.TryDropItem(pm, listing.Item, false) == true)
            {
                pm.SendMessage(0x59, $"Твой непроданный лот ({listing.Item.Name ?? listing.Item.GetType().Name}) возвращён в банк.");
                return;
            }

            // Mahaon: банк переполнен (GlobalMaxItems = 125) — раньше падало сюда и
            // бросало предмет на землю рядом с ботом НАВСЕГДА, что и разрослось до
            // почти 2 млн предметов по всему миру за долгую жизнь шарда. Вместо этого —
            // честная, пусть и упрощённая, ликвидация: конвертируем в золото через
            // тот же Banker.Deposit, что использует обычная банковская механика, и
            // удаляем сам предмет. Не идеальная рыночная оценка (нет универсального
            // способа узнать цену произвольного предмета без вендора, у которого он
            // явно зарегистрирован в SellInfo) — честная приближённая эвристика.
            if (bank != null)
            {
                var estimatedValue = EstimateLiquidationValue(listing.Item);
                Banker.Deposit(pm, estimatedValue);
                pm.SendMessage(0x59, $"Банк переполнен — непроданный лот ликвидирован за {estimatedValue} золота.");
                listing.Item.Delete();
                return;
            }
        }

        // Fallback: drop it at the seller's current location if the bank isn't available.
        if (listing.Seller?.Deleted == false)
        {
            listing.Item.MoveToWorld(listing.Seller.Location, listing.Seller.Map);
        }
    }

    private static int EstimateLiquidationValue(Item item)
    {
        var baseValue = item switch
        {
            BaseWeapon or BaseArmor or BaseJewel => 80,
            _                                      => 8
        };

        return baseValue * Math.Max(1, item.Amount);
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(_nextId);
        writer.WriteEncodedInt(Listings.Count);

        foreach (var listing in Listings.Values)
        {
            writer.WriteEncodedInt(listing.Id);
            writer.Write(listing.Seller);
            writer.Write(listing.Item);
            writer.Write(listing.Price);
            writer.Write(listing.Expires);
            writer.Write(listing.City);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version
        _nextId = reader.ReadEncodedInt();

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var id = reader.ReadEncodedInt();
            var seller = reader.ReadEntity<Mobile>();
            var item = reader.ReadEntity<Item>();
            var price = reader.ReadLong();
            var expires = reader.ReadDateTime();
            var city = reader.ReadString();

            if (item != null)
            {
                Listings[id] = new AuctionListing
                {
                    Id = id,
                    Seller = seller,
                    Item = item,
                    Price = price,
                    Expires = expires,
                    City = city
                };
            }
        }
    }
}

public enum AuctionBuyResult
{
    Success,
    NotFound,
    OwnListing,
    CantAfford
}
