using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/LordBritishThroneAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LordBritishThroneAddon : BaseAddon
{
    [Constructible]
    public LordBritishThroneAddon()
    {
        this.AddComponent(new AddonComponent(0x1526), 0, 0, 0);
        this.AddComponent(new AddonComponent(0x1527), 0, -1, 0);
    }

    public override BaseAddonDeed Deed => new LordBritishThroneDeed();
}

[SerializationGenerator(0, false)]
public partial class LordBritishThroneDeed : BaseAddonDeed
{
    [Constructible]
    public LordBritishThroneDeed()
    {
        this.LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new LordBritishThroneAddon();

    public override int LabelNumber => 1073243; // Replica of Lord British's Throne - Museum of Vesper
}
