using System;
using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonQuests;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotQuestTests
{
    private static BotMobile NewBot(Point3D at, double fighting)
    {
        var bot = new BotMobile { Name = "Искатель", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.Skills.Wrestling.Base = fighting;
        bot.Skills.Tactics.Base = fighting;
        bot.Skills.Fletching.Base = 60;
        bot.MoveToWorld(at, Map.Felucca);
        bot.Hits = bot.HitsMax;
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null) { Caution = 20 };
        return bot;
    }

    [Fact]
    public void Bot_ReadsTheGuildBoard_AndDeliversTheOrder()
    {
        if (CraftingGuildSystem.CurrentItemType == null)
        {
            CraftingGuildSystem.Initialize();
        }

        var bot = NewBot(new Point3D(2500, 1850, 0), 30);
        var board = new MahaonCraftingOrderBoard();
        try
        {
            board.MoveToWorld(new Point3D(2501, 1850, 0), Map.Felucca);

            var stamp = CraftingGuildSystem.OrderStamp;
            var amount = CraftingGuildSystem.CurrentAmount;
            var goods = CraftingGuildSystem.CurrentItemType.CreateEntityInstance<Item>();
            goods.Amount = amount;
            bot.Backpack.DropItem(goods);

            Assert.Equal(BotActionResult.Done(1500).Status, new UseBoardAction(board).Tick(bot.Brain).Status);

            Assert.Equal(amount * CraftingGuildSystem.RewardPerUnit, bot.Backpack.GetAmount(typeof(Gold)));
            Assert.NotEqual(stamp, CraftingGuildSystem.OrderStamp);
            Assert.Equal(CraftingGuildSystem.OrderStamp, bot.Brain.SeenOrderStamp);
        }
        finally
        {
            board.Delete();
            bot.Delete();
        }
    }

    [Fact]
    public void Fighter_LearnsTheBountyFromTheBoard_AndHuntsTheOutlaw()
    {
        var hunter = NewBot(new Point3D(2500, 1860, 0), 90);
        var outlaw = NewBot(new Point3D(2530, 1860, 0), 40);
        var board = new MahaonBountyBoard();
        try
        {
            outlaw.IsPk = true;
            board.MoveToWorld(new Point3D(2501, 1860, 0), Map.Felucca);

            Assert.Same(outlaw, BountyHunterSystem.CurrentTarget);
            new UseBoardAction(board).Tick(hunter.Brain);
            Assert.Same(outlaw, hunter.Brain.KnownBounty);

            Assert.True(BotGoals.BountyHunt.Score(hunter.Brain) > 0.5);
            Assert.Equal(0, BotGoals.BountyHunt.Score(outlaw.Brain));

            var plan = BotGoals.BountyHunt.Plan(hunter.Brain);
            Assert.IsType<BountyFightAction>(plan[^1]);
        }
        finally
        {
            board.Delete();
            hunter.Delete();
            outlaw.Delete();
        }
    }
}
