/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Multis/*.cs — общая часть дея и
///     сухого дока галер).
///
///     В ServUO у <c>BaseBoatDeed</c>/<c>BaseDockedBoat</c> было свойство <c>BoatDirection</c>, поэтому
///     галера ставилась в любом из четырёх направлений. В этом движке таких свойств нет, а менять
///     <c>BaseBoatDeed</c>/<c>BaseDockedBoat</c> нельзя, поэтому направление хранится здесь:
///     <see cref="BaseGalleonDeed.Facing" /> выбирается в <see cref="ShipPlacementGump" />, а
///     <see cref="BaseDockedGalleon" /> запоминает направление при постановке в сухой док (иначе
///     вытащенная из дока галера всегда разворачивалась бы на север).
///
///     Постановка/снятие при этом целиком на штатном коде: <c>BaseBoatDeed.OnPlacement</c> и
///     <c>BaseDockedBoat.OnPlacement</c> — они лишь спрашивают <c>Boat</c>, а он создаёт галеру с
///     нужным <c>Facing</c> (как <c>LargeBoat</c>/<c>MediumBoat</c> в этом репозитории).
/// </summary>
using ModernUO.Serialization;
using Server.Gumps;
using Server.Multis;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public abstract partial class BaseGalleonDeed : BaseBoatDeed
{
    [SerializableField(0)]
    private Direction _facing;

    protected BaseGalleonDeed(int id, Point3D offset) : base(id, offset) => _facing = Direction.North;

    public abstract BaseGalleon CreateGalleon(Direction facing);

    public override BaseBoat Boat => CreateGalleon(_facing);

    /// <summary>ID мульти для курсора прицела с учётом выбранного направления.</summary>
    public int GetMultiId(Direction facing) => MultiId + (int)facing / 2;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (from.AccessLevel < AccessLevel.GameMaster && (from.Map == Map.Ilshenar || from.Map == Map.Malas))
        {
            from.SendLocalizedMessage(1010567, null, 0x25); // You may not place a boat from this location.
            return;
        }

        ShipPlacementGump.DisplayTo(from, this);
    }
}

[SerializationGenerator(0, false)]
public abstract partial class BaseDockedGalleon : BaseDockedBoat
{
    [SerializableField(0)]
    private Direction _facing;

    protected BaseDockedGalleon(int id, BaseBoat boat) : base(id, Point3D.Zero, boat)
    {
        _facing = boat?.Facing ?? Direction.North;

        // Превью прицела берёт MultiId, поэтому храним уже повёрнутый ID мульти.
        MultiId = id + (int)_facing / 2;
    }

    public abstract BaseGalleon CreateGalleon(Direction facing);

    public override BaseBoat Boat => CreateGalleon(_facing);
}
