using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonQuests;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotKillQuestTests
{
    private static BotMobile NewFighter(Point3D at)
    {
        var bot = new BotMobile { Name = "Наёмник", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.Skills.Wrestling.Base = 80;
        bot.Skills.Tactics.Base = 80;
        bot.MoveToWorld(at, Map.Felucca);
        bot.Hits = bot.HitsMax;
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        return bot;
    }

    [Fact]
    public void Bot_TakesTheMayorsQuest_CountsKills_AndCollectsTheReward()
    {
        var bot = NewFighter(new Point3D(2500, 1870, 0));
        var mayor = new MahaonMayor();
        try
        {
            mayor.MoveToWorld(new Point3D(2501, 1870, 0), Map.Felucca);

            new TalkToGiverAction(mayor, QuestGiverKind.Mayor).Tick(bot.Brain);
            var quest = BotQuests.QuestOf(bot, QuestGiverKind.Mayor);
            Assert.NotNull(quest);
            Assert.Same(mayor, quest.Value.Giver);
            Assert.False(quest.Value.IsComplete);

            var type = AssemblyHandler.FindTypeByName(quest.Value.Target);
            for (var i = 0; i < quest.Value.Required; i++)
            {
                var prey = type.CreateEntityInstance<BaseCreature>();
                MayorQuestSystem.OnCreatureKilled(bot, prey);
                prey.Delete();
            }

            Assert.True(BotQuests.QuestOf(bot, QuestGiverKind.Mayor)!.Value.IsComplete);
            Assert.Equal(0.75, BotGoals.MayorQuest.Score(bot.Brain));

            new TalkToGiverAction(mayor, QuestGiverKind.Mayor).Tick(bot.Brain);
            Assert.Null(BotQuests.QuestOf(bot, QuestGiverKind.Mayor));
            Assert.True(bot.Backpack.GetAmount(typeof(Gold)) > 0);
        }
        finally
        {
            mayor.Delete();
            bot.Delete();
        }
    }

    [Fact]
    public void RangerQuest_CountsHeadsCarried_AndWantsOnlyItsKind()
    {
        var bot = NewFighter(new Point3D(2500, 1880, 0));
        var ranger = new MahaonRanger();
        try
        {
            ranger.MoveToWorld(new Point3D(2501, 1880, 0), Map.Felucca);
            new TalkToGiverAction(ranger, QuestGiverKind.Ranger).Tick(bot.Brain);
            var quest = BotQuests.QuestOf(bot, QuestGiverKind.Ranger)!.Value;

            var wanted = new MahaonAnimalHead(quest.Target, "голова");
            var other = new MahaonAnimalHead(quest.Target == "Boar" ? "Eagle" : "Boar", "голова");
            Assert.True(BotQuests.WantsTrophy(bot, wanted));
            Assert.False(BotQuests.WantsTrophy(bot, other));
            other.Delete();

            bot.Backpack.DropItem(wanted);
            Assert.Equal(1, BotQuests.QuestOf(bot, QuestGiverKind.Ranger)!.Value.Done);

            for (var i = 1; i < quest.Required; i++)
            {
                bot.Backpack.DropItem(new MahaonAnimalHead(quest.Target, "голова"));
            }

            Assert.True(BotQuests.QuestOf(bot, QuestGiverKind.Ranger)!.Value.IsComplete);
            new TalkToGiverAction(ranger, QuestGiverKind.Ranger).Tick(bot.Brain);
            Assert.Null(BotQuests.QuestOf(bot, QuestGiverKind.Ranger));
            Assert.Null(bot.Backpack.FindItemByType<MahaonAnimalHead>());
        }
        finally
        {
            ranger.Delete();
            bot.Delete();
        }
    }
}
