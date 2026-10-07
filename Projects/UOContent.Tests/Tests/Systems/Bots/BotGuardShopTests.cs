using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotGuardShopTests
{
    [Fact]
    public void CappedBot_BuysAtGuido_ReadsItsOwnScrolls_AndSellsTheRest()
    {
        var bot = new BotMobile { Name = "Ветеран", Player = true, Body = 0x190 };
        var guido = new MahaonGuardSergeant();
        try
        {
            bot.AddItem(new Backpack());
            bot.Skills.Swords.Cap = 100;
            bot.Skills.Swords.Base = 100;
            bot.MoveToWorld(new Point3D(2450, 1950, 0), Map.Felucca);
            guido.MoveToWorld(new Point3D(2451, 1950, 0), Map.Felucca);
            bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);

            Assert.Equal(105, BotScrolls.WantedPowerScroll(bot));

            // Bought through the shop's own button: the gold goes, a power scroll comes.
            bot.Backpack.DropItem(new Gold(30000));
            Assert.Equal(BotActionStatus.Done, new GuardShopBuyAction(guido, 0).Tick(bot.Brain).Status);
            Assert.True(bot.Backpack.GetAmount(typeof(Gold)) < 30000);
            Assert.NotNull(bot.Backpack.FindItemByType<PowerScroll>());

            // Its own skill's scroll it reads; a scroll for a skill it doesn't train it sells.
            foreach (var bought in bot.Backpack.EnumerateItemsByType<PowerScroll>())
            {
                bought.Delete();
            }

            var own = new PowerScroll(SkillName.Swords, 105);
            var stray = new PowerScroll(SkillName.Fishing, 105);
            bot.Backpack.DropItem(own);
            bot.Backpack.DropItem(stray);
            Assert.True(BotScrolls.HasScrollToRead(bot));
            Assert.Equal(0.7, BotGoals.GuardShop.Score(bot.Brain));

            new ReadScrollsAction().Tick(bot.Brain);
            Assert.Equal(105.0, bot.Skills.Swords.Cap);
            Assert.True(own.Deleted);
            Assert.False(stray.Deleted);
            Assert.True(BotGoods.IsForSale(bot, stray));
            Assert.Equal(4000, BotGoods.BaseUnitPrice(stray));

            // Stats at their cap: a stat scroll lifts it.
            bot.StatCap = bot.RawStatTotal;
            Assert.True(BotScrolls.WantsStatScroll(bot));
            var cap = bot.StatCap;
            bot.Backpack.DropItem(new MahaonStatScroll(5));
            new ReadScrollsAction().Tick(bot.Brain);
            Assert.Equal(cap + 5, bot.StatCap);
        }
        finally
        {
            guido.Delete();
            bot.Delete();
        }
    }
}
