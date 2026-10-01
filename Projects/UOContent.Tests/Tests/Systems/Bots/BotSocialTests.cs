using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotSocialTests
{
    private static BotMobile NewBot(Point3D at, double skill = 60)
    {
        var bot = new BotMobile { Name = "Боец", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.RawStr = 80;
        bot.Skills.Wrestling.Base = skill;
        bot.Skills.Tactics.Base = skill;
        bot.Hits = bot.HitsMax;
        bot.MoveToWorld(at, Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        return bot;
    }

    [Fact]
    public void Group_JoinsAndDisbands()
    {
        var leader = NewBot(new Point3D(1500, 1600, 0));
        var member = NewBot(new Point3D(1502, 1600, 0));
        try
        {
            var group = new BotGroup(leader);
            Assert.True(group.TryAdd(member));
            Assert.False(group.TryAdd(member));
            Assert.Same(group, member.Brain.Group);
            Assert.True(BotSocialRules.IsFriend(leader, member));
            Assert.Equal(2.0, BotGoals.FollowGroup.Score(member.Brain));
            Assert.Equal(0, BotGoals.FollowGroup.Score(leader.Brain));
            Assert.True(group.MaxPreyFame() > BotCombatStyles.MaxPreyFame(leader.Brain));

            group.Disband();
            Assert.Null(member.Brain.Group);
            Assert.False(BotSocialRules.IsFriend(leader, member));
        }
        finally
        {
            leader.Delete();
            member.Delete();
        }
    }

    [Fact]
    public void FriendUnderAttack_GetsHelp()
    {
        var bot = NewBot(new Point3D(1500, 1600, 0));
        var friend = NewBot(new Point3D(1503, 1600, 0));
        var rat = new Rat();
        try
        {
            var group = new BotGroup(bot);
            group.TryAdd(friend);
            rat.MoveToWorld(new Point3D(1504, 1600, 0), Map.Felucca);
            friend.Combatant = rat;

            BotCombat.Think(bot.Brain);

            Assert.Same(rat, bot.Combatant);
        }
        finally
        {
            bot.Delete();
            friend.Delete();
            rat.Delete();
        }
    }

    [Fact]
    public void Grudge_IsRemembered()
    {
        var bot = NewBot(new Point3D(1500, 1600, 0));
        var enemy = NewBot(new Point3D(1510, 1600, 0));
        try
        {
            Assert.False(bot.Brain.HoldsGrudge(enemy));
            bot.Brain.AddGrudge(enemy, 60_000);
            Assert.True(bot.Brain.HoldsGrudge(enemy));
        }
        finally
        {
            bot.Delete();
            enemy.Delete();
        }
    }

    [Fact]
    public void Pk_OnlyForPlayerKillers()
    {
        var bot = NewBot(new Point3D(1500, 1600, 0), 80);
        try
        {
            Assert.Equal(0, BotGoals.Pk.Score(bot.Brain));
            bot.IsPk = true;
            Assert.True(BotGoals.Pk.Score(bot.Brain) > 0);
        }
        finally
        {
            bot.Delete();
        }
    }
}
