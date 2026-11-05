/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Gumps/CommodityBrokerGump.cs).
///
///     Окна товарного маклера: меню владельца, подсказка при выборе товара, цены и лимиты, склад маклера,
///     витрина для покупателей, подтверждения удаления позиции и сделки.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             Все окна — обычные <see cref="Gump" /> со статическим <c>DisplayTo</c>; <c>TextRelay</c> →
///             <c>info.GetTextEntry(id)</c>, <c>Convert.ToInt32</c> → <c>int.TryParse</c>.
///         </item>
///         <item>
///             <c>BaseConfirmGump</c> ServUO использовал английские строки в <c>TitleString</c>/<c>LabelString</c>,
///             которых в ModernUO нет (там <c>TitleNumber</c>/<c>LabelNumber</c>). Окно подтверждения удаления
///             сделано обычным <see cref="Gump" /> с русским текстом.
///         </item>
///         <item><c>from.HasGump(typeof(X))</c>/<c>CloseGump(typeof(X))</c> → обобщённые <c>HasGump&lt;X&gt;()</c>/<c>CloseGump&lt;X&gt;()</c>.</item>
///         <item><c>String.Format</c> → интерполяция; собственные подписи переведены, cliloc не тронуты.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.NewMagincia;

public class CommodityBrokerGump : BaseBazaarGump
{
    private readonly CommodityBroker _broker;

    private CommodityBrokerGump(Mobile from, CommodityBroker broker) : base(520, 520)
    {
        _broker = broker;

        AddHtmlLocalized(10, 10, 500, 18, 1114513, "#1150636", RedColor16, false, false); // Commodity Broker
        AddShopHeader(broker, 500);

        AddHtmlLocalized(10, 100, 500, 18, 1114513, "#1150328", GreenColor16, false, false); // OWNER MENU

        AddButton(150, 150, 4005, 4007, 1);
        AddHtmlLocalized(190, 150, 200, 18, 1150392, OrangeColor16, false, false); // INFORMATION

        AddHtmlLocalized(38, 180, 200, 18, 1150199, RedColor16, false, false); // Broker Account Balance
        AddHtml(190, 180, 300, 18, FormatAmt(broker.BankBalance), false, false);

        var balance = Banker.GetBalance(from);
        AddHtmlLocalized(68, 200, 200, 18, 1150149, GreenColor16, false, false); // Your Bank Balance:
        AddHtml(190, 200, 200, 18, FormatAmt(balance), false, false);

        AddHtmlLocalized(32, 230, 200, 18, 1150329, OrangeColor16, false, false); // Broker Sales Comission
        AddHtmlLocalized(190, 230, 100, 18, 1150330, OrangeColor16, false, false); // 5%

        AddHtmlLocalized(109, 250, 200, 18, 1150331, OrangeColor16, false, false); // Weekly Fee:
        AddHtml(190, 250, 100, 18, FormatAmt(broker.GetWeeklyFee()), false, false);

        AddHtmlLocalized(113, 280, 200, 18, 1150332, OrangeColor16, false, false); // Shop Name:
        AddBackground(190, 280, 285, 22, 9350);
        AddTextEntry(191, 280, 285, 20, LabelHueBlue, 0, broker.Plot?.ShopName ?? "");
        AddButton(480, 280, 4014, 4016, 2);

        AddHtmlLocalized(85, 305, 150, 18, 1150195, OrangeColor16, false, false); // Withdraw Funds
        AddBackground(190, 305, 285, 22, 9350);
        AddTextEntry(191, 305, 285, 20, LabelHueBlue, 1, "");
        AddButton(480, 305, 4014, 4016, 3);

        AddHtmlLocalized(97, 330, 150, 18, 1150196, OrangeColor16, false, false); // Deposit Funds
        AddBackground(190, 330, 285, 22, 9350);
        AddTextEntry(191, 330, 285, 20, LabelHueBlue, 2, "");
        AddButton(480, 330, 4014, 4016, 4);

        AddButton(150, 365, 4005, 4007, 5);
        AddHtmlLocalized(190, 365, 200, 18, 1150192, OrangeColor16, false, false); // ADD TO INVENTORY

        AddButton(150, 390, 4005, 4007, 6);
        AddHtmlLocalized(190, 390, 300, 18, 1150193, OrangeColor16, false, false); // VIEW INVENTORY / REMOVE ITEMS

        AddButton(150, 415, 4005, 4007, 7);
        AddHtmlLocalized(190, 415, 300, 18, 1150194, OrangeColor16, false, false); // SET PRICES AND LIMITS
    }

    /// <summary>Название магазина и имя владельца — шапка всех окон маклера.</summary>
    private void AddShopHeader(CommodityBroker broker, int width)
    {
        if (broker.Plot?.ShopName is { Length: > 0 } shopName)
        {
            AddHtml(10, 37, width, 18, Color(FormatStallName(shopName), BlueColor), false, false);
        }
        else
        {
            AddHtmlLocalized(10, 37, width, 18, 1114513, "#1150314", BlueColor16, false, false); // This Shop Has No Name
        }

        AddHtmlLocalized(10, 55, 260, 18, 1114514, "#1150313", BlueColor16, false, false); // Proprietor:
        AddHtml(280, 55, 260, 18, Color(broker.Name, BlueColor), false, false);
    }

