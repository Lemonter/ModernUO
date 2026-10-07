using System.Collections.Generic;
using Server.Commands;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonAuction;
using Server.Systems.MahaonBots;
using Server.Systems.MahaonCities;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class CityTaxTests
{
    [Fact]
    public void HeldCity_TaxesVendorPricesAndAuctionSales_IntoTheGuildBank()
    {
        if (!CommandSystem.Entries.ContainsKey("ClaimCity"))
        {
            CityControlSystem.Configure();
        }

        const string city = "Moonglow";
        var (center, map) = CityControlSystem.Cities[city];
        var town = new Server.Regions.TownRegion(
            city,
            map,
            50,
            new Rectangle3D(new Point3D(center.X - 30, center.Y - 30, -128), new Point3D(center.X + 30, center.Y + 30, 127))
        );
        town.Register();

        var leader = new BotMobile { Name = "Казначей", Player = true, Body = 0x190 };
        var buyer = new PlayerMobile { Name = "Покупатель", Body = 0x190 };
        var vendor = new Provisioner();
        try
        {
            leader.AddItem(new Backpack());
            buyer.AddItem(new Backpack());
            leader.MoveToWorld(center, map);
            buyer.MoveToWorld(center, map);
            vendor.MoveToWorld(center, map);
            BotGuilds.Join("ТестКазна", leader);
            var guild = (Guild)leader.Guild;

            Assert.Equal(city, CityControlSystem.CityAt(vendor));
            Assert.Equal(100, vendor.GetPriceScalar());

            CityControlSystem.Capture(city, guild);
            CommandSystem.Handle(leader, $"{CommandSystem.Prefix}SetCityTax {city} 20");
            Assert.Equal(20, CityControlSystem.GetTaxRate(city));
            Assert.Equal(120, vendor.GetPriceScalar());

            // Buying from the vendor: the price carries the tax, the guild gets that part.
            vendor.UpdateBuyInfo();
            GenericBuyInfo stock = null;
            foreach (var info in vendor.GetBuyInfo())
            {
                if (info is GenericBuyInfo gbi && gbi.Amount > 0 && gbi.Price >= 10)
                {
                    stock = gbi;
                    break;
                }
            }

            Assert.NotNull(stock);
            var price = stock.Price;
            buyer.Backpack.DropItem(new Gold(price));
            var before = GuildBank.GetGoldValue(guild.Name);
            Assert.True(vendor.OnBuyItems(buyer, [new BuyItemResponse(stock.GetDisplayEntity().Serial, 1)]));
            Assert.Equal(before + price * 20 / 120, GuildBank.GetGoldValue(guild.Name));

            // An auction sale in the city: the tax comes out of the seller's share.
            var lot = new Longsword();
            var listing = AuctionHouseSystem.CreateListing(leader, lot, 1000, "moonglow");
            buyer.Backpack.DropItem(new Gold(1000));
            before = GuildBank.GetGoldValue(guild.Name);
            Assert.Equal(AuctionBuyResult.Success, AuctionHouseSystem.TryBuy(buyer, listing.Id));
            Assert.Equal(before + 200, GuildBank.GetGoldValue(guild.Name));
        }
        finally
        {
            foreach (var guard in new List<CityGuard>(CityGuard.Of(city)))
            {
                guard.Delete();
            }

            vendor.Delete();
            buyer.Delete();
            leader.Delete();
            town.Unregister();
        }
    }
}
