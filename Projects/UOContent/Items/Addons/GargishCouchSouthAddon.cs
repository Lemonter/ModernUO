using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/GargishCouchSouthAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishCouchSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new GargishCouchSouthDeed();

    #region Mondain's Legacy
    public override bool RetainDeedHue => true;
    #endregion

    [Constructible]
    public GargishCouchSouthAddon()
    {
        this.AddComponent(new AddonComponent(0x4027), 0, 0, 0);
        this.AddComponent(new AddonComponent(0x4028), 1, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class GargishCouchSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new GargishCouchSouthAddon();
    public override int LabelNumber => 1111775; // gargish couch (South)

    [Constructible]
    public GargishCouchSouthDeed()
    {
    }
}