    public static void DisplayTo(Mobile from, CommodityBroker broker)
    {
        if (from == null || broker?.Deleted != false)
        {
            return;
        }

        from.CloseGump<CommodityBrokerGump>();
        from.SendGump(new CommodityBrokerGump(from, broker));
    }

    private void Redraw(Mobile from) => DisplayTo(from, _broker);

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _broker?.Deleted != false || _broker.Plot == null)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 0:
                {
                    return;
                }
            case 1:
                {
                    BazaarInformationGump.DisplayTo(
                        from,
                        0,
                        1150637,
                        new CommodityBrokerGump(from, _broker)
                    );
                    return;
                }
            case 2:
                {
                    if (!_broker.Plot.TrySetShopName(from, info.GetTextEntry(0)))
                    {
                        // Shop names are limited to 40 characters... The text you have entered is not valid.
                        from.SendLocalizedMessage(1150775);
                    }

                    break;
                }
            case 3:
                {
                    if (int.TryParse(info.GetTextEntry(1), out var amount) && amount > 0)
                    {
                        _broker.TryWithdrawFunds(from, amount);
                    }

                    break;
                }
            case 4:
                {
                    if (int.TryParse(info.GetTextEntry(2), out var amount) && amount > 0)
                    {
                        _broker.TryDepositFunds(from, amount);
                    }

                    break;
                }
            case 5:
                {
                    if (_broker.BankBalance < 0)
                    {
                        BazaarInformationGump.DisplayTo(from, 1150623, 1150615);
                        return;
                    }

                    from.Target = new InternalTarget(_broker);
                    CommodityTargetGump.DisplayTo(from, _broker);
                    return;
                }
            case 6:
                {
                    if (_broker.BankBalance < 0)
                    {
                        BazaarInformationGump.DisplayTo(from, 1150623, 1150615);
                        return;
                    }

                    ViewInventoryGump.DisplayTo(from, _broker);
                    return;
                }
            case 7:
                {
                    if (_broker.BankBalance < 0)
                    {
                        BazaarInformationGump.DisplayTo(from, 1150623, 1150615);
                        return;
                    }

                    SetPricesAndLimitsGump.DisplayTo(from, _broker);
                    return;
                }
        }

        Redraw(from);
    }

    /// <summary>Прицел на предмет в рюкзаке: добавляет товар или пополняет уже существующую позицию.</summary>
    public class InternalTarget : Target
    {
        private readonly CommodityBroker _broker;
        private readonly bool _hasPickedCommodity;

        public InternalTarget(CommodityBroker broker) : this(broker, false)
        {
        }

        public InternalTarget(CommodityBroker broker, bool hasPickedCommodity) : base(-1, false, TargetFlags.None)
        {
            _broker = broker;
            _hasPickedCommodity = hasPickedCommodity;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            from.CloseGump<CommodityTargetGump>();

            if (targeted is Item item && (targeted is ICommodity || targeted is CommodityDeed))
            {
                if (item.IsChildOf(from.Backpack))
                {
                    if (item is CommodityDeed { Commodity: null })
                    {
                        // That item is not a commodity that a broker can trade.
                        from.SendLocalizedMessage(1150222);
                    }
                    else if (_broker.CommodityEntries.Count < CommodityBroker.MaxEntries)
                    {
                        if (!_broker.TryAddBrokerEntry(item, from))
                        {
                            _broker.AddInventory(from, item);
                        }
                    }
                    else
                    {
                        from.SendMessage(
                            $"У маклера может быть не больше {CommodityBroker.MaxEntries} товарных позиций одновременно."
                        );
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1054107); // This item must be in your backpack.
                }
            }
            else
            {
                from.SendLocalizedMessage(1150222); // That item is not a commodity that a broker can trade.
            }

            from.Target = new InternalTarget(_broker, true);
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            from.CloseGump<CommodityTargetGump>();

            if (_hasPickedCommodity)
            {
                SetPricesAndLimitsGump.DisplayTo(from, _broker);
            }
            else
            {
                CommodityBrokerGump.DisplayTo(from, _broker);
            }
        }
    }
}

