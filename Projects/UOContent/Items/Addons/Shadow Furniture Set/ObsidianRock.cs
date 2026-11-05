using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Shadow Furniture Set/ObsidianRock.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ObsidianRockAddon : BaseAddon
{
    [Constructible]
    public ObsidianRockAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x364E, 1076677), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new ObsidianRockDeed();
}

[SerializationGenerator(0, false)]
public partial class ObsidianRockDeed : BaseAddonDeed
{
    public override int LabelNumber => 1076677; // Obsidian Rock

    [Constructible]
    public ObsidianRockDeed()
    {
        LootType = LootType.Blessed;
        Hue = 1908;
    }

    public override BaseAddon Addon => new ObsidianRockAddon();
}
