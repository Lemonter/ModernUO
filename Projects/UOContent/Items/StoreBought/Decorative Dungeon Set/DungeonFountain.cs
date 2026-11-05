using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DungeonFountainAddon : BaseAddon
{
    [Constructible]
    public DungeonFountainAddon(bool east = true)
    {
        AddComponent(new AddonComponent(42226), 0, 0, 0);
        AddComponent(new AddonComponent(42227), 1, 0, 0);
        AddComponent(new AddonComponent(42225), 0, 1, 0);
        AddComponent(new AddonComponent(42228), 1, 1, 0);
    }

    public override BaseAddonDeed Deed => new DungeonFountainDeed();
}

[SerializationGenerator(0, false)]
public partial class DungeonFountainDeed : BaseAddonDeed
{
    [Constructible]
    public DungeonFountainDeed()
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1159474; // Dungeon Fountain
    public override BaseAddon Addon => new DungeonFountainAddon();
}
