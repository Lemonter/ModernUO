using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

public enum SpiritualityTileType
{
    North = 0,
    West = 1
}

[SerializationGenerator(0, false)]
public partial class SpiritualityVirtueTileAddon : BaseAddon
{
    [Constructible]
    public SpiritualityVirtueTileAddon() : this(SpiritualityTileType.West)
    {
    }

    [Constructible]
    public SpiritualityVirtueTileAddon(SpiritualityTileType type)
    {
        var offset = type == SpiritualityTileType.North ? 4 : 0;

        AddComponent(new AddonComponent(5311 + offset), 0, 0, 0);
        AddComponent(new AddonComponent(5312 + offset), 0, 1, 0);
        AddComponent(new AddonComponent(5313 + offset), 1, 1, 0);
        AddComponent(new AddonComponent(5314 + offset), 1, 0, 0);
    }

    public override BaseAddonDeed Deed => new SpiritualityVirtueTileDeed();
}

[SerializationGenerator(0, false)]
public partial class SpiritualityVirtueTileDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private SpiritualityTileType _tileType;

    [Constructible]
    public SpiritualityVirtueTileDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new SpiritualityVirtueTileAddon(_tileType);

    public override int LabelNumber => 1080484; // Spirituality Virtue Tile Deed

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
        list.Add((int)SpiritualityTileType.North, 1080227);
        list.Add((int)SpiritualityTileType.West, 1080226);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _tileType = (SpiritualityTileType)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }
}