public class CommodityTargetGump : BaseBazaarGump
{
    private CommodityTargetGump(CommodityBroker broker) : base(520, 520, false)
    {
        AddHtmlLocalized(10, 10, 500, 18, 1114513, "#1150636", RedColor16, false, false); // Commodity Broker

        if (broker.Plot?.ShopName is { Length: > 0 } shopName)
        {
            AddHtml(10, 37, 500, 18, Color(FormatStallName(shopName), BlueColor), false, false);
        }
        else
        {
            AddHtmlLocalized(10, 37, 500, 18, 1114513, "#1150314", BlueColor16, false, false); // This Shop Has No Name
        }

        AddHtmlLocalized(10, 55, 260, 18, 1114514, "#1150313", BlueColor16, false, false); // Proprietor:
        AddHtml(280, 55, 260, 18, Color(broker.Name, BlueColor), false, false);

        // Target commodity items or filled commodity deeds in your backpack to add them to the broker's inventory...
        AddHtmlLocalized(10, 100, 500, 400, 1150209, OrangeColor16, false, false);
    }

    public static void DisplayTo(Mobile from, CommodityBroker broker)
    {
        if (from == null || broker?.Deleted != false)
        {
            return;
        }

        from.CloseGump<CommodityTargetGump>();
        from.SendGump(new CommodityTargetGump(broker));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (state.Mobile?.Target is CommodityBrokerGump.InternalTarget)
        {
            Target.Cancel(state.Mobile);
        }
    }
}

public class SetPricesAndLimitsGump : BaseBazaarGump
{
    private const int PerPage = 10;

    private readonly CommodityBroker _broker;
    private readonly int _index;
    private readonly int _page;

    private SetPricesAndLimitsGump(CommodityBroker broker, int index, int page) : base(520, 520)
    {
        _broker = broker;
        _index = index;
        _page = index > -1 ? (index <= 0 ? 0 : index / PerPage) : page;

        AddHtmlLocalized(10, 10, 500, 18, 1114513, "#1150636", RedColor16, false, false); // Commodity Broker

        AddButton(150, 40, 4005, 4007, 1);
        AddHtmlLocalized(190, 40, 200, 18, 1150392, OrangeColor16, false, false); // INFORMATION

        AddHtmlLocalized(100, 65, 100, 18, 1150140, OrangeColor16, false, false); // ITEM
        AddHtmlLocalized(170, 65, 100, 18, 1150204, OrangeColor16, false, false); // BUY AT
        AddHtmlLocalized(230, 65, 100, 18, 1150645, OrangeColor16, false, false); // BUY LMT
        AddHtmlLocalized(315, 65, 100, 18, 1150205, OrangeColor16, false, false); // SELL AT
        AddHtmlLocalized(380, 65, 100, 18, 1150646, OrangeColor16, false, false); // SELL LMT
        AddHtmlLocalized(475, 65, 100, 18, 1150647, OrangeColor16, false, false); // EDIT

        var y = 85;
        var start = _page * PerPage;

        for (var i = start; i < broker.CommodityEntries.Count && i < start + PerPage; i++)
        {
            var entry = broker.CommodityEntries[i];
            var highlighted = i == index;
            var color = highlighted ? YellowColor : OrangeColor;
            var color16 = highlighted ? YellowColor16 : OrangeColor16;

            AddHtmlLocalized(1, y, 130, 18, 1114514, $"#{entry.Label}", color16, false, false); // <DIV ALIGN=RIGHT>~1_TOKEN~</DIV>
            AddHtml(170, y, 45, 18, Color(AlignRight(FormatAmt(entry.BuyPricePer)), color), false, false);
            AddHtml(230, y, 80, 18, Color(AlignRight(FormatAmt(entry.BuyLimit)), color), false, false);
            AddHtml(315, y, 45, 18, Color(AlignRight(FormatAmt(entry.SellPricePer)), color), false, false);
            AddHtml(380, y, 80, 18, Color(AlignRight(FormatAmt(entry.SellLimit)), color), false, false);

            AddButton(475, y, 4014, 4016, 2 + i);

            y += 22;
        }

        if (_page > 0)
        {
            AddButton(162, 321, 4014, 4016, 400);
        }

        if (broker.CommodityEntries.Count - start > PerPage)
        {
            AddButton(390, 321, 4005, 4007, 401);
        }

        AddHtmlLocalized(124, 345, 100, 18, 1150204, BlueColor16, false, false); // BUY AT
        AddBackground(190, 345, 300, 18, 9350);
        AddTextEntry(191, 344, 320, 22, LabelHueBlue, 0, "");

        AddHtmlLocalized(114, 370, 100, 18, 1150645, BlueColor16, false, false); // BUY LMT
        AddBackground(190, 370, 300, 18, 9350);
        AddTextEntry(191, 369, 320, 22, LabelHueBlue, 1, "");

        AddHtmlLocalized(118, 395, 100, 18, 1150205, BlueColor16, false, false); // SELL AT
        AddBackground(190, 395, 300, 18, 9350);
        AddTextEntry(191, 394, 320, 22, LabelHueBlue, 2, "");

        AddHtmlLocalized(108, 420, 100, 18, 1150646, BlueColor16, false, false); // SELL LMT
        AddBackground(190, 420, 300, 18, 9350);
        AddTextEntry(191, 419, 320, 22, LabelHueBlue, 3, "");

        AddButton(140, 450, 4005, 4007, 500);
        AddHtmlLocalized(190, 450, 200, 18, 1150232, RedColor16, false, false); // SET PRICES

        AddButton(10, 490, 4014, 4016, 501);
        AddHtmlLocalized(50, 490, 100, 18, 1149777, BlueColor16, false, false); // MAIN MENU
    }

