using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotUnloadTests
{
    [Fact]
    public void OverloadedBot_WithNothingToSell_ThrowsJunkAway()
    {
        var bot = new BotMobile { Name = "Грузчик", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.RawStr = 50;
        bot.MoveToWorld(new Point3D(1740, 1700, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        var dropped = new System.Collections.Generic.List<Item>();

        try
        {
            for (var i = 0; i < 6; i++)
            {
                var junk = new PlateChest { Weight = 40 };
                bot.Backpack.DropItem(junk);
                dropped.Add(junk);
            }

            Assert.True(UnloadGoal.Load(bot) >= UnloadGoal.HeavyShare);
            Assert.True(BotGoals.Unload.Score(bot.Brain) > 0);

            var plan = BotGoals.Unload.Plan(bot.Brain);
            Assert.Equal(BotActionStatus.Done, plan[0].Tick(bot.Brain).Status);
            Assert.True(UnloadGoal.Load(bot) < UnloadGoal.LightShare);
            Assert.Equal(0, BotGoals.Unload.Score(bot.Brain));
        }
        finally
        {
            bot.Delete();
            foreach (var item in dropped)
            {
                item.Delete();
            }
        }
    }
}
