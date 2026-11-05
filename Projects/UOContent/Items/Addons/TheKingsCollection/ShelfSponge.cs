using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/TheKingsCollection/ShelfSponge.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShelfSpongeAddon : BaseAddon, IDyable
{
    [Constructible]
    public ShelfSpongeAddon()
    {
        AddComponent(new AddonComponent(0x4C2F), 0, 0, 0);
    }

    public virtual bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
            return false;

        Hue = sender.DyedHue;
        return true;
    }

    public override BaseAddonDeed Deed => new ShelfSpongeDeed();
}

[SerializationGenerator(0, false)]
public partial class ShelfSpongeDeed : BaseAddonDeed
{
    public override int LabelNumber => 1098375; // Shelf Sponge

    [Constructible]
    public ShelfSpongeDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new ShelfSpongeAddon();
}