    public static void DisplayTo(Mobile from, CommodityBroker broker, int index = -1, int page = 0)
    {
        if (from == null || broker?.Deleted != false)
        {
            return;
        }

        from.CloseGump<SetPricesAndLimitsGump>();
        from.SendGump(new SetPricesAndLimitsGump(broker, index, page));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _broker?.Deleted != false)
        {
            return;
        }

        var index = _index;
        var page = _page;

        switch (info.ButtonID)
        {
            case 0:
                {
                    return;
                }
            case 1:
                {
                    BazaarInformationGump.DisplayTo(from, 0, 1150644, new SetPricesAndLimitsGump(_broker, index, page));
                    return;
                }
            case 400:
                {
                    DisplayTo(from, _broker, -1, page - 1);
                    return;
                }
            case 401:
                {
                    DisplayTo(from, _broker, -1, page + 1);
                    return;
                }
            case 500:
                {
                    SetPrices(from, info, index);
                    break;
                }
            case 501:
                {
                    CommodityBrokerGump.DisplayTo(from, _broker);
                    return;
                }
            default:
                {
                    var id = info.ButtonID - 2;

                    if (id >= 0 && id < _broker.CommodityEntries.Count)
                    {
                        index = id;
                    }

                    break;
                }
        }

        DisplayTo(from, _broker, index, page);
    }

    private void SetPrices(Mobile from, in RelayInfo info, int index)
    {
        if (index < 0 || index >= _broker.CommodityEntries.Count)
        {
            // You did not select a commodity.
            from.SendLocalizedMessage(1150642);
            return;
        }

        var entry = _broker.CommodityEntries[index];

        var buyAt = ParseOr(info.GetTextEntry(0), entry.BuyPricePer);
        var buyLimit = ParseOr(info.GetTextEntry(1), entry.BuyLimit);
        var sellAt = ParseOr(info.GetTextEntry(2), entry.SellPricePer);
        var sellLimit = ParseOr(info.GetTextEntry(3), entry.SellLimit);

        if (buyLimit is < 0 or > 60000 || sellLimit is < 0 or > 60000)
        {
            // You have entered an invalid numeric value. Negative values are not allowed...
            from.SendLocalizedMessage(1150776);
            return;
        }

        entry.BuyPricePer = buyAt;
        entry.BuyLimit = buyLimit;
        entry.SellPricePer = sellAt;
        entry.SellLimit = sellLimit;
    }

    private static int ParseOr(string text, int fallback) => int.TryParse(text, out var value) ? value : fallback;
}

public class ViewInventoryGump : BaseBazaarGump
{
    private const int PerPage = 10;

    private readonly CommodityBroker _broker;
    private readonly int _index;
    private readonly int _page;

    private ViewInventoryGump(CommodityBroker broker, int index, int page) : base(660, 520)
    {
        _broker = broker;
        _index = index;
        _page = index > -1 ? (index <= 0 ? 0 : index / PerPage) : page;

        AddHtmlLocalized(10, 10, 640, 18, 1114513, "#1150636", RedColor16, false, false); // Commodity Broker
        AddHtmlLocalized(10, 40, 640, 60, 1150641, OrangeColor16, false, false); // Click the button next to the commodity...

        AddHtmlLocalized(235, 125, 150, 18, 1150140, OrangeColor16, false, false); // ITEM
        AddHtmlLocalized(360, 125, 150, 18, 1150201, OrangeColor16, false, false); // IN STOCK
        AddHtmlLocalized(440, 125, 150, 18, 1150639, OrangeColor16, false, false); // SELECT
        AddHtmlLocalized(540, 125, 150, 18, 1150855, OrangeColor16, false, false); // DOWN
        AddHtmlLocalized(600, 125, 150, 18, 1150856, OrangeColor16, false, false); // UP

        var y = 150;
        var start = _page * PerPage;

        for (var i = start; i < broker.CommodityEntries.Count && i < start + PerPage; i++)
        {
            var entry = broker.CommodityEntries[i];
            var color = i == index ? YellowColor : OrangeColor;
            var color16 = i == index ? YellowColor16 : OrangeColor16;

            AddHtmlLocalized(1, y, 260, 18, 1114514, $"#{entry.Label}", color16, false, false); // <DIV ALIGN=RIGHT>~1_TOKEN~</DIV>
            AddHtml(360, y, 55, 18, Color(AlignRight(FormatAmt(entry.Stock)), color), false, false);

            AddButton(440, y, 4014, 4016, 1000 + i);

            if (i > 0)
            {
                AddButton(600, y, 250, 251, 1500 + i);
            }

            if (i < broker.CommodityEntries.Count - 1)
            {
                AddButton(540, y, 252, 253, 2000 + i);
            }

            y += 20;
        }

        if (_page > 0)
        {
            AddButton(162, 350, 4014, 4016, 400);
        }

        if (broker.CommodityEntries.Count - start > PerPage)
        {
            AddButton(390, 350, 4005, 4007, 401);
        }

        AddHtmlLocalized(160, 415, 150, 18, 1150202, OrangeColor16, false, false); // WITHDRAW
        AddBackground(250, 415, 360, 22, 9350);
        AddTextEntry(251, 415, 358, 20, LabelHueBlue, 0, "");
        AddButton(620, 415, 4014, 4016, 1);

        if (index > -1)
        {
            AddHtml(50, 460, 200, 18, Color("Убрать из ассортимента", RedColor), false, false);
            AddButton(10, 460, 4020, 4022, 3);
        }

        AddButton(10, 490, 4014, 4016, 501);
        AddHtmlLocalized(50, 490, 100, 18, 1149777, BlueColor16, false, false); // MAIN MENU
    }

