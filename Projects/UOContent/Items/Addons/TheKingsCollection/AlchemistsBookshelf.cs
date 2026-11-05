using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/TheKingsCollection/AlchemistsBookshelf.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AlchemistsBookshelfAddon : BaseAddonContainer
{
    public override BaseAddonContainerDeed Deed => new AlchemistsBookshelfDeed();
    public override bool RetainDeedHue => true;
    public override int DefaultGumpID => 0x4D;
    public override int DefaultDropSound => 0x42;

    public override bool ForceShowProperties => true;

    public AlchemistsBookshelfAddon(DirectionType type)
        : base(type == DirectionType.South ? 0x4C24 : 0x4C25)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class AlchemistsBookshelfDeed : BaseAddonContainerDeed, IRewardOption
{
    public override BaseAddonContainer Addon => new AlchemistsBookshelfAddon(_Direction);
    public override int LabelNumber => 1154192; // Alchemist Bookcase

    private DirectionType _Direction;

    [Constructible]
    public AlchemistsBookshelfDeed()
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
}
