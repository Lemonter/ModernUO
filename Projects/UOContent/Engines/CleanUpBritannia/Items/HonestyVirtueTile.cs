using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

public enum HonestyTileType
{
    North = 0,
    West = 1
}

[SerializationGenerator(0, false)]
public partial class HonestyVirtueTileAddon : BaseAddon
{
    [Constructible]
    public HonestyVirtueTileAddon() : this(HonestyTileType.West)
    {
    }

    [Constructible]
    public HonestyVirtueTileAddon(HonestyTileType type)
    {
        var offset = type == HonestyTileType.North ? 4 : 0;

        AddComponent(new AddonComponent(5279 + offset), 0, 0, 0);
        AddComponent(new AddonComponent(5280 + offset), 0, 1, 0);
        AddComponent(new AddonComponent(5281 + offset), 1, 1, 0);
        AddComponent(new AddonComponent(5282 + offset), 1, 0, 0);
    }

    public override BaseAddonDeed Deed => new HonestyVirtueTileDeed();
}

[SerializationGenerator(0, false)]
public partial class HonestyVirtueTileDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private HonestyTileType _tileType;

    [Constructible]
    public HonestyVirtueTileDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new HonestyVirtueTileAddon(_tileType);

    public override int LabelNumber => 1080488; // Honesty Virtue Tile Deed

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
        list.Add((int)HonestyTileType.North, 1080215);
        list.Add((int)HonestyTileType.West, 1080214);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _tileType = (HonestyTileType)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }
}
