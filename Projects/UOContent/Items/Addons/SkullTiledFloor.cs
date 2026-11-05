using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/SkullTiledFloor.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SkullTiledFloorAddon : BaseAddon
{
    public SkullTiledFloorAddon(bool east)
        : base()
    {
        if (!east)
        {
            AddComponent(new LocalizedAddonComponent(0xA34F, 1125827), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xA350, 1125827), -1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xA34E, 1125827), 1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xA34B, 1125827), 1, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xA34C, 1125827), 0, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xA34D, 1125827), -1, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xA351, 1125827), 1, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xA352, 1125827), 0, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xA353, 1125827), -1, -1, 0);
        }
        else
        {
            AddComponent(new LocalizedAddonComponent(0xA358, 1125827), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xA357, 1125827), 0, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xA359, 1125827), 0, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xA354, 1125827), 1, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xA355, 1125827), 1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xA356, 1125827), 1, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xA35A, 1125827), -1, 1, 0);
            AddComponent(new LocalizedAddonComponent(0xA35B, 1125827), -1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xA35C, 1125827), -1, -1, 0);
        }
    }

    public override BaseAddonDeed Deed => new SkullTiledFloorAddonDeed();
}

[SerializationGenerator(0, false)]
public partial class SkullTiledFloorAddonDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private bool _east;

    [Constructible]
    public SkullTiledFloorAddonDeed()
    {
    }

    public override int LabelNumber => 1159020; // Skull Tiled Floor

    public override BaseAddon Addon => new SkullTiledFloorAddon(_east);

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.CloseGump<RewardOptionGump>();
            RewardOptionGump.DisplayTo(from, this);
        }
        else
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
    }

    public void GetOptions(RewardOptionList list)
    {
        list.Add(1, 1150124); // South
        list.Add(2, 1150123); // East
    }

    public void OnOptionSelected(Mobile from, int option)
    {
        switch ( option )
        {
            case 1:
                _east = false;
                break;
            case 2:
                _east = true;
                break;
        }

        if (!Deleted)
            base.OnDoubleClick(from);
    }
}
