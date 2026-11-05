using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/FancyCouchSouth.cs).
/// </summary>
[Furniture]
[SerializationGenerator(0, false)]
public partial class FancyCouchSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new FancyCouchSouthDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public FancyCouchSouthAddon()
    {
        AddComponent(new AddonComponent(0x4C8D), 0, 0, 0);
        AddComponent(new AddonComponent(0x4C8E), -1, 0, 0);
        AddComponent(new AddonComponent(0x4C8F), 1, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class FancyCouchSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new FancyCouchSouthAddon();
    public override int LabelNumber => 1154139; // Fancy Couch (South)

    [Constructible]
    public FancyCouchSouthDeed()
    {
    }
}
