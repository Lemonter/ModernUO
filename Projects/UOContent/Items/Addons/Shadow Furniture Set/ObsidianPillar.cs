using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Shadow Furniture Set/ObsidianPillar.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ObsidianPillarAddon : BaseAddon
{
    [Constructible]
    public ObsidianPillarAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x364F, 1076678), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new ObsidianPillarDeed();
}

[SerializationGenerator(0, false)]
public partial class ObsidianPillarDeed : BaseAddonDeed
{
    public override int LabelNumber => 1076678; // Obsidian Pillar

    [Constructible]
    public ObsidianPillarDeed()
    {
        LootType = LootType.Blessed;
        Hue = 1908;
    }

    public override BaseAddon Addon => new ObsidianPillarAddon();
}
