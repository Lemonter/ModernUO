using ModernUO.Serialization;
using System;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/GreenMarbleFireplace.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GreenMarbleFireplaceAddon : BaseAddon
{
    public GreenMarbleFireplaceAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.South:
                AddComponent(new LocalizedAddonComponent(0x9A5C, 1021117), -1, 0, 0);
                AddComponent(new LocalizedAddonComponent(0x9A5B, 1021117), 0, 0, 0);
                AddComponent(new LocalizedAddonComponent(0x9A5A, 1021117), 1, 0, 0);
                break;
            case DirectionType.East:
                AddComponent(new LocalizedAddonComponent(0x9A59, 1021117), 0, -1, 0);
                AddComponent(new LocalizedAddonComponent(0x9A58, 1021117), 0, 0, 0);
                AddComponent(new LocalizedAddonComponent(0x9A57, 1021117), 0, 1, 0);
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
                    switch(x.ItemID)
                    {
                        case 0x9A5B:
                            {
                                x.ItemID = 0x9A7B;
                                break;
                            }
                        case 0x9A7B:
                            {
                                x.ItemID = 0x9A5B;
                                break;
                            }
                        case 0x9A58:
                            {
                                x.ItemID = 0x9A75;
                                break;
                            }
                        case 0x9A75:
                            {
                                x.ItemID = 0x9A58;
                                break;
                            }
                    }
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

    public override BaseAddonDeed Deed => new GreenMarbleFireplaceDeed();
}

[SerializationGenerator(0, false)]
public partial class GreenMarbleFireplaceDeed : BaseAddonDeed, IRewardOption
{
    public override int LabelNumber => 1155698; // Green Marble Fireplace

    public override BaseAddon Addon => new GreenMarbleFireplaceAddon(_Direction);

    private DirectionType _Direction;

    [Constructible]
    public GreenMarbleFireplaceDeed()
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
