using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/FancyLoveseatNorth.cs).
/// </summary>
[Furniture]
[SerializationGenerator(0, false)]
public partial class FancyLoveseatNorthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new FancyLoveseatNorthDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public FancyLoveseatNorthAddon()
    {
        AddComponent(new AddonComponent(0x9C5A), 0, 0, 0);
        AddComponent(new AddonComponent(0x9C59), 1, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class FancyLoveseatNorthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new FancyLoveseatNorthAddon();
    public override int LabelNumber => 1156560; // Fancy Loveseat (North)

    [Constructible]
    public FancyLoveseatNorthDeed()
    {
    }
}
