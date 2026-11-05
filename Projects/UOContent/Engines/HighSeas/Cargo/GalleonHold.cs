/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Multis/GalleonHold.cs — классы
///     <c>GalleonHold</c> и <c>HoldItem</c>).
///
///     Настоящий трюм галеры: контейнер, который стоит на палубе и в который уходит груз. В ServUO
///     таких предметов на корабле было девять — сам контейнер (<c>HoldItem</c> в терминах таблиц
///     <c>HoldItemIDs</c>) и восемь его «стенок» (<c>HoldIDs</c>), каждая из которых по двойному
///     клику открывала главный трюм.
///
///     Адаптация под этот движок:
///     <list type="bullet">
///         <item>
///             В ServUO трюм жил в общем списке фикстур <c>BaseGalleon.Fixtures</c>; здесь он —
///             <c>[SerializableField]</c> самого корабля (<c>BaseGalleon._galleonHold</c>), а «стенки»
///             лежат в <c>_holdFixtures</c> (см. <c>GalleonCargo.cs</c>).
///         </item>
///         <item>
///             Позиция задаётся смещением в «северной» системе координат, как у пушек и пятаков:
///             поворот корабля и смена арта повреждённого корпуса пересчитываются одной формулой
///             (<c>BaseGalleon.GetPadLocation</c>), а не поиском по тайлам палубы.
///         </item>
///         <item>
///             Арт трюма зависит от направления корпуса (в ServUO — таблицы <c>HoldItemIDs</c>/
///             <c>HoldIDs</c>). Номер столбца в таблице хранится в самом предмете, а ID берётся по
///             текущему <c>Facing</c> корабля; пересчёт идёт из <see cref="OnLocationChange" /> —
///             движок двигает предметы палубы при повороте корабля.
///         </item>
///         <item>
///             <c>TillerManSay</c> здесь нет (у галеры штатный рычаг скрыт), поэтому отказ «вы не на
///             корабле» уходит игроку напрямую тем же cliloc-ом.
///         </item>
///     </list>
/// </summary>
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Engines.HighSeas;

/// <summary>Предмет палубы, привязанный к смещению в «северной» системе координат корабля.</summary>
public interface IGalleonHoldFixture
{
    /// <summary>Смещение от центра корабля в «северной» системе координат.</summary>
    Point3D HoldOffset { get; }

    /// <summary>Пересчитывает арт под текущий борт корабля.</summary>
    void UpdateItemID();
}

/// <summary>Трюм галеры: контейнер на палубе (в ServUO — <c>GalleonHold</c>).</summary>
[SerializationGenerator(0, false)]
public partial class GalleonHold : Container, IGalleonHoldFixture
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BaseGalleon _galleon;

    /// <summary>Смещение от центра корабля в «северной» системе координат.</summary>
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _holdOffset;

    /// <summary>Столбец таблицы <c>HoldItemIDs</c>, из которого берётся арт для текущего борта.</summary>
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _column;

    private TimerExecutionToken _deleteToken;

    /// <summary>Пустой конструктор — только ради <c>[add</c>; настоящий трюм создаёт корабль.</summary>
    [Constructible]
    public GalleonHold() : this(null, 0x3EAE, Point3D.Zero, 0)
    {
    }

    public GalleonHold(BaseGalleon galleon, int itemID, Point3D holdOffset, int column) : base(itemID)
    {
        Movable = false;
        _galleon = galleon;
        _holdOffset = holdOffset;
        _column = column;
    }

    public override int LabelNumber => 1149699; // cargo hold

    public override bool ForceShowProperties => true;

    public override bool IsDecoContainer => false;

    public override Rectangle2D Bounds => new(46, 74, 150, 110);

    public override int DefaultGumpID => 0x4C;

    public override int DefaultDropSound => 0x42;

    /// <summary>Вместимость трюма — своя у каждого корпуса (в ServUO — switch по типу галеры).</summary>
    public override int DefaultMaxWeight => _galleon?.HoldMaxWeight ?? 16000;

    /// <summary>Точка на палубе, соответствующая смещению.</summary>
    public Point3D GetWorldHoldLocation() => _galleon?.GetPadLocation(_holdOffset) ?? Location;

    /// <summary>Пересчитывает арт под текущий борт корабля (в ServUO — <c>SetFacingComponents</c>).</summary>
    public void UpdateItemID()
    {
        var galleon = _galleon;

        if (galleon?.Deleted != false)
        {
            return;
        }

        var itemID = galleon.ResolveHoldFixtureID(galleon.HoldItemIDs, _column, galleon.Facing);

        if (itemID != 0 && ItemID != itemID)
        {
            ItemID = itemID;
        }
    }

    /// <summary>
    ///     Движок переставляет предметы палубы при повороте корабля — здесь же подхватываем новый
    ///     борт и меняем арт (в ServUO это делал <c>SetFacingComponents</c>).
    /// </summary>
    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);

        UpdateItemID();
    }

    public override void OnDoubleClick(Mobile from)
    {
        var galleon = _galleon;

        if (from == null)
        {
            return;
        }

        if (galleon?.Deleted != false || from.AccessLevel > AccessLevel.Player)
        {
            base.OnDoubleClick(from);
            return;
        }

        if (!galleon.Contains(from))
        {
            from.SendLocalizedMessage(502490); // You must be on the ship to open the hold.
            return;
        }

        // В ServUO трюм открывался офицеру и выше; затопленный корабль не запирается.
        if (galleon.Owner is PlayerMobile && !galleon.Scuttled &&
            galleon.GetSecurityLevel(from) < SecurityLevel.Officer)
        {
            from.SendLocalizedMessage(1010436); // You do not have permission to do this.
            return;
        }

        base.OnDoubleClick(from);
    }

    /// <summary>
    ///     «Забрать груз» — то, что в ServUO делал <c>DockMaster.TryRetrieveHold</c> через своё контекстное
    ///     меню: трюм за пошлину перекладывается в грузовой ящик. Докмастера в этом движке нет, поэтому
    ///     пункт висит на самом трюме.
    /// </summary>
    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        var galleon = _galleon;

        if (from != null && galleon?.Deleted == false && Items.Count > 0 && galleon.CanAccessHold(from))
        {
            list.Add(new RetrieveCargoEntry(galleon, from));
        }
    }

    private class RetrieveCargoEntry : ContextMenuEntry
    {
        private readonly BaseGalleon _galleon;
        private readonly Mobile _from;

        public RetrieveCargoEntry(BaseGalleon galleon, Mobile from) : base(1116504, 3) // Retrieve Ship Cargo
        {
            _galleon = galleon;
            _from = from;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_galleon?.Deleted == false)
            {
                _galleon.RetrieveCargoCrate(_from);
            }
        }
    }

    public override void OnAfterDelete()
    {
        _deleteToken.Cancel();

        var galleon = _galleon;
        _galleon = null;

        galleon?.RemoveFixture(this);

        base.OnAfterDelete();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_galleon?.Deleted != false)
        {
            Timer.StartTimer(Delete, out _deleteToken);
            return;
        }

        UpdateItemID();
    }
}