    public static void DisplayTo(Mobile from, CommodityBroker broker, int index = -1, int page = 0)
    {
        if (from == null || broker?.Deleted != false)
        {
            return;
        }

        from.CloseGump<ViewInventoryGump>();
        from.SendGump(new ViewInventoryGump(broker, index, page));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _broker?.Deleted != false)
        {
            return;
        }

        var index = _index;
        var page = _page;

        switch (info.ButtonID)
        {
            case 0:
                {
                    return;
                }
            case 1:
                {
                    Withdraw(from, info, index);
                    break;
                }
            case 2:
                {
                    CommodityBrokerGump.DisplayTo(from, _broker);
                    return;
                }
            case 3:
                {
                    if (index < 0 || index >= _broker.CommodityEntries.Count)
                    {
                        from.SendLocalizedMessage(1150642); // You did not select a commodity.
                        break;
                    }

                    ConfirmRemoveEntryGump.DisplayTo(from, _broker, index);
                    return;
                }
            case 400:
                {
                    DisplayTo(from, _broker, -1, page - 1);
                    return;
                }
            case 401:
                {
                    DisplayTo(from, _broker, -1, page + 1);
                    return;
                }
            case 501:
                {
                    CommodityBrokerGump.DisplayTo(from, _broker);
                    return;
                }
            default:
                {
                    if (info.ButtonID < 1500)
                    {
                        var id = info.ButtonID - 1000;

                        if (id >= 0 && id < _broker.CommodityEntries.Count)
                        {
                            DisplayTo(from, _broker, id, page);
                            return;
                        }

                        break;
                    }

                    MoveEntry(info.ButtonID);
                    break;
                }
        }

        DisplayTo(from, _broker, index, page);
    }

    private void Withdraw(Mobile from, in RelayInfo info, int index)
    {
        if (index < 0 || index >= _broker.CommodityEntries.Count)
        {
            from.SendLocalizedMessage(1150642); // You did not select a commodity.
            return;
        }

        var entry = _broker.CommodityEntries[index];

        if (!int.TryParse(info.GetTextEntry(0), out var amount) || amount <= 0 || amount > entry.Stock)
        {
            // You have entered an invalid value, or a non-numeric value. Please try again.
            from.SendLocalizedMessage(1150215);
            return;
        }

        _broker.WithdrawInventory(from, amount, entry);
    }

    /// <summary>Переставляет позицию вверх/вниз: кнопки 1500+i (вверх) и 2000+i (вниз).</summary>
    private void MoveEntry(int buttonId)
    {
        var up = buttonId < 2000;
        var id = buttonId - (up ? 1500 : 2000);

        if (up ? id <= 0 : id >= _broker.CommodityEntries.Count - 1)
        {
            return;
        }

        if (id < 0 || id >= _broker.CommodityEntries.Count)
        {
            return;
        }

        var entry = _broker.CommodityEntries[id];
        _broker.CommodityEntries.RemoveAt(id);
        _broker.CommodityEntries.Insert(up ? id - 1 : id + 1, entry);
    }
}

public class CommodityInventoryGump : BaseBazaarGump
{
    private readonly CommodityBroker _broker;
    private readonly List<CommodityBrokerEntry> _entries;
    private readonly bool _buy;
    private readonly int _page;
    private readonly int _index;

