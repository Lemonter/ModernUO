using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/ElvenWashbasinSouthAddonWithDrawer.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ElvenWashBasinSouthAddonWithDrawer : BaseAddonContainer
{
    [Constructible]
    public ElvenWashBasinSouthAddonWithDrawer()
        : base(0x30E2)
    {
        this.AddComponent(new AddonContainerComponent(0x30E1), -1, 0, 0);
    }

    public override BaseAddonContainerDeed Deed => new ElvenWashBasinSouthWithDrawerDeed();
    public override bool RetainDeedHue => true;
    public override int DefaultGumpID => 0x0104;
    public override int DefaultDropSound => 0x0042;
}

[SerializationGenerator(0, false)]
public partial class ElvenWashBasinSouthWithDrawerDeed : BaseAddonContainerDeed
{
    [Constructible]
    public ElvenWashBasinSouthWithDrawerDeed()
    {
    }

    public override BaseAddonContainer Addon => new ElvenWashBasinSouthAddonWithDrawer();
    public override int LabelNumber => 1072865; // elven wash basin (south)
}
