using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

public enum SacrificeTileType
{
    North = 0,
    West = 1
}

[SerializationGenerator(0, false)]
public partial class SacrificeVirtueTileAddon : BaseAddon
{
    [Constructible]
    public SacrificeVirtueTileAddon() : this(SacrificeTileType.West)
    {
    }

    [Constructible]
    public SacrificeVirtueTileAddon(SacrificeTileType type)
    {
        var offset = type == SacrificeTileType.North ? 4 : 0;

        AddComponent(new AddonComponent(5386 + offset), 0, 0, 0);
        AddComponent(new AddonComponent(5387 + offset), 0, 1, 0);
        AddComponent(new AddonComponent(5388 + offset), 1, 1, 0);
        AddComponent(new AddonComponent(5389 + offset), 1, 0, 0);
    }

    public override BaseAddonDeed Deed => new SacrificeVirtueTileDeed();
}

[SerializationGenerator(0, false)]
public partial class SacrificeVirtueTileDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private SacrificeTileType _tileType;

    [Constructible]
    public SacrificeVirtueTileDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new SacrificeVirtueTileAddon(_tileType);

    public override int LabelNumber => 1080482; // Sacrifice Virtue Tile Deed

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
        list.Add((int)SacrificeTileType.North, 1080236);
        list.Add((int)SacrificeTileType.West, 1080235);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _tileType = (SacrificeTileType)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }
}
