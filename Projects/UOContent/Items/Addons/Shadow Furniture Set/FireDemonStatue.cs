using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Shadow Furniture Set/FireDemonStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FireDemonStatueAddon : BaseAddon
{
    [Constructible]
    public FireDemonStatueAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x364B, 1076674), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new FireDemonStatueDeed();

    public virtual void Flip(Mobile from, Direction direction)
    {
        switch (direction)
        {
            case Direction.East:
                AddComponent(new LocalizedAddonComponent(0x369B, 1076674), 0, 0, 0);
                break;
            case Direction.South:
                AddComponent(new LocalizedAddonComponent(0x364B, 1076674), 0, 0, 0);
                break;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class FireDemonStatueDeed : BaseAddonDeed
{
    public override int LabelNumber => 1076674; // Fire Demon Statue

    [Constructible]
    public FireDemonStatueDeed()
    {
        LootType = LootType.Blessed;
        Hue = 1908;
    }

    public override BaseAddon Addon => new FireDemonStatueAddon();
}
