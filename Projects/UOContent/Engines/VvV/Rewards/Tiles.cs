using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Items/Rewards/Tiles/*.cs)
// — 8 near-identical dungeon-entrance teleport tile decorations (an addon + its deed, each a
// pair), one class pair per dungeon matching ServUO's own per-file convention there.
// Simplification: ServUO's North/East orientation choice (an `IRewardOption` gump asking which
// way the tile should face) is dropped — this codebase's real `RewardOptionGump` only accepts
// localized cliloc numbers for option labels, not arbitrary strings like "Covetous (North)",
// and no matching clilocs were available. Placing always uses North orientation.
[SerializationGenerator(0, false)]
public abstract partial class BaseVvVTileAddon : BaseAddon
{
    protected abstract int BaseArtId { get; }

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TileType _tileType;

    [Constructible]
    protected BaseVvVTileAddon(TileType type)
    {
        _tileType = type;

        var offset = type == TileType.North ? 0 : 4;

        AddComponent(new AddonComponent(BaseArtId + offset), 0, 0, 0);
        AddComponent(new AddonComponent(BaseArtId + 1 + offset), 1, 0, 0);
        AddComponent(new AddonComponent(BaseArtId + 2 + offset), 0, 1, 0);
        AddComponent(new AddonComponent(BaseArtId + 3 + offset), 1, 1, 0);
    }
}

[SerializationGenerator(0, false)]
public abstract partial class BaseVvVTileDeed : BaseAddonDeed
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TileType _tileType = TileType.North;

    protected BaseVvVTileDeed() => LootType = LootType.Blessed;
}

public enum TileType
{
    North,
    West
}

[SerializationGenerator(0, false)]
public partial class CovetousTileAddon : BaseVvVTileAddon
{
    protected override int BaseArtId => 39372;
    public override BaseAddonDeed Deed => new CovetousTileDeed();

    [Constructible]
    public CovetousTileAddon(TileType type) : base(type)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class CovetousTileDeed : BaseVvVTileDeed
{
    public override BaseAddon Addon => new CovetousTileAddon(TileType);
    public override int LabelNumber => 1155516; // Covetous Tile
    [Constructible]
    public CovetousTileDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DeceitTileAddon : BaseVvVTileAddon
{
    protected override int BaseArtId => 39380;
    public override BaseAddonDeed Deed => new DeceitTileDeed();

    [Constructible]
    public DeceitTileAddon(TileType type) : base(type)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DeceitTileDeed : BaseVvVTileDeed
{
    public override BaseAddon Addon => new DeceitTileAddon(TileType);
    public override int LabelNumber => 1155517; // Deceit Tile
    [Constructible]
    public DeceitTileDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DespiseTileAddon : BaseVvVTileAddon
{
    protected override int BaseArtId => 39388;
    public override BaseAddonDeed Deed => new DespiseTileDeed();

    [Constructible]
    public DespiseTileAddon(TileType type) : base(type)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DespiseTileDeed : BaseVvVTileDeed
{
    public override BaseAddon Addon => new DespiseTileAddon(TileType);
    public override int LabelNumber => 1155518; // Despise Tile
    [Constructible]
    public DespiseTileDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DestardTileAddon : BaseVvVTileAddon
{
    protected override int BaseArtId => 39396;
    public override BaseAddonDeed Deed => new DestardTileDeed();

    [Constructible]
    public DestardTileAddon(TileType type) : base(type)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DestardTileDeed : BaseVvVTileDeed
{
    public override BaseAddon Addon => new DestardTileAddon(TileType);
    public override int LabelNumber => 1155519; // Destard Tile
    [Constructible]
    public DestardTileDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class HythlothTileAddon : BaseVvVTileAddon
{
    protected override int BaseArtId => 39404;
    public override BaseAddonDeed Deed => new HythlothTileDeed();

    [Constructible]
    public HythlothTileAddon(TileType type) : base(type)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class HythlothTileDeed : BaseVvVTileDeed
{
    public override BaseAddon Addon => new HythlothTileAddon(TileType);
    public override int LabelNumber => 1155520; // Hythloth Tile
    [Constructible]
    public HythlothTileDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class PrideTileAddon : BaseVvVTileAddon
{
    protected override int BaseArtId => 39412;
    public override BaseAddonDeed Deed => new PrideTileDeed();

    [Constructible]
    public PrideTileAddon(TileType type) : base(type)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class PrideTileDeed : BaseVvVTileDeed
{
    public override BaseAddon Addon => new PrideTileAddon(TileType);
    public override int LabelNumber => 1155521; // Pride Tile
    [Constructible]
    public PrideTileDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ShameTileAddon : BaseVvVTileAddon
{
    protected override int BaseArtId => 39420;
    public override BaseAddonDeed Deed => new ShameTileDeed();

    [Constructible]
    public ShameTileAddon(TileType type) : base(type)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ShameTileDeed : BaseVvVTileDeed
{
    public override BaseAddon Addon => new ShameTileAddon(TileType);
    public override int LabelNumber => 1155522; // Shame Tile
    [Constructible]
    public ShameTileDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class WrongTileAddon : BaseVvVTileAddon
{
    protected override int BaseArtId => 39428;
    public override BaseAddonDeed Deed => new WrongTileDeed();

    [Constructible]
    public WrongTileAddon(TileType type) : base(type)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class WrongTileDeed : BaseVvVTileDeed
{
    public override BaseAddon Addon => new WrongTileAddon(TileType);
    public override int LabelNumber => 1155523; // Wrong Tile
    [Constructible]
    public WrongTileDeed()
    {
    }
}
