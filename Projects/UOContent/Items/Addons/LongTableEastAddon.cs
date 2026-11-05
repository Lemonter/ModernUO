using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/LongTableEastAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LongTableEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new LongTableEastDeed();

    [Constructible]
    public LongTableEastAddon()
    {
        AddComponent(new AddonComponent(0x4031), 0, 0, 0);
        AddComponent(new AddonComponent(0x4032), 0, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class LongTableEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new LongTableEastAddon();
    public override int LabelNumber => 1111782; // long table (east)

    [Constructible]
    public LongTableEastDeed()
    {
    }
}