    private CommodityInventoryGump(CommodityBroker broker, int index, bool buy, int page, int cliloc) : base(655, 520)
    {
        _broker = broker;
        _entries = broker.CommodityEntries;
        _buy = buy;
        _page = page;
        _index = index;

        AddHtmlLocalized(10, 10, 640, 18, 1114513, "#1150636", RedColor16, false, false); // Commodity Broker

        if (broker.Plot?.ShopName is { Length: > 0 } shopName)
        {
            AddHtml(10, 37, 640, 18, Color(FormatStallName(shopName), BlueColor), false, false);
        }
        else
        {
            AddHtmlLocalized(10, 37, 640, 18, 1114513, "#1150314", BlueColor16, false, false); // This Shop Has No Name
        }

        AddHtmlLocalized(10, 55, 310, 18, 1114514, "#1150313", BlueColor16, false, false); // Proprietor:
        AddHtml(330, 55, 320, 18, Color(broker.Name, BlueColor), false, false);

        if (cliloc != 0)
        {
            if (cliloc == 1150654)
            {
                // This broker trades in commodities...
                AddHtmlLocalized(10, 127, 640, 354, 1150654, OrangeColor16, true, true);
            }
            else
            {
                AddHtmlLocalized(10, 127, 640, 354, 1114513, $"#{cliloc}", OrangeColor16, false, false);
            }

            AddButton(10, 490, 0xFAE, 0xFAF, 999);
            AddHtmlLocalized(50, 490, 210, 20, 1149777, BlueColor16, false, false); // MAIN MENU
            return;
        }

        AddButton(190, 91, 4005, 4007, 1);
        AddHtmlLocalized(230, 91, 420, 22, 1150392, OrangeColor16, false, false); // INFORMATION

        AddHtmlLocalized(10, 124, 54, 20, 1150656, OrangeColor16, false, false); // BUY
        AddHtmlLocalized(70, 124, 54, 20, 1150659, OrangeColor16, false, false); // PRICE
        AddHtmlLocalized(130, 124, 109, 20, 1150658, OrangeColor16, false, false); // LIMIT
        AddHtmlLocalized(245, 124, 164, 20, 1114513, "#1150655", OrangeColor16, false, false); // COMMODITY
        AddHtmlLocalized(415, 124, 54, 20, 1114514, "#1150659", OrangeColor16, false, false); // PRICE
        AddHtmlLocalized(475, 124, 109, 20, 1114514, "#1150658", OrangeColor16, false, false); // LIMIT
        AddHtmlLocalized(590, 124, 54, 20, 1114514, "#1150657", OrangeColor16, false, false); // SELL

        if (_entries.Count == 0)
        {
            // There are no commodities in this broker's inventory.
            AddHtmlLocalized(10, 153, 640, 20, 1114513, "#1150638", OrangeColor16, false, false);
            return;
        }

        var y = 153;
        const int perPage = 10;
        var start = _page * perPage;

        for (var i = start; i < _entries.Count && i < start + perPage; i++)
        {
            var entry = _entries[i];

            if (entry.SellPricePer > 0)
            {
                AddHtml(70, y, 54, 20, Color(FormatAmt(entry.SellPricePer), OrangeColor), false, false);
            }
            else
            {
                AddHtmlLocalized(70, y, 54, 20, 1150397, OrangeColor16, false, false); // N/A
            }

            if (entry.SellPricePer > 0 && entry.SellLimit > 0)
            {
                AddHtml(130, y, 109, 20, Color(FormatAmt(entry.ActualSellLimit), OrangeColor), false, false);
            }

            AddHtmlLocalized(200, y, 164, 20, 1114514, $"#{entry.Label}", OrangeColor16, false, false); // <DIV ALIGN=RIGHT>~1_TOKEN~</DIV>
            AddTooltip(entry.Label);

            if (entry.BuyPricePer > 0)
            {
                AddHtml(415, y, 54, 20, Color(AlignRight(FormatAmt(entry.BuyPricePer)), OrangeColor), false, false);
            }
            else
            {
                AddHtmlLocalized(415, y, 54, 20, 1150397, OrangeColor16, false, false); // N/A
            }

            if (entry.BuyPricePer > 0 && entry.BuyLimit > 0)
            {
                AddHtml(475, y, 109, 20, Color(AlignRight(FormatAmt(entry.ActualBuyLimit)), OrangeColor), false, false);
            }

            if (entry.PlayerCanBuy(0))
            {
                AddRadio(10, y, 0xFA5, 0xFA6, false, 2 + i);
            }

            if (entry.PlayerCanSell(0))
            {
                AddRadio(590, y, 0xFAE, 0xFAF, false, 102 + i);
            }

            y += 20;
        }

        if (_page > 0)
        {
            AddButton(60, 362, 0xFAE, 0xFAF, 400);
            AddHtmlLocalized(100, 362, 230, 40, 1044044, OrangeColor16, false, false); // PREV PAGE
        }

        if (_entries.Count - start > perPage)
        {
            AddButton(570, 362, 0xFA5, 0xFA6, 401);
            AddHtmlLocalized(330, 362, 230, 40, 1114514, "#1044045", OrangeColor16, false, false); // NEXT PAGE
        }

        AddHtmlLocalized(10, 421, 210, 22, 1114514, "#1150662", BlueColor16, false, false); // QUANTITY
        AddBackground(230, 421, 420, 22, 0x2486);
        AddTextEntry(232, 423, 416, 18, LabelHueBlue, 0, "");

        AddButton(190, 445, 4005, 4007, 500);
        AddHtmlLocalized(230, 445, 420, 22, 1077761, OrangeColor16, false, false); // TRADE
    }

