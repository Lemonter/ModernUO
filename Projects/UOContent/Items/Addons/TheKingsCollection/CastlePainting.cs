using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/TheKingsCollection/CastlePainting.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CastlePaintingAddon : BaseAddon
{
    public CastlePaintingAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(0x4C21, 1154184), 0, 0, 0);
                break;
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(0x4C20, 1154184), 0, 0, 0);
                break;
        }
    }

    public override BaseAddonDeed Deed => new CastlePaintingDeed();
}

[SerializationGenerator(0, false)]
public partial class CastlePaintingDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1154184; // Castle Painting

    private DirectionType _Direction;

    [Constructible]
    public CastlePaintingDeed()
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

    public override BaseAddon Addon => new CastlePaintingAddon(_Direction);
}
