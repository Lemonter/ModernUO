using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.SkillMasteries;
using Server.Systems.Bots;
using Server.Systems.MahaonMasteries;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotMasteryTests
{
    [Fact]
    public void SwordBot_BuysAndReadsItsTome_TakesThePath_AndUsesItInAFight()
    {
        if (MasteryInfo.Infos == null)
        {
            MasteryInfo.Configure();
        }

        var bot = new BotMobile { Name = "Мечник", Player = true, Body = 0x190, RawInt = 100, RawStr = 100 };
        var guido = new MahaonGuardSergeant();
        var foe = new Orc();
        try
        {
            bot.AddItem(new Backpack());
            bot.AddItem(new Longsword { Layer = Layer.OneHanded }); // the test host has no tile data for layers
            bot.Skills.Swords.Base = 100;
            bot.Skills.Tactics.Base = 100;
            bot.MoveToWorld(new Point3D(2460, 1960, 0), Map.Felucca);
            guido.MoveToWorld(new Point3D(2461, 1960, 0), Map.Felucca);
            foe.MoveToWorld(new Point3D(2462, 1961, 0), Map.Felucca);
            bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
            bot.Mana = bot.ManaMax;

            Assert.Equal(SkillName.Swords, BotMasteries.BestSkill(bot));
            Assert.Null(BotMasteries.Path(bot));
            Assert.Equal(1, BotMasteries.WantedPrimerVolume(bot));

            // Guido's tome comes for the skill the buyer can already use.
            bot.Backpack.DropItem(new Gold(20000));
            var index = Server.Systems.MahaonGuard.GuardRewardShop.IndexOf(Server.Systems.MahaonGuard.GuardShopKind.Primer, 1);
            Assert.Equal(BotActionStatus.Done, new GuardShopBuyAction(guido, index).Tick(bot.Brain).Status);
            Assert.Equal(SkillName.Swords, bot.Backpack.FindItemByType<SkillMasteryPrimer>()?.Skill);

            new ReadScrollsAction().Tick(bot.Brain);
            Assert.True(MasteryInfo.HasLearned(bot, SkillName.Swords, 1));
            Assert.Equal(SkillName.Swords, BotMasteries.Path(bot));
            Assert.Equal(SkillName.Swords, MasteryState.GetCurrentMastery(bot));
            Assert.Equal(2, BotMasteries.WantedPrimerVolume(bot));

            // In a fight it calls on the path: Onslaught armed for the next swing, or Focused Eye cast.
            var used = false;
            for (var i = 0; i < 100 && !used; i++)
            {
                used = BotMasteries.TryUse(bot.Brain, foe);
            }

            Assert.True(used);
            Assert.True(SpecialMove.GetCurrentMove(bot) is OnslaughtMove || bot.Spell is FocusedEyeSpell);
        }
        finally
        {
            SpecialMove.ClearCurrentMove(bot);
            foe.Delete();
            guido.Delete();
            bot.Delete();
        }
    }
}
