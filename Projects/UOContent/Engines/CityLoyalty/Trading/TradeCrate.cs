/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Trading/TradeCrate.cs).
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             <c>TradeEntry</c> is a plain class, not a serializable entity, so the crate stores its data in flat
///             serializable fields (details as <c>"type|worth|amount|name"</c> strings) and rebuilds
///             <see cref="Entry" /> in <c>AfterDeserialization</c>.
///         </item>
///         <item>
///             <c>ObjectPropertyList</c> became <see cref="IPropertyList" />;
///             <c>GetContextMenuEntries(Mobile, List&lt;…&gt;)</c> became
///             <c>(Mobile, ref PooledRefList&lt;…&gt;)</c>; <c>OnClick()</c> became <c>OnClick(Mobile, IEntity)</c>.
///         </item>
///         <item>Krampus seasonal-event art/labels dropped (see CityTradeSystem.cs).</item>
///         <item><c>DateTime.UtcNow</c> for the expiry deadline became <c>Core.Now</c>.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.CityLoyalty;

[SerializationGenerator(0, false)]
public partial class TradeOrderCrate : Container
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _expires;

    [SerializableField(2)]
    private City _origin;

    [SerializableField(3)]
    private City _destination;

    [SerializableField(4)]
    private int _distance;

    [SerializableField(5)]
    private int _kills;

    [SerializableField(6)]
    private DateTime _lastAmbush;

    [SerializableField(7, setter: "private")]
    private List<string> _details;

    [Constructible]
    public TradeOrderCrate() : base(GetID())
    {
        _expires = Core.Now + TimeSpan.FromHours(CityTradeSystem.CrateDuration);
    }

    public TradeOrderCrate(Mobile from, TradeEntry entry) : base(GetID())
    {
        _owner = from;
        Entry = entry;

        _origin = entry.Origin;
        _destination = entry.Destination;
        _distance = entry.Distance;

        _details = new List<string>();
        foreach (var detail in entry.Details)
        {
            _details.Add($"{detail.ItemType.Name}|{detail.Worth}|{detail.Amount}|{detail.Name}");
        }

        _expires = Core.Now + TimeSpan.FromHours(CityTradeSystem.CrateDuration);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public TradeEntry Entry { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Fulfilled
    {
        get
        {
            if (Entry == null)
            {
                return false;
            }

            foreach (var details in Entry.Details)
            {
                if (GetAmount(details.ItemType) < details.Amount)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public bool Expired => _expires < Core.Now;

    public override int LabelNumber => 1041361; // a trade order crate
    public override int DefaultMaxWeight => 1000;
    public override bool DisplaysContent => false;

    public override int DefaultGumpID => ItemID switch
    {
        0x46A2 => 0x11B,
        0x46A3 => 0x11C,
        0x46A4 => 0x11D,
        0x46A5 or 0x46A6 => 0x11E,
        _ => base.DefaultGumpID
    };

    private static int GetID() => Utility.Random(0xE3C, 4);

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Entry != null)
        {
            // Destination City: ~1_city~
            list.Add(1151737, $"#{CityLoyaltySystem.CityLocalization(Entry.Destination)}");
        }

        list.Add(1076255); // NO-TRADE

        var weight = 0;
        foreach (var item in Items)
        {
            weight += item.Amount;
        }

        // Contents: ~1_COUNT~/~2_MAXCOUNT items, ~3_WEIGHT~/~4_MAXWEIGHT~ stones
        list.Add(1072241, $"{Items.Count}\t{DefaultMaxItems}\t{weight}\t{DefaultMaxWeight}");

        list.Add(1072210, "75"); // Weight reduction: ~1_PERCENTAGE~%

        if (Entry?.Details == null)
        {
            return;
        }

        for (var i = 0; i < Entry.Details.Count; i++)
        {
            var detail = Entry.Details[i];
            var amount = GetAmount(detail.ItemType);

            if (Utility.ToInt32(detail.Name) > 0)
            {
                // ~1_val~: ~2_val~/~3_val~
                list.Add(1116453 + i, $"#{detail.Name}\t{amount}\t{detail.Amount}");
            }
            else
            {
                list.Add(1116453 + i, $"{detail.Name}\t{amount}\t{detail.Amount}");
            }
        }

        if (Expired)
        {
            return;
        }

        var hours = (int)Math.Max(1, (_expires - Core.Now).TotalHours);
        list.Add(1153090, hours.ToString()); // Lifespan: ~1_val~ hours
    }

    public override void Delete()
    {
        CityLoyaltySystem.CityTrading?.RemoveCrate(_owner, this);
        base.Delete();
    }

    public override bool TryDropItem(Mobile from, Item item, bool message)
    {
        if (Entry == null || !TryAddItem(from, item, message))
        {
            return false;
        }

        return base.TryDropItem(from, item, message);
    }

    public bool TryAddItem(Mobile from, Item item, bool message = true)
    {
        var canAdd = false;

        if (Entry == null)
        {
            return false;
        }

        foreach (var details in Entry.Details)
        {
            if (item.GetType() != details.ItemType)
            {
                continue;
            }

            if (GetAmount(item.GetType()) + item.Amount > details.Amount)
            {
                if (message)
                {
                    // You are trying to add too many of this item to the trade order.
                    from.SendLocalizedMessage(1151726);
                }

                return false;
            }

            canAdd = true;
            break;
        }

        if (!canAdd && message)
        {
            // This trade order does not require this item.
            from.SendLocalizedMessage(1151725);
        }

        return canAdd;
    }

    public override int GetTotal(TotalType type)
    {
        if (type == TotalType.Weight)
        {
            var weight = base.GetTotal(type);

            if (weight > 0)
            {
                return (int)Math.Max(1, base.GetTotal(type) * .25);
            }
        }

        return base.GetTotal(type);
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (!IsChildOf(from.Backpack))
        {
            return;
        }

        list.Add(new FillFromPackEntry(this, from));
        list.Add(new CancelOrderEntry(this, from));
    }

    public override bool OnDroppedOnto(Mobile from, Item target)
    {
        from.SendLocalizedMessage(1076254); // That item cannot be dropped.
        return false;
    }

    public override bool OnDroppedInto(Mobile from, Container target, Point3D p)
    {
        if (target == from.Backpack)
        {
            return base.OnDroppedInto(from, target, p);
        }

        from.SendLocalizedMessage(1076254);
        return false;
    }

    public override bool OnDroppedToWorld(Mobile from, Point3D point)
    {
        from.SendLocalizedMessage(1076254);
        return false;
    }

    public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
    {
        from.SendLocalizedMessage(1076256); // That item cannot be traded.
        return false;
    }

    private void AfterDeserialization()
    {
        Entry = new TradeEntry(_destination, _origin, _distance)
        {
            Kills = _kills,
            LastAmbush = _lastAmbush
        };

        if (_details == null)
        {
            return;
        }

        foreach (var line in _details)
        {
            var parts = line.Split('|');

            if (parts.Length != 4)
            {
                continue;
            }

            var type = AssemblyHandler.FindTypeByName(parts[0]);

            Entry.Details.Add(
                TradeEntry.TradeDetails.Restore(type, Utility.ToInt32(parts[1]), Utility.ToInt32(parts[2]), parts[3])
            );
        }
    }

    private class FillFromPackEntry : ContextMenuEntry
    {
        private readonly TradeOrderCrate _crate;

        public FillFromPackEntry(TradeOrderCrate crate, Mobile player) : base(1154908, 3) // Fill from pack
        {
            _crate = crate;
            Clicker = player;
        }

        private Mobile Clicker { get; }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (!_crate.IsChildOf(Clicker.Backpack) || _crate.Deleted || _crate.Entry == null)
            {
                return;
            }

            foreach (var detail in _crate.Entry.Details)
            {
                var items = Clicker.Backpack.FindItemsByType(detail.ItemType);

                foreach (var item in items)
                {
                    if (item.Amount == 1 && _crate.TryAddItem(Clicker, item, false))
                    {
                        _crate.DropItem(item);
                    }
                }
            }
        }
    }

    private class CancelOrderEntry : ContextMenuEntry
    {
        private readonly TradeOrderCrate _crate;

        public CancelOrderEntry(TradeOrderCrate crate, Mobile player) : base(1151727, 3) // cancel trade order
        {
            _crate = crate;
            Clicker = player;
        }

        private Mobile Clicker { get; }

        public override void OnClick(Mobile from, IEntity target) => Clicker.SendGump(new CancelTradeOrderGump(_crate, Clicker));
    }
}

public class CancelTradeOrderGump : Gump
{
    private readonly TradeOrderCrate _crate;
    private readonly Mobile _player;

    public CancelTradeOrderGump(TradeOrderCrate crate, Mobile player) : base(100, 100)
    {
        _crate = crate;
        _player = player;

        AddPage(0);

        AddBackground(0, 0, 291, 93, 0x13BE);
        AddImageTiled(5, 5, 280, 60, 0xA40);

        // Are you sure you wish to cancel this trade order?
        AddHtmlLocalized(9, 9, 272, 60, 1151728, 0x7FFF, false, false);

        AddButton(160, 67, 0xFB7, 0xFB8, 1);
        AddHtmlLocalized(195, 69, 120, 20, 1006044, 0x7FFF); // OKAY

        AddButton(5, 67, 0xFB1, 0xFB2, 0);
        AddHtmlLocalized(40, 69, 100, 20, 1060051, 0x7FFF); // CANCEL
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1 && _crate != null && !_crate.Deleted && _crate.IsChildOf(_player.Backpack))
        {
            CityTradeSystem.CancelTradeOrder(_player, _crate);
        }
    }
}
