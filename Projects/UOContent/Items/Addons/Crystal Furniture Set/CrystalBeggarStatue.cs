using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Crystal Furniture Set/CrystalBeggarStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CrystalBeggarStatueAddon : BaseAddon
{
    public CrystalBeggarStatueAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(0x35F8, 1076668), 0, 0, 0);
                break;
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(0x35F9, 1076668), 0, 0, 0);
                break;
        }
    }

    public override BaseAddonDeed Deed => new CrystalBeggarStatueDeed();
}

[SerializationGenerator(0, false)]
public partial class CrystalBeggarStatueDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1076668; // Crystal Beggar Statue

    public override BaseAddon Addon => new CrystalBeggarStatueAddon(_Direction);

    private DirectionType _Direction;

    [Constructible]
    public CrystalBeggarStatueDeed()
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
            RewardOptionGump.DisplayTo(from, this, 1076723); // Please select your crystal beggar statue position
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }
}
