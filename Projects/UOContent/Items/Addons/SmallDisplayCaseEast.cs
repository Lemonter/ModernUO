using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/SmallDisplayCaseEast.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SmallDisplayCaseEastAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new SmallDisplayCaseEastDeed();

    [Constructible]
    public SmallDisplayCaseEastAddon()
    {
        AddComponent(new AddonComponent(0x0B09), 0, 0, 0);
        AddComponent(new AddonComponent(0x0B0B), 0, 0, 3);
    }
}

[SerializationGenerator(0, false)]
public partial class SmallDisplayCaseEastDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new SmallDisplayCaseEastAddon();
    public override int LabelNumber => 1155843; // Small Display Case (East)

    [Constructible]
    public SmallDisplayCaseEastDeed()
    {
    }
}
