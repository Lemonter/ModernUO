using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/RusticBenchEast.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class RusticBenchEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new RusticBenchEastDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public RusticBenchEastAddon()
    {
        AddComponent(new AddonComponent(0x0E53), 0, 0, 0);
        AddComponent(new AddonComponent(0x0E52), 0, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class RusticBenchEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new RusticBenchEastAddon();
    public override int LabelNumber => 1150594; // rustic bench (east)

    [Constructible]
    public RusticBenchEastDeed()
    {
    }
}
