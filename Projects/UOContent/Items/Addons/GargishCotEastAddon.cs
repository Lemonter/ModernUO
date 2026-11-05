using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/GargishCotEastAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishCotEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new GargishCotEastDeed();

    #region Mondain's Legacy
    public override bool RetainDeedHue => true;
    #endregion

    [Constructible]
    public GargishCotEastAddon()
    {
        this.AddComponent(new AddonComponent(0x400E), 0, 0, 0);
        this.AddComponent(new AddonComponent(0x400F), 1, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class GargishCotEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new GargishCotEastAddon();
    public override int LabelNumber => 1111921; // gargish cot (east)

    [Constructible]
    public GargishCotEastDeed()
    {
    }
}
