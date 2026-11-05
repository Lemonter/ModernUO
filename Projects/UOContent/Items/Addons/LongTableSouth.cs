using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/LongTableSouth.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LongTableSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new LongTableSouthDeed();

    [Constructible]
    public LongTableSouthAddon()
    {
        AddComponent(new AddonComponent(0x402F), 0, 0, 0);
        AddComponent(new AddonComponent(0x4030), 1, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class LongTableSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new LongTableSouthAddon();
    public override int LabelNumber => 1111781; // long table (south)

    [Constructible]
    public LongTableSouthDeed()
    {
    }
}
