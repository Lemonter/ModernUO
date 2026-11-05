/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Mobiles/CommodityBroker.cs).
///
///     Товарный маклер: закупает и продаёт сырьё за банковское золото. Держит до 50 позиций, каждая со своей
///     ценой покупки/продажи и лимитами.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             <c>from.CloseGump(typeof(X))</c> / <c>new X(...)</c> → статические <c>X.DisplayTo(from, …)</c>
///             обычных <see cref="Server.Gumps.Gump" />-ов.
///         </item>
///         <item>
///             Список позиций не пишет <c>[SerializationGenerator]</c>: <see cref="CommodityBrokerEntry" /> — не
///             сущность и не примитив, поэтому генератор такие поля не умеет. Позиции пишет
///             <see cref="MaginciaBazaarPlot.Serialize" /> через <see cref="SerializeEntries" /> — прилавок и
///             так владеет маклером.
///         </item>
///         <item>
///             Конструктора без параметров в ServUO не было; он нужен генератору для десериализации и помечен
///             <c>[Constructible]</c>, как в <c>WritOfLease</c>. Маклер без прилавка инертен: окна и склад
///             проверяют <see cref="BaseBazaarBroker.Plot" /> на <c>null</c>.
///         </item>
///         <item>
///             <c>Container.FindItemsByType(Type)</c> в ModernUO — перечисление, а не массив, поэтому перед
///             изменением содержимого результаты собираются в список.
///         </item>
///         <item><c>String.Format</c> в аргументах cliloc → интерполяция с приведением к <c>string</c>.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.NewMagincia;

[SerializationGenerator(0, false)]
public partial class CommodityBroker : BaseBazaarBroker
{
    public static readonly int MaxEntries = 50;

    private List<CommodityBrokerEntry> _commodityEntries = new();

    [Constructible]
    public CommodityBroker() : this(null)
    {
    }

    public CommodityBroker(MaginciaBazaarPlot plot) : base(plot)
    {
    }

