using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotAlchemyCookingTests
{
    private static BotMobile NewCrafter(CraftSystem system, SkillName skill)
    {
        var bot = new BotMobile { Name = "Мастер", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.Skills[skill].Base = 60;
        bot.MoveToWorld(new Point3D(2500, 1800, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);

        foreach (var item in system.CraftItems)
        {
            if (item.Recipe != null)
            {
                bot.AcquireRecipe(item.Recipe);
            }
        }

        return bot;
    }

    [Fact]
    public void Alchemist_BrewsFromReagents_AndKeepsSomePotionsForItself()
    {
        if (DefAlchemy.CraftSystem == null)
        {
            DefAlchemy.Initialize();
        }

        var bot = NewCrafter(DefAlchemy.CraftSystem, SkillName.Alchemy);
        try
        {
            Assert.True(BotCrafting.IsAlchemist(bot));
            bot.Backpack.DropItem(new MortarPestle());
            bot.Backpack.DropItem(new Ginseng(20));
            bot.Backpack.DropItem(new Bottle(20));

            Assert.True(BotCrafting.KeepsForCraft(bot, bot.Backpack.FindItemByType<Ginseng>()));
            Assert.True(BotCrafting.TryPick(bot, DefAlchemy.CraftSystem, out var choice));
            Assert.NotNull(choice.Item);

            var potions = new HealPotion { Amount = 3, PlayerConstructed = true };
            bot.Backpack.DropItem(potions);
            Assert.False(BotGoods.IsForSale(bot, potions));
            potions.Amount = 30;
            Assert.True(BotGoods.IsForSale(bot, potions));
        }
        finally
        {
            bot.Delete();
        }
    }

    [Fact]
    public void Cook_GrillsRawMeat_ButNeverPicksAnOvenRecipe()
    {
        if (DefCooking.CraftSystem == null)
        {
            DefCooking.Initialize();
        }

        var bot = NewCrafter(DefCooking.CraftSystem, SkillName.Cooking);
        Item forge = null;
        try
        {
            bot.Backpack.DropItem(new Skillet());
            bot.Backpack.DropItem(new RawRibs(10));

            // Grilling needs a fire within reach: a forge stands by.
            forge = new Static(0x0FB1);
            forge.MoveToWorld(new Point3D(bot.X + 1, bot.Y, bot.Z), bot.Map);
            Assert.True(BotCrafting.TryPick(bot, DefCooking.CraftSystem, out var choice));
            Assert.False(choice.Item.NeedOven);
            Assert.Equal(typeof(Ribs), choice.Item.ItemType);
        }
        finally
        {
            forge?.Delete();
            bot.Delete();
        }
    }
}
