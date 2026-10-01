using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonMetals;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotCraftingTests
{
    private static BotMobile NewSmith(double skill)
    {
        if (DefBlacksmithy.CraftSystem == null)
        {
            DefBlacksmithy.Initialize();
        }

        var bot = new BotMobile { Name = "Кузнец", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.Skills.Blacksmith.Base = skill;
        bot.MoveToWorld(new Point3D(1500, 1600, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        return bot;
    }

    private static void LearnAll(PlayerMobile bot, CraftSystem system)
    {
        foreach (var item in system.CraftItems)
        {
            if (item.Recipe != null)
            {
                bot.AcquireRecipe(item.Recipe);
            }
        }
    }

    [Fact]
    public void Smith_KeepsIngots_ButSellsTheSurplus()
    {
        var bot = NewSmith(60);
        try
        {
            var ingots = new MahaonIngot(MahaonMetal.Iron, 50);
            bot.Backpack.DropItem(ingots);
            Assert.True(BotCrafting.KeepsForCraft(bot, ingots));
            Assert.False(BotGoods.IsForSale(bot, ingots));

            ingots.Amount = 1000;
            Assert.False(BotCrafting.KeepsForCraft(bot, ingots));
            Assert.True(BotGoods.IsForSale(bot, ingots));
        }
        finally
        {
            bot.Delete();
        }
    }

    [Fact]
    public void NonCrafter_SellsIngots()
    {
        var bot = NewSmith(0);
        try
        {
            var ingots = new MahaonIngot(MahaonMetal.Iron, 50);
            bot.Backpack.DropItem(ingots);
            Assert.True(BotGoods.IsForSale(bot, ingots));
        }
        finally
        {
            bot.Delete();
        }
    }

    [Fact]
    public void TryPick_NeedsMaterials()
    {
        var bot = NewSmith(100);
        var system = DefBlacksmithy.CraftSystem;
        try
        {
            LearnAll(bot, system);
            Assert.False(BotCrafting.TryPick(bot, system, out _));

            bot.Backpack.DropItem(new MahaonIngot(MahaonMetal.Iron, 200));
            Assert.True(BotCrafting.TryPick(bot, system, out var choice));
            Assert.True(choice.Chance >= 0.4);
        }
        finally
        {
            bot.Delete();
        }
    }

    [Fact]
    public void CheapestUnknownRecipe_RespectsSkill()
    {
        var novice = NewSmith(0);
        var master = NewSmith(100);
        var system = DefBlacksmithy.CraftSystem;
        try
        {
            var forNovice = BotCrafting.CheapestUnknownRecipe(novice, system);
            if (forNovice != null)
            {
                foreach (var skill in forNovice.CraftItem.Skills)
                {
                    Assert.True(skill.MinSkill <= 0);
                }
            }

            LearnAll(master, system);
            Assert.Null(BotCrafting.CheapestUnknownRecipe(master, system));
        }
        finally
        {
            novice.Delete();
            master.Delete();
        }
    }
}
