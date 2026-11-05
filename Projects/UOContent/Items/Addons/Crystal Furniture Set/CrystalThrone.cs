using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Crystal Furniture Set/CrystalThrone.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CrystalThroneAddon : BaseAddon
{
    public CrystalThroneAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(0x35ED, 1076666), 0, 0, 0);
                break;
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(0x35EE, 1076666), 0, 0, 0);
                break;
        }
    }

    public override BaseAddonDeed Deed => new CrystalThroneDeed();
}

[SerializationGenerator(0, false)]
public partial class CrystalThroneDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1076666; // Crystal Throne

    public override BaseAddon Addon => new CrystalThroneAddon(_Direction);

    private DirectionType _Direction;

    [Constructible]
    public CrystalThroneDeed()
        : base()
    {
        LootType = LootType.Blessed;
        Hue = 1173;
    }

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)DirectionType.South, 1075386); // South
        list.Add((int)DirectionType.East, 1075387); // East
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _Direction = (DirectionType)choice;

        if (!Deleted)
            base.OnDoubleClick(from);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.CloseGump<RewardOptionGump>();
            RewardOptionGump.DisplayTo(from, this, 1076722); // Please select your crystal throne position
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }
}
