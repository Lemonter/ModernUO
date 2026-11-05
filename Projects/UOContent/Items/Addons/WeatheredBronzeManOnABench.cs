using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/WeatheredBronzeManOnABench.cs).
/// </summary>
[Flippable(0x9D05, 0x9D07)]
[SerializationGenerator(0, false)]
public partial class WeatheredBronzeManOnABenchComponent : AddonComponent
{
    public override int LabelNumber => 1156882; // weathered bronze man on a bench sculpture

    public WeatheredBronzeManOnABenchComponent()
        : base(0x9D05)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class WeatheredBronzeManOnABenchAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new WeatheredBronzeManOnABenchDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public WeatheredBronzeManOnABenchAddon()
    {
        AddComponent(new WeatheredBronzeManOnABenchComponent(), 0, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class WeatheredBronzeManOnABenchDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new WeatheredBronzeManOnABenchAddon();
    public override int LabelNumber => 1156882; // weathered bronze man on a bench sculpture

    [Constructible]
    public WeatheredBronzeManOnABenchDeed()
    {
    }
}
