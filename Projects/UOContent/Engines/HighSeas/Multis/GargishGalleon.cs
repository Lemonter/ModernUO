/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Multis/GargishGalleon.cs).
///
///     Гаргойльская галера: корпус 0x24–0x27, палуба на высоте 16, кормчий ближе к центру
///     (<c>TillerManDistance</c> 1). Повреждения корпуса — этап 2: ступени арта сдвигают ID на
///     <c>DamageValue * 4</c> (0x24 → 0x28 → 0x2C).
///
///     Орудийная палуба — этап 3 (<c>GalleonCannons.cs</c>): семь орудийных мест.
///
///     Трюм — этап 4: 12000 стоунов (самый тесный), восемь стенок трюма и лаз в него.
/// </summary>
using ModernUO.Serialization;
using Server.Multis;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class GargishGalleon : BaseGalleon
{
    [Constructible]
    public GargishGalleon(Direction facing = Direction.North) : base(facing)
    {
    }

    public override int NorthID => 0x24 + DamageValue * 4;
    public override int EastID => 0x25 + DamageValue * 4;
    public override int SouthID => 0x26 + DamageValue * 4;
    public override int WestID => 0x27 + DamageValue * 4;

    public override int HoldDistance => 6;
    public override int TillerManDistance => 1;

    public override int MaxHits => 140000;

    public override int ZSurface => 16;

    public override int MaxCannons => 7;

    /// <summary>Самый тесный трюм: 12000 стоунов (в ServUO — <c>GalleonHold</c>).</summary>
    public override int HoldMaxWeight => 12000;

    public override int[][] HoldIDs => _holdIDs;

    public override int[][] HoldItemIDs => _holdItemIDs;

    private static readonly int[][] _holdIDs =
    [
        [33408, 33409, 33414, 33394, 33395, 33400, 33401, 33402], // South
        [33713, 33714, 33719, 33699, 33700, 33705, 33706, 33707], // West
        [34015, 34016, 34021, 34001, 34002, 34007, 34008, 34009], // North
        [34318, 34319, 34324, 34304, 34305, 34310, 34311, 34312]  // East
    ];

    private static readonly int[][] _holdItemIDs =
    [
        [33387], // South
        [33692], // West
        [33994], // North
        [34297]  // East
    ];

    public override int[][] CannonTileIDs => _cannonTileIDs;

    private static readonly int[][] _cannonTileIDs =
    [
        [33463, 33440, 33438, 33327, 33322, 33357, 33355], // South
        [33768, 33745, 33743, 33632, 33627, 33662, 33660], // West
        [34070, 34047, 34045, 33934, 33929, 33964, 33962], // North
        [34373, 34350, 34348, 34237, 34232, 34267, 34265]  // East
    ];

    public override BaseDockedBoat DockedBoat => new DockedGargishGalleon(this);
}

[SerializationGenerator(0, false)]
public partial class GargishGalleonDeed : BaseGalleonDeed
{
    [Constructible]
    public GargishGalleonDeed() : base(0x24, Point3D.Zero)
    {
    }

    public override int LabelNumber => 1116739; // Gargish Galleon Deed

    public override BaseGalleon CreateGalleon(Direction facing) => new GargishGalleon(facing);
}

[SerializationGenerator(0, false)]
public partial class DockedGargishGalleon : BaseDockedGalleon
{
    public DockedGargishGalleon(BaseBoat boat) : base(0x24, boat)
    {
    }

    public override int LabelNumber => 1116748; // Gargoyle Ship

    public override BaseGalleon CreateGalleon(Direction facing) => new GargishGalleon(facing);
}
