using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonReagentPouch : Bag
{
    [Constructible]
    public MahaonReagentPouch()
    {
        ItemID = 0x0E76;
        Name = "мешочек с реагентами";

        DropItem(new Garlic(20));
        DropItem(new Ginseng(20));
        DropItem(new MandrakeRoot(20));
        DropItem(new SpidersSilk(20));
        DropItem(new BlackPearl(20));
        DropItem(new SulfurousAsh(20));
        DropItem(new Nightshade(20));
        DropItem(new BatWing(20));
    }
}
