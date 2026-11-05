using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ChaosTileAddon : BaseAddon
{
    [Constructible]
    public ChaosTileAddon()
    {
        AddComponent(new AddonComponent(5347), 0, 0, 0);
        AddComponent(new AddonComponent(5348), 0, 1, 0);
        AddComponent(new AddonComponent(5349), 1, 1, 0);
        AddComponent(new AddonComponent(5350), 1, 0, 0);
    }

    public override BaseAddonDeed Deed => new ChaosTileDeed();
}

[SerializationGenerator(0, false)]
public partial class ChaosTileDeed : BaseAddonDeed
{
    [Constructible]
    public ChaosTileDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new ChaosTileAddon();

    public override int LabelNumber => 1080490; // Chaos Tile Deed
}
