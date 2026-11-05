using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/FancyWoodenShelfEast.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FancyWoodenShelfEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new FancyWoodenShelfEastDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public FancyWoodenShelfEastAddon()
    {
        AddComponent(new AddonComponent(0x4C39), 0, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class FancyWoodenShelfEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new FancyWoodenShelfEastAddon();
    public override int LabelNumber => 1154159; // Fancy Wooden Shelf (East)

    [Constructible]
    public FancyWoodenShelfEastDeed()
    {
    }
}
