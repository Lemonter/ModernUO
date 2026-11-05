using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/GargishCotSouthAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishCotSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new GargishCotSouthDeed();

    #region Mondain's Legacy
    public override bool RetainDeedHue => true;
    #endregion

    [Constructible]
    public GargishCotSouthAddon()
    {
        this.AddComponent(new AddonComponent(0x400D), 0, 0, 0);
        this.AddComponent(new AddonComponent(0x400C), 0, -1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class GargishCotSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new GargishCotSouthAddon();
    public override int LabelNumber => 1111920; // gargish cot (south)

    [Constructible]
    public GargishCotSouthDeed()
    {
    }
}
