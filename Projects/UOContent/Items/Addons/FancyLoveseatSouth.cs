using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/FancyLoveseatSouth.cs).
/// </summary>
[Furniture]
[SerializationGenerator(0, false)]
public partial class FancyLoveseatSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new FancyLoveseatSouthDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public FancyLoveseatSouthAddon()
    {
        AddComponent(new AddonComponent(0x4C87), 0, 0, 0);
        AddComponent(new AddonComponent(0x4C86), 1, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class FancyLoveseatSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new FancyLoveseatSouthAddon();
    public override int LabelNumber => 1154137; // Fancy Loveseat (South)

    [Constructible]
    public FancyLoveseatSouthDeed()
    {
    }
}
