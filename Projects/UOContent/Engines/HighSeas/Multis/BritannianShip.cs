/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Multis/BritannianShip.cs).
///
///     Британская галера: корпус 0x40–0x43 (север/восток/юг/запад), палуба на высоте 18.
///     Сухой док и постановка — штатные <c>BaseBoatDeed</c>/<c>BaseDockedBoat</c> через
///     <see cref="BaseGalleonDeed" />/<see cref="BaseDockedGalleon" />.
///
///     Повреждения корпуса — этап 2 (<c>GalleonDamage.cs</c>): ступени арта сдвигают ID на
///     <c>DamageValue * 4</c>; у британской галеры ступеней две, поэтому Heavily и Severely дают
///     один и тот же арт (0x44–0x47), как и в ServUO.
///
///     Орудийная палуба — этап 3 (<c>GalleonCannons.cs</c>): девять орудийных пятаков, полный ход
///     без замедления от пушек (<c>CannonDamageMod</c> 1.0).
///
///     Не портировано: <c>TurnDelay</c>/<c>BoatDecayDelay</c>, <c>FillerIDs</c>/<c>WheelItemIDs</c>
///     (декор палубы рисует сам мульти), <c>Contains</c> с прямоугольным ограничением и
///     <c>ExemptOverheadComponent</c> (в этом движке таких хуков у мульти нет; <c>Contains</c>
///     работает по реальному футпринту мульти).
/// </summary>
using ModernUO.Serialization;
using Server.Multis;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class BritannianShip : BaseGalleon
{
    [Constructible]
    public BritannianShip(Direction facing = Direction.North) : base(facing)
    {
    }

    public override int NorthID => 0x40 + DamageValue * 4;
    public override int EastID => 0x41 + DamageValue * 4;
    public override int SouthID => 0x42 + DamageValue * 4;
    public override int WestID => 0x43 + DamageValue * 4;

    public override int HoldDistance => 9;
    public override int TillerManDistance => 7;

    public override int MaxHits => 200000;

    public override int ZSurface => 18;

    /// <summary>Девять орудийных мест — по числу маркеров палубы в мульти.</summary>
    public override int MaxCannons => 9;

    /// <summary>Самая вместительная галера: 28000 стоунов (в ServUO — <c>GalleonHold</c>).</summary>
    public override int HoldMaxWeight => 28000;

    public override int[][] HoldIDs => _holdIDs;

    public override int[][] HoldItemIDs => _holdItemIDs;

    // Арт стенок трюма: строки — юг/запад/север/восток (в ServUO те же данные повторялись для
    // повреждённых корпусов; здесь трюм строится один раз по северному мульти).
    private static readonly int[][] _holdIDs =
    [
        [23649, 23650, 23652, 23653, 23654, 23655, 23656, 23651], // South
        [23703, 23704, 23706, 23707, 23708, 23709, 23710, 23705], // West
        [23595, 23596, 23598, 23599, 23600, 23601, 23602, 23597], // North
        [23541, 23542, 23544, 23545, 23546, 23547, 23548, 23543]  // East
    ];

    /// <summary>Арт самого трюма (лаз в трюм), по одному тайлу на борт.</summary>
    private static readonly int[][] _holdItemIDs =
    [
        [23648], // South
        [23702], // West
        [23594], // North
        [23540]  // East
    ];

    public override int[][] CannonTileIDs => _cannonTileIDs;

    // Строки — по направлению корпуса (юг/запад/север/восток); в ServUO те же данные повторялись
    // ещё раз для повреждённых корпусов, но пятаки создаются один раз по северному мульти.
    private static readonly int[][] _cannonTileIDs =
    [
        [23612, 23631, 23630, 23632, 23634, 23639, 23637, 23643, 23641], // South
        [23666, 23684, 23685, 23686, 23688, 23691, 23693, 23695, 23697], // West
        [23558, 23576, 23577, 23580, 23578, 23583, 23585, 23587, 23589], // North
        [23504, 23522, 23523, 23526, 23524, 23529, 23531, 23533, 23535]  // East
    ];

    /// <summary>У британской галеры всего две ступени: Heavily и Severely выглядят одинаково.</summary>
    public override int DamageValue => DamageTaken switch
    {
        DamageLevel.Pristine or DamageLevel.Slightly or DamageLevel.Moderately => 0,
        _ => 1
    };

    public override BaseDockedBoat DockedBoat => new DockedBritannianShip(this);
}

[SerializationGenerator(0, false)]
public partial class BritannianShipDeed : BaseGalleonDeed
{
    [Constructible]
    public BritannianShipDeed() : base(0x40, Point3D.Zero)
    {
    }

    public override int LabelNumber => 1150017; // Britannian Ship Deed

    public override BaseGalleon CreateGalleon(Direction facing) => new BritannianShip(facing);
}

[SerializationGenerator(0, false)]
public partial class DockedBritannianShip : BaseDockedGalleon
{
    public DockedBritannianShip(BaseBoat boat) : base(0x40, boat)
    {
    }

    public override int LabelNumber => 1150100; // Britannian Ship

    public override BaseGalleon CreateGalleon(Direction facing) => new BritannianShip(facing);
}