    public static void DisplayTo(
        Mobile from, CommodityBroker broker, int index = -1, bool buy = true, int page = 0, int cliloc = 0
    )
    {
        if (from == null || broker?.Deleted != false)
        {
            return;
        }

        from.CloseGump<CommodityInventoryGump>();
        from.SendGump(new CommodityInventoryGump(broker, index, buy, page, cliloc));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _broker?.Deleted != false)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 0:
                {
                    return;
                }
            case 999:
                {
                    DisplayTo(from, _broker, _index, _buy, _page);
                    return;
                }
            case 1:
                {
                    DisplayTo(from, _broker, _index, _buy, _page, 1150654);
                    return;
                }
            case 400:
                {
                    DisplayTo(from, _broker, -1, _buy, _page - 1);
                    return;
                }
            case 401:
                {
                    DisplayTo(from, _broker, -1, _buy, _page + 1);
                    return;
                }
            case 500:
                {
                    Trade(from, info);
                    return;
                }
        }
    }

    private void Trade(Mobile from, in RelayInfo info)
    {
        var buy = _buy;
        var index = _index;

        for (var i = 0; i < _entries.Count; i++)
        {
            if (info.IsSwitched(2 + i) && _broker.CommodityEntries.Contains(_entries[i]))
            {
                buy = true;
                index = i;
            }
            else if (info.IsSwitched(102 + i) && _broker.CommodityEntries.Contains(_entries[i]))
            {
                buy = false;
                index = i;
            }
        }

        if (index < 0 || index >= _entries.Count)
        {
            DisplayTo(from, _broker, index, buy, _page, 1150642); // You did not select a commodity.
            return;
        }

        var entry = _entries[index];

        if (!_broker.CommodityEntries.Contains(entry))
        {
            // Transaction no longer available
            DisplayTo(from, _broker, index, buy, _page, 1150244);
            return;
        }

        if (!int.TryParse(info.GetTextEntry(0), out var amount) || amount <= 0)
        {
            // You have entered an invalid value, or a non-numeric value. Please try again.
            DisplayTo(from, _broker, index, buy, _page, 1150215);
            return;
        }

        if (buy && Banker.GetBalance(from) < entry.SellPricePer * amount)
        {
            // You do not have the funds needed to make this trade...
            DisplayTo(from, _broker, index, buy, _page, 1150252);
            return;
        }

        if (!buy && !_broker.SellCommodityControl(from, entry, amount))
        {
            // You do not have the requested amount of that commodity...
            DisplayTo(from, _broker, index, buy, _page, 1150667);
            return;
        }

        if (buy && entry.PlayerCanBuy(amount))
        {
            ConfirmBuyCommodityGump.DisplayTo(
                from,
                _broker,
                amount,
                entry,
                true,
                new CommodityInventoryGump(_broker, index, buy, _page, 0)
            );
            return;
        }

        if (!buy && entry.PlayerCanSell(amount))
        {
            ConfirmBuyCommodityGump.DisplayTo(
                from,
                _broker,
                amount,
                entry,
                false,
                new CommodityInventoryGump(_broker, index, buy, _page, 0)
            );
            return;
        }

        // You have entered an invalid value, or a non-numeric value. Please try again.
        DisplayTo(from, _broker, index, buy, _page, 1150215);
    }
}

public class ConfirmRemoveEntryGump : BaseBazaarGump
{
    private readonly CommodityBroker _broker;
    private readonly CommodityBrokerEntry _entry;
    private readonly int _index;

    private ConfirmRemoveEntryGump(CommodityBroker broker, int index) : base(520, 260)
    {
        _broker = broker;
        _index = index;
        _entry = index >= 0 && index < broker.CommodityEntries.Count ? broker.CommodityEntries[index] : null;

        AddHtml(10, 20, 500, 22, Color("Удаление товарной позиции", RedColor), false, false);
        AddHtml(
            10,
            60,
            500,
            80,
            "Вы уверены, что хотите убрать эту позицию из ассортимента маклера? " +
            "Неиспользованный запас будет переведён в банковские квитанции и отправлен вам в рюкзак.",
            false,
            false
        );

        AddButton(175, 190, 4005, 4007, 1);
        AddHtml(215, 190, 150, 18, Color("ПОДТВЕРДИТЬ", OrangeColor), false, false);

        AddButton(10, 190, 4014, 4016, 2);
        AddHtml(50, 190, 150, 18, Color("ОТМЕНА", BlueColor), false, false);
    }

    public static void DisplayTo(Mobile from, CommodityBroker broker, int index)
    {
        if (from == null || broker?.Deleted != false)
        {
            return;
        }

        from.CloseGump<ConfirmRemoveEntryGump>();
        from.SendGump(new ConfirmRemoveEntryGump(broker, index));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _broker?.Deleted != false)
        {
            return;
        }

