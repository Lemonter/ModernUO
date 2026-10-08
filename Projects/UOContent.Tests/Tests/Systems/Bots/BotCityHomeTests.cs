using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.Bots;
using Server.Systems.MahaonWorld;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class BotCityHomeTests
{
    [Fact]
    public void Bot_BuysTheApartment_AndLocksAChestInIt()
    {
        var tiles = new List<Point3D>();
        for (var dx = 0; dx < 4; dx++)
        {
            for (var dy = 0; dy < 3; dy++)
            {
                tiles.Add(new Point3D(3300 + dx, 3100 + dy, 0));
            }
        }

        var house = new MahaonCityHouse(Map.Felucca, tiles, "Квартира");
        MahaonCityHouseSystem.Register(house);
        var sign = new MahaonCityHouseSign(house);
        sign.MoveToWorld(new Point3D(3299, 3100, 0), Map.Felucca);

        var bot = new BotMobile { Name = "Жилец", Player = true, Body = 0x190 };
        bot.AddItem(new Backpack());
        bot.MoveToWorld(new Point3D(3299, 3101, 0), Map.Felucca);
        bot.Brain = new BotBrain(bot, bot.Location, bot.Map, null);

        try
        {
            Assert.Same(sign, BotCityHomes.FindSign(house));
            bot.BankBox.DropItem(new Gold(house.SalePrice + 20_000));

            Assert.Equal(BotActionStatus.Done, new BuyCityHomeAction(house, sign).Tick(bot.Brain).Status);
            Assert.Same(bot, house.Owner);
            Assert.Same(house, BotCityHomes.CityHome(bot));
            Assert.Equal(HouseFitting.Chest, FurnishCityHomeGoal.NextFitting(bot, house));

            bot.Backpack.DropItem(new WoodenChest());
            Assert.Equal(BotActionStatus.Done, new FitCityHomeAction(house, HouseFitting.Chest).Tick(bot.Brain).Status);
            Assert.Single(house.Secures);
            Assert.NotEqual(HouseFitting.Chest, FurnishCityHomeGoal.NextFitting(bot, house));

            bot.Delete();
            Assert.Null(house.Owner);
        }
        finally
        {
            bot.Delete();
            house.Delete();
            sign.Delete();
        }
    }
}
