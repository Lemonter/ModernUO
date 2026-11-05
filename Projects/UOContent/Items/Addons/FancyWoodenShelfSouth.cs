using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/FancyWoodenShelfSouth.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FancyWoodenShelfSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new FancyWoodenShelfSouthDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public FancyWoodenShelfSouthAddon()
    {
        AddComponent(new AddonComponent(0x4C38), 0, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class FancyWoodenShelfSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new FancyWoodenShelfSouthAddon();
    public override int LabelNumber => 1154158; // Fancy Wooden Shelf (South)

    [Constructible]
    public FancyWoodenShelfSouthDeed()
    {
    }
}
