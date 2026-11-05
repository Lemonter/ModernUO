using ModernUO.Serialization;
using System;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Shadow Furniture Set/ShadowFirePit.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShadowFirePitAddon : BaseAddon
{
    [Constructible]
    public ShadowFirePitAddon()
    {
        AddComponent(new LocalizedAddonComponent(0x3653, 1076680), 1, 0, 0);
        AddComponent(new LocalizedAddonComponent(0x3652, 1076680), 0, 1, 0);
        AddComponent(new LocalizedAddonComponent(0x3651, 1076680), 1, 1, 0);
    }

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
        if (from.InRange(c.Location, 3))
        {
            if (c.ItemID >= 0x3651 && c.ItemID <= 0x3653)
            {
                Components.ForEach(x => x.ItemID += 3);
            }
            else
            {
                Components.ForEach(x => x.ItemID -= 3);
            }
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }

    public override BaseAddonDeed Deed => new ShadowFirePitDeed();
}

[SerializationGenerator(0, false)]
public partial class ShadowFirePitDeed : BaseAddonDeed
{
    public override int LabelNumber => 1076680; // Shadow Fire Pit

    [Constructible]
    public ShadowFirePitDeed()
    {
        LootType = LootType.Blessed;
        Hue = 1908;
    }

    public override BaseAddon Addon => new ShadowFirePitAddon();
}
