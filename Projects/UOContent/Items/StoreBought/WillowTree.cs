using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class WillowTreeAddon : BaseAddon
{
    [Constructible]
    public WillowTreeAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x224A, 1071105), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new WillowTreeDeed();
}

[SerializationGenerator(0, false)]
public partial class WillowTreeDeed : BaseAddonDeed
{
    [Constructible]
    public WillowTreeDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new WillowTreeAddon();
    public override int LabelNumber => 1071105; // Willow Tree
}
