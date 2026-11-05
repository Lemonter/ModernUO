using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/WoodenTableEast.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WoodenTableEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new WoodenTableEastDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public WoodenTableEastAddon()
    {
        AddComponent(new AddonComponent(0x4CCB), 0, 0, 0);
        AddComponent(new AddonComponent(0x4CCA), 0, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class WoodenTableEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new WoodenTableEastAddon();
    public override int LabelNumber => 1154157; // Wooden Table (East)

    [Constructible]
    public WoodenTableEastDeed()
    {
    }
}
