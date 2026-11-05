using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

public enum HonorTileType
{
    North = 0,
    West = 1
}

[SerializationGenerator(0, false)]
public partial class HonorVirtueTileAddon : BaseAddon
{
    [Constructible]
    public HonorVirtueTileAddon() : this(HonorTileType.West)
    {
    }

    [Constructible]
    public HonorVirtueTileAddon(HonorTileType type)
    {
        var offset = type == HonorTileType.North ? 4 : 0;

        AddComponent(new AddonComponent(5319 + offset), 0, 0, 0);
        AddComponent(new AddonComponent(5320 + offset), 0, 1, 0);
        AddComponent(new AddonComponent(5321 + offset), 1, 1, 0);
        AddComponent(new AddonComponent(5322 + offset), 1, 0, 0);
    }

    public override BaseAddonDeed Deed => new HonorVirtueTileDeed();
}

[SerializationGenerator(0, false)]
public partial class HonorVirtueTileDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private HonorTileType _tileType;

    [Constructible]
    public HonorVirtueTileDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new HonorVirtueTileAddon(_tileType);

    public override int LabelNumber => 1080485; // Honor Virtue Tile Deed

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
        list.Add((int)HonorTileType.North, 1080230);
        list.Add((int)HonorTileType.West, 1080229);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _tileType = (HonorTileType)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }
}
