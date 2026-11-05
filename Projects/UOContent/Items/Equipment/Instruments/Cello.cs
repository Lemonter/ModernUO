using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Instruments/Cello.cs).
/// </summary>
[Flippable(0x4C3E, 0x4C3F)]
[SerializationGenerator(0, false)]
public partial class CelloComponent : InstrumentedAddonComponent
{
    public override int LabelNumber => 1098390; // cello

    public CelloComponent()
        : base(0x4C3E, 0x66D)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class CelloDeed : BaseAddonDeed
{
    public override int LabelNumber => 1098390; // cello

    [Constructible]
    public CelloDeed()
    {
    }

    public override BaseAddon Addon => new CelloAddon();
}

[SerializationGenerator(0, false)]
public partial class CelloAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new CelloDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public CelloAddon()
    {
        AddComponent(new CelloComponent(), 0, 0, 0);
    }
}
