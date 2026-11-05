using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/TheKingsCollection/15thAnniversaryLithograph.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FifteenthAnniversaryLithographAddon : BaseAddon
{
    public FifteenthAnniversaryLithographAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(19508, 1154129), 0, 1, 0);
                AddComponent(new LocalizedAddonComponent(19507, 1154129), 0, 0, 0);
                AddComponent(new LocalizedAddonComponent(19506, 1154129), 0, -1, 0);
                break;
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(19510, 1154129), 0, 0, 0);
                AddComponent(new LocalizedAddonComponent(19509, 1154129), 1, 0, 0);
                AddComponent(new LocalizedAddonComponent(19511, 1154129), -1, 0, 0);
                break;
        }
    }

    public override BaseAddonDeed Deed => new FifteenthAnniversaryLithographDeed();
}

[SerializationGenerator(0, false)]
public partial class FifteenthAnniversaryLithographDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1154129; // 15th Anniversary Lithograph

    private DirectionType _Direction;

    [Constructible]
    public FifteenthAnniversaryLithographDeed()
        : base()
    {
        LootType = LootType.Blessed;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            RewardOptionGump.DisplayTo(from, this, 1154194); // Choose a Facing:
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
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

    public override BaseAddon Addon => new FifteenthAnniversaryLithographAddon(_Direction);
}
