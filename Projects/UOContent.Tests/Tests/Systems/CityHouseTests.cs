using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonWorld;
using Xunit;

namespace Server.Tests.Systems;

[Collection("Sequential UOContent Tests")]
public class CityHouseTests
{
    private static MahaonCityHouse NewHouse(int x, int y)
    {
        var tiles = new List<Point3D>();
        for (var dx = 0; dx < 4; dx++)
        {
            for (var dy = 0; dy < 3; dy++)
            {
                tiles.Add(new Point3D(x + dx, y + dy, 0));
            }
        }

        var house = new MahaonCityHouse(Map.Felucca, tiles, "Тест");
        MahaonCityHouseSystem.Register(house);
        return house;
    }

    private static PlayerMobile NewPlayer(Point3D at)
    {
        var m = new PlayerMobile { Name = "Жилец", Body = 0x190 };
        m.AddItem(new Backpack());
        m.MoveToWorld(at, Map.Felucca);
        return m;
    }

    [Fact]
    public void Buy_LockDown_DoorAndSellBack()
    {
        var house = NewHouse(3100, 3100);
        var buyer = NewPlayer(new Point3D(3099, 3100, 0));
        var stranger = NewPlayer(new Point3D(3099, 3101, 0));
        var chest = new WoodenChest();
        var door = new LightWoodDoor(DoorFacing.WestCW);

        try
        {
            Assert.True(MahaonCityHouseSystem.IsInsideAnyHouse(Map.Felucca, 3101, 3101));
            Assert.Same(house, MahaonCityHouseSystem.Find(new Point3D(3101, 3101, 0), Map.Felucca));
            Assert.NotNull(house.Region);

            // Not enough gold, then enough.
            Assert.Null(house.Owner);
            MahaonCityHouseSystem.TryBuy(buyer, house, null);
            Assert.Null(house.Owner);

            buyer.BankBox.DropItem(new Gold(house.SalePrice));
            MahaonCityHouseSystem.TryBuy(buyer, house, null);
            Assert.Same(buyer, house.Owner);
            Assert.Same(house, MahaonCityHouseSystem.OwnedBy(buyer));

            // One per account: a second house is refused.
            var second = NewHouse(3120, 3100);
            buyer.BankBox.DropItem(new Gold(second.SalePrice));
            MahaonCityHouseSystem.TryBuy(buyer, second, null);
            Assert.Null(second.Owner);
            second.Delete();

            chest.MoveToWorld(new Point3D(3101, 3101, 0), Map.Felucca);
            house.LockDown(buyer, chest, true);
            Assert.True(house.IsLockedDown(chest));
            Assert.False(chest.Movable);
            Assert.True(house.CanAccess(buyer, chest));
            Assert.False(house.CanAccess(stranger, chest));

            door.MoveToWorld(new Point3D(3099, 3101, 0), Map.Felucca);
            Assert.True(MahaonCityHouseSystem.DoorBlocks(door, stranger));
            Assert.False(MahaonCityHouseSystem.DoorBlocks(door, buyer));

            MahaonCityHouseSystem.SellBack(house, null);
            Assert.Null(house.Owner);
            Assert.True(chest.Movable);
            Assert.False(MahaonCityHouseSystem.DoorBlocks(door, stranger));
        }
        finally
        {
            house.Delete();
            buyer.Delete();
            stranger.Delete();
            chest.Delete();
            door.Delete();
        }
    }
}
