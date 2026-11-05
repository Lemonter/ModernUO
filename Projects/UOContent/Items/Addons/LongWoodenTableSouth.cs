using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/LongWoodenTableSouth.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LongWoodenTableSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new LongWoodenTableSouthDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public LongWoodenTableSouthAddon()
    {
        AddComponent(new AddonComponent(0x4CCC), -1, 0, 0);
        AddComponent(new AddonComponent(0x4CD1), -1, 1, 0);
        AddComponent(new AddonComponent(0x4CCE), 0, 0, 0);
        AddComponent(new AddonComponent(0x4CCD), 1, 0, 0);
        AddComponent(new AddonComponent(0x4CD0), 0, 1, 0);
        AddComponent(new AddonComponent(0x4CCF), 1, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class LongWoodenTableSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new LongWoodenTableSouthAddon();
    public override int LabelNumber => 1154166; // Long Wooden Table (South)

    [Constructible]
    public LongWoodenTableSouthDeed()
    {
    }
}
