using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonMetals;
using Xunit;

namespace Server.Tests.Systems.Bots;

[Collection("Sequential UOContent Tests")]
public class SmeltingTests
{
    private static int Smelt(Item forge)
    {
        var smith = new PlayerMobile { Name = "Кузнец", Body = 0x190 };
        smith.AddItem(new Backpack());
        smith.Skills.Mining.Base = 100.0;
        smith.MoveToWorld(new Point3D(1500, 1600, 0), Map.Felucca);
        forge.MoveToWorld(new Point3D(1501, 1600, 0), Map.Felucca);

        var ore = new MahaonOre(MahaonMetal.Iron, 10);
        smith.Backpack.DropItem(ore);

        try
        {
            ore.OnDoubleClick(smith);
            smith.Target.Invoke(smith, forge);
            return smith.Backpack.GetAmount(typeof(MahaonIngot));
        }
        finally
        {
            smith.Delete();
            forge.Delete();
        }
    }

    [Fact]
    public void Forge_OneOreMakesTwoIngots() => Assert.Equal(20, Smelt(new Item(4017)));

    [Fact]
    public void LargeForge_OneOreMakesThreeIngots() => Assert.Equal(30, Smelt(new LargeForgeWest()));
}
