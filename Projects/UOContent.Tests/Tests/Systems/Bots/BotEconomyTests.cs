using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonMetals;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotEconomyTests
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
    public void Goods_OreIsPricedBetweenOneAndTwoIngots()
    {
        var common = new MahaonIngot(MahaonMetal.Iron, 1);
        var ore = new MahaonOre(MahaonMetal.Iron, 1);

        try
        {
            Assert.True(BotGoods.IsRawGood(common));
            // An ore smelts into two ingots: worth more than one, less than the two it yields.
            Assert.True(BotGoods.BaseUnitPrice(ore) > BotGoods.BaseUnitPrice(common));
            Assert.True(BotGoods.BaseUnitPrice(ore) < 2 * BotGoods.BaseUnitPrice(common));
            Assert.False(BotGoods.IsRawGood(new Gold(1)));
        }
        finally
        {
            common.Delete();
            ore.Delete();
        }
    }

    [Fact]
    public void Gather_NeedsATool()
    {
        var bot = NewBot();
        try
        {
            Assert.Equal(0, BotGoals.Mine.Score(bot.Brain));

            bot.Backpack.DropItem(new Pickaxe());
            Assert.True(BotGoals.Mine.Score(bot.Brain) > 0);
        }
        finally
        {
            bot.Delete();
        }
    }

    [Fact]
    public void Trade_WantsAFullPack()
    {
        var bot = NewBot();
        try
        {
            Assert.Equal(0, BotGoals.Trade.Score(bot.Brain));

            bot.Backpack.DropItem(new MahaonIngot(MahaonMetal.Iron, 300));
            Assert.True(BotGoals.Trade.Score(bot.Brain) > 0.5);
        }
        finally
        {
            bot.Delete();
        }
    }

    private sealed class CountingAction : BotAction
    {
        public int Ticks;
        private readonly int _until;

        public CountingAction(int until) => _until = until;

        public override BotActionResult Tick(BotBrain brain) =>
            ++Ticks >= _until ? BotActionResult.Done() : BotActionResult.Running(10);

        public override string Describe(BotBrain brain) => "count";
    }

    [Fact]
    public void Sequence_RunsStepsInOrder()
    {
        var bot = NewBot();
        try
        {
            var a = new CountingAction(2);
            var b = new CountingAction(1);
            var seq = new Sequence(a, b);
            seq.Start(bot.Brain);

            Assert.Equal(BotActionStatus.Running, seq.Tick(bot.Brain).Status);
            Assert.Equal(BotActionStatus.Running, seq.Tick(bot.Brain).Status);
            Assert.Equal(0, b.Ticks);
            Assert.Equal(BotActionStatus.Done, seq.Tick(bot.Brain).Status);
            Assert.Equal(1, b.Ticks);
        }
        finally
        {
            bot.Delete();
        }
    }
}
