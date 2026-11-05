using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/BuffetTable.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BuffetTableAddon : BaseAddon
{
    public BuffetTableAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.South:
                AddComponent(new AddonComponent(40269), 0, 0, 0);
                AddComponent(new AddonComponent(40270), 0, 1, 0);
                AddComponent(new AddonComponent(40271), 1, 0, 0);
                AddComponent(new AddonComponent(40272), 1, 1, 0);
                AddComponent(new AddonComponent(40273), 2, 0, 0);
                AddComponent(new AddonComponent(40274), 2, 1, 0);
                AddComponent(new AddonComponent(40265), -2, 0, 0);
                AddComponent(new AddonComponent(40266), -2, 1, 0);
                AddComponent(new AddonComponent(40267), -1, 0, 0);
                AddComponent(new AddonComponent(40268), -1, 1, 0);
                break;
            case DirectionType.East:
                AddComponent(new AddonComponent(40264), 1, 2, 0);
                AddComponent(new AddonComponent(40263), 0, 2, 0);
                AddComponent(new AddonComponent(40262), 1, 1, 0);
                AddComponent(new AddonComponent(40261), 0, 1, 0);
                AddComponent(new AddonComponent(40260), 1, 0, 0);
                AddComponent(new AddonComponent(40259), 0, 0, 0);
                AddComponent(new AddonComponent(40258), 1, -1, 0);
                AddComponent(new AddonComponent(40257), 0, -1, 0);
                AddComponent(new AddonComponent(40256), 1, -2, 0);
                AddComponent(new AddonComponent(40255), 0, -2, 0);
                break;
        }
    }

    public override BaseAddonDeed Deed => new BuffetTableDeed();
}

[SerializationGenerator(0, false)]
public partial class BuffetTableDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1157068; // Buffet Table

    public override BaseAddon Addon => new BuffetTableAddon(_Direction);

    private DirectionType _Direction;

    [Constructible]
    public BuffetTableDeed()
        : base()
    {
        LootType = LootType.Blessed;
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
            RewardOptionGump.DisplayTo(from, this, 1154194); // Choose a Facing:
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }
}
