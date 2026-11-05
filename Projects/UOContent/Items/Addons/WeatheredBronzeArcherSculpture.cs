using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/WeatheredBronzeArcherSculpture.cs).
/// </summary>
[Flippable(0x9D2E, 0x9D2F)]
[SerializationGenerator(0, false)]
public partial class WeatheredBronzeArcherComponent : AddonComponent
{
    public override int LabelNumber => 1156884; // weathered bronze archer sculpture

    public WeatheredBronzeArcherComponent()
        : base(0x9D2E)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class WeatheredBronzeArcherAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new WeatheredBronzeArcherDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public WeatheredBronzeArcherAddon()
    {
        AddComponent(new WeatheredBronzeArcherComponent(), 0, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class WeatheredBronzeArcherDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new WeatheredBronzeArcherAddon();
    public override int LabelNumber => 1156884; // weathered bronze archer sculpture

    [Constructible]
    public WeatheredBronzeArcherDeed()
    {
    }
}
