using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/LongMetalTableSouth.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LongMetalTableSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new LongMetalTableSouthDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public LongMetalTableSouthAddon()
    {
        AddComponent(new AddonComponent(0x4CC1), -1, 1, 0);
        AddComponent(new AddonComponent(0x4CBD), -1, 0, 0);
        AddComponent(new AddonComponent(0x4CBE), 0, 0, 0);
        AddComponent(new AddonComponent(0x4CC0), 0, 1, 0);
        AddComponent(new AddonComponent(0x4CBC), 1, 0, 0);
        AddComponent(new AddonComponent(0x4CBF), 1, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class LongMetalTableSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new LongMetalTableSouthAddon();
    public override int LabelNumber => 1154164; // Long Metal Table (South)

    [Constructible]
    public LongMetalTableSouthDeed()
    {
    }
}
