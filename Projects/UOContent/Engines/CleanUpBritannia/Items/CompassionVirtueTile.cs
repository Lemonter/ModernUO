using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

public enum CompassionTileType
{
    North = 0,
    West = 1
}

[SerializationGenerator(0, false)]
public partial class CompassionVirtueTileAddon : BaseAddon
{
    [Constructible]
    public CompassionVirtueTileAddon() : this(CompassionTileType.West)
    {
    }

    [Constructible]
    public CompassionVirtueTileAddon(CompassionTileType type)
    {
        var offset = type == CompassionTileType.North ? 4 : 0;

        AddComponent(new AddonComponent(5287 + offset), 0, 0, 0);
        AddComponent(new AddonComponent(5288 + offset), 0, 1, 0);
        AddComponent(new AddonComponent(5289 + offset), 1, 1, 0);
        AddComponent(new AddonComponent(5290 + offset), 1, 0, 0);
    }

    public override BaseAddonDeed Deed => new CompassionVirtueTileDeed();
}

[SerializationGenerator(0, false)]
public partial class CompassionVirtueTileDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private CompassionTileType _tileType;

    [Constructible]
    public CompassionVirtueTileDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new CompassionVirtueTileAddon(_tileType);

    public override int LabelNumber => 1080481; // Compassion Virtue Tile Deed

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
        list.Add((int)CompassionTileType.North, 1080218);
        list.Add((int)CompassionTileType.West, 1080217);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _tileType = (CompassionTileType)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }
}
