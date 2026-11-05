using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Instruments/Trumpet.cs).
/// </summary>
[Flippable(0x4C3C, 0x4C3D)]
[SerializationGenerator(0, false)]
public partial class TrumpetComponent : InstrumentedAddonComponent
{
    public override int LabelNumber => 1098388; // trumpet

    public TrumpetComponent()
        : base(0x4C3C, 0x66F)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class TrumpetDeed : BaseAddonDeed
{
    public override int LabelNumber => 1098388; // trumpet

    [Constructible]
    public TrumpetDeed()
    {
    }

    public override BaseAddon Addon => new TrumpetAddon();
}

[SerializationGenerator(0, false)]
public partial class TrumpetAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new TrumpetDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public TrumpetAddon()
    {
        AddComponent(new TrumpetComponent(), 0, 0, 0);
    }
}
