using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/StoneAnvilSouthAddon.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class StoneAnvilSouthAddon : BaseAddon
{
    [Constructible]
    public StoneAnvilSouthAddon()
    {
        this.AddComponent(new AnvilComponent(0x2DD5), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new StoneAnvilSouthDeed();
    public override bool RetainDeedHue => true;
}

[SerializationGenerator(0, false)]
public partial class StoneAnvilSouthDeed : BaseAddonDeed
{
    [Constructible]
    public StoneAnvilSouthDeed()
    {
    }

    public override BaseAddon Addon => new StoneAnvilSouthAddon();
    public override int LabelNumber => 1072876; // stone anvil (south)
}
