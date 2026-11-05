using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Instruments/Cowbell.cs).
/// </summary>
[Flippable(0x4C5A, 0x4C5B)]
[SerializationGenerator(0, false)]
public partial class CowBellComponent : InstrumentedAddonComponent
{
    public override int LabelNumber => 1098418; // cowbell

    public CowBellComponent()
        : base(0x4C5A, 0x66E)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class CowBellDeed : BaseAddonDeed
{
    public override int LabelNumber => 1098418; // cowbell

    [Constructible]
    public CowBellDeed()
    {
    }

    public override BaseAddon Addon => new CowBellAddon();
}

[SerializationGenerator(0, false)]
public partial class CowBellAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new CowBellDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public CowBellAddon()
    {
        AddComponent(new CowBellComponent(), 0, 0, 0);
    }
}
