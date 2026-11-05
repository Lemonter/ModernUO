/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Multis/TokunoGalleon.cs).
///
///     Токунская галера: корпус 0x30–0x33, палуба на высоте 7, кормчий чуть позади центра
///     (<c>TillerManDistance</c> −1). Повреждения корпуса — этап 2: ступени арта сдвигают ID на
///     <c>DamageValue * 4</c> (0x30 → 0x34 → 0x38).
///
///     Орудийная палуба — этап 3 (<c>GalleonCannons.cs</c>): пять орудийных мест, меньше всех.
///
///     Трюм — этап 4: 16000 стоунов, пять стенок трюма и лаз в него.
/// </summary>
using ModernUO.Serialization;
using Server.Multis;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class TokunoGalleon : BaseGalleon
{
    [Constructible]
    public TokunoGalleon(Direction facing = Direction.North) : base(facing)
    {
    }

    public override int NorthID => 0x30 + DamageValue * 4;
    public override int EastID => 0x31 + DamageValue * 4;
    public override int SouthID => 0x32 + DamageValue * 4;
    public override int WestID => 0x33 + DamageValue * 4;

    public override int HoldDistance => -5;
    public override int TillerManDistance => -1;

    public override int MaxHits => 100000;

    public override int ZSurface => 7;

    public override int MaxCannons => 5;

    public override int[][] HoldIDs => _holdIDs;

    public override int[][] HoldItemIDs => _holdItemIDs;

    private static readonly int[][] _holdIDs =
    [
        [36963, 36970, 36968, 36961, 37964], // South
        [37098, 37105, 37103, 37096, 37097], // West
        [37233, 37240, 37238, 37231, 37232], // North
        [37368, 37375, 37373, 37366, 37367]  // East
    ];

    private static readonly int[][] _holdItemIDs =
    [
        [36969], // South
        [37104], // West
        [37239], // North
        [37374]  // East
    ];

    public override int[][] CannonTileIDs => _cannonTileIDs;

    private static readonly int[][] _cannonTileIDs =
    [
        [37054, 37020, 37016, 36985, 36981], // South
        [37189, 37155, 37151, 37120, 37116], // West
        [37324, 37290, 37286, 37255, 37251], // North
        [37459, 37425, 37421, 37390, 37386]  // East
    ];

    public override BaseDockedBoat DockedBoat => new DockedTokunoGalleon(this);
}

[SerializationGenerator(0, false)]
public partial class TokunoGalleonDeed : BaseGalleonDeed
{
    [Constructible]
    public TokunoGalleonDeed() : base(0x30, Point3D.Zero)
    {
    }

    public override int LabelNumber => 1116740; // Tokuno Galleon Deed

    public override BaseGalleon CreateGalleon(Direction facing) => new TokunoGalleon(facing);
}

[SerializationGenerator(0, false)]
public partial class DockedTokunoGalleon : BaseDockedGalleon
{
    public DockedTokunoGalleon(BaseBoat boat) : base(0x30, boat)
    {
    }

    public override int LabelNumber => 1116749; // Tokuno Ship

    public override BaseGalleon CreateGalleon(Direction facing) => new TokunoGalleon(facing);
}
