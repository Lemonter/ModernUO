using ModernUO.Serialization;
using Server.Multis;

namespace Server.Items;

public enum MahaonFenceKind
{
    WallX, // runs east-west (blocks north-south movement) — same graphic as ThinStoneWall's SouthWall
    WallY, // runs north-south (blocks east-west movement) — same graphic as ThinStoneWall's EastWall
    Post   // corner / junction piece — same graphic as ThinStoneWall's CornerPost
}

/// <summary>
///     One segment of a house's fence (Systems.MahaonWorld.MahaonHouseFenceSystem) — reuses
///     ThinStoneWall's already-verified, already-Impassable-flagged graphics (real game
///     content, not a guessed ID) instead of inventing a new tileset. Blocking comes for
///     free from those graphics' TileData flags, same as every other BaseWall-derived item
///     in the game — no custom movement-blocking code needed here.
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

    private static int GraphicFor(MahaonFenceKind kind) => kind switch
    {
        MahaonFenceKind.WallX => 0x001A + (int)ThinStoneWallTypes.SouthWall,
        MahaonFenceKind.WallY => 0x001A + (int)ThinStoneWallTypes.EastWall,
        _                     => 0x001A + (int)ThinStoneWallTypes.CornerPost
    };

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
