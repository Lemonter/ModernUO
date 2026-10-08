using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotSchoolsTests
{
    private static BotMobile NewBot()
    {
        var bot = new BotMobile { Name = "Школяр", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.MoveToWorld(new Point3D(1500, 1600, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        return bot;
    }

    [Fact]
    public void Necromancer_FightsAtRange()
    {
        var bot = NewBot();
        try
        {
            bot.Skills.Necromancy.Base = 80;
            Assert.Equal(BotCombatStyle.Mage, BotCombatStyles.Of(bot));
            Assert.Equal(80, BotCombatStyles.FightingSkill(bot));
        }
        finally
        {
            bot.Delete();
        }
    }

    [Fact]
    public void Tithe_TurnsGoldIntoPoints()
    {
        var bot = NewBot();
        var ankh = new AnkhWest();
        try
        {
            ankh.MoveToWorld(new Point3D(1501, 1600, 0), Map.Felucca);
            bot.Backpack.DropItem(new Gold(3000));

            var result = new TitheAction(ankh, 2000).Tick(bot.Brain);

            Assert.Equal(BotActionStatus.Done, result.Status);
            Assert.Equal(2000, bot.TithingPoints);
            Assert.Equal(1000, bot.Backpack.GetAmount(typeof(Gold)));
        }
        finally
        {
            bot.Delete();
            ankh.Delete();
        }
    }

    [Fact]
    public void Tamer_WantsAFollower_OnlyWithRoom()
    {
        var bot = NewBot();
        try
        {
            Assert.Equal(0, BotGoals.Tame.Score(bot.Brain));

            bot.Skills.AnimalTaming.Base = 80;
            bot.Skills.AnimalLore.Base = 80;
            Assert.True(BotGoals.Tame.Score(bot.Brain) > 0);

            bot.Followers = bot.FollowersMax;
            Assert.Equal(0, BotGoals.Tame.Score(bot.Brain));
        }
        finally
        {
            bot.Delete();
        }
    }
}
