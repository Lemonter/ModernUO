using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecorativeBlackwidowAddon : BaseAddon
{
    [Constructible]
    public DecorativeBlackwidowAddon()
    {
        AddComponent(new AddonComponent(40360), 1, 1, 0);
        AddComponent(new AddonComponent(40362), 1, 0, 0);
        AddComponent(new AddonComponent(40361), 0, 1, 0);
        AddComponent(new AddonComponent(40363), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new DecorativeBlackwidowDeed();
}

[SerializationGenerator(0, false)]
public partial class DecorativeBlackwidowDeed : BaseAddonDeed
{
    [Constructible]
    public DecorativeBlackwidowDeed()
    {
    }

    public override BaseAddon Addon => new DecorativeBlackwidowAddon();
    public override int LabelNumber => 1157897;
}
