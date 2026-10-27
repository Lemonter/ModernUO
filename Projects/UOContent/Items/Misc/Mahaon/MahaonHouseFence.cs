using ModernUO.Serialization;
using Server.Multis;

namespace Server.Items;

/// <summary>
///     Which iron-fence piece a ring tile gets. UO fence art stands on the tile's SOUTH or EAST
///     edge, so a straight piece joins a tile to its west (WallX) or north (WallY) neighbour, and
///     a tile joined both ways takes the V-shaped corner — exactly how the iron fences on the
///     original Felucca map are laid (V only at south-east corners, the north-west corner tile
///     left empty, the north-east one WallX only, the south-west one WallY only).
/// </summary>
public enum MahaonFenceKind
{
    WallX,  // runs east-west — iron fence 0x0823
    WallY,  // runs north-south — iron fence 0x0821
    Corner  // joins a westward and a northward run — iron fence corner 0x0822
}

/// <summary>
///     One segment of a house's fence (Systems.MahaonWorld.MahaonHouseFenceSystem): the tall
///     wrought-iron fence of the original map (0x0821-0x0823, the set the IronGate belongs to)
///     — real game art, already Impassable in TileData, so blocking comes for free like every
///     other wall item. Until October 2026 the fence was built from ThinStoneWall pieces with a
///     narrow post in every corner, which left gaps at three corners out of four;
///     MahaonHouseFenceSystem.Initialize rebuilds such fences on start.
///
///     Carries its own OwnerHouse + Radius so MahaonHouseFenceSystem can find and remove
///     every piece belonging to a house (by scanning World.Items, same pattern as
///     GuildBank/MahaonGuildBankContainer) without needing a separate in-memory list that
///     wouldn't survive a server restart.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonHouseFence : Item
{
    [SerializableField(0, setter: "private")]
    private BaseHouse _ownerHouse;

    [SerializableField(1, setter: "private")]
    private int _radius;

    [Constructible]
    public MahaonHouseFence(MahaonFenceKind kind, BaseHouse ownerHouse, int radius) : base(GraphicFor(kind))
    {
        Movable = false;
        Name = "Ограда усадьбы";
        OwnerHouse = ownerHouse;
        Radius = radius;
    }

    public static int GraphicFor(MahaonFenceKind kind) => kind switch
    {
        MahaonFenceKind.WallX => 0x0823,
        MahaonFenceKind.WallY => 0x0821,
        _                     => 0x0822
    };

    /// <summary>A piece of the old stone fence (ThinStoneWall 0x1A-0x1D).</summary>
    public bool IsLegacy => ItemID is >= 0x001A and <= 0x001D;

    // A house deleted before fences went with their house left its fence standing; it is
    // cleared on the next load.
    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (OwnerHouse?.Deleted != false)
        {
            Timer.DelayCall(Delete);
        }
    }
}
