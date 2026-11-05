using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/FancyCouchWest.cs).
/// </summary>
[Furniture]
[SerializationGenerator(0, false)]
public partial class FancyCouchWestAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new FancyCouchWestDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public FancyCouchWestAddon()
    {
        AddComponent(new AddonComponent(0x9C5F), 0, -1, 0);
        AddComponent(new AddonComponent(0x9C5E), 0, 0, 0);
        AddComponent(new AddonComponent(0x9C5D), 0, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class FancyCouchWestDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new FancyCouchWestAddon();
    public override int LabelNumber => 1156583; // Fancy Couch (West)

    [Constructible]
    public FancyCouchWestDeed()
    {
    }
}
