using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/GargishCouchEastAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishCouchEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new GargishCouchEastDeed();

    #region Mondain's Legacy
    public override bool RetainDeedHue => true;
    #endregion

    [Constructible]
    public GargishCouchEastAddon()
    {
        this.AddComponent(new AddonComponent(0x402A), 0, 0, 0);
        this.AddComponent(new AddonComponent(0x4029), 0, -1, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class GargishCouchEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new GargishCouchEastAddon();
    public override int LabelNumber => 1111776; // gargish couch (East)

    [Constructible]
    public GargishCouchEastDeed()
    {
    }
}
