using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecorativeStableSet : Backpack
{
    [Constructible]
    public DecorativeStableSet()
    {
        DropItem(new CowStatue());
        DropItem(new HorseStatue());
        DropItem(new ChickenStatue());
        DropItem(new MetalTubDeed());
        DropItem(new Feedbag());
        DropItem(new CowPie());

        DropItem(new Bag());
    }

    public override int LabelNumber => 1159272; // Decorative Stable Set
}
