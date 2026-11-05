using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/goldcarpetAddon.cs).
/// </summary>
[TypeAlias("Server.Items.goldcarpetAddon")]
[SerializationGenerator(0, false)]
public partial class GoldCarpetAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new goldcarpetAddonDeed();

    [Constructible]
    public GoldCarpetAddon()
    {
        AddonComponent ac = null;
        ac = new AddonComponent( 2779 );
        AddComponent( ac, 2, 2, 0 );
        ac = new AddonComponent( 2785 );
        AddComponent( ac, 2, 1, 0 );
        ac = new AddonComponent( 2785 );
        AddComponent( ac, 2, 0, 0 );
        ac = new AddonComponent( 2786 );
        AddComponent( ac, 1, 2, 0 );
        ac = new AddonComponent( 2786 );
        AddComponent( ac, 0, 2, 0 );
        ac = new AddonComponent( 2781 );
        AddComponent( ac, -2, 2, 0 );
        ac = new AddonComponent( 2786 );
        AddComponent( ac, -1, 2, 0 );
        ac = new AddonComponent( 2782 );
        AddComponent( ac, 2, -2, 0 );
        ac = new AddonComponent( 2783 );
        AddComponent( ac, -2, 1, 0 );
        ac = new AddonComponent( 2783 );
        AddComponent( ac, -2, 0, 0 );
        ac = new AddonComponent( 2783 );
        AddComponent( ac, -2, -1, 0 );
        ac = new AddonComponent( 2784 );
        AddComponent( ac, 1, -2, 0 );
        ac = new AddonComponent( 2784 );
        AddComponent( ac, 0, -2, 0 );
        ac = new AddonComponent( 2784 );
        AddComponent( ac, -1, -2, 0 );
        ac = new AddonComponent( 2785 );
        AddComponent( ac, 2, -1, 0 );
        ac = new AddonComponent( 2780 );
        AddComponent( ac, -2, -2, 0 );
        ac = new AddonComponent( 2778 );
        AddComponent( ac, -1, -1, 0 );
        ac = new AddonComponent( 2778 );
        AddComponent( ac, -1, 0, 0 );
        ac = new AddonComponent( 2778 );
        AddComponent( ac, -1, 1, 0 );
        ac = new AddonComponent( 2778 );
        AddComponent( ac, 0, 1, 0 );
        ac = new AddonComponent( 2778 );
        AddComponent( ac, 0, 0, 0 );
        ac = new AddonComponent( 2778 );
        AddComponent( ac, 0, -1, 0 );
        ac = new AddonComponent( 2778 );
        AddComponent( ac, 1, -1, 0 );
        ac = new AddonComponent( 2778 );
        AddComponent( ac, 1, 0, 0 );
        ac = new AddonComponent( 2778 );
        AddComponent( ac, 1, 1, 0 );
    }
}

[SerializationGenerator(0, false)]
public partial class goldcarpetAddonDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new GoldCarpetAddon();

    [Constructible]
    public goldcarpetAddonDeed()
    {
        Name = "золотой ковёр";
    }
}
