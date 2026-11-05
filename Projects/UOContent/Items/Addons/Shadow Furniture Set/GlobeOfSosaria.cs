using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Shadow Furniture Set/GlobeOfSosaria.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GlobeOfSosariaAddon : BaseAddon
{
    [Constructible]
    public GlobeOfSosariaAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x3657, 1076681), 1, 1, 0);
        AddComponent(new LocalizedAddonComponent(0x3658, 1076681), 0, 1, 0);
        AddComponent(new LocalizedAddonComponent(0x3659, 1076681), 1, 0, 0);
        AddComponent(new LocalizedAddonComponent(0x3661, 1076681), 1, 1, 0);
    }

    public override BaseAddonDeed Deed => new GlobeOfSosariaDeed();
}

[SerializationGenerator(0, false)]
public partial class GlobeOfSosariaDeed : BaseAddonDeed
{
    public override int LabelNumber => 1076681; // Globe of Sosaria

    [Constructible]
    public GlobeOfSosariaDeed()
    {
        LootType = LootType.Blessed;
        Hue = 1908;
    }

    public override BaseAddon Addon => new GlobeOfSosariaAddon();
}
