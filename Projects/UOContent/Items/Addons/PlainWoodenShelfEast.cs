using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/PlainWoodenShelfEast.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PlainWoodenShelfEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new PlainWoodenShelfEastDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public PlainWoodenShelfEastAddon()
    {
        AddComponent(new AddonComponent(0x4C3B), 0, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class PlainWoodenShelfEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new PlainWoodenShelfEastAddon();
    public override int LabelNumber => 1154161; // Plain Wooden Shelf (East)

    [Constructible]
    public PlainWoodenShelfEastDeed()
    {
    }
}
