using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/ElvenWashbasinEastAddonWithDrawer.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ElvenWashBasinEastAddonWithDrawer : BaseAddonContainer
{
    [Constructible]
    public ElvenWashBasinEastAddonWithDrawer()
        : base(0x30E0)
    {
        this.AddComponent(new AddonContainerComponent(0x30DF), 0, -1, 0);
    }

    public override BaseAddonContainerDeed Deed => new ElvenWashBasinEastWithDrawerDeed();
    public override bool RetainDeedHue => true;
    public override int DefaultGumpID => 0x0104;
    public override int DefaultDropSound => 0x0042;
}

[SerializationGenerator(0, false)]
public partial class ElvenWashBasinEastWithDrawerDeed : BaseAddonContainerDeed
{
    [Constructible]
    public ElvenWashBasinEastWithDrawerDeed()
        : base()
    {
    }

    public override BaseAddonContainer Addon => new ElvenWashBasinEastAddonWithDrawer();

    public override int LabelNumber => 1073387; // elven wash basin (east)
}
