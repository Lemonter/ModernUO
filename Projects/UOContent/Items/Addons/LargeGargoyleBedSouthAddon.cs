using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/LargeGargoyleBedSouthAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LargeGargoyleBedSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new LargeGargoyleBedSouthDeed();

    #region Mondain's Legacy
    public override bool RetainDeedHue => true;
    #endregion

    [Constructible]
    public LargeGargoyleBedSouthAddon()
    {
        //Left Side
        this.AddComponent(new AddonComponent(0x4010), 0, 0, 0);
        this.AddComponent(new AddonComponent(0x4013), 0, 1, 0);
        this.AddComponent(new AddonComponent(0x4016), 0, 2, 0);
        //Middle
        this.AddComponent(new AddonComponent(0x4011), 1, 0, 0);
        this.AddComponent(new AddonComponent(0x4014), 1, 1, 0);
        this.AddComponent(new AddonComponent(0x4017), 1, 2, 0);
        //Right Side
        this.AddComponent(new AddonComponent(0x4012), 2, 0, 0);
        this.AddComponent(new AddonComponent(0x4015), 2, 1, 0);
        this.AddComponent(new AddonComponent(0x4018), 2, 2, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class LargeGargoyleBedSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new LargeGargoyleBedSouthAddon();
    public override int LabelNumber => 1111761; // large gargish bed (south)

    [Constructible]
    public LargeGargoyleBedSouthDeed()
    {
    }
}
