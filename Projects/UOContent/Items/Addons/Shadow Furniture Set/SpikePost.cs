using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Shadow Furniture Set/SpikePost.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SpikePostAddon : BaseAddon
{
    [Constructible]
    public SpikePostAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x364D, 1076676), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new SpikePostDeed();

    public virtual void Flip(Mobile from, Direction direction)
    {
        switch (direction)
        {
            case Direction.East:
                AddComponent(new LocalizedAddonComponent(0x369C, 1076676), 0, 0, 0);
                break;
            case Direction.South:
                AddComponent(new LocalizedAddonComponent(0x364D, 1076676), 0, 0, 0);
                break;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class SpikePostDeed : BaseAddonDeed
{
    public override int LabelNumber => 1076676; // Spike Post

    [Constructible]
    public SpikePostDeed()
    {
        LootType = LootType.Blessed;
        Hue = 1908;
    }

    public override BaseAddon Addon => new SpikePostAddon();
}
