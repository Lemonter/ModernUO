using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/MetalTableSouth.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MetalTableSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new MetalTableSouthDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public MetalTableSouthAddon()
    {
        AddComponent(new AddonComponent(0x4CB4), 0, 0, 0);
        AddComponent(new AddonComponent(0x4CB7), 0, 1, 0);
        AddComponent(new AddonComponent(0x4CB5), 1, 0, 0);
        AddComponent(new AddonComponent(0x4CB6), 1, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class MetalTableSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new MetalTableSouthAddon();
    public override int LabelNumber => 1154154; // Metal Table (South)

    [Constructible]
    public MetalTableSouthDeed()
    {
    }
}
