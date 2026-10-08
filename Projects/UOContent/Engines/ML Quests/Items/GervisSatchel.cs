using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GervisSatchel : Backpack
{
    [Constructible]
    public GervisSatchel()
    {
        Hue = Utility.RandomBrightHue();
        // Наш слиток — см. CharacterCreation, причина та же.
        DropItem(new MahaonIngot(Systems.MahaonMetals.MahaonMetal.Iron, 10));
        DropItem(new SmithHammer());
    }
}
