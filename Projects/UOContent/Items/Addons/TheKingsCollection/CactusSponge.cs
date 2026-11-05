using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/TheKingsCollection/CactusSponge.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CactusSpongeAddon : BaseAddon, IDyable
{
    [Constructible]
    public CactusSpongeAddon()
    {
        AddComponent(new AddonComponent(0x4C2E), 0, 0, 0);
    }

    public virtual bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
            return false;

        Hue = sender.DyedHue;
        return true;
    }

    public override BaseAddonDeed Deed => new CactusSpongeDeed();
}

[SerializationGenerator(0, false)]
public partial class CactusSpongeDeed : BaseAddonDeed
{
    public override int LabelNumber => 1098374; // Cactus Sponge

    [Constructible]
    public CactusSpongeDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new CactusSpongeAddon();
}
