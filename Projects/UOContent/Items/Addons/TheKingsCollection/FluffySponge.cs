using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/TheKingsCollection/FluffySponge.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FluffySpongeAddon : BaseAddon, IDyable
{
    [Constructible]
    public FluffySpongeAddon()
    {
        AddComponent(new AddonComponent(0x4C31), 0, 0, 0);
    }

    public virtual bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
            return false;

        Hue = sender.DyedHue;
        return true;
    }

    public override BaseAddonDeed Deed => new FluffySpongeDeed();
}

[SerializationGenerator(0, false)]
public partial class FluffySpongeDeed : BaseAddonDeed
{
    public override int LabelNumber => 1098377; // Fluffy Sponge

    [Constructible]
    public FluffySpongeDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new FluffySpongeAddon();
}
