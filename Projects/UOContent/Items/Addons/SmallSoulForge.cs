using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/SmallSoulForge.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SmallSoulForge : BaseAddon
{
    public override BaseAddonDeed Deed => new SmallSoulForgeDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public SmallSoulForge()
    {
        AddComponent(new ForgeComponent(17607), 0, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class SmallSoulForgeDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new SmallSoulForge();
    public override int LabelNumber => 1149695;

    [Constructible]
    public SmallSoulForgeDeed()
    {
    }
}
