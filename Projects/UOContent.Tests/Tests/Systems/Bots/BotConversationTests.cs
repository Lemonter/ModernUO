using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotConversationTests
{
    [Theory]
    [InlineData("Привет!", BotIntent.Greeting)]
    [InlineData("здравствуйте, Иван", BotIntent.Greeting)]
    [InlineData("hi", BotIntent.Greeting)]
    [InlineData("his sword", BotIntent.None)]
    [InlineData("Ну пока", BotIntent.Farewell)]
    [InlineData("покажи товар", BotIntent.None)]
    [InlineData("привет, дурак", BotIntent.Insult)]
    [InlineData("Как дела?", BotIntent.HowAreYou)]
    [InlineData("как тебя зовут?", BotIntent.Who)]
    [InlineData("Что делаешь?", BotIntent.WhatDoing)]
    [InlineData("что нового в городе", BotIntent.News)]
    [InlineData("где ты живёшь?", BotIntent.Home)]
    [InlineData("ты в гильдии?", BotIntent.Guild)]
    [InlineData("спасибо!", BotIntent.Thanks)]
    [InlineData("наступил в лужу", BotIntent.None)]
    public void Classifies_WhatThePlayerMeant(string speech, BotIntent expected) =>
        Assert.Equal(expected, BotConversation.Classify(BotConversation.Normalize(speech)));

    [Fact]
    public void Replies_FromTheBotsOwnState_AndRemembersRudeness()
    {
        var bot = new BotMobile { Name = "Иван", Player = true, Body = 0x190, Title = "кузнец" };
        var player = new PlayerMobile { Name = "Гость", Body = 0x190 };

        try
        {
            bot.AddItem(new Backpack());
            bot.MoveToWorld(new Point3D(2600, 1800, 0), Map.Felucca);
            player.MoveToWorld(new Point3D(2601, 1800, 0), Map.Felucca);
            bot.Hits = bot.HitsMax;
            bot.Brain = new BotBrain(bot, bot.Location, bot.Map, "Бритайн");

            Assert.Equal("Я Иван, кузнец.", BotConversation.Reply(bot.Brain, player, BotIntent.Who));
            Assert.Contains("Бритайн", BotConversation.Reply(bot.Brain, player, BotIntent.Home));
            Assert.Equal("Ремеслом занят, заказов полно.", BotConversation.Doing(new CraftGoal(0)));

            for (var i = 0; i < 4; i++)
            {
                Server.Systems.MahaonBots.BotRelationships.OnSpokenTo(bot, player, -1.0);
            }

            Assert.True(Server.Systems.MahaonBots.BotRelationships.IsHostileTo(bot, player));
            var curt = BotConversation.Reply(bot.Brain, player, BotIntent.Greeting);
            Assert.DoesNotContain("Гость", curt);
        }
        finally
        {
            Server.Systems.MahaonBots.BotRelationships.OnSpokenTo(bot, player, 4.0);
            bot.Delete();
            player.Delete();
        }
    }
}
