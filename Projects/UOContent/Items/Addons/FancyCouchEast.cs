using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/FancyCouchEast.cs).
/// </summary>
[Furniture]
[SerializationGenerator(0, false)]
public partial class FancyCouchEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new FancyCouchEastDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public FancyCouchEastAddon()
    {
        AddComponent(new AddonComponent(0x4C8C), 0, -1, 0);
        AddComponent(new AddonComponent(0x4C8A), 0, 0, 0);
        AddComponent(new AddonComponent(0x4C8B), 0, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class FancyCouchEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new FancyCouchEastAddon();
    public override int LabelNumber => 1154140; // Fancy Couch (East)

    [Constructible]
    public FancyCouchEastDeed()
    {
    }
}
