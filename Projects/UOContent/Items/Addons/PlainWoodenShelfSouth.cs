using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/PlainWoodenShelfSouth.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PlainWoodenShelfSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new PlainWoodenShelfSouthDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public PlainWoodenShelfSouthAddon()
    {
        AddComponent(new AddonComponent(0x4C3A), 0, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class PlainWoodenShelfSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new PlainWoodenShelfSouthAddon();
    public override int LabelNumber => 1154160; // Plain Wooden Shelf (South)

    [Constructible]
    public PlainWoodenShelfSouthDeed()
    {
    }
}
