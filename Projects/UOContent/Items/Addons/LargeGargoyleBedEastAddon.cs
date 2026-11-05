using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/LargeGargoyleBedEastAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LargeGargoyleBedEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new LargeGargoyleBedEastDeed();

    #region Mondain's Legacy
    public override bool RetainDeedHue => true;
    #endregion

    [Constructible]
    public LargeGargoyleBedEastAddon()
    {
        //Left Side
        this.AddComponent(new AddonComponent(0x4019), 0, 0, 0);
        this.AddComponent(new AddonComponent(0x401C), 0, 1, 0);
        this.AddComponent(new AddonComponent(0x401F), 0, 2, 0);
        //Middle
        this.AddComponent(new AddonComponent(0x401A), 1, 0, 0);
        this.AddComponent(new AddonComponent(0x401D), 1, 1, 0);
        this.AddComponent(new AddonComponent(0x4020), 1, 2, 0);
        //Right Side
        this.AddComponent(new AddonComponent(0x401B), 2, 0, 0);
        this.AddComponent(new AddonComponent(0x401E), 2, 1, 0);
        this.AddComponent(new AddonComponent(0x4021), 2, 2, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class LargeGargoyleBedEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new LargeGargoyleBedEastAddon();
    public override int LabelNumber => 1111762; // large gargish bed (east)

    [Constructible]
    public LargeGargoyleBedEastDeed()
    {
    }
}
