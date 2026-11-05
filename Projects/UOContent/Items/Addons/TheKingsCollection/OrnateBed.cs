using ModernUO.Serialization;
using System;
using Server.Gumps;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/TheKingsCollection/OrnateBed.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class OrnateBedAddon : BaseAddon, IDyable
{
    public OrnateBedAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(0x4C7F, 1154133), 1, 1, 0);
                AddComponent(new LocalizedAddonComponent(0x4C7A, 1154133), 0, 0, 0);
                AddComponent(new LocalizedAddonComponent(0x4C7C, 1154133), -1, 0, 0);
                AddComponent(new LocalizedAddonComponent(0x4C7E, 1154133), 0, 1, 0);
                AddComponent(new LocalizedAddonComponent(0x4C7B, 1154133), 1, 0, 0);
                AddComponent(new LocalizedAddonComponent(0x4C7D, 1154133), -1, 1, 0);
                break;
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(0x4C77, 1154133), 0, -1, 0);
                AddComponent(new LocalizedAddonComponent(0x4C79, 1154133), 0, 1, 0);
                AddComponent(new LocalizedAddonComponent(0x4C74, 1154133), 1, 0, 0);
                AddComponent(new LocalizedAddonComponent(0x4C76, 1154133), 1, -1, 0);
                AddComponent(new LocalizedAddonComponent(0x4C75, 1154133), 1, 1, 0);
                AddComponent(new LocalizedAddonComponent(0x4C78, 1154133), 0, 0, 0);
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

    public override BaseAddonDeed Deed => new OrnateBedDeed();
}

[SerializationGenerator(0, false)]
public partial class OrnateBedDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1154133; // Ornate Bed

    private DirectionType _Direction;

    [Constructible]
    public OrnateBedDeed()
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

    public override BaseAddon Addon => new OrnateBedAddon(_Direction);
}
