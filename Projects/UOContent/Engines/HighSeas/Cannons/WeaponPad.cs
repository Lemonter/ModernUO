/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Multis/GalleonItem.cs — класс
///     <c>WeaponPad</c>).
///
///     «Орудийный пятак» — площадка на палубе галеры, на которую ставится пушка. В ServUO такие
///     площадки создавались из компонентов мульти с флагом <c>TileFlag.None</c> (это маркеры, клиент
///     их не рисует) и складывались в общий список фикстур корабля.
///
///     Здесь сделан только сам пятак: общий список фикстур (трюм, штурвал, деки) — этап 4, поэтому
///     <see cref="BaseGalleon" /> ведёт свой список <c>WeaponPads</c>. Смещение от центра корабля
///     хранится в «северной» системе координат, как и у пушек: тогда поворот корабля не ломает
///     привязку (см. <c>BaseGalleon.GetPadLocation</c>).
/// </summary>
using ModernUO.Serialization;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class WeaponPad : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _padOffset;

    [SerializableField(1, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BaseGalleon _galleon;

    /// <summary>Пустой конструктор — только ради <c>[add</c>; настоящие пятаки создаёт корабль.</summary>
    [Constructible]
    public WeaponPad() : this(0, null, Point3D.Zero)
    {
    }

    public WeaponPad(int itemID, BaseGalleon galleon, Point3D padOffset) : base(itemID)
    {
        Movable = false;
        _galleon = galleon;
        _padOffset = padOffset;
    }

    public override int LabelNumber => 1102376; // weapon pad

    /// <summary>Стоит ли на пятаке пушка.</summary>
    public bool Occupied => _galleon?.IsCannonAt(X, Y) == true;

    public override void OnDoubleClick(Mobile from)
    {
        if (from == null || _galleon?.Deleted != false)
        {
            return;
        }

        if (_galleon.IsCannonAt(X, Y))
        {
            from.SendMessage(0x22, "На этом пятаке уже стоит пушка.");
            return;
        }

        from.SendMessage(0x22, "На этот орудийный пятак ставится пушка — примените дей пушки.");
    }
}
