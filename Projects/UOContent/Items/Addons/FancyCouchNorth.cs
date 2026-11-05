using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/FancyCouchNorth.cs).
/// </summary>
[Furniture]
[SerializationGenerator(0, false)]
public partial class FancyCouchNorthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new FancyCouchNorthDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public FancyCouchNorthAddon()
    {
        AddComponent(new AddonComponent(0x9C62), -1, 0, 0);
        AddComponent(new AddonComponent(0x9C61), 0, 0, 0);
        AddComponent(new AddonComponent(0x9C60), 1, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class FancyCouchNorthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new FancyCouchNorthAddon();
    public override int LabelNumber => 1156582; // Fancy Couch (North)

    [Constructible]
    public FancyCouchNorthDeed()
    {
    }
}
