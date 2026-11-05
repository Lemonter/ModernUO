using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/RitualTableAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class RitualTableAddon : BaseAddon
{
    [Constructible]
    public RitualTableAddon()
        : this(0)
    {
    }

    public RitualTableAddon(int hue)
    {
        AddComponent(new AddonComponent(0x4985), 0, 0, 0);
        AddComponent(new AddonComponent(0x4984), 0, 1, 0);
        AddComponent(new AddonComponent(0x4983), 1, 0, 0);
        AddComponent(new AddonComponent(0x4982), 1, 1, 0);
        Hue = hue;
    }

    public override BaseAddonDeed Deed => new RitualTableDeed();
    public override bool RetainDeedHue => true;
}

[SerializationGenerator(0, false)]
public partial class RitualTableDeed : BaseAddonDeed
{
    public override int LabelNumber => 1097690; // ritual table

    [Constructible]
    public RitualTableDeed()
    {
    }

    public override BaseAddon Addon => new RitualTableAddon(Hue);
}
