using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/PlantTapestrySouth.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PlantTapestrySouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new PlantTapestrySouthDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public PlantTapestrySouthAddon()
    {
        AddComponent(new AddonComponent(0x4C9C), 0, 0, 0);
        AddComponent(new AddonComponent(0x4C9D), 1, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class PlantTapestrySouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new PlantTapestrySouthAddon();
    public override int LabelNumber => 1154146; // Plant Tapestry (South)

    [Constructible]
    public PlantTapestrySouthDeed()
    {
    }
}