    public List<CommodityBrokerEntry> CommodityEntries => _commodityEntries;

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(Location, 4) || Plot == null)
        {
            base.OnDoubleClick(from);
            return;
        }

        if (Plot.Owner == from)
        {
            CommodityBrokerGump.DisplayTo(from, this);
        }
        else
        {
            CommodityInventoryGump.DisplayTo(from, this);
        }
    }

    public override int GetWeeklyFee()
    {
        var total = 0;

        foreach (var entry in _commodityEntries)
        {
            if (entry.SellPricePer > 0)
            {
                total += entry.Stock * entry.SellPricePer;
            }
        }

        return (int)(total * 0.05);
    }

    public bool TryAddBrokerEntry(Item item, Mobile from)
    {
        var realItem = item is CommodityDeed deed ? deed.Commodity : item;

        if (realItem == null)
        {
            return false;
        }

        var type = realItem.GetType();
        var amount = realItem.Amount;

        if (HasType(type))
        {
            return false;
        }

        var entry = new CommodityBrokerEntry(realItem, this, amount);
        _commodityEntries.Add(entry);

        if (amount > 0 && from != null && Plot != null)
        {
            // You have added ~1_QUANTITY~ units of ~2_ITEMNAME~ to the inventory of "~3_SHOPNAME~"
            from.SendLocalizedMessage(
                1150220,
                $"{amount.ToString()}\t#{entry.Label}\t{Plot.ShopName ?? "безымянный магазин"}"
            );
        }

        item.Delete();
        return true;
    }

    public void RemoveEntry(Mobile from, CommodityBrokerEntry entry)
    {
        if (!_commodityEntries.Contains(entry))
        {
            return;
        }

        if (entry.Stock > 0)
        {
            WithdrawInventory(from, GetStock(entry), entry);
        }

        _commodityEntries.Remove(entry);
    }

    public override bool HasValidEntry(Mobile m)
    {
        foreach (var entry in _commodityEntries)
        {
            if (entry.Stock > 0)
            {
                return true;
            }
        }

        return false;
    }

    public void AddInventory(Mobile from, Item item)
    {
        if (item == null)
        {
            return;
        }

        var type = item.GetType();
        var amountToAdd = item.Amount;

        if (item is CommodityDeed deed && deed.Commodity != null)
        {
            type = deed.Commodity.GetType();
            amountToAdd = deed.Commodity.Amount;
        }

        foreach (var entry in _commodityEntries)
        {
            if (entry.CommodityType != type)
            {
                continue;
            }

            entry.Stock += amountToAdd;
            item.Delete();

            if (from != null && Plot?.Owner == from)
            {
                // You have added ~1_QUANTITY~ units of ~2_ITEMNAME~ to the inventory of "~3_SHOPNAME~"
                from.SendLocalizedMessage(
                    1150220,
                    $"{amountToAdd.ToString()}\t#{entry.Label}\t{Plot.ShopName ?? "безымянный магазин"}"
                );
            }

            break;
        }
    }

    private bool HasType(Type type)
    {
        foreach (var entry in _commodityEntries)
        {
            if (entry.CommodityType == type)
            {
                return true;
            }
        }

        return false;
    }

    public int GetStock(CommodityBrokerEntry entry) => entry.Stock;

    public void WithdrawInventory(Mobile from, int amount, CommodityBrokerEntry entry)
    {
        if (from == null || Plot == null || entry == null || !_commodityEntries.Contains(entry))
        {
            return;
        }

        var pack = from.Backpack;

        if (pack != null)
        {
            while (amount > 60000)
            {
                var deed = new CommodityDeed();
                var item = Loot.Construct(entry.CommodityType);
                item.Amount = 60000;
                deed.SetCommodity(item);
                pack.DropItem(deed);
                amount -= 60000;
                entry.Stock -= 60000;
            }

            var lastDeed = new CommodityDeed();
            var newItem = Loot.Construct(entry.CommodityType);
            newItem.Amount = amount;
            lastDeed.SetCommodity(newItem);
            pack.DropItem(lastDeed);
            entry.Stock -= amount;
        }

        if (from == Plot.Owner)
        {
            // You have removed ~1_QUANTITY~ units of ~2_ITEMNAME~ from the inventory of "~3_SHOPNAME~"
            from.SendLocalizedMessage(
                1150221,
                $"{amount.ToString()}\t#{entry.Label}\t{Plot.ShopName ?? "магазин без названия"}"
            );
        }
    }

    public int GetBuyCost(Mobile from, CommodityBrokerEntry entry, int amount)
    {
        var totalCost = entry.SellPricePer * amount;

        return totalCost + (int)(totalCost * ((double)CommissionFee / 100.0));
    }

    /// <summary>Игрок покупает товар у маклера.</summary>
    public void TryBuyCommodity(Mobile from, CommodityBrokerEntry entry, int amount)
    {
        var totalCost = entry.SellPricePer * amount;
        var toAdd = totalCost + (int)(totalCost * ((double)CommissionFee / 100.0));

        if (!Banker.Withdraw(from, totalCost))
        {
            // You do not have the funds needed to make this trade...
            from.SendLocalizedMessage(1150252);
            return;
        }

        // A commodity deed worth ~1_AMOUNT~ ~2_ITEM~ has been placed in your backpack.
        from.SendLocalizedMessage(1150643, $"{amount.ToString("###,###,###")}\t#{entry.Label}");

        WithdrawInventory(from, amount, entry);
        BankBalance += toAdd;
    }

    public bool SellCommodityControl(Mobile from, CommodityBrokerEntry entry, int amount)
    {
        if (from.Backpack == null)
        {
            return false;
        }

        var type = entry.CommodityType;

        return from.Backpack.GetAmount(type) + GetCommodityType(from.Backpack, type) >= amount;
    }

    /// <summary>Игрок продаёт товар маклеру.</summary>
    public void TrySellCommodity(Mobile from, CommodityBrokerEntry entry, int amount)
    {
        var totalCost = entry.BuyPricePer * amount;
        var type = entry.CommodityType;

        if (BankBalance < totalCost)
        {
            // Молча: эту ветку уже обработал вызывающий.
            return;
        }

        if (from.Backpack == null)
        {
            return;
        }

        var total = amount;

        if (from.Backpack.GetAmount(type) + GetCommodityType(from.Backpack, type) < total)
        {
            // You do not have the requested amount of that commodity...
            from.SendLocalizedMessage(1150667);
            return;
        }

        if (!Banker.Deposit(from, totalCost))
        {
            // Your bank box cannot hold the proceeds from this transaction.
            from.SendLocalizedMessage(1150265);
            return;
        }

        TakeItems(from.Backpack, type, ref total);

        if (total > 0)
        {
            TakeCommodities(from.Backpack, type, ref total);
        }

        BankBalance -= totalCost + (int)(totalCost * ((double)CommissionFee / 100.0));

        // You have sold ~1_QUANTITY~ units of ~2_COMMODITY~ to the broker...
        from.SendLocalizedMessage(1150668, $"{amount.ToString()}\t#{entry.Label}");
    }

    private void TakeCommodities(Container container, Type type, ref int amount)
    {
        if (container == null)
        {
            return;
        }

        List<Item> toSell = null;

        foreach (var item in container.FindItemsByType(typeof(CommodityDeed)))
        {
            if (item is not CommodityDeed commodityDeed || commodityDeed.Commodity == null
                || commodityDeed.Commodity.GetType() != type)
            {
                continue;
            }

            var commodity = commodityDeed.Commodity;

            if (commodity.Amount <= amount)
            {
                (toSell ??= new List<Item>()).Add(item);
                amount -= commodity.Amount;
                continue;
            }

            if (amount <= 0)
            {
                continue;
            }

            var newDeed = new CommodityDeed();
            var newItem = Loot.Construct(type);
            newItem.Amount = amount;
            newDeed.SetCommodity(newItem);

            commodity.Amount -= amount;
            commodityDeed.InvalidateProperties();
            (toSell ??= new List<Item>()).Add(newDeed);
            amount = 0;
        }

        if (toSell == null)
        {
            return;
        }

        foreach (var item in toSell)
        {
            AddInventory(null, item);
        }
    }

    private void TakeItems(Container container, Type type, ref int amount)
    {
        if (container == null)
        {
            return;
        }

        List<Item> toSell = null;

        foreach (var item in container.FindItemsByType(type))
        {
            if (amount <= 0)
            {
                break;
            }

            if (item.Amount <= amount)
            {
                (toSell ??= new List<Item>()).Add(item);
                amount -= item.Amount;
                continue;
            }

            var newItem = Loot.Construct(type);
            newItem.Amount = amount;
            item.Amount -= amount;
            (toSell ??= new List<Item>()).Add(newItem);
            amount = 0;
        }

        if (toSell == null)
        {
            return;
        }

        foreach (var item in toSell)
        {
            AddInventory(null, item);
        }
    }

    public int GetCommodityType(Container container, Type type)
    {
        if (container == null)
        {
            return 0;
        }

        var total = 0;

        foreach (var item in container.FindItemsByType(typeof(CommodityDeed)))
        {
            if (item is CommodityDeed { Commodity: not null } deed && deed.Commodity.GetType() == type)
            {
                total += deed.Commodity.Amount;
            }
        }

        return total;
    }

    public int GetLabelID(CommodityBrokerEntry entry) => entry?.Label ?? 1;

    /// <summary>Пишется прилавком: маклер владеет позициями, но не сериализуется генератором.</summary>
    public void SerializeEntries(IGenericWriter writer)
    {
        writer.WriteEncodedInt(_commodityEntries.Count);

        foreach (var entry in _commodityEntries)
        {
            entry.Serialize(writer);
        }
    }

    public static void DeserializeEntries(IGenericReader reader, CommodityBroker broker)
    {
        var count = reader.ReadEncodedInt();

        broker?._commodityEntries.Clear();

        for (var i = 0; i < count; i++)
        {
            var entry = CommodityBrokerEntry.Deserialize(reader);

            if (entry != null)
            {
                broker?._commodityEntries.Add(entry);
            }
        }
    }
}
