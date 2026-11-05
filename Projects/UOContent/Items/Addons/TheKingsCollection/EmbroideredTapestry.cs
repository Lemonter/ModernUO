using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/TheKingsCollection/EmbroideredTapestry.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class EmbroideredTapestryAddon : BaseAddon, IDyable
{
    public EmbroideredTapestryAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(19610, 1154143), 0, 0, 0);
                AddComponent(new LocalizedAddonComponent(19611, 1154143), 0, 1, 0);
                break;
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(19609, 1154143), 1, 0, 0);
                AddComponent(new LocalizedAddonComponent(19608, 1154143), 0, 0, 0);
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

    public override BaseAddonDeed Deed => new EmbroideredTapestryDeed();
}

[SerializationGenerator(0, false)]
public partial class EmbroideredTapestryDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1154143; // Embroidered Tapestry

    private DirectionType _Direction;

    [Constructible]
    public EmbroideredTapestryDeed()
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

    public override BaseAddon Addon => new EmbroideredTapestryAddon(_Direction);
}
