using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Crystal Furniture Set/CrystalAltar.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CrystalAltarAddon : BaseAddon
{
    [Constructible]
    public CrystalAltarAddon()
    {
        AddComponent(new LocalizedAddonComponent(13826, 1076672), 1, 1, 0);
        AddComponent(new LocalizedAddonComponent(13828, 1076672), 1, 0, 0);
        AddComponent(new LocalizedAddonComponent(15778, 1076672), 0, 0, 0);
        AddComponent(new LocalizedAddonComponent(13827, 1076672), 0, 1, 0);
    }

    public override BaseAddonDeed Deed => new CrystalAltarDeed();
}

[SerializationGenerator(0, false)]
public partial class CrystalAltarDeed : BaseAddonDeed
{
    public override int LabelNumber => 1076672; // Crystal Altar

    public override BaseAddon Addon => new CrystalAltarAddon();

    [Constructible]
    public CrystalAltarDeed()
        : base()
    {
        LootType = LootType.Blessed;
        Hue = 1173;
    }
}
