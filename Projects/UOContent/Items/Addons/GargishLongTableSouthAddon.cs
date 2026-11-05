using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/GargishLongTableSouthAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishLongTableSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new GargishLongTableSouthDeed();

    #region Mondain's Legacy
    public override bool RetainDeedHue => true;
    #endregion

    [Constructible]
    public GargishLongTableSouthAddon()
    {
        this.AddComponent(new AddonComponent(0x402F), -1, 0, 0);
        this.AddComponent(new AddonComponent(0x4030), 0, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class GargishLongTableSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new GargishLongTableSouthAddon();
    public override int LabelNumber => 1111781; // long table

    [Constructible]
    public GargishLongTableSouthDeed()
    {
    }
}
