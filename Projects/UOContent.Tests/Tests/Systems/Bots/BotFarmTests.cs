using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonSeasons;
using Server.Systems.MahaonWorld;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotFarmTests
{
    private static BotMobile NewBot(Point3D at)
    {
        var bot = new BotMobile { Name = "Фермер", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.RawStr = 80;
        bot.MoveToWorld(at, Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        return bot;
    }

    [Fact]
    public void Autumn_HarvestsTheField_AndCottonIsForSale()
    {
        var season = SeasonSystem.CurrentSeason;
        var bot = NewBot(new Point3D(2000, 1800, 0));
        var tiles = new MahaonCropTile[4];
        try
        {
            SeasonSystem.SetSeason(MahaonSeason.Autumn);
            for (var i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new MahaonCropTile(MahaonCropType.Cotton);
                tiles[i].MoveToWorld(new Point3D(2001 + i, 1801, 0), Map.Felucca);
            }

            FarmAtlas.Rebuild();
            var field = FarmAtlas.NearestRipeField(bot.Map, bot.Location, 50);
            Assert.NotNull(field);
            Assert.True(BotGoals.Farm.Score(bot.Brain) > 0);

            var harvest = new HarvestFieldAction(field, 2);
            harvest.Start(bot.Brain);
            for (var i = 0; i < 6 && harvest.Tick(bot.Brain).Status == BotActionStatus.Running; i++)
            {
            }

            var cotton = bot.Backpack.FindItemByType<Cotton>();
            Assert.NotNull(cotton);
            Assert.True(BotGoods.IsForSale(bot, cotton));
            Assert.True(BotGoods.ValueCarried(bot) > 0);
        }
        finally
        {
            SeasonSystem.SetSeason(season);
            bot.Delete();
            foreach (var tile in tiles)
            {
                tile?.Delete();
            }

            FarmAtlas.Rebuild();
        }
    }

    [Fact]
    public void Summer_PicksFruit_AndKeepsWhatTheHorseEats()
    {
        var season = SeasonSystem.CurrentSeason;
        var bot = NewBot(new Point3D(2010, 1800, 0));
        var horse = new Horse();
        MahaonTreeFoliage tree = null;
        try
        {
            SeasonSystem.SetSeason(MahaonSeason.Summer);
            tree = new MahaonTreeFoliage(MahaonTreeSpecies.Apple);
            tree.MoveToWorld(new Point3D(2011, 1800, 0), Map.Felucca);
            Assert.True(FarmAtlas.HasFruit(tree));

            var pick = new PickFruitAction(1);
            pick.Start(bot.Brain);
            for (var i = 0; i < 4; i++)
            {
                pick.Tick(bot.Brain);
            }

            var apples = bot.Backpack.FindItemByType<Apple>();
            Assert.NotNull(apples);
            Assert.True(BotGoods.IsForSale(bot, apples)); // no animals yet

            horse.MoveToWorld(new Point3D(2012, 1800, 0), Map.Felucca);
            horse.SetControlMaster(bot);
            Assert.False(BotGoods.IsForSale(bot, apples)); // the horse's food now
        }
        finally
        {
            SeasonSystem.SetSeason(season);
            bot.Delete();
            horse.Delete();
            tree?.Delete();
        }
    }
}
