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

        var backpack = buyer.Backpack;
        if (backpack == null || !CurrencyHelper.TryWithdrawCopperValue(backpack, listing.Price))
        {
            return AuctionBuyResult.CantAfford;
        }

        Listings.Remove(listingId);

        if (backpack.TryDropItem(buyer, listing.Item, false) != true)
        {
            listing.Item.MoveToWorld(buyer.Location, buyer.Map);
        }

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

        var sellerCut = (long)(listing.Price * (1.0 - cutPercent));

        if (listing.Seller is PlayerMobile sellerPm)
        {
            var sellerBank = sellerPm.BankBox;
            if (sellerBank != null)
            {
                CurrencyHelper.DepositCopperValue(sellerBank, sellerCut);
                sellerPm.SendMessage(0x59, $"Твой лот продан за {listing.Price} золота (в банк зачислено {sellerCut} после комиссии аукциона).");
            }
        }

        return AuctionBuyResult.Success;
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
        }

        // Fallback: drop it at the seller's current location if the bank isn't available.
        if (listing.Seller?.Deleted == false)
        {
            listing.Item.MoveToWorld(listing.Seller.Location, listing.Seller.Map);
        }
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
