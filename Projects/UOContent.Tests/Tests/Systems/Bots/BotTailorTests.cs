using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotTailorTests
{
    private static BotMobile NewTailor()
    {
        if (DefTailoring.CraftSystem == null)
        {
            DefTailoring.Initialize();
        }

        var bot = new BotMobile { Name = "Портной", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.Skills.Tailoring.Base = 60;
        bot.MoveToWorld(new Point3D(2400, 1800, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);

        foreach (var item in DefTailoring.CraftSystem.CraftItems)
        {
            if (item.Recipe != null)
            {
                bot.AcquireRecipe(item.Recipe);
            }
        }

        return bot;
    }

    [Fact]
    public void Tailor_CutsBolts_KeepsCloth_AndFindsSomethingToSew()
    {
        var bot = NewTailor();
        try
        {
            Assert.True(BotCrafting.IsTailor(bot));
            bot.Backpack.DropItem(new SewingKit());
            bot.Backpack.DropItem(new Scissors());
            bot.Backpack.DropItem(new BoltOfCloth(2));

            new CutMaterialsAction().Tick(bot.Brain);
            var cloth = bot.Backpack.FindItemByType<Cloth>();
            Assert.NotNull(cloth);
            Assert.Null(bot.Backpack.FindItemByType<BoltOfCloth>());

            Assert.True(BotCrafting.KeepsForCraft(bot, cloth));
            Assert.False(BotGoods.IsForSale(bot, cloth));
            Assert.True(BotCrafting.TryPick(bot, DefTailoring.CraftSystem, out var choice));
            Assert.NotNull(choice.Item);
        }
        finally
        {
            bot.Delete();
        }
    }
}