        if (info.ButtonID == 1)
        {
            if (_entry != null && _broker.CommodityEntries.Contains(_entry))
            {
                _broker.RemoveEntry(from, _entry);
            }

            ViewInventoryGump.DisplayTo(from, _broker);
            return;
        }

        if (info.ButtonID == 2)
        {
            ViewInventoryGump.DisplayTo(from, _broker, _index);
        }
    }
}

public class ConfirmBuyCommodityGump : BaseBazaarGump
{
    private readonly Gump _returnGump;
    private readonly CommodityBroker _broker;
    private readonly CommodityBrokerEntry _entry;
    private readonly int _amount;

    private ConfirmBuyCommodityGump(
        CommodityBroker broker, int amount, CommodityBrokerEntry entry, bool buy, Gump returnGump
    ) : base(660, 520)
    {
        _broker = broker;
        _amount = amount;
        _entry = entry;
        _returnGump = returnGump;

        AddHtmlLocalized(10, 10, 640, 18, 1114513, "#1150636", RedColor16, false, false); // Commodity Broker

        if (broker.Plot?.ShopName is { Length: > 0 } shopName)
        {
            AddHtml(10, 37, 640, 18, Color(FormatStallName(shopName), BlueColor), false, false);
        }
        else
        {
            AddHtmlLocalized(10, 37, 640, 18, 1114513, "#1150314", BlueColor16, false, false); // This Shop Has No Name
        }

        AddHtmlLocalized(10, 55, 310, 18, 1114514, "#1150313", BlueColor16, false, false); // Proprietor:
        AddHtml(330, 55, 320, 18, Color(broker.Name, BlueColor), false, false);

        // Please review the details of this transaction...
        AddHtmlLocalized(10, 127, 640, 64, 1114513, "#1150666", RedColor16, false, false);

        if (buy)
        {
            var cost = entry.SellPricePer * amount;

            AddHtmlLocalized(10, 193, 210, 18, 1114514, "#1150144", OrangeColor16, false, false); // You are BUYING:
            AddHtmlLocalized(230, 193, 420, 18, broker.GetLabelID(entry), DarkGreenColor16, false, false);

            AddHtmlLocalized(10, 213, 210, 18, 1114514, "#1150152", OrangeColor16, false, false); // Quantity to Buy:
            AddHtml(230, 213, 420, 18, Color($"{amount}", DarkGreenColor), false, false);

            AddHtmlLocalized(10, 233, 210, 18, 1114514, "#1150246", OrangeColor16, false, false); // Total Cost:
            AddHtml(230, 233, 420, 18, Color(FormatAmt(cost), DarkGreenColor), false, false);

            AddHtmlLocalized(230, 253, 420, 22, 1150143, OrangeColor16, false, false); // TRADE
            AddButton(190, 253, 0xFA5, 0xFA6, 1);
        }
        else
        {
            var cost = entry.BuyPricePer * amount;

            AddHtmlLocalized(10, 193, 210, 18, 1114514, "#1150145", OrangeColor16, false, false); // You are SELLING:
            AddHtmlLocalized(230, 193, 420, 18, broker.GetLabelID(entry), DarkGreenColor16, false, false);

            AddHtmlLocalized(10, 213, 210, 18, 1114514, "#1150153", OrangeColor16, false, false); // Quantity to Sell:
            AddHtml(230, 213, 420, 18, Color($"{amount}", DarkGreenColor), false, false);

            AddHtmlLocalized(10, 233, 210, 18, 1114514, "#1150251", OrangeColor16, false, false); // Gold You Will Receive:
            AddHtml(230, 233, 420, 18, Color(FormatAmt(cost), DarkGreenColor), false, false);

            AddHtmlLocalized(230, 253, 420, 22, 1150143, OrangeColor16, false, false); // TRADE
            AddButton(190, 253, 0xFA5, 0xFA6, 2);
        }

        AddButton(10, 490, 0xFAE, 0xFAF, 3);
        AddHtmlLocalized(50, 490, 210, 20, 1149777, BlueColor16, false, false); // MAIN MENU
    }

    public static void DisplayTo(
        Mobile from, CommodityBroker broker, int amount, CommodityBrokerEntry entry, bool buy, Gump returnGump
    )
    {
        if (from == null || broker?.Deleted != false || entry == null)
        {
            return;
        }

        from.CloseGump<ConfirmBuyCommodityGump>();
        from.SendGump(new ConfirmBuyCommodityGump(broker, amount, entry, buy, returnGump));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _broker?.Deleted != false)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                {
                    _broker.TryBuyCommodity(from, _entry, _amount);
                    break;
                }
            case 2:
                {
                    _broker.TrySellCommodity(from, _entry, _amount);
                    break;
                }
            case 3:
                {
                    if (_returnGump != null)
                    {
                        from.SendGump(_returnGump);
                    }

                    break;
                }
        }
    }
}
