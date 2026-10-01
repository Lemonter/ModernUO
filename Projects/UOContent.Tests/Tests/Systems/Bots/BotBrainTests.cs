using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotBrainTests
{
    private static BotMobile NewBot()
    {
        var bot = new BotMobile { Name = "Тест", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.MoveToWorld(new Point3D(1500, 1600, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        return bot;
    }

    [Fact]
    public void Exhausted_ChoosesRest()
    {
        var bot = NewBot();
        try
        {
            var brain = bot.Brain;
            brain.Fatigue = 1.0;
            brain.Loneliness = 0;
            brain.Restlessness = 0;

            brain.Think();

            Assert.Same(BotGoals.Rest, brain.Goal);
            Assert.IsType<RestAction>(brain.Action);
        }
        finally
        {
            bot.Delete();
        }
    }

    [Fact]
    public void BankGoal_WantsOnlyGoldBeyondPocketMoney()
    {
        var bot = NewBot();
        try
        {
            var brain = bot.Brain;
            Assert.Equal(0, BotGoals.Bank.Score(brain));

            bot.Backpack.DropItem(new Gold(DepositGoldAction.PocketMoney(brain)));
            Assert.Equal(0, BotGoals.Bank.Score(brain));

            bot.Backpack.DropItem(new Gold(5000));
            Assert.True(BotGoals.Bank.Score(brain) > 0.5);
        }
        finally
        {
            bot.Delete();
        }
    }

    [Fact]
    public void Restlessness_RaisesTravel()
    {
        var bot = NewBot();
        try
        {
            var brain = bot.Brain;
            brain.Restlessness = 0;
            var calm = BotGoals.Travel.Score(brain);

            brain.Restlessness = 1;
            var restless = BotGoals.Travel.Score(brain);

            Assert.True(restless > calm);
            Assert.True(restless > BotGoals.Loiter.Score(brain));
        }
        finally
        {
            bot.Delete();
        }
    }
}
