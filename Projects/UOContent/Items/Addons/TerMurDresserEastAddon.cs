using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/TerMurDresserEastAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class TerMurDresserEastAddon : BaseAddon
{
    [Constructible]
    public TerMurDresserEastAddon()
    {
        this.AddComponent(new AddonComponent(0x402E), 0, 0, 0);
        this.AddComponent(new AddonComponent(0x402D), 0, -1, 0);
    }

    public override BaseAddonDeed Deed => new TerMurDresserEastDeed();
}

[SerializationGenerator(0, false)]
public partial class TerMurDresserEastDeed : BaseAddonDeed
{
    [Constructible]
    public TerMurDresserEastDeed()
    {
    }

    public override BaseAddon Addon => new TerMurDresserEastAddon();
    public override int LabelNumber => 1111784; // Ter-Mur style dresser (east)
}
