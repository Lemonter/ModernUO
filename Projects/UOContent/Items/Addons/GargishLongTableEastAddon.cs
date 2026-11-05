using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/GargishLongTableEastAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishLongTableEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new GargishLongTableEastDeed();

    #region Mondain's Legacy
    public override bool RetainDeedHue => true;
    #endregion

    [Constructible]
    public GargishLongTableEastAddon()
    {
        this.AddComponent(new AddonComponent(0x4032), 0, 1, 0);
        this.AddComponent(new AddonComponent(0x4031), 0, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class GargishLongTableEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new GargishLongTableEastAddon();
    public override int LabelNumber => 1111782; // long table

    [Constructible]
    public GargishLongTableEastDeed()
    {
    }
}
