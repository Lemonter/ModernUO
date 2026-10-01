using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotCombatTests
{
    private static BotMobile NewBot(Point3D at)
    {
        var bot = new BotMobile { Name = "Боец", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.RawStr = 80;
        bot.Hits = bot.HitsMax;
        bot.MoveToWorld(at, Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        return bot;
    }

    [Fact]
    public void Style_FollowsSkillsAndWeapon()
    {
        var bot = NewBot(new Point3D(1500, 1600, 0));
        try
        {
            Assert.Equal(BotCombatStyle.Melee, BotCombatStyles.Of(bot));

            bot.Skills.Magery.Base = 90;
            Assert.Equal(BotCombatStyle.Mage, BotCombatStyles.Of(bot));

            bot.Skills.Magery.Base = 0;
            bot.Skills.Archery.Base = 80;
            bot.AddItem(new Bow());
            Assert.Equal(BotCombatStyle.Archer, BotCombatStyles.Of(bot));
        }
        finally
        {
            bot.Delete();
        }
    }

    [Fact]
    public void PreyFame_GrowsWithSkill()
    {
        var bot = NewBot(new Point3D(1500, 1600, 0));
        try
        {
            bot.Skills.Wrestling.Base = 20;
            bot.Skills.Tactics.Base = 20;
            var novice = BotCombatStyles.MaxPreyFame(bot.Brain);

            bot.Skills.Wrestling.Base = 100;
            bot.Skills.Tactics.Base = 100;
            var master = BotCombatStyles.MaxPreyFame(bot.Brain);

            Assert.True(master > novice * 5);
        }
        finally
        {
            bot.Delete();
        }
    }

    [Fact]
    public void Attacked_FightsBack()
    {
        var bot = NewBot(new Point3D(1500, 1600, 0));
        var rat = new Rat();
        try
        {
            rat.MoveToWorld(new Point3D(1501, 1600, 0), Map.Felucca);
            rat.DoHarmful(bot);

            Assert.True(BotCombat.Think(bot.Brain) >= 0);
            Assert.Same(rat, bot.Combatant);
            Assert.True(bot.Warmode);
        }
        finally
        {
            bot.Delete();
            rat.Delete();
        }
    }

    [Fact]
    public void Dying_Flees()
    {
        var bot = NewBot(new Point3D(1500, 1600, 0));
        var rat = new Rat();
        try
        {
            rat.MoveToWorld(new Point3D(1501, 1600, 0), Map.Felucca);
            rat.DoHarmful(bot);
            bot.Hits = 1;

            BotCombat.Think(bot.Brain);

            Assert.True(bot.Brain.Combat.Fleeing);
            Assert.Null(bot.Combatant);
        }
        finally
        {
            bot.Delete();
            rat.Delete();
        }
    }

    [Fact]
    public void Vendors_AreNotPrey()
    {
        var vendor = new Banker();
        var rat = new Rat();
        try
        {
            Assert.False(HuntingAtlas.IsFairPrey(vendor));
            Assert.True(HuntingAtlas.IsFairPrey(rat));
        }
        finally
        {
            vendor.Delete();
            rat.Delete();
        }
    }
}
