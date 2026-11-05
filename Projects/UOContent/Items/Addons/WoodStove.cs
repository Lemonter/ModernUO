using ModernUO.Serialization;
using System;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/WoodStove.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WoodStoveAddon : BaseAddon
{
    public WoodStoveAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(0xA2A8, 1125660), 0, 0, 0);
                break;
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(0xA2A4, 1125660), 0, 0, 0);
                break;
        }
    }

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
        if ((from.InRange(c.Location, 3)))
        {
            BaseHouse house = BaseHouse.FindHouseAt(from);

            if (house != null && (house.IsOwner(from) || (house.LockDowns.Contains(this) && house.IsCoOwner(from))))
            {
                Components.ForEach(x =>
                {
                    if (x.ItemID == 0xA2A4 || x.ItemID == 0xA2A8)
                    {
                        x.ItemID++;
                        from.PlaySound(958);
                    }
                    else
                        x.ItemID--;
                });

                from.PlaySound(958);
            }
            else
            {
                from.SendLocalizedMessage(502092); // You must be in your house to do this.
            }
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }

    public override BaseAddonDeed Deed => new WoodStoveDeed();
}

[SerializationGenerator(0, false)]
public partial class WoodStoveDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1158967; // Wood Stove

    public override BaseAddon Addon => new WoodStoveAddon(_Direction);

    private DirectionType _Direction;

    [Constructible]
    public WoodStoveDeed()
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
