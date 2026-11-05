using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/WeatheredBronzeGlobeSculpture.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WeatheredBronzeGlobeSculptureAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new WeatheredBronzeGlobeSculptureDeed();
    public override bool RetainDeedHue => true;

    [Constructible]
    public WeatheredBronzeGlobeSculptureAddon()
    {
        AddComponent(new AddonComponent(0x9CEF), 0, 0, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class WeatheredBronzeGlobeSculptureDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new WeatheredBronzeGlobeSculptureAddon();
    public override int LabelNumber => 1156881; // weathered bronze globe sculpture

    [Constructible]
    public WeatheredBronzeGlobeSculptureDeed()
    {
    }
}
