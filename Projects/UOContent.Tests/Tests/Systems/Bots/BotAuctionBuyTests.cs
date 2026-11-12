using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonAuction;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotAuctionBuyTests
{
    [Fact]
    public void Bot_BuysWhatNoVendorSells_FromTheTownAuction()
    {
        var city = new BotCity("ТестАукцион", Map.Felucca, new Point3D(1700, 1700, 0), null);
        var seller = new PlayerMobile { Name = "Продавец", Body = 0x190 };
        seller.AddItem(new Backpack());
        var bot = new BotMobile { Name = "Покупатель", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.MoveToWorld(new Point3D(1700, 1700, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, city.Name);

        try
        {
            bot.Backpack.DropItem(new Gold(5000));
            var listing = AuctionHouseSystem.CreateListing(seller, new Arrow(100), 100, city.Name);

            // Свой лот не выкупает, чужой по разумной цене — да.
            var steps = BotShopping.PlanPurchase(bot, city, typeof(Arrow), 150);
            Assert.NotNull(steps);
            var buy = Assert.IsType<BuyAtAuctionAction>(steps[^1]);

            Assert.Equal(BotActionStatus.Done, buy.Tick(bot.Brain).Status);
            Assert.Equal(100, bot.Backpack.GetAmount(typeof(Arrow)));
            Assert.Equal(4900, bot.Backpack.GetAmount(typeof(Gold)));
            Assert.False(AuctionHouseSystem.TryGetListing(listing.Id, out _));
        }
        finally
        {
            bot.Delete();
            seller.Delete();
        }
    }

    [Fact]
    public void Bot_PassesOverAnOverpricedLot()
    {
        var city = new BotCity("ТестАукцион2", Map.Felucca, new Point3D(1710, 1700, 0), null);
        var seller = new PlayerMobile { Name = "Жадина", Body = 0x190 };
        seller.AddItem(new Backpack());
        var bot = new BotMobile { Name = "Скупой", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.MoveToWorld(new Point3D(1710, 1700, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, city.Name);
        AuctionListing listing = null;

        try
        {
            bot.Backpack.DropItem(new Gold(5000));
            listing = AuctionHouseSystem.CreateListing(seller, new Arrow(10), 4000, city.Name);

            Assert.Null(BotShopping.PlanPurchase(bot, city, typeof(Arrow), 150));
        }
        finally
        {
            if (listing != null)
            {
                AuctionHouseSystem.TryCancel(seller, listing.Id);
            }

            bot.Delete();
            seller.Delete();
        }
    }
}
