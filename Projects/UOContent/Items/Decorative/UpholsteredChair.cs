using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0x4C80, 0x4C81)]
[SerializationGenerator(0, false)]
public partial class UpholsteredChairComponent : AddonComponent
{
    [Constructible]
    public UpholsteredChairComponent() : base(0x4C80)
    {
    }

    public override int LabelNumber => 1154173; // Upholstered Chair
}

[SerializationGenerator(0, false)]
public partial class UpholsteredChairDeed : BaseAddonDeed
{
    [Constructible]
    public UpholsteredChairDeed()
    {
    }

    public override int LabelNumber => 1154173; // Upholstered Chair

    public override BaseAddon Addon => new UpholsteredChairAddon();
}

[SerializationGenerator(0, false)]
public partial class UpholsteredChairAddon : BaseAddon
{
    [Constructible]
    public UpholsteredChairAddon() => AddComponent(new UpholsteredChairComponent(), 0, 0, 0);

    public override BaseAddonDeed Deed => new UpholsteredChairDeed();

    public override bool RetainDeedHue => true;
}
