using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Shadow Furniture Set/ShadowPillar.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShadowPillarAddon : BaseAddon
{
    [Constructible]
    public ShadowPillarAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x3650, 1076679), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new ShadowPillarDeed();
}

[SerializationGenerator(0, false)]
public partial class ShadowPillarDeed : BaseAddonDeed
{
    public override int LabelNumber => 1076679; // Shadow Pillar

    [Constructible]
    public ShadowPillarDeed()
    {
        LootType = LootType.Blessed;
        Hue = 1908;
    }

    public override BaseAddon Addon => new ShadowPillarAddon();
}
