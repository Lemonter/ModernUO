using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/LongWoodenTableEast.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LongWoodenTableEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new LongWoodenTableEastDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public LongWoodenTableEastAddon()
    {
        AddComponent(new AddonComponent(0x4CD3), 1, -1, 0);
        AddComponent(new AddonComponent(0x4CD6), 0, -1, 0);
        AddComponent(new AddonComponent(0x4CD7), 0, 0, 0);
        AddComponent(new AddonComponent(0x4CD2), 1, 0, 0);
        AddComponent(new AddonComponent(0x4CD5), 0, 1, 0);
        AddComponent(new AddonComponent(0x4CD4), 1, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class LongWoodenTableEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new LongWoodenTableEastAddon();
    public override int LabelNumber => 1154167; // Long Wooden Table (East)

    [Constructible]
    public LongWoodenTableEastDeed()
    {
    }
}
