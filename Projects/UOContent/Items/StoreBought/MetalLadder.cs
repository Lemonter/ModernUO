using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

public enum MetalLadderType
{
    SouthCastle = 0,
    EastCastle = 1,
    NorthCastle = 2,
    WestCastle = 3,
    South = 4,
    East = 5,
    North = 6,
    West = 7
}

[SerializationGenerator(0, false)]
public partial class MetalLadderAddon : BaseAddon, IDyable
{
    [Constructible]
    public MetalLadderAddon(MetalLadderType type = MetalLadderType.SouthCastle)
    {
        switch (type)
        {
            case MetalLadderType.SouthCastle:
                AddComponent(new LocalizedAddonComponent(0xA559, 1076791), 0, 2, 20);
                AddComponent(new AddonComponent(0xA557), 0, 0, 0);
                break;
            case MetalLadderType.EastCastle:
                AddComponent(new LocalizedAddonComponent(0xA55A, 1076791), 2, 0, 20);
                AddComponent(new AddonComponent(0xA558), 0, 0, 0);
                break;
            case MetalLadderType.NorthCastle:
                AddComponent(new LocalizedAddonComponent(0xA55C, 1154133), 0, 0, 0);
                break;
            case MetalLadderType.WestCastle:
                AddComponent(new LocalizedAddonComponent(0xA55B, 1154133), 0, 0, 0);
                break;
            case MetalLadderType.South:
                AddComponent(new LocalizedAddonComponent(0xA557, 1154133), 0, 0, 0);
                break;
            case MetalLadderType.East:
                AddComponent(new LocalizedAddonComponent(0xA558, 1154133), 0, 0, 0);
                break;
            case MetalLadderType.North:
                AddComponent(new LocalizedAddonComponent(0xA55C, 1154133), 0, 0, 0);
                break;
            case MetalLadderType.West:
                AddComponent(new LocalizedAddonComponent(0xA55B, 1154133), 0, 0, 0);
                break;
        }
    }

    public override BaseAddonDeed Deed => new MetalLadderDeed();

    public virtual bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;
        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class MetalLadderDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private MetalLadderType _direction;

    [Constructible]
    public MetalLadderDeed()
    {
    }

    public override BaseAddon Addon => new MetalLadderAddon(_direction);
    public override int LabelNumber => 1159445; // metal ladder

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)MetalLadderType.SouthCastle, 1076794);
        list.Add((int)MetalLadderType.EastCastle, 1076795);
        list.Add((int)MetalLadderType.NorthCastle, 1076792);
        list.Add((int)MetalLadderType.WestCastle, 1076793);
        list.Add((int)MetalLadderType.South, 1075386);
        list.Add((int)MetalLadderType.East, 1075387);
        list.Add((int)MetalLadderType.North, 1075389);
        list.Add((int)MetalLadderType.West, 1075390);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _direction = (MetalLadderType)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            return;
        }

        // AddonOptionGump в этом репозитории нет; заголовок «Choose a Facing:» передан напрямую.
        RewardOptionGump.DisplayTo(from, this, 1154194);
    }
}
