using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Shadow Furniture Set/SpikeColumn.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SpikeColumnAddon : BaseAddon
{
    [Constructible]
    public SpikeColumnAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x364C, 1076675), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new SpikeColumnDeed();
}

[SerializationGenerator(0, false)]
public partial class SpikeColumnDeed : BaseAddonDeed
{
    public override int LabelNumber => 1076675; // Spike Column

    [Constructible]
    public SpikeColumnDeed()
    {
        LootType = LootType.Blessed;
        Hue = 1908;
    }

    public override BaseAddon Addon => new SpikeColumnAddon();
}
