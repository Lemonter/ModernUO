using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

public enum JusticeTileType
{
    North = 0,
    West = 1
}

[SerializationGenerator(0, false)]
public partial class JusticeVirtueTileAddon : BaseAddon
{
    [Constructible]
    public JusticeVirtueTileAddon() : this(JusticeTileType.West)
    {
    }

    [Constructible]
    public JusticeVirtueTileAddon(JusticeTileType type)
    {
        var offset = type == JusticeTileType.North ? 4 : 0;

        AddComponent(new AddonComponent(5295 + offset), 0, 0, 0);
        AddComponent(new AddonComponent(5296 + offset), 0, 1, 0);
        AddComponent(new AddonComponent(5297 + offset), 1, 1, 0);
        AddComponent(new AddonComponent(5298 + offset), 1, 0, 0);
    }

    public override BaseAddonDeed Deed => new JusticeVirtueTileDeed();
}

[SerializationGenerator(0, false)]
public partial class JusticeVirtueTileDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private JusticeTileType _tileType;

    [Constructible]
    public JusticeVirtueTileDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new JusticeVirtueTileAddon(_tileType);

    public override int LabelNumber => 1080487; // Justice Virtue Tile Deed

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
        list.Add((int)JusticeTileType.North, 1080221);
        list.Add((int)JusticeTileType.West, 1080220);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _tileType = (JusticeTileType)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }
}
