using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/FancyLoveseatEast.cs).
/// </summary>
[Furniture]
[SerializationGenerator(0, false)]
public partial class FancyLoveseatEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new FancyLoveseatEastDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public FancyLoveseatEastAddon()
    {
        AddComponent(new AddonComponent(0x4C88), 0, 0, 0);
        AddComponent(new AddonComponent(0x4C89), 0, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class FancyLoveseatEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new FancyLoveseatEastAddon();
    public override int LabelNumber => 1154138; // Fancy Loveseat (East)

    [Constructible]
    public FancyLoveseatEastDeed()
    {
    }
}
