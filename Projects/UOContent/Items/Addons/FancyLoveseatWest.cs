using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/FancyLoveseatWest.cs).
/// </summary>
[Furniture]
[SerializationGenerator(0, false)]
public partial class FancyLoveseatWestAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new FancyLoveseatWestDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public FancyLoveseatWestAddon()
    {
        AddComponent(new AddonComponent(0x9C58), 0, 0, 0);
        AddComponent(new AddonComponent(0x9C57), 0, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class FancyLoveseatWestDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new FancyLoveseatWestAddon();
    public override int LabelNumber => 1156561; // Fancy Loveseat (West)

    [Constructible]
    public FancyLoveseatWestDeed()
    {
    }
}
