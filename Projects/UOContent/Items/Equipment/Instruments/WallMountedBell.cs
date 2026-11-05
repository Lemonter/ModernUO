using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Instruments/WallMountedBell.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WallMountedBellSouthDeed : BaseAddonDeed
{
    public override int LabelNumber => 1154162; // Wall Mounted Bell (South)

    [Constructible]
    public WallMountedBellSouthDeed()
    {
    }

    public override BaseAddon Addon => new WallMountedBellSouthAddon();
}

[SerializationGenerator(0, false)]
public partial class WallMountedBellSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new WallMountedBellSouthDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public WallMountedBellSouthAddon()
    {
        AddComponent(new InstrumentedAddonComponent(0x4C5C, 0x66C), 0, 0, 10);
    }
}

[SerializationGenerator(0, false)]
public partial class WallMountedBellEastDeed : BaseAddonDeed
{
    public override int LabelNumber => 1154163; // Wall Mounted Bell (East)

    [Constructible]
    public WallMountedBellEastDeed()
    {
    }

    public override BaseAddon Addon => new WallMountedBellEastAddon();
}

[SerializationGenerator(0, false)]
public partial class WallMountedBellEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new WallMountedBellEastDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public WallMountedBellEastAddon()
    {
        AddComponent(new InstrumentedAddonComponent(0x4C5D, 0x66C), 0, 0, 10);
    }
}
