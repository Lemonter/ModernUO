using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/TheKingsCollection/HorsePainting.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class HorsePaintingAddon : BaseAddon, IDyable
{
    public HorsePaintingAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(0x4C23, 1154186), 0, 0, 0);
                break;
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(0x4C22, 1154186), 0, 0, 0);
                break;
        }
    }

    public virtual bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
            return false;

        Hue = sender.DyedHue;
        return true;
    }

    public override BaseAddonDeed Deed => new HorsePaintingDeed();
}

[SerializationGenerator(0, false)]
public partial class HorsePaintingDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1154186; // Horse Painting

    private DirectionType _Direction;

    [Constructible]
    public HorsePaintingDeed()
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

    public override BaseAddon Addon => new HorsePaintingAddon(_Direction);
}
