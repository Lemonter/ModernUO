using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

public enum ValorTileType
{
    North = 0,
    West = 1
}

[SerializationGenerator(0, false)]
public partial class ValorVirtueTileAddon : BaseAddon
{
    [Constructible]
    public ValorVirtueTileAddon() : this(ValorTileType.West)
    {
    }

    [Constructible]
    public ValorVirtueTileAddon(ValorTileType type)
    {
        var offset = type == ValorTileType.North ? 4 : 0;

        AddComponent(new AddonComponent(5303 + offset), 0, 0, 0);
        AddComponent(new AddonComponent(5304 + offset), 0, 1, 0);
        AddComponent(new AddonComponent(5305 + offset), 1, 1, 0);
        AddComponent(new AddonComponent(5306 + offset), 1, 0, 0);
    }

    public override BaseAddonDeed Deed => new ValorVirtueTileDeed();
}

[SerializationGenerator(0, false)]
public partial class ValorVirtueTileDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private ValorTileType _tileType;

    [Constructible]
    public ValorVirtueTileDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new ValorVirtueTileAddon(_tileType);

    public override int LabelNumber => 1080486; // Valor Virtue Tile Deed

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
        list.Add((int)ValorTileType.North, 1080224);
        list.Add((int)ValorTileType.West, 1080223);
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _tileType = (ValorTileType)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }
}
