using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/Crystal Furniture Set/CrystalRunnerStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CrystalRunnerStatueAddon : BaseAddon
{
    public CrystalRunnerStatueAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(0x35FD, 1076670), 0, 0, 0);
                break;
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(0x35FC, 1076670), 0, 0, 0);
                break;
        }
    }

    public override BaseAddonDeed Deed => new CrystalRunnerStatueDeed();
}

[SerializationGenerator(0, false)]
public partial class CrystalRunnerStatueDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1076670; // Crystal Runner Statue

    public override BaseAddon Addon => new CrystalRunnerStatueAddon(_Direction);

    private DirectionType _Direction;

    [Constructible]
    public CrystalRunnerStatueDeed()
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
            RewardOptionGump.DisplayTo(from, this, 1076725); // Please select your crystal runner statue position
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }
}
