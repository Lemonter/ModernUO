using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Crystal Furniture Set/CrystalBrazier.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CrystalBrazierAddon : BaseAddon
{
    [Constructible]
    public CrystalBrazierAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x35EF, 1076667), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new CrystalBrazierDeed();
}

[SerializationGenerator(0, false)]
public partial class CrystalBrazierDeed : BaseAddonDeed
{
    public override int LabelNumber => 1076667; // Crystal Brazier

    public override BaseAddon Addon => new CrystalBrazierAddon();

    [Constructible]
    public CrystalBrazierDeed()
        : base()
    {
        LootType = LootType.Blessed;
        Hue = 1173;
    }
}
