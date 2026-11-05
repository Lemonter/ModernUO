/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/Time Of Legends/AuctionSafe/AuctionMap.cs).
///
///     Карта аукциона: выдаётся сделавшему ставку, показывает сейф на карте мира, из контекстного меню открывает
///     карту и предлагает телепорт к дому с сейфом (с подтверждением). При падении на землю уничтожается.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             Ручные <c>Serialize</c>/<c>Deserialize</c> → <c>[SerializationGenerator(0, false)]</c> и
///             <see cref="SerializableField" />: ссылки на сейф и предмет пишутся сущностями
///             (<c>Write(entity)</c>/<c>ReadEntity&lt;T&gt;()</c>), остальное — простые поля.
///         </item>
///         <item>
///             <c>ObjectPropertyList</c> → <see cref="IPropertyList" />;
///             <c>GetContextMenuEntries(Mobile, List&lt;ContextMenuEntry&gt;)</c> →
///             <c>GetContextMenuEntries(Mobile, ref PooledRefList&lt;ContextMenuEntry&gt;)</c>,
///             <c>OnClick()</c> → <c>OnClick(Mobile from, IEntity target)</c>.
///         </item>
///         <item>
///             <c>RecallRune.CalculateHue(Map, Item, bool)</c> в этом репозитории нет (там это приватный метод
///             экземпляра), поэтому оттенок карты не выставляется.
///         </item>
///         <item>
///             <c>BaseGump</c> → обычный <see cref="Gump" /> с явным <c>DisplayTo</c>; <c>RelayInfo</c> —
///             <c>in RelayInfo</c>. Телепорт выполняется <see cref="RecallSpell" /> с
///             <see cref="RunebookEntry" />, потому что прежняя перегрузка <c>RecallSpell(Mobile, Item, ...)</c>
///             здесь отсутствует.
///         </item>
///         <item>
///             Плата за телепорт, как и в ServUO, не списывается: <see cref="OnBeforeTravel" /> никто не вызывает.
///             Поведение сохранено намеренно — это отдельный вопрос, а не часть порта.
///         </item>
///         <item>Собственные строки переведены на русский, cliloc-идентификаторы не тронуты.</item>
///     </list>
/// </summary>
using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.Auction;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Spells.Fourth;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AuctionMap : MapItem
{
    public const int TeleportCost = 1000;
    public const int DeleteDelayMinutes = 30;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private AuctionSafe _auctionSafe;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Item _auctionItem;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _safeLocation;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Map _safeMap;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _houseName;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _setLocation;

    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Map _setMap;

    [Constructible]
    public AuctionMap() : base(null)
    {
    }

    public AuctionMap(AuctionSafe auctionSafe) : base(auctionSafe?.Map)
    {
        _auctionSafe = auctionSafe;
        _auctionItem = AuctionStore.Get(auctionSafe)?.AuctionItem;
        _safeLocation = auctionSafe?.Location ?? Point3D.Zero;
        _safeMap = auctionSafe?.Map;
        _houseName = GetHouse()?.Sign?.GetName() ?? "Неизвестный дом";

        LootType = LootType.Blessed;

        Width = 400;
        Height = 400;

        if (auctionSafe != null)
        {
            Bounds = new Rectangle2D(auctionSafe.X - 100, auctionSafe.Y - 100, 200, 200);
            AddWorldPin(auctionSafe.X, auctionSafe.Y);
        }
    }

    public override bool DropToWorld(Mobile from, Point3D p)
    {
        from?.SendLocalizedMessage(500424); // You destroyed the item.
        Delete();

        return true;
    }

    public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted) => false;

    public override void AddNameProperty(IPropertyList list)
    {
        if (_auctionItem == null)
        {
            // Map to Auction ~1_ITEMNAME~: ~2_HOUSE~
            list.Add(1156474, $"{_houseName}\tнеизвестный предмет");
        }
        else if (_auctionItem.LabelNumber != 0)
        {
            list.Add(1156474, $"{_houseName}\t#{_auctionItem.LabelNumber}");
        }
        else
        {
            list.Add(1156474, $"{_houseName}\t{_auctionItem.Name}");
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var coords = GetCoords();

        // Vendor Located at ~1_loc~ (~2_facet~)
        list.Add(1154639, $"{coords[0]}\t{coords[1]}");

        if (!CheckItem())
        {
            list.Add(1154700); // Item no longer for sale.
        }

        list.Add(1075269); // Destroyed when dropped
    }

    public string[] GetCoords()
    {
        var location = Point3D.Zero;
        var map = Map.Internal;

        if (_setLocation != Point3D.Zero && _setMap != Map.Internal)
        {
            location = _setLocation;
            map = _setMap;
        }
        else if (_safeLocation != Point3D.Zero && _safeMap != Map.Internal)
        {
            location = _safeLocation;
            map = _safeMap;
        }

        if (location == Point3D.Zero || map == null || map == Map.Internal)
        {
            return new[] { "неизвестное место", "Неизвестно" };
        }

        var xLong = 0;
        var yLat = 0;
        var xMins = 0;
        var yMins = 0;
        var xEast = false;
        var ySouth = false;

        if (!Sextant.Format(location, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
        {
            return new[] { "неизвестное место", "Неизвестно" };
        }

        var lat = $"{yLat}° {yMins}'{(ySouth ? "S" : "N")}";
        var lon = $"{xLong}° {xMins}'{(xEast ? "E" : "W")}";

        return new[] { $"{lat}, {lon}", map.ToString() };
    }

    public BaseHouse GetHouse() => _auctionSafe == null ? null : BaseHouse.FindHouseAt(_auctionSafe);

    public void OnBeforeTravel(Mobile from)
    {
        Banker.Withdraw(from, TeleportCost);
        // ~1_AMOUNT~ gold has been withdrawn from your bank box.
        from.SendLocalizedMessage(1060398, $"{TeleportCost}");

        if (_setLocation != Point3D.Zero)
        {
            _setLocation = Point3D.Zero;
            _setMap = null;
        }
        else
        {
            _setLocation = from.Location;
            _setMap = from.Map;
        }

        InvalidateProperties();
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        list.Add(new OpenMapEntry(from, this));
        list.Add(new TeleportEntry(from, this));
    }

    public bool CheckItem() =>
        GetHouse() != null && _auctionItem != null && _auctionSafe?.CheckAuctionItem(_auctionItem) == true;

    public Point3D GetLocation(Mobile m)
    {
        BaseHouse house = null;

        if (_setLocation != Point3D.Zero)
        {
            house = BaseHouse.FindHouseAt(_setLocation, _setMap, 16);
        }
        else if (_auctionSafe != null)
        {
            house = BaseHouse.FindHouseAt(_auctionSafe);
        }

        if (house != null)
        {
            m?.SendLocalizedMessage(1070905); // Strong magics have redirected you to a safer location!
            return house.BanLocation;
        }

        return _setLocation != Point3D.Zero ? _setLocation : Point3D.Zero;
    }

    public Map GetMap()
    {
        if (_setLocation != Point3D.Zero)
        {
            return _setMap;
        }

        return _auctionSafe?.Map;
    }

    public class OpenMapEntry : ContextMenuEntry
    {
        private readonly Mobile _clicker;
        private readonly AuctionMap _map;

        public OpenMapEntry(Mobile from, AuctionMap map) : base(3006150, -1) // Open Map
        {
            _clicker = from;
            _map = map;
        }

        public override void OnClick(Mobile from, IEntity target) => _map.DisplayTo(from ?? _clicker);
    }

    public class TeleportEntry : ContextMenuEntry
    {
        private readonly AuctionMap _map;

        public TeleportEntry(Mobile from, AuctionMap map) : base(1154558, -1) // Teleport To Vendor
        {
            _map = map;
            Enabled = map.CheckItem();
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from is PlayerMobile player)
            {
                ConfirmTeleportGump.DisplayTo(player, _map);
            }
        }
    }

    public class ConfirmTeleportGump : Gump
    {
        private readonly AuctionMap _map;

        private ConfirmTeleportGump(Mobile user, AuctionMap map) : base(10, 10)
        {
            _map = map;

            AddPage(0);

            AddBackground(0, 0, 414, 214, 0x7752);

            // Please select 'Accept' if you would like to pay ~1_cost~ gold to teleport to auction house
            // ~2_name~. For this price you will also be able to teleport back to this location within the next
            // ~3_minutes~ minutes.
            AddHtmlLocalized(
                27,
                47,
                380,
                80,
                1156475,
                $"@{TeleportCost}@{map.HouseName}@{DeleteDelayMinutes}",
                0xFFFF,
                false,
                false
            );

            AddButton(7, 167, 0x7747, 0x7747, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(47, 167, 100, 40, 1150300, 0x4E73, false, false); // CANCEL

            AddButton(377, 167, 0x7746, 0x7746, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(267, 167, 100, 40, 1114514, "#1150299", 0xFFFF, false, false);
        }

        public static void DisplayTo(Mobile from, AuctionMap map)
        {
            if (from == null || map?.Deleted != false)
            {
                return;
            }

            from.CloseGump<ConfirmTeleportGump>();
            from.SendGump(new ConfirmTeleportGump(from, map));
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (info.ButtonID != 1)
            {
                return;
            }

            var from = state.Mobile;

            if (from == null || _map == null || _map.Deleted)
            {
                return;
            }

            if (Banker.GetBalance(from) < TeleportCost)
            {
                from.SendLocalizedMessage(1154672); // You cannot afford to teleport to the vendor.
                return;
            }

            if (!_map.CheckItem())
            {
                from.SendLocalizedMessage(1154643); // That item is no longer for sale.
                return;
            }

            if (_map.SetLocation != Point3D.Zero &&
                (!Utility.InRange(_map.SetLocation, from.Location, 100) || _map.SetMap != from.Map))
            {
                from.SendLocalizedMessage(501035); // You cannot teleport from here to the destination.
                return;
            }

            var location = _map.GetLocation(from);
            var map = _map.GetMap();

            if (location == Point3D.Zero || map == null)
            {
                from.SendLocalizedMessage(501035); // You cannot teleport from here to the destination.
                return;
            }

            new RecallSpell(from, new RunebookEntry(null, location, map)).Cast();
        }
    }
}
