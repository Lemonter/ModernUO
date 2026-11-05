using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

public enum HorseBardingType
{
    South = 0,
    East = 1
}

[SerializationGenerator(0, false)]
public partial class HorseBardingAddon : BaseAddon
{
    [Constructible]
    public HorseBardingAddon() : this(HorseBardingType.South)
    {
    }

    [Constructible]
    public HorseBardingAddon(HorseBardingType type)
    {
        switch (type)
        {
            case HorseBardingType.East:
                {
                    AddComponent(new AddonComponent(4983), 0, 0, 0);
                    AddComponent(new AddonComponent(4982), -1, 0, 0);
                    break;
                }
            default:
                {
                    AddComponent(new AddonComponent(4984), 0, 0, 0);
                    AddComponent(new AddonComponent(4985), 0, -1, 0);
                    break;
                }
        }
    }

    public override BaseAddonDeed Deed => new HorseBardingDeed();
}

[SerializationGenerator(0, false)]
public partial class HorseBardingDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private HorseBardingType _bardingType;

    [Constructible]
    public HorseBardingDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new HorseBardingAddon(_bardingType);

    public override int LabelNumber => 1080212; // Horse Barding

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            return;
        }

        RewardOptionGump.DisplayTo(from, this);
    }

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)HorseBardingType.South, 1080211);
        list.Add((int)HorseBardingType.East, 1080210);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _bardingType = (HorseBardingType)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }
}
