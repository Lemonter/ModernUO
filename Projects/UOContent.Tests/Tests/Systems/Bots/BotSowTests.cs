using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonWorld;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotSowTests
{
    [Fact]
    public void Sow_PlantsOneSeedPerPlot_AndKeepsTheRest()
    {
        var bot = new BotMobile { Name = "Сеятель", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        // С навыком посев не срывается: тест проверяет действие, а не бросок кубика.
        bot.Skills[SkillName.Farming].Base = 100;
        bot.MoveToWorld(new Point3D(2200, 1800, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);
        var planted = new List<Item>();

        // Посев бросает проверку навыка, а тестовый хост её обработчики не ставит.
        var locationCheck = Mobile.SkillCheckLocationHandler;
        var directLocationCheck = Mobile.SkillCheckDirectLocationHandler;
        var targetCheck = Mobile.SkillCheckTargetHandler;
        var directTargetCheck = Mobile.SkillCheckDirectTargetHandler;
        Server.Misc.SkillCheck.Initialize();

        try
        {
            bot.Backpack.DropItem(new MahaonCropSeed(MahaonCropType.Cotton) { Amount = 5 });
            var plots = new List<Point3D> { new(2201, 1800, 0), new(2201, 1801, 0) };

            var sow = new SowAction(plots);
            sow.Start(bot.Brain);
            for (var i = 0; i < 4 && sow.Tick(bot.Brain).Status == BotActionStatus.Running; i++)
            {
            }

            foreach (var plot in plots)
            {
                foreach (var tile in Map.Felucca.GetItemsAt<MahaonCropTile>(plot))
                {
                    planted.Add(tile);
                }
            }

            Assert.Equal(2, planted.Count);
            Assert.Equal(3, bot.Backpack.GetAmount(typeof(MahaonCropSeed)));
        }
        finally
        {
            Mobile.SkillCheckLocationHandler = locationCheck;
            Mobile.SkillCheckDirectLocationHandler = directLocationCheck;
            Mobile.SkillCheckTargetHandler = targetCheck;
            Mobile.SkillCheckDirectTargetHandler = directTargetCheck;
            bot.Delete();
            foreach (var item in planted)
            {
                item.Delete();
            }
        }
    }

    [Fact]
    public void FindPlot_LaysOutRowsOnOpenGround()
    {
        var plot = FarmAtlas.FindPlot(Map.Felucca, new Point3D(2300, 1900, 0), 20, 40, 8);

        Assert.Equal(8, plot.Count);
        foreach (var p in plot)
        {
            Assert.True(FarmAtlas.IsSowable(Map.Felucca, p.X, p.Y, out _));
            Assert.True(p.GetDistanceToSqrt(new Point3D(2300, 1900, 0)) >= 15);
        }
    }
}
