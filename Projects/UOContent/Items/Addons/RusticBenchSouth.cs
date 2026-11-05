using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/RusticBenchSouth.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class RusticBenchSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new RusticBenchSouthDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public RusticBenchSouthAddon()
    {
        AddComponent(new AddonComponent(0x0E50), 0, 0, 0);
        AddComponent(new AddonComponent(0x0E51), 1, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class RusticBenchSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new RusticBenchSouthAddon();
    public override int LabelNumber => 1150593; // rustic bench (south)

    [Constructible]
    public RusticBenchSouthDeed()
    {
    }
}
