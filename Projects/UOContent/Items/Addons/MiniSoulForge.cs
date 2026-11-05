using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/MiniSoulForge.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MiniSoulForge : BaseAddon
{
    public override BaseAddonDeed Deed => new MiniSoulForgeDeed();

    [Constructible]
    public MiniSoulForge()
    {
        AddComponent( new AddonComponent( 17607 ), 0, 0, 0 );
    }
}
[SerializationGenerator(0, false)]
public partial class MiniSoulForgeDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new MiniSoulForge();

    [Constructible]
    public MiniSoulForgeDeed()
    {
        Name = "Кузница Душ";
    }
}
