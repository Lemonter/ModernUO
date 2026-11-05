using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/WeatheredBronzeFairySculpture.cs).
/// </summary>
[Flippable(0x9D09, 0x9D0A)]
[SerializationGenerator(0, false)]
public partial class WeatheredBronzeFairySculptureComponent : AddonComponent
{
    public override int LabelNumber => 1156883; // weathered bronze fairy sculpture

    public WeatheredBronzeFairySculptureComponent()
        : base(0x9D09)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class WeatheredBronzeFairySculptureAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new WeatheredBronzeFairySculptureDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public WeatheredBronzeFairySculptureAddon()
    {
        AddComponent(new WeatheredBronzeFairySculptureComponent(), 0, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class WeatheredBronzeFairySculptureDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new WeatheredBronzeFairySculptureAddon();
    public override int LabelNumber => 1156883; // weathered bronze fairy sculpture

    [Constructible]
    public WeatheredBronzeFairySculptureDeed()
    {
    }
}
