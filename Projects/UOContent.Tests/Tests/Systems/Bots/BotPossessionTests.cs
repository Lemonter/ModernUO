using System;
using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonBots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotPossessionTests
{
    [Fact]
    public void Possessed_Player_WeighsOnlyFocusAndUpkeep()
    {
        var player = new PlayerMobile { Name = "Игрок", Body = 0x190 };
        player.AddItem(new Backpack());
        player.MoveToWorld(new Point3D(1800, 1800, 0), Map.Felucca);

        try
        {
            var brain = BotSystem.Possess(player);
            Assert.NotNull(brain);
            Assert.Same(brain, player.GetBrain());
            Assert.True(BotSystem.IsPossessed(player));

            brain.Focus = BotGoals.Mine;
            brain.Think();

            Assert.NotEmpty(brain.LastScores);
            foreach (var (goal, _) in brain.LastScores)
            {
                Assert.True(goal == BotGoals.Mine || goal.IsUpkeep, goal.Name);
            }

            BotSystem.Unregister(player);
            Assert.Null(player.GetBrain());
            Assert.False(BotSystem.IsPossessed(player));
        }
        finally
        {
            BotSystem.Unregister(player);
            player.Delete();
        }
    }

    [Fact]
    public void Outfit_WearsTheWeaponItCarries()
    {
        var bot = new BotMobile { Name = "Мечник", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.RawStr = 100;
        bot.RawDex = 60;
        bot.RawInt = 40;
        bot.Skills.Swords.Base = 70;
        bot.MoveToWorld(new Point3D(1810, 1800, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);

        try
        {
            Assert.NotNull(OutfitGoal.Missing(bot, out var weapon));
            Assert.True(weapon);
            Assert.Equal(0, BotGoals.Outfit.Score(bot.Brain)); // no gold, nothing to wear

            // The test host has no tile data, so items carry no layer of their own.
            bot.Backpack.DropItem(new Katana { Layer = Layer.TwoHanded });
            Assert.Equal(1.0, BotGoals.Outfit.Score(bot.Brain));

            var plan = BotGoals.Outfit.Plan(bot.Brain);
            Assert.Single(plan);
            Assert.Equal(BotActionStatus.Done, plan[0].Tick(bot.Brain).Status);
            Assert.IsType<Katana>(bot.FindItemOnLayer(Layer.TwoHanded));

            Assert.NotNull(OutfitGoal.Missing(bot, out weapon));
            Assert.False(weapon); // armour next
        }
        finally
        {
            bot.Delete();
        }
    }

    [Fact]
    public void Rumours_ArePassedOn()
    {
        BotRumors.Spread("Тестовый слух");
        Assert.True(BotRumors.TryPick(out var text));
        Assert.False(string.IsNullOrEmpty(text));
    }

    [Fact]
    public void Brain_SurvivesSaveAndLoad()
    {
        var bot = new BotMobile { Name = "Сохранённый", Player = true, Body = 0x190 };
        bot.MoveToWorld(new Point3D(1820, 1800, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, new Point3D(1500, 1600, 0), Map.Felucca, "Britain");
        BotMobile copy = null;

        try
        {
            var writer = new BufferWriter(true);
            bot.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

            copy = new BotMobile(World.NewMobile);
            copy.Deserialize(new BufferReader(buffer));

            Assert.NotNull(copy.Brain);
            Assert.Same(copy, copy.Brain.Bot);
            Assert.Equal(new Point3D(1500, 1600, 0), copy.Brain.Home);
            Assert.Equal("Britain", copy.Brain.HomeCity);
            Assert.Equal(bot.Brain.Greed, copy.Brain.Greed);
        }
        finally
        {
            bot.Delete();
            copy?.Delete();
        }
    }
}
