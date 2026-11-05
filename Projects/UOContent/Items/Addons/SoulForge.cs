using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/SoulForge.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SoulForge : BaseAddon
{
    public override BaseAddonDeed Deed => new SoulForgeDeed();

    [Constructible]
    public SoulForge()
    {
        AddComponent(new AddonComponent(0x4263), 0, 0, 0);
        AddComponent(new AddonComponent(0x4264), +1, 0, 0);
        AddComponent(new AddonComponent(0x4265), +2, 0, 0);
        AddComponent(new AddonComponent(0x4266), +3, 0, 0);

        AddComponent(new AddonComponent(0x4267), 0, +1, 0);
        AddComponent(new AddonComponent(0x4268), +1, +1, 0);
        AddComponent(new AddonComponent(0x4269), +2, +1, 0);
        AddComponent(new AddonComponent(0x426A), +3, +1, 0);

        AddComponent(new AddonComponent(0x426B), 0, +2, 0);
        AddComponent(new AddonComponent(0x426C), +1, +2, 0);
        AddComponent(new AddonComponent(0x426D), +2, +2, 0);
        AddComponent(new AddonComponent(0x426E), +3, +2, 0);

        AddComponent(new AddonComponent(0x426F), 0, +3, 0);
        AddComponent(new AddonComponent(0x4270), +1, +3, 0);
        AddComponent(new AddonComponent(0x4271), +2, +3, 0);
        AddComponent(new AddonComponent(0x4272), +3, +3, 0);
    }
}

[SerializationGenerator(0, false)]
public partial class SoulForgeDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new SoulForge();
    public override int LabelNumber => 1031696;

    [Constructible]
    public SoulForgeDeed()
    {
    }
}
