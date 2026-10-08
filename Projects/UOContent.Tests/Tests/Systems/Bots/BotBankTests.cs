using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotBankTests
{
    [Fact]
    public void Visit_TakesReturnedLotsOut_AndFoldsGoldIntoCheques()
    {
        var bot = new BotMobile { Name = "Вкладчик", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.RawStr = 80;
        bot.MoveToWorld(new Point3D(2100, 1800, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);

        try
        {
            var box = bot.BankBox;
            for (var i = 0; i < 110; i++)
            {
                box.DropItem(new Gold(1000));
            }

            box.DropItem(new Katana());
            box.DropItem(new Longsword());

            var balance = Banker.GetBalance(bot);
            Assert.Equal(2, BotBank.WaitingGoods(bot));
            Assert.True(BotGoals.Bank.Score(bot.Brain) > 0);

            Assert.Equal(2, BotBank.TakeOut(bot));
            Assert.Equal(0, BotBank.WaitingGoods(bot));
            var katana = bot.Backpack.FindItemByType<Katana>();
            Assert.NotNull(katana);
            Assert.True(BotGoods.IsForSale(bot, katana));

            BotBank.Consolidate(bot);
            Assert.True(box.Items.Count < 5);
            Assert.Equal(balance, Banker.GetBalance(bot));
        }
        finally
        {
            bot.Delete();
        }
    }
}
