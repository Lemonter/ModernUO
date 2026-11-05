using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MetalTubAddon : BaseAddon, IWaterSource
{
    [Constructible]
    public MetalTubAddon(DirectionType type)
    {
        switch (type)
        {
            case DirectionType.South:
                {
                    AddComponent(new LocalizedAddonComponent(0xA4E0, 1126234), 0, 0, 0);
                    break;
                }
            case DirectionType.East:
                {
                    AddComponent(new LocalizedAddonComponent(0xA4E2, 1126234), 0, 0, 0);
                    break;
                }
        }
    }

    public override BaseAddonDeed Deed => new MetalTubDeed();

    int IHasQuantity.Quantity
    {
        get => 500;
        set { }
    }
}

[SerializationGenerator(0, false)]
public partial class MetalTubDeed : BaseAddonDeed, IRewardOption
{
    private DirectionType _direction;

    [Constructible]
    public MetalTubDeed()
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1126234; // metal tub

    public override BaseAddon Addon => new MetalTubAddon(_direction);

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)DirectionType.South, 1075386); // South
        list.Add((int)DirectionType.East, 1075387); // East
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _direction = (DirectionType)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
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
