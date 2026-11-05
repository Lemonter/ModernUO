using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/PlantTapestryEast.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PlantTapestryEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new PlantTapestryEastDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public PlantTapestryEastAddon()
    {
        AddComponent(new AddonComponent(0x4C9F), 0, 0, 0);
        AddComponent(new AddonComponent(0x4C9E), 0, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class PlantTapestryEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new PlantTapestryEastAddon();
    public override int LabelNumber => 1154147; // Plant Tapestry (East)

    [Constructible]
    public PlantTapestryEastDeed()
    {
    }
}
