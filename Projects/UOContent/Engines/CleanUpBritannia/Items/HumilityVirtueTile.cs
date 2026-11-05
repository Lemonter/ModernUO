using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

public enum HumilityTileType
{
    North = 0,
    West = 1
}

[SerializationGenerator(0, false)]
public partial class HumilityVirtueTileAddon : BaseAddon
{
    [Constructible]
    public HumilityVirtueTileAddon() : this(HumilityTileType.West)
    {
    }

    [Constructible]
    public HumilityVirtueTileAddon(HumilityTileType type)
    {
        var offset = type == HumilityTileType.North ? 4 : 0;

        AddComponent(new AddonComponent(5327 + offset), 0, 0, 0);
        AddComponent(new AddonComponent(5328 + offset), 0, 1, 0);
        AddComponent(new AddonComponent(5329 + offset), 1, 1, 0);
        AddComponent(new AddonComponent(5330 + offset), 1, 0, 0);
    }

    public override BaseAddonDeed Deed => new HumilityVirtueTileDeed();
}

[SerializationGenerator(0, false)]
public partial class HumilityVirtueTileDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private HumilityTileType _tileType;

    [Constructible]
    public HumilityVirtueTileDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new HumilityVirtueTileAddon(_tileType);

    public override int LabelNumber => 1080483; // Humility Virtue Tile Deed

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
        list.Add((int)HumilityTileType.North, 1080233);
        list.Add((int)HumilityTileType.West, 1080232);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _tileType = (HumilityTileType)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }
}
