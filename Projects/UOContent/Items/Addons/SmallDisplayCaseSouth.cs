using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/SmallDisplayCaseSouth.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SmallDisplayCaseSouthAddon : BaseAddon
{
    public override BaseAddonDeed Deed => new SmallDisplayCaseSouthDeed();

    [Constructible]
    public SmallDisplayCaseSouthAddon()
    {
        AddComponent(new AddonComponent(0x0B0A), 0, 0, 0);
        AddComponent(new AddonComponent(0x0B0C), 0, 0, 3);
    }
}

[SerializationGenerator(0, false)]
public partial class SmallDisplayCaseSouthDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new SmallDisplayCaseSouthAddon();
    public override int LabelNumber => 1155842; // Small Display Case (South)

    [Constructible]
    public SmallDisplayCaseSouthDeed()
    {
    }
}
