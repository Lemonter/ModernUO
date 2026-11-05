using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/TerMurDresserSouthAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TerMurDresserSouthAddon : BaseAddon
{
    [Constructible]
    public TerMurDresserSouthAddon()
    {
        this.AddComponent(new AddonComponent(0x402B), 0, 0, 0);
        this.AddComponent(new AddonComponent(0x402C), 1, 0, 0);
    }

    public override BaseAddonDeed Deed => new TerMurDresserSouthDeed();
}

[SerializationGenerator(0, false)]
public partial class TerMurDresserSouthDeed : BaseAddonDeed
{
    [Constructible]
    public TerMurDresserSouthDeed()
    {
    }

    public override BaseAddon Addon => new TerMurDresserSouthAddon();
    public override int LabelNumber => 1111783; // Ter-Mur style dresser (south)
}
