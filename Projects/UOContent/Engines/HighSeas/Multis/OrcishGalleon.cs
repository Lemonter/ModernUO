/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Multis/OrcishGalleon.cs).
///
///     Орочья галера: корпус 0x18–0x1B, палуба на высоте 14. Повреждения корпуса — этап 2:
///     ступени арта сдвигают ID на <c>DamageValue * 4</c> (0x18 → 0x1C → 0x20).
///
///     Орудийная палуба — этап 3 (<c>GalleonCannons.cs</c>): семь орудийных мест и единственный
///     корпус с бонусом к урону пушек (<c>CannonDamageMod</c> 1.3, как в ServUO).
///
///     Трюм — этап 4: 14000 стоунов, пять стенок трюма и лаз в него.
/// </summary>
using ModernUO.Serialization;
using Server.Multis;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class OrcishGalleon : BaseGalleon
{
    [Constructible]
    public OrcishGalleon(Direction facing = Direction.North) : base(facing)
    {
    }

    public override int NorthID => 0x18 + DamageValue * 4;
    public override int EastID => 0x19 + DamageValue * 4;
    public override int SouthID => 0x1A + DamageValue * 4;
    public override int WestID => 0x1B + DamageValue * 4;

    public override int HoldDistance => 9;
    public override int TillerManDistance => 6;

    public override int MaxHits => 100000;

    public override int ZSurface => 14;

    public override int MaxCannons => 7;

    /// <summary>Трюм на 14000 стоунов (в ServUO — <c>GalleonHold</c>).</summary>
    public override int HoldMaxWeight => 14000;

    public override int[][] HoldIDs => _holdIDs;

    public override int[][] HoldItemIDs => _holdItemIDs;

    private static readonly int[][] _holdIDs =
    [
        [30127, 30124, 30122, 30115, 30120], // South
        [30627, 30624, 30622, 30615, 30620], // West
        [31127, 31124, 31122, 31115, 31120], // North
        [31627, 31624, 31622, 31615, 31620]  // East
    ];

    private static readonly int[][] _holdItemIDs =
    [
        [30117], // South
        [30617], // West
        [31117], // North
        [31617]  // East
    ];

    /// <summary>Орочьи пушки бьют на 30% сильнее (в ServUO — <c>CannonDamageMod</c> 1.3).</summary>
    public override double CannonDamageMod => 1.3;

    public override int[][] CannonTileIDs => _cannonTileIDs;

    private static readonly int[][] _cannonTileIDs =
    [
        [30012, 30037, 30041, 30065, 30069, 30093, 30097], // South
        [30512, 30537, 30541, 30565, 30569, 30593, 30597], // West
        [31012, 31037, 31041, 31065, 31069, 31093, 31097], // North
        [31512, 31537, 31541, 31565, 31569, 31593, 31597]  // East
    ];

    public override BaseDockedBoat DockedBoat => new DockedOrcishGalleon(this);
}

[SerializationGenerator(0, false)]
public partial class OrcishGalleonDeed : BaseGalleonDeed
{
    [Constructible]
    public OrcishGalleonDeed() : base(0x18, Point3D.Zero)
    {
    }

    public override int LabelNumber => 1116738; // Orcish Galleon Deed

    public override BaseGalleon CreateGalleon(Direction facing) => new OrcishGalleon(facing);
}

[SerializationGenerator(0, false)]
public partial class DockedOrcishGalleon : BaseDockedGalleon
{
    public DockedOrcishGalleon(BaseBoat boat) : base(0x18, boat)
    {
    }

    public override int LabelNumber => 1116747; // Orc Ship

    public override BaseGalleon CreateGalleon(Direction facing) => new OrcishGalleon(facing);
}
