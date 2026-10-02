using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonMetals;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotStableTests
{
    private static BotMobile NewBot(Point3D at)
    {
        var bot = new BotMobile { Name = "Возчик", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.RawStr = 60;
        bot.Skills.Mining.Base = 60;
        bot.MoveToWorld(at, Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        return bot;
    }

    private static PackLlama NewLlama(BotMobile owner)
    {
        var llama = new PackLlama();
        llama.MoveToWorld(new Point3D(owner.X + 1, owner.Y, owner.Z), owner.Map);
        llama.SetControlMaster(owner);
        llama.ControlOrder = OrderType.Stop;
        return llama;
    }

    [Fact]
    public void Upkeep_LeadsAndFeedsTheAnimals()
    {
        var bot = NewBot(new Point3D(1900, 1800, 0));
        var llama = NewLlama(bot);
        try
        {
            llama.Loyalty = 50;
            bot.Backpack.DropItem(new Apple(5));

            BotStable.Upkeep(bot.Brain);

            Assert.Equal(OrderType.Follow, llama.ControlOrder);
            Assert.True(llama.Loyalty > 50);
            Assert.Equal(4, bot.Backpack.GetAmount(typeof(Apple)));
        }
        finally
        {
            bot.Delete();
            llama.Delete();
        }
    }

    [Fact]
    public void PackAnimal_CarriesTheLoad_AndHandsItBackForSale()
    {
        var bot = NewBot(new Point3D(1910, 1800, 0));
        var llama = NewLlama(bot);
        try
        {
            var ore = (int)(bot.MaxWeight * 0.8 / 0.5);
            bot.Backpack.DropItem(new MahaonOre(MahaonMetal.Iron, ore));
            var value = BotGoods.ValueCarried(bot);
            Assert.True(BotGoods.TripCapacity(bot) > 1);

            Assert.True(BotStable.TryOffload(bot));
            Assert.True(llama.Backpack.GetAmount(typeof(MahaonOre)) > 0);
            Assert.True(Mobile.BodyWeight + bot.TotalWeight < bot.MaxWeight * 0.6);
            Assert.Equal(value, BotGoods.ValueCarried(bot)); // still counted

            BotStable.Unload(bot);
            Assert.True(bot.Backpack.GetAmount(typeof(MahaonOre)) > 0);
            Assert.True(Mobile.BodyWeight + bot.TotalWeight <= bot.MaxWeight);
        }
        finally
        {
            bot.Delete();
            llama.Delete();
        }
    }

    [Fact]
    public void Unload_TakesOnlyWhatFits_SplittingTheStack()
    {
        var bot = NewBot(new Point3D(1915, 1800, 0));
        var llama = NewLlama(bot);
        try
        {
            var total = (int)(bot.MaxWeight * 3 / 0.5);
            llama.Backpack.DropItem(new MahaonOre(MahaonMetal.Iron, total));

            Assert.True(BotStable.Unload(bot) > 0);

            var carried = bot.Backpack.GetAmount(typeof(MahaonOre));
            Assert.True(carried > 0);
            Assert.Equal(total, carried + llama.Backpack.GetAmount(typeof(MahaonOre)));
            Assert.True(Mobile.BodyWeight + bot.TotalWeight <= bot.MaxWeight);
            Assert.True(Mobile.BodyWeight + bot.TotalWeight > bot.MaxWeight - 12);
        }
        finally
        {
            bot.Delete();
            llama.Delete();
        }
    }

    [Fact]
    public void StableGoal_HorseFirst_ThenPackAnimalForHaulers()
    {
        var bot = NewBot(new Point3D(1920, 1800, 0));
        var horse = new Horse();
        try
        {
            Assert.Equal(0, BotGoals.Stable.Score(bot.Brain)); // can't afford

            bot.Backpack.DropItem(new Gold(3000));
            Assert.True(BotGoals.Stable.Score(bot.Brain) > 0);

            horse.MoveToWorld(new Point3D(1921, 1800, 0), Map.Felucca);
            horse.SetControlMaster(bot);
            Assert.True(BotStable.HasMount(bot));
            Assert.True(BotGoals.Stable.Score(bot.Brain) > 0); // a miner wants a pack animal too

            bot.Skills.Mining.Base = 0;
            Assert.Equal(0, BotGoals.Stable.Score(bot.Brain));
        }
        finally
        {
            bot.Delete();
            horse.Delete();
        }
    }
}
