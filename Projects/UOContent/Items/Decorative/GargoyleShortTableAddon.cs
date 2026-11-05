using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GargoyleShortTableAddon : BaseAddon
{
    [Constructible]
    public GargoyleShortTableAddon()
    {
        AddComponent(new AddonComponent(0x4033), 0, 0, 0);
        AddComponent(new AddonComponent(0x4035), 0, 1, 0);
        AddComponent(new AddonComponent(0x4034), 1, 0, 0);
        AddComponent(new AddonComponent(0x4036), 1, 1, 0);
    }

    public override BaseAddonDeed Deed => new GargoyleShortTableDeed();

    public override bool RetainDeedHue => true;
}

[SerializationGenerator(0, false)]
public partial class GargoyleShortTableDeed : BaseAddonDeed
{
    [Constructible]
    public GargoyleShortTableDeed()
    {
    }

    public override BaseAddon Addon => new GargoyleShortTableAddon();

    public override int LabelNumber => 1095307; // short gargish table
}
