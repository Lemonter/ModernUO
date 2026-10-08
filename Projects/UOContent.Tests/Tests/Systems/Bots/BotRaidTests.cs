using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonRaids;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotRaidTests
{
    private static BotMobile NewBot(Point3D at, double skill)
    {
        var bot = new BotMobile { Name = "Горожанин", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.Skills.Wrestling.Base = skill;
        bot.Skills.Tactics.Base = skill;
        bot.MoveToWorld(at, Map.Felucca);
        bot.Hits = bot.HitsMax;
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null) { Caution = 20 };
        return bot;
    }

    [Fact]
    public void Raid_CallsFighters_SendsTheWeakAway_AndEndsWithTheRaiders()
    {
        var center = new Point3D(2600, 1800, 0);
        var raider = new RaidBandit();
        var fighter = NewBot(new Point3D(2620, 1800, 0), 80);
        var weakling = NewBot(new Point3D(2610, 1800, 0), 10);

        try
        {
            raider.MoveToWorld(center, Map.Felucca);
            RaidAlarm.Raise(Map.Felucca, center, "Тестград");

            Assert.True(BotGoals.DefendTown.Score(fighter.Brain) > 0.8);
            Assert.Equal(0, BotGoals.Shelter.Score(fighter.Brain));
            Assert.Equal(0, BotGoals.DefendTown.Score(weakling.Brain));
            Assert.True(BotGoals.Shelter.Score(weakling.Brain) > 1);

            raider.Delete();
            Assert.Null(RaidAlarm.Nearest(Map.Felucca, center, 100));
            Assert.Equal(0, BotGoals.DefendTown.Score(fighter.Brain));
        }
        finally
        {
            raider.Delete();
            fighter.Delete();
            weakling.Delete();
        }
    }
}
