using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/PlushLoveseatEast.cs).
/// </summary>
[Furniture]
[SerializationGenerator(0, false)]
public partial class PlushLoveseatEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new PlushLoveseatEastDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public PlushLoveseatEastAddon()
    {
        AddComponent(new AddonComponent(0x4C84), 0, 0, 0);
        AddComponent(new AddonComponent(0x4C85), 0, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class PlushLoveseatEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new PlushLoveseatEastAddon();
    public override int LabelNumber => 1154136; // Plush Loveseat (East)

    [Constructible]
    public PlushLoveseatEastDeed()
    {
    }
}
