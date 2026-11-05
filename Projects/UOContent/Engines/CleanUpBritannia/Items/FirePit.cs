using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FirePitAddon : BaseAddon
{
    [Constructible]
    public FirePitAddon()
    {
        AddComponent(new AddonComponent(10749), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new FirePitDeed();
}

[SerializationGenerator(0, false)]
public partial class FirePitDeed : BaseAddonDeed
{
    [Constructible]
    public FirePitDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new FirePitAddon();

    public override int LabelNumber => 1080206; // Fire Pit
}
