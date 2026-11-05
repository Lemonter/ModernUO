using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/WoodenTableSouth.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WoodenTableSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new WoodenTableSouthDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public WoodenTableSouthAddon()
    {
        AddComponent(new AddonComponent(0x4CC8), 0, 0, 0);
        AddComponent(new AddonComponent(0x4CC9), 1, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class WoodenTableSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new WoodenTableSouthAddon();
    public override int LabelNumber => 1154156; // Wooden Table (South)

    [Constructible]
    public WoodenTableSouthDeed()
    {
    }
}
