using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/MetalTableEast.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MetalTableEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new MetalTableEastDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public MetalTableEastAddon()
    {
        AddComponent(new AddonComponent(0x4CBA), 0, 0, 0);
        AddComponent(new AddonComponent(0x4CB9), 0, 1, 0);
        AddComponent(new AddonComponent(0x4CBB), 1, 0, 0);
        AddComponent(new AddonComponent(0x4CB8), 1, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class MetalTableEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new MetalTableEastAddon();
    public override int LabelNumber => 1154155; // Metal Table (East)

    [Constructible]
    public MetalTableEastDeed()
    {
    }
}