/// <summary>
///     «Стенка» трюма: предмет-арт на палубе, по двойному клику открывающий главный трюм
///     (в ServUO — <c>HoldItem</c>).
/// </summary>
[SerializationGenerator(0, false)]
public partial class HoldItem : Item, IGalleonHoldFixture
{
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BaseGalleon _galleon;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _holdOffset;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _column;

    private TimerExecutionToken _deleteToken;

    /// <summary>Пустой конструктор — только ради <c>[add</c>; настоящие стенки создаёт корабль.</summary>
    [Constructible]
    public HoldItem() : this(null, 0, Point3D.Zero, 0)
    {
    }

    public HoldItem(BaseGalleon galleon, int itemID, Point3D holdOffset, int column) : base(itemID)
    {
        Movable = false;
        _galleon = galleon;
        _holdOffset = holdOffset;
        _column = column;
    }

    public override int LabelNumber => 1149699; // cargo hold

    public override bool ForceShowProperties => true;

    /// <summary>Точка на палубе, соответствующая смещению.</summary>
    public Point3D GetWorldHoldLocation() => _galleon?.GetPadLocation(_holdOffset) ?? Location;

    /// <summary>Пересчитывает арт под текущий борт корабля.</summary>
    public void UpdateItemID()
    {
        var galleon = _galleon;

        if (galleon?.Deleted != false)
        {
            return;
        }

        var itemID = galleon.ResolveHoldFixtureID(galleon.HoldIDs, _column, galleon.Facing);

        if (itemID != 0 && ItemID != itemID)
        {
            ItemID = itemID;
        }
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);

        UpdateItemID();
    }

    public override void OnDoubleClick(Mobile from)
    {
        var hold = _galleon?.GalleonHold;

        if (hold?.Deleted != false || from?.InRange(Location, 2) != true)
        {
            return;
        }

        hold.OnDoubleClick(from);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var hold = _galleon?.GalleonHold;

        if (hold?.Deleted == false)
        {
            list.Add(
                1072241, // Contents: ~1_COUNT~/~2_MAXCOUNT~ items, ~3_WEIGHT~/~4_MAXWEIGHT~ stones
                $"{hold.TotalItems}\t{hold.MaxItems}\t{hold.TotalWeight}\t{hold.MaxWeight}"
            );
        }
    }

    public override void OnAfterDelete()
    {
        _deleteToken.Cancel();

        var galleon = _galleon;
        _galleon = null;

        galleon?.RemoveFixture(this);

        base.OnAfterDelete();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_galleon?.Deleted != false)
        {
            Timer.StartTimer(Delete, out _deleteToken);
            return;
        }

        UpdateItemID();
    }
}
