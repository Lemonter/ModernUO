/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/ShippingCrate.cs — класс
///     <c>ShipCrate</c>).
///
///     Грузовой ящик: во что портовый служащий складывает трюм корабля и что потом грузится обратно на
///     корабль. Ящик принадлежит конкретному игроку и конкретному кораблю, живёт полчаса и исчезает
///     вместе с содержимым.
///
///     Отличия от исходника:
///     <list type="bullet">
///         <item>
///             <c>BaseConfirmGump</c> в этом движке нет — подтверждение уничтожения сделано обычным
///             <see cref="Gump" /> со статическим <c>DisplayTo</c> (правило «без пустых окон»:
///             <c>DisplayTo</c> проверяет, что ящик ещё существует, до постройки окна).
///         </item>
///         <item>
///             <c>ObjectPropertyList</c> заменён на <see cref="IPropertyList" />.
///         </item>
///         <item>
///             <c>Server.Mobiles.DockMaster.RemoveCrate</c> здесь нет (докмастера не портировали),
///             поэтому список выданных ящиков живёт в самом ящике: <see cref="HasCrateFor" /> смотрит
///             рюкзак и банк игрока.
///         </item>
///     </list>
/// </summary>
using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class ShipCrate : SmallCrate
{
    /// <summary>Сколько минут ящик ждёт владельца (в ServUO — <c>ShipCrate.DT</c>).</summary>
    public const int DecayMinutes = 30;

    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [SerializableField(1, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BaseBoat _boat;

    /// <summary>Пустой конструктор — только ради <c>[add</c>; настоящий ящик выдаёт трюм корабля.</summary>
    [Constructible]
    public ShipCrate() : this(null, null)
    {
    }

    public ShipCrate(Mobile owner, BaseBoat boat)
    {
        LiftOverride = true;
        _owner = owner;
        _boat = boat;

        Movable = false;
    }

    public override TimeSpan DecayTime => TimeSpan.FromMinutes(DecayMinutes);

    public override bool Decays => true;

    public override void AddNameProperty(IPropertyList list)
    {
        if (_owner != null)
        {
            list.Add(1116515, _owner.Name); // a shipping crate for ~1_NAME~
            return;
        }

        list.Add("грузовой ящик");
    }

    /// <summary>Есть ли у игрока уже выданный ящик — второй выдавать нельзя (в ServUO вёл докмастер).</summary>
    public static bool HasCrateFor(Mobile from) =>
        from?.Backpack?.FindItemByType<ShipCrate>() != null || from?.BankBox?.FindItemByType<ShipCrate>() != null;

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        list.Add(new DestroyCrateEntry(from, this));

        if (_boat?.Deleted == false && Items.Count > 0)
        {
            list.Add(new LoadShipEntry(from, this));
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_owner != null && from != _owner)
        {
            from.SendLocalizedMessage(1112589); // This does not belong to you! Find your own!
            return;
        }

        base.OnDoubleClick(from);
    }

    /// <summary>
    ///     Перекладывает содержимое ящика в трюм корабля (в ServUO — <c>LoadShip</c>).
    ///     Трюм галеры — <see cref="BaseGalleon.GalleonHold" />, у классической лодки — штатный <c>Hold</c>.
    /// </summary>
    public bool TryLoadIntoBoat(Mobile from)
    {
        var boat = _boat;

        if (from == null || boat?.Deleted != false)
        {
            from?.SendMessage(0x22, "Корабль, к которому был приписан этот ящик, больше не существует.");
            return false;
        }

        if (!from.InRange(boat.Location, CargoRange))
        {
            from.SendLocalizedMessage(1116519); // I can't find your ship! You need to bring it in closer.
            return false;
        }

        Container hold = boat is BaseGalleon galleon ? galleon.GalleonHold : boat.Hold;

        if (hold?.Deleted != false)
        {
            from.SendMessage(0x22, "У корабля нет трюма.");
            return false;
        }

        var items = Items.ToArray();

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];

            if (item?.Deleted != false)
            {
                continue;
            }

            if (!hold.TryDropItem(from, item, false))
            {
                from.SendMessage(0x22, "В трюме не хватило места — часть груза осталась в ящике.");
                return false;
            }
        }

        from.SendMessage(0x22, "Трюм загружен из грузового ящика.");
        return true;
    }

    /// <summary>Дальность, с которой ящик видит корабль (в ServUO — <c>DockMaster.DryDockDistance</c>).</summary>
    private const int CargoRange = 300;

    public override void OnItemRemoved(Item item)
    {
        base.OnItemRemoved(item);

        if (TotalItems == 0 && !Deleted)
        {
            Delete();
        }
    }

    public override void OnAfterDelete()
    {
        _owner = null;
        _boat = null;

        base.OnAfterDelete();
    }

    private class LoadShipEntry : ContextMenuEntry
    {
        private readonly Mobile _from;
        private readonly ShipCrate _crate;

        public LoadShipEntry(Mobile from, ShipCrate crate) : base(1116521, 3) // Load Ship from Crate
        {
            _from = from;
            _crate = crate;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_crate?.Deleted == false)
            {
                _crate.TryLoadIntoBoat(_from);
            }
        }
    }

    private class DestroyCrateEntry : ContextMenuEntry
    {
        private readonly Mobile _from;
        private readonly ShipCrate _crate;

        public DestroyCrateEntry(Mobile from, ShipCrate crate) : base(1116522, 3) // Destroy Crate
        {
            _from = from;
            _crate = crate;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_crate?.Deleted == false)
            {
                DestroyCrateGump.DisplayTo(_from, _crate);
            }
        }
    }
}

/// <summary>Подтверждение уничтожения грузового ящика (в ServUO — <c>BaseConfirmGump</c>).</summary>
public class DestroyCrateGump : Gump
{
    private readonly ShipCrate _crate;

    private DestroyCrateGump(ShipCrate crate) : base(150, 200)
    {
        _crate = crate;

        AddPage(0);
        AddBackground(0, 0, 260, 170, 5054);
        AddBackground(10, 10, 240, 150, 3000);

        AddHtml(20, 20, 220, 80, "<CENTER>Уничтожить грузовой ящик вместе с содержимым?</CENTER>", false, false);

        AddButton(20, 110, 4005, 4007, 2);
        AddLabel(55, 110, 0x7FFF, "УНИЧТОЖИТЬ");

        AddButton(20, 135, 4005, 4007, 1);
        AddLabel(55, 135, 0x7FFF, "ОТМЕНА");
    }

    public static void DisplayTo(Mobile from, ShipCrate crate)
    {
        if (from?.NetState == null || crate?.Deleted != false)
        {
            return;
        }

        from.CloseGump<DestroyCrateGump>();
        from.SendGump(new DestroyCrateGump(crate));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (info.ButtonID != 2 || from == null || _crate?.Deleted != false)
        {
            return;
        }

        _crate.Delete();
    }
}
