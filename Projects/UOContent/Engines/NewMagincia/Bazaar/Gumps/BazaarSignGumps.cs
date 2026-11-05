/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Gumps/BizaarSignGump.cs).
///
///     Окна вывески прилавка: аренда, мой прилавок, подтверждения, ставки, «ответная» ставка, справка и руна
///     возврата. Подсистема разбита на три файла так же, как в ServUO: вывеска
///     (<c>BazaarSignGumps.cs</c>), товарный маклер (<c>CommodityBrokerGumps.cs</c>) и животный маклер
///     (<c>PetBrokerGumps.cs</c>), плюс общая основа <c>BaseBazaarGump.cs</c>.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             Все окна — обычные <see cref="Gump" /> со статическим <c>DisplayTo(Mobile, …)</c>; игрок передаётся
///             в конструктор, а не подставляется после него. <c>OnResponse</c> получает <c>in RelayInfo</c>, поля
///             ввода читаются через <c>info.GetTextEntry(id)</c> (<c>TextRelay</c>/<c>TextEntries</c> в ModernUO нет).
///         </item>
///         <item>
///             <c>Convert.ToInt32</c> заменён на <c>int.TryParse</c>: исключение на каждую опечатку в поле ввода —
///             лишняя нагрузка на игровой цикл, а поведение («нечисло → 0») то же.
///         </item>
///         <item>
///             <c>Console.WriteLine("Error")</c> в <c>StallBidGump</c> удалён: писать в консоль из игрового кода
///             запрещено, окно просто перерисовывается.
///         </item>
///         <item>
///             Логика <c>ConfirmBidGump</c> разложена на три ветки (та же ставка, смена прилавка, ставка «на первый
///             свободный»); наборы cliloc-ов и аргументов сохранены. В ServUO ветка «не хватает золота на
///             повышение» заполняла <c>args2</c> вместо <c>args1</c>, из-за чего cliloc 1150571 показывался без
///             аргументов — здесь аргументы идут в своё поле.
///         </item>
///         <item>
///             <c>ShopRecallRuneGump</c> в ServUO помечал руну текущей позицией игрока, хотя продаёт её «к
///             прилавку»; здесь руна помечается точкой прилавка.
///         </item>
///         <item>Собственные подписи переведены на русский, идентификаторы cliloc не тронуты.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.NewMagincia;

public class StallLeasingGump : BaseBazaarGump
{
    private readonly Mobile _from;
    private readonly MaginciaBazaarPlot _plot;

    private StallLeasingGump(Mobile from, MaginciaBazaarPlot plot) : base(520, 520)
    {
        _from = from;
        _plot = plot;

        AddHtmlLocalized(10, 10, 500, 18, 1114513, "#1150385", RedColor16, false, false); // New Magincia Bazaar
        AddHtmlLocalized(10, 48, 500, 18, 1114513, "#1150386", RedColor16, false, false); // Stall Leasing

        AddButton(225, 95, 4005, 4007, 1);
        AddHtmlLocalized(265, 95, 245, 22, 1150392, OrangeColor16, false, false); // INFORMATION

        AddHtmlLocalized(10, 139, 245, 20, 1114514, "#1150534", RedColor16, false, false); // This Stall:
        AddHtmlLocalized(265, 139, 245, 20, 1150541, plot.PlotDef.ID, BlueColor16, false, false); // Stall ~1_NAME

        AddHtmlLocalized(10, 159, 245, 20, 1114514, "#1150536", RedColor16, false, false); // Current Tenant:

        if (plot.Owner == null)
        {
            AddHtmlLocalized(265, 159, 245, 20, 1150542, BlueColor16, false, false); // Stall is Not Occupied
        }
        else if (plot.IsOwner(from))
        {
            AddHtmlLocalized(265, 159, 245, 20, 1150539, BlueColor16, false, false); // You are leasing this stall
        }
        else
        {
            AddHtmlLocalized(265, 159, 245, 20, 1150541, plot.Owner.Name, BlueColor16, false, false); // ~1_TOKEN~
        }

        AddHtmlLocalized(10, 179, 245, 20, 1114514, "#1062509", RedColor16, false, false); // Shop Name:

        if (plot.ShopName is { Length: > 0 })
        {
            AddHtmlLocalized(265, 179, 245, 20, 1150312, plot.ShopName, BlueColor16, false, false); // "~1_NAME~"
        }
        else
        {
            AddHtmlLocalized(265, 179, 245, 20, 1150314, BlueColor16, false, false); // This Shop Has No Name
        }

        AddHtmlLocalized(10, 199, 245, 20, 1114514, "#1150388", RedColor16, false, false); // Lease Duration:
        AddHtmlLocalized(
            265,
            199,
            245,
            20,
            1150543,
            $"{(int)MaginciaBazaar.GetLongAuctionTime.TotalDays}",
            BlueColor16,
            false,
            false
        ); // ~1_DAYS~ Days

        AddButton(225, 237, 4005, 4007, 2);
        AddHtmlLocalized(265, 237, 200, 18, 1150555, OrangeColor16, false, false); // SEE TOP BIDS

        var isOwner = plot.IsOwner(from);

        AddButton(225, 261, 4005, 4007, 3);
        AddHtmlLocalized(265, 261, 200, 18, 1150557, isOwner ? GrayColor16 : OrangeColor16, false, false); // MY STALL LEASE

        AddButton(225, 285, 4005, 4007, 4);
        AddHtmlLocalized(265, 285, 200, 18, 1150556, OrangeColor16, false, false); // MY STALL BID

        AddButton(225, 309, 4005, 4007, 5);
        AddHtmlLocalized(265, 309, 200, 18, 1150540, isOwner ? GrayColor16 : OrangeColor16, false, false); // MY BID MATCHING
    }

    public static void DisplayTo(Mobile from, MaginciaBazaarPlot plot)
    {
        if (from == null || plot == null)
        {
            return;
        }

        from.CloseGump<StallLeasingGump>();
        from.SendGump(new StallLeasingGump(from, plot));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _plot == null)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                {
                    BazaarInformationGump.DisplayTo(from, 1150386, 1150391, new StallLeasingGump(from, _plot));
                    break;
                }
            case 2:
                {
                    TopBidsGump.DisplayTo(from, _plot);
                    break;
                }
            case 3:
                {
                    if (_plot.IsOwner(from))
                    {
                        MyStallLeaseGump.DisplayTo(from, _plot);
                    }
                    else
                    {
                        // You are currently viewing a stall that you are not leasing...
                        from.SendLocalizedMessage(1150685);
                    }

                    break;
                }
            case 4:
                {
                    StallBidGump.DisplayTo(from, _plot);
                    break;
                }
            case 5:
                {
                    if (_plot.IsOwner(from))
                    {
                        MatchBidGump.DisplayTo(from, _plot);
                    }
                    else
                    {
                        from.SendLocalizedMessage(1150685);
                    }

                    break;
                }
        }
    }
}

public class MyStallLeaseGump : BaseBazaarGump
{
    private readonly MaginciaBazaarPlot _plot;

    private MyStallLeaseGump(MaginciaBazaarPlot plot) : base(520, 520)
    {
        _plot = plot;

        AddHtmlLocalized(195, 5, 150, 18, 1150385, RedColor16, false, false); // New Magincia Bazaar
        AddHtmlLocalized(217, 40, 150, 18, 1150386, RedColor16, false, false); // Stall Leasing

        AddHtmlLocalized(138, 135, 150, 18, 1150387, RedColor16, false, false); // Your Stall:
        AddHtmlLocalized(210, 135, 150, 18, 1150530, plot.PlotDef.ID, BlueColor16, false, false); // Stall ~1_NAME

        AddHtmlLocalized(101, 160, 150, 18, 1150388, RedColor16, false, false); // Lease Duration:
        AddHtmlLocalized(
            210,
            160,
            150,
            18,
            1150543,
            $"{(int)MaginciaBazaar.GetLongAuctionTime.TotalDays}",
            BlueColor16,
            false,
            false
        ); // ~1_DAYS~ Days

        if (plot.Merchant == null)
        {
            AddButton(175, 220, 4005, 4007, 1);
        }

        AddHtmlLocalized(210, 220, 200, 18, 1150686, plot.Merchant == null ? OrangeColor16 : GrayColor16, false, false); // HIRE ANIMAL BROKER

        if (plot.Merchant == null)
        {
            AddButton(175, 244, 4005, 4007, 2);
        }

        AddHtmlLocalized(210, 244, 200, 18, 1150687, plot.Merchant == null ? OrangeColor16 : GrayColor16, false, false); // HIRE COMMODITY BROKER

        if (plot.Merchant != null)
        {
            AddButton(175, 268, 4005, 4007, 3);
        }

        AddHtmlLocalized(210, 268, 200, 18, 1150688, plot.Merchant != null ? OrangeColor16 : GrayColor16, false, false); // FIRE BROKER

        AddButton(175, 292, 4005, 4007, 4);
        AddHtmlLocalized(210, 292, 200, 18, 1150689, OrangeColor16, false, false); // ABANDON LEASE

        AddButton(10, 490, 4014, 4016, 5);
        AddHtmlLocalized(50, 490, 200, 18, 1149777, BlueColor16, false, false); // MAIN MENU
    }

    public static void DisplayTo(Mobile from, MaginciaBazaarPlot plot)
    {
        if (from == null || plot == null)
        {
            return;
        }

        from.CloseGump<MyStallLeaseGump>();
        from.SendGump(new MyStallLeaseGump(plot));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _plot == null || !_plot.IsOwner(from))
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                {
                    if (_plot.Merchant != null)
                    {
                        break;
                    }

                    if (_plot.HasTempMulti())
                    {
                        ConfirmAddMultiGump.DisplayTo(from, _plot, false);
                    }
                    else
                    {
                        HireBrokerGump.DisplayTo(from, _plot, false);
                    }

                    break;
                }
            case 2:
                {
                    if (_plot.Merchant != null)
                    {
                        break;
                    }

                    if (_plot.HasTempMulti())
                    {
                        ConfirmAddMultiGump.DisplayTo(from, _plot, true);
                    }
                    else
                    {
                        HireBrokerGump.DisplayTo(from, _plot, true);
                    }

                    break;
                }
            case 3:
                {
                    if (_plot.Merchant != null)
                    {
                        ConfirmFireBrokerGump.DisplayTo(from, _plot);
                    }

                    break;
                }
            case 4:
                {
                    ConfirmAbandonLeaseGump.DisplayTo(from, _plot);
                    break;
                }
            case 5:
                {
                    StallLeasingGump.DisplayTo(from, _plot);
                    break;
                }
        }
    }
}

public class ConfirmAbandonLeaseGump : BaseBazaarGump
{
    private readonly MaginciaBazaarPlot _plot;

    private ConfirmAbandonLeaseGump(MaginciaBazaarPlot plot) : base(520, 520)
    {
        _plot = plot;

        AddHtmlLocalized(195, 5, 150, 18, 1150385, RedColor16, false, false); // New Magincia Bazaar
        AddHtmlLocalized(217, 40, 150, 18, 1150386, RedColor16, false, false); // Stall Leasing

        AddHtmlLocalized(10, 100, 500, 100, plot.Merchant != null ? 1150693 : 1150694, RedColor16, false, false);

        if (plot.Merchant == null)
        {
            AddHtmlLocalized(215, 350, 150, 18, 1150695, OrangeColor16, false, false); // CONFIRM
            AddButton(175, 350, 4005, 4007, 1);
        }
    }

    public static void DisplayTo(Mobile from, MaginciaBazaarPlot plot)
    {
        if (from == null || plot == null)
        {
            return;
        }

        from.CloseGump<ConfirmAbandonLeaseGump>();
        from.SendGump(new ConfirmAbandonLeaseGump(plot));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _plot == null || !_plot.IsOwner(from))
        {
            return;
        }

        if (info.ButtonID == 1 && _plot.Merchant == null)
        {
            _plot.Abandon();
        }
    }
}

public class ConfirmFireBrokerGump : BaseBazaarGump
{
    private readonly MaginciaBazaarPlot _plot;
    private readonly bool _hasInventory;

    private ConfirmFireBrokerGump(MaginciaBazaarPlot plot) : base(520, 520)
    {
        _plot = plot;

        _hasInventory = plot.Merchant switch
        {
            PetBroker pets => pets.BrokerEntries.Count > 0 || pets.BankBalance > 0,
            CommodityBroker commodity => commodity.CommodityEntries.Count > 0 || commodity.BankBalance > 0,
            _ => false
        };

        AddHtmlLocalized(195, 5, 150, 18, 1150385, RedColor16, false, false); // New Magincia Bazaar
        AddHtmlLocalized(217, 40, 150, 18, 1150386, RedColor16, false, false); // Stall Leasing

        AddHtmlLocalized(10, 100, 500, 100, _hasInventory ? 1150691 : 1150692, RedColor16, false, false);

        if (!_hasInventory)
        {
            AddHtmlLocalized(215, 350, 150, 18, 1150695, OrangeColor16, false, false); // CONFIRM
            AddButton(175, 350, 4005, 4007, 1);
        }
    }

    public static void DisplayTo(Mobile from, MaginciaBazaarPlot plot)
    {
        if (from == null || plot == null)
        {
            return;
        }

        from.CloseGump<ConfirmFireBrokerGump>();
        from.SendGump(new ConfirmFireBrokerGump(plot));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _plot == null || !_plot.IsOwner(from))
        {
            return;
        }

        if (info.ButtonID == 1 && !_hasInventory)
        {
            _plot.FireBroker();
        }
    }
}

public class HireBrokerGump : BaseBazaarGump
{
    private readonly MaginciaBazaarPlot _plot;
    private readonly bool _commodity;

    private HireBrokerGump(MaginciaBazaarPlot plot, bool commodity) : base(520, 520)
    {
        _plot = plot;
        _commodity = commodity;

        AddHtmlLocalized(195, 5, 150, 18, 1150385, RedColor16, false, false); // New Magincia Bazaar
        AddHtmlLocalized(217, 40, 150, 18, 1150386, RedColor16, false, false); // Stall Leasing

        AddHtmlLocalized(191, 100, 150, 18, commodity ? 1150687 : 1150686, RedColor16, false, false);
        AddHtmlLocalized(10, 130, 500, 100, 1150690, RedColor16, false, false); // Choose a stall style...

        AddButton(175, 244, 4005, 4007, 1);
        AddHtmlLocalized(210, 244, 150, 18, 1150701, RedColor16, false, false); // STYLE 1

        AddButton(175, 268, 4005, 4007, 2);
        AddHtmlLocalized(210, 268, 150, 18, 1150702, RedColor16, false, false); // STYLE 2

        AddButton(175, 292, 4005, 4007, 3);
        AddHtmlLocalized(210, 292, 150, 18, 1150703, RedColor16, false, false); // STYLE 3

        AddButton(10, 490, 4014, 4016, 4);
        AddHtmlLocalized(50, 490, 200, 18, 1149777, BlueColor16, false, false); // MAIN MENU
    }

    public static void DisplayTo(Mobile from, MaginciaBazaarPlot plot, bool commodity)
    {
        if (from == null || plot == null)
        {
            return;
        }

        from.CloseGump<HireBrokerGump>();
        from.SendGump(new HireBrokerGump(plot, commodity));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _plot == null || !_plot.IsOwner(from))
        {
            return;
        }

        var styleIndex = _commodity ? 0 : 1;

        switch (info.ButtonID)
        {
            case 1:
            case 2:
            case 3:
                {
                    _plot.AddTempMulti(styleIndex, info.ButtonID - 1);
                    ConfirmAddMultiGump.DisplayTo(from, _plot, _commodity);
                    break;
                }
            case 4:
                {
                    MyStallLeaseGump.DisplayTo(from, _plot);
                    break;
                }
        }
    }
}

public class ConfirmAddMultiGump : BaseBazaarGump
{
    private readonly MaginciaBazaarPlot _plot;
    private readonly bool _commodity;

    private ConfirmAddMultiGump(MaginciaBazaarPlot plot, bool commodity) : base(520, 520)
    {
        _plot = plot;
        _commodity = commodity;

        AddHtmlLocalized(195, 5, 150, 18, 1150385, RedColor16, false, false); // New Magincia Bazaar
        AddHtmlLocalized(217, 40, 150, 18, 1150386, RedColor16, false, false); // Stall Leasing

        AddHtmlLocalized(200, 100, 150, 18, commodity ? 1150687 : 1150686, RedColor16, false, false);
        AddHtmlLocalized(10, 150, 500, 200, 1150696, RedColor16, false, false); // The selected style is now being...

        AddHtmlLocalized(210, 370, 150, 18, 1150697, OrangeColor16, false, false); // CHOOSE OTHER STYLE
        AddButton(175, 370, 4005, 4007, 1);

        AddHtmlLocalized(210, 394, 150, 18, 1150695, OrangeColor16, false, false); // CONFIRM
        AddButton(175, 394, 4005, 4007, 2);

        AddButton(10, 490, 4014, 4016, 3);
        AddHtmlLocalized(50, 490, 200, 18, 1149777, BlueColor16, false, false); // MAIN MENU
    }

    public static void DisplayTo(Mobile from, MaginciaBazaarPlot plot, bool commodity)
    {
        if (from == null || plot == null)
        {
            return;
        }

        from.CloseGump<ConfirmAddMultiGump>();
        from.SendGump(new ConfirmAddMultiGump(plot, commodity));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _plot == null || !_plot.IsOwner(from))
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                {
                    HireBrokerGump.DisplayTo(from, _plot, _commodity);
                    break;
                }
            case 2:
                {
                    if (_plot.HasTempMulti())
                    {
                        _plot.ConfirmMulti(_commodity);
                    }
                    else
                    {
                        HireBrokerGump.DisplayTo(from, _plot, _commodity);
                        from.SendMessage("Предпросмотр стиля истёк. Выберите стиль заново и нажмите «подтвердить».");
                    }

                    break;
                }
            case 3:
                {
                    MyStallLeaseGump.DisplayTo(from, _plot);
                    break;
                }
        }
    }
}

public class StallBidGump : BaseBazaarGump
{
    private readonly Mobile _from;
    private readonly MaginciaBazaarPlot _plot;

    private StallBidGump(Mobile from, MaginciaBazaarPlot plot) : base(520, 520)
    {
        _from = from;
        _plot = plot;

        var biddingPlot = MaginciaBazaar.GetBiddingPlot(from);
        var bidAmount = biddingPlot != null ? biddingPlot.GetBid(from) : MaginciaBazaar.GetNextAvailableBid(from);

        AddHtmlLocalized(10, 10, 500, 18, 1114513, "#1150385", RedColor16, false, false); // New Magincia Bazaar
        AddHtmlLocalized(10, 48, 500, 18, 1114513, "#1150386", RedColor16, false, false); // Stall Leasing

        AddHtmlLocalized(86, 135, 200, 18, 1150389, RedColor16, false, false); // You are bidding on:

        if (biddingPlot != null)
        {
            AddHtmlLocalized(215, 135, 100, 18, 1150541, biddingPlot.PlotDef.ID, BlueColor16, false, false); // ~1_TOKEN~
        }
        else if (MaginciaBazaar.IsBiddingNextAvailable(from))
        {
            AddHtmlLocalized(215, 135, 100, 18, 1150538, BlueColor16, false, false); // Next Available Stall
        }
        else
        {
            AddHtmlLocalized(215, 135, 100, 18, 1150396, BlueColor16, false, false); // NONE
        }

        AddHtmlLocalized(135, 160, 150, 18, 1150407, RedColor16, false, false); // Bid Amount:
        AddHtml(215, 160, 100, 18, Color(FormatAmt(bidAmount), BlueColor), false, false);

        AddButton(260, 220, 4005, 4007, 1);
        AddHtmlLocalized(300, 220, 150, 18, 1150566, OrangeColor16, false, false); // INSTRUCTIONS

        AddHtmlLocalized(125, 265, 200, 18, 1150560, RedColor16, false, false); // BID AMOUNT
        AddBackground(215, 265, 295, 22, 9350);
        AddTextEntry(216, 267, 295, 20, 0, 0, "");

        var isOwner = plot.IsOwner(from);

        AddHtmlLocalized(215, 304, 250, 18, 1150568, plot.PlotDef.ID, isOwner ? GrayColor16 : BlueColor16, false, false); // BID ON THIS STALL

        if (!isOwner)
        {
            AddButton(175, 304, 4005, 4007, 2);
        }

        AddHtmlLocalized(215, 328, 250, 18, 1150569, BlueColor16, false, false); // BID ON FIRST AVAILABLE
        AddButton(175, 328, 4005, 4007, 3);

        // Minimum bid 1,000gp. Bids are rounded down to nearest 1,000gp (1,999gp becomes 1,000gp).
        AddHtmlLocalized(10, 360, 500, 40, 1150792, RedColor16, false, false);

        AddButton(10, 490, 4014, 4016, 4);
        AddHtmlLocalized(50, 490, 200, 18, 1149777, BlueColor16, false, false); // MAIN MENU
    }

    public static void DisplayTo(Mobile from, MaginciaBazaarPlot plot)
    {
        if (from == null || plot == null)
        {
            return;
        }

        from.CloseGump<StallBidGump>();
        from.SendGump(new StallBidGump(from, plot));
    }

    /// <summary>Ставка округляется вниз до 1000 и не может быть меньше 1000; нечисло даёт 0 (значит «снять»).</summary>
    private static int NormalizeBid(string text)
    {
        if (!int.TryParse(text, out var amount) || amount <= 0)
        {
            return 0;
        }

        amount = (int)(Math.Floor(amount / 1000.0) * 1000.0);

        return amount < 1000 ? 1000 : amount;
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _plot == null)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                {
                    BazaarInformationGump.DisplayTo(from, 0, 1150567, new StallBidGump(from, _plot));
                    break;
                }
            case 2:
                {
                    if (_plot.IsOwner(from))
                    {
                        break;
                    }

                    var amount = NormalizeBid(info.GetTextEntry(0));

                    if (amount <= 0 && HasBid(from))
                    {
                        break;
                    }

                    ConfirmBidGump.DisplayTo(from, _plot, _plot, amount, amount <= 0);
                    break;
                }
            case 3:
                {
                    var amount = NormalizeBid(info.GetTextEntry(0));

                    if (amount <= 0 && HasBid(from))
                    {
                        break;
                    }

                    ConfirmBidGump.DisplayTo(from, _plot, null, amount, amount <= 0);
                    break;
                }
            case 4:
                {
                    StallLeasingGump.DisplayTo(from, _plot);
                    break;
                }
        }
    }

    private static bool HasBid(Mobile from) =>
        MaginciaBazaar.GetBiddingPlot(from) != null || MaginciaBazaar.IsBiddingNextAvailable(from);
}

public class TopBidsGump : BaseBazaarGump
{
    private readonly MaginciaBazaarPlot _plot;

    private TopBidsGump(MaginciaBazaarPlot plot) : base(520, 520)
    {
        _plot = plot;

        AddHtmlLocalized(10, 10, 500, 18, 1114513, "#1150385", RedColor16, false, false); // New Magincia Bazaar
        AddHtmlLocalized(10, 48, 500, 18, 1114513, "#1150386", RedColor16, false, false); // Stall Leasing

        AddHtmlLocalized(10, 95, 500, 18, 1150562, plot.PlotDef.ID, RedColor16, false, false); // Viewing top bids...

        AddHtmlLocalized(10, 133, 245, 18, 1150560, RedColor16, false, false); // BID AMOUNT
        AddHtmlLocalized(265, 133, 245, 18, 1150561, RedColor16, false, false); // BID TYPE

        if (plot.Auction == null || plot.Auction.Auctioners.Count == 0)
        {
            AddHtmlLocalized(10, 171, 500, 90, 1114513, "#1150563", RedColor16, false, false); // There are currently no bids...
        }
        else
        {
            var list = new List<BidEntry>(plot.Auction.Auctioners.Values);
            list.Sort();

            for (var i = 0; i < list.Count && i < 10; i++)
            {
                AddHtml(10, 171 + i * 18, 200, 18, Color(FormatAmt(list[i].Amount), BlueColor16), false, false);
                AddHtmlLocalized(265, 171 + i * 18, 245, 18, 1150558 + (int)list[i].BidType, BlueColor16, false, false);
            }
        }

        AddButton(10, 490, 4014, 4016, 1);
        AddHtmlLocalized(50, 490, 200, 18, 1149777, BlueColor16, false, false); // MAIN MENU
    }

    public static void DisplayTo(Mobile from, MaginciaBazaarPlot plot)
    {
        if (from == null || plot == null)
        {
            return;
        }

        from.CloseGump<TopBidsGump>();
        from.SendGump(new TopBidsGump(plot));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 1 && state.Mobile != null && _plot != null)
        {
            StallLeasingGump.DisplayTo(state.Mobile, _plot);
        }
    }
}

public class ConfirmBidGump : BaseBazaarGump
{
    private readonly MaginciaBazaarPlot _plot;

    private ConfirmBidGump(Mobile from, MaginciaBazaarPlot plot, MaginciaBazaarPlot newPlot, int newBid, bool retract)
        : base(520, 520)
    {
        _plot = plot;

        AddHtmlLocalized(10, 10, 500, 18, 1114513, "#1150385", RedColor16, false, false); // New Magincia Bazaar
        AddHtmlLocalized(10, 48, 500, 18, 1114513, "#1150386", RedColor16, false, false); // Stall Leasing

        var result = ApplyBid(from, newPlot, newBid, retract);

        if (result.Cliloc1 > 0)
        {
            AddLocalized(10, 100, 500, 120, result.Cliloc1, result.Args1);
        }

        if (result.Cliloc2 > 0)
        {
            AddLocalized(10, 240, 500, 100, result.Cliloc2, result.Args2);
        }

        AddHtmlLocalized(50, 490, 150, 18, 1150556, BlueColor16, false, false); // MY STALL BID
        AddButton(10, 490, 4005, 4007, 1);
    }

    public static void DisplayTo(Mobile from, MaginciaBazaarPlot plot, MaginciaBazaarPlot newPlot, int newBid, bool retract)
    {
        if (from == null || plot == null)
        {
            return;
        }

        from.CloseGump<ConfirmBidGump>();
        from.SendGump(new ConfirmBidGump(from, plot, newPlot, newBid, retract));
    }

    private void AddLocalized(int x, int y, int width, int height, int cliloc, string args)
    {
        if (args == null)
        {
            AddHtmlLocalized(x, y, width, height, cliloc, GreenColor16, false, false);
        }
        else
        {
            AddHtmlLocalized(x, y, width, height, cliloc, args, GreenColor16, false, false);
        }
    }

    /// <summary>
    ///     Разбирает текущее состояние ставок игрока, применяет изменение и возвращает пару сообщений для показа:
    ///     основное и «на что вы теперь ставите».
    /// </summary>
    private static BidResult ApplyBid(Mobile from, MaginciaBazaarPlot newPlot, int newBid, bool retract)
    {
        var oldPlot = MaginciaBazaar.GetBiddingPlot(from);
        var hasNextAvailable = MaginciaBazaar.IsBiddingNextAvailable(from);
        var hasSpecific = oldPlot != null;
        var specific = newPlot != null;

        var bankBalance = Banker.GetBalance(from);
        var oldBid = hasSpecific
            ? oldPlot.Auction.GetBidAmount(from)
            : hasNextAvailable ? MaginciaBazaar.GetNextAvailableBid(from) : 0;

        if (newBid < 1000)
        {
            newBid = 1000;
        }

        if (retract)
        {
            return MaginciaBazaar.TryRetractBid(from)
                ? new BidResult(
                    1150576,
                    $"@{FormatAmt(oldBid)}@empty@empty@{FormatAmt(bankBalance)}@{FormatAmt(bankBalance + oldBid)}",
                    0,
                    null
                )
                : new BidResult(1150575, $"@{FormatAmt(oldBid)}@empty@empty@{FormatAmt(bankBalance)}", 0, null);
        }

        if (oldBid == newBid)
        {
            return KeepBid(from, oldPlot, newPlot, newBid, hasSpecific, hasNextAvailable);
        }

        if (specific)
        {
            return hasSpecific
                ? ChangeSpecificBid(from, oldPlot, newPlot, newBid, oldBid, bankBalance)
                : PlaceSpecificBid(from, newPlot, newBid, hasNextAvailable, bankBalance);
        }

        return PlaceNextAvailableBid(from, newBid, oldBid, hasSpecific, hasNextAvailable, oldPlot, bankBalance);
    }

    private static BidResult KeepBid(
        Mobile from, MaginciaBazaarPlot oldPlot, MaginciaBazaarPlot newPlot, int newBid, bool hasSpecific, bool hasNextAvailable
    )
    {
        // Your bid amount of ~1_BID~gp has not changed.
        var result = new BidResult(1150570, FormatAmt(newBid), 0, null);

        if (hasSpecific)
        {
            if (newPlot == null)
            {
                oldPlot.Auction.RemoveBid(from);
                MaginciaBazaar.MakeBidNextAvailable(from, newBid);

                // You were previously bidding on Stall ~1_OLDSTALL~, and now you are bidding on the first available stall.
                return new BidResult(1150580, oldPlot.PlotDef.ID, 1150584, null);
            }

            if (oldPlot != newPlot)
            {
                oldPlot.Auction.RemoveBid(from);
                newPlot.Auction.MakeBid(from, newBid);

                // You were previously bidding on Stall ~1_OLDSTALL~ and now you are bidding on Stall ~2_NEWSTALL~.
                return new BidResult(1150582, $"@{oldPlot.PlotDef.ID}@{newPlot.PlotDef.ID}", 1150585, newPlot.PlotDef.ID);
            }

            // You are still bidding on Stall ~2_NEWSTALL~.
            return new BidResult(1150570, FormatAmt(newBid), 1150583, $"@empty@{oldPlot.PlotDef.ID}");
        }

        if (!hasNextAvailable)
        {
            return result;
        }

        if (newPlot == null)
        {
            // You are still bidding on the next available stall.
            return new BidResult(1150570, FormatAmt(newBid), 1150581, null);
        }

        newPlot.Auction.MakeBid(from, newBid);
        MaginciaBazaar.RemoveBidNextAvailable(from);

        // You were previously bidding on the first available stall, and now you are bidding specifically for Stall ~2_NEWSTALL~.
        return new BidResult(1150579, $"@empty@{newPlot.PlotDef.ID}", 1150584, null);
    }

    /// <summary>Ставка на конкретный прилавок, у игрока уже была ставка на другой (или тот же) прилавок.</summary>
    private static BidResult ChangeSpecificBid(
        Mobile from, MaginciaBazaarPlot oldPlot, MaginciaBazaarPlot newPlot, int newBid, int oldBid, int bankBalance
    )
    {
        var increase = newBid > oldBid;
        var dif = increase ? newBid - oldBid : oldBid - newBid;
        var cliloc2 = oldPlot == newPlot ? 1150583 : 1150582;
        var args2 = oldPlot == newPlot ? $"@empty@{newPlot.PlotDef.ID}" : $"@{oldPlot.PlotDef.ID}@{newPlot.PlotDef.ID}";

        if (increase && bankBalance < dif)
        {
            // You are attempting to increase your stall bid ... The status of your bid has not changed.
            return new BidResult(
                1150571,
                $"@{FormatAmt(oldBid)}@{FormatAmt(newBid)}@{FormatAmt(dif)}@{FormatAmt(bankBalance)}",
                cliloc2,
                args2
            );
        }

        if (!MaginciaBazaar.TryRetractBid(from))
        {
            // The bid cannot be refunded to your bank account, because your bank box cannot hold the additional funds.
            return new BidResult(
                1150575,
                $"@{FormatAmt(oldBid)}@empty@empty@{FormatAmt(bankBalance)}",
                cliloc2,
                args2
            );
        }

        if (increase)
        {
            Banker.Withdraw(from, dif);
            newPlot.Auction.MakeBid(from, newBid);

            if (oldPlot == newPlot)
            {
                // You are increasing your stall bid from ~1_OLDBID~gp to ~2_NEWBID~gp...
                return new BidResult(
                    1150572,
                    $"@{FormatAmt(oldBid)}@{FormatAmt(newBid)}@{FormatAmt(dif)}@{FormatAmt(bankBalance)}@{FormatAmt(bankBalance - dif)}",
                    cliloc2,
                    args2
                );
            }

            // You have posted a new bid of ~2_NEWBID~gp...
            return new BidResult(
                1150578,
                $"@empty@{FormatAmt(newBid)}@empty@{FormatAmt(bankBalance)}@{FormatAmt(bankBalance - dif)}",
                cliloc2,
                args2
            );
        }

        Banker.Withdraw(from, newBid);
        newPlot.Auction.MakeBid(from, newBid);

        if (oldPlot == newPlot)
        {
            // You are decreasing your stall bid from ~1_OLDBID~gp to ~2_NEWBID~gp...
            return new BidResult(
                1150574,
                $"@{FormatAmt(oldBid)}@{FormatAmt(newBid)}@{FormatAmt(dif)}@{FormatAmt(bankBalance)}@{FormatAmt(bankBalance + dif)}",
                cliloc2,
                args2
            );
        }

        // You have posted a new bid of ~2_NEWBID~gp...
        return new BidResult(
            1150578,
            $"@empty@{FormatAmt(newBid)}@empty@{FormatAmt(bankBalance)}@{FormatAmt(bankBalance - newBid)}",
            cliloc2,
            args2
        );
    }

    /// <summary>Ставка на конкретный прилавок, конкретной ставки ещё не было (возможно, была «первый свободный»).</summary>
    private static BidResult PlaceSpecificBid(
        Mobile from, MaginciaBazaarPlot newPlot, int newBid, bool hasNextAvailable, int bankBalance
    )
    {
        if (hasNextAvailable && bankBalance < newBid)
        {
            // You are attempting to place a new bid of ~2_NEWBID~gp. This amount exceeds your current bank balance...
            return new BidResult(1150577, $"@empty@{FormatAmt(newBid)}@empty@{FormatAmt(bankBalance)}", 0, null);
        }

        if (hasNextAvailable && !MaginciaBazaar.TryRetractBid(from))
        {
            return new BidResult(1150575, $"@empty@empty@empty@{FormatAmt(bankBalance)}", 0, null);
        }

        Banker.Withdraw(from, newBid);
        newPlot.Auction.MakeBid(from, newBid);

        var cliloc2 = hasNextAvailable ? 1150579 : 1150585;
        var args2 = hasNextAvailable ? $"@empty@{newPlot.PlotDef.ID}" : newPlot.PlotDef.ID;

        // You have posted a new bid of ~2_NEWBID~gp...
        return new BidResult(
            1150578,
            $"@empty@{FormatAmt(newBid)}@empty@{FormatAmt(bankBalance)}@{FormatAmt(bankBalance - newBid)}",
            cliloc2,
            args2
        );
    }

    /// <summary>Ставка «на первый свободный прилавок».</summary>
    private static BidResult PlaceNextAvailableBid(
        Mobile from, int newBid, int oldBid, bool hasSpecific, bool hasNextAvailable, MaginciaBazaarPlot oldPlot,
        int bankBalance
    )
    {
        var increase = newBid > oldBid;
        var dif = increase ? newBid - oldBid : oldBid - newBid;

        if (increase && bankBalance < dif)
        {
            return new BidResult(1150577, $"@empty@{FormatAmt(newBid)}@empty@{FormatAmt(bankBalance)}", 0, null);
        }

        if ((hasSpecific || hasNextAvailable) && !MaginciaBazaar.TryRetractBid(from))
        {
            return new BidResult(1150575, $"@{FormatAmt(oldBid)}@empty@empty@{FormatAmt(bankBalance)}", 0, null);
        }

        var withdraw = increase ? dif : newBid;
        Banker.Withdraw(from, withdraw);
        MaginciaBazaar.MakeBidNextAvailable(from, newBid);

        var cliloc2 = hasSpecific ? 1150580 : hasNextAvailable ? 1150581 : 1150584;
        var args2 = hasSpecific ? oldPlot.PlotDef.ID : null;

        // You have posted a new bid of ~2_NEWBID~gp...
        return new BidResult(
            1150578,
            $"@empty@{FormatAmt(newBid)}@empty@{FormatAmt(bankBalance)}@{FormatAmt(bankBalance - withdraw)}",
            cliloc2,
            args2
        );
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 1 && state.Mobile != null && _plot != null)
        {
            StallBidGump.DisplayTo(state.Mobile, _plot);
        }
    }
}

/// <summary>Итог изменения ставки: два сообщения cliloc (основное и «на что вы теперь ставите»).</summary>
public readonly record struct BidResult(int Cliloc1, string Args1, int Cliloc2, string Args2);

public class MatchBidGump : BaseBazaarGump
{
    private readonly Mobile _from;
    private readonly MaginciaBazaarPlot _plot;

    private MatchBidGump(Mobile from, MaginciaBazaarPlot plot) : base(520, 520)
    {
        _from = from;
        _plot = plot;

        var amount = MaginciaBazaar.GetBidMatching(from);

        AddHtmlLocalized(10, 10, 500, 18, 1114513, "#1150385", RedColor16, false, false); // New Magincia Bazaar
        AddHtmlLocalized(10, 48, 500, 18, 1114513, "#1150386", RedColor16, false, false); // Stall Leasing

        AddHtmlLocalized(12, 100, 150, 18, 1150387, RedColor16, false, false); // Your Stall:
        AddHtmlLocalized(12, 120, 150, 18, 1150393, RedColor16, false, false); // Bid Match Limit:

        if (plot != null)
        {
            AddHtml(260, 100, 150, 18, Color(plot.PlotDef.ID, BlueColor), false, false);
        }
        else
        {
            AddHtml(260, 100, 250, 18, Color("Вы не арендуете прилавок.", BlueColor), false, false);
        }

        AddHtml(260, 120, 150, 18, Color(FormatAmt(amount), BlueColor), false, false);

        AddButton(175, 175, 4005, 4007, 1);
        AddHtmlLocalized(215, 175, 150, 18, 1150392, OrangeColor16, false, false); // INFORMATION

        AddHtmlLocalized(10, 230, 200, 18, 1150587, RedColor16, false, false); // CHANGE MATCH BID
        AddBackground(215, 280, 295, 22, 9350);
        AddTextEntry(216, 281, 295, 20, 0, 0, "");
        AddButton(175, 280, 4005, 4007, 2);

        if (plot != null)
        {
            AddButton(10, 490, 4014, 4016, 3);
            AddHtmlLocalized(50, 490, 200, 18, 1149777, BlueColor16, false, false); // MAIN MENU
        }
    }

    public static void DisplayTo(Mobile from, MaginciaBazaarPlot plot)
    {
        if (from == null)
        {
            return;
        }

        from.CloseGump<MatchBidGump>();
        from.SendGump(new MatchBidGump(from, plot));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        if (_plot != null && !_plot.IsOwner(from))
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                {
                    BazaarInformationGump.DisplayTo(from, 1150399, 1150398, new MatchBidGump(from, _plot));
                    break;
                }
            case 2:
                {
                    if (!int.TryParse(info.GetTextEntry(0), out var amount))
                    {
                        break;
                    }

                    ConfirmMatchBidGump.DisplayTo(from, amount, _plot);
                    break;
                }
            case 3:
                {
                    if (_plot != null)
                    {
                        StallLeasingGump.DisplayTo(from, _plot);
                    }

                    break;
                }
        }
    }
}

public class ConfirmMatchBidGump : BaseBazaarGump
{
    private readonly MaginciaBazaarPlot _plot;

    private ConfirmMatchBidGump(Mobile from, int amount, MaginciaBazaarPlot plot) : base(520, 520)
    {
        _plot = plot;

        if (amount < 0)
        {
            amount = 0;
        }

        AddHtmlLocalized(10, 10, 500, 18, 1114513, "#1150385", RedColor16, false, false); // New Magincia Bazaar
        AddHtmlLocalized(10, 48, 500, 18, 1114513, "#1150386", RedColor16, false, false); // Stall Leasing

        var current = MaginciaBazaar.GetBidMatching(from);
        var bankBalance = Banker.GetBalance(from);

        int cliloc;
        string args;

        if (amount > current)
        {
            var dif = amount - current;

            if (dif > bankBalance)
            {
                // Your attempt to increase your Match Bid ... failed.
                cliloc = 1150588;
                args = $"@{FormatAmt(current)}@{FormatAmt(amount)}@{FormatAmt(dif)}@{FormatAmt(bankBalance)}";
            }
            else
            {
                Banker.Withdraw(from, amount);
                MaginciaBazaar.AddToReserve(from, dif);

                // You have increased your Match Bid ...
                cliloc = 1150589;
                args =
                    $"@{FormatAmt(current)}@{FormatAmt(amount)}@{FormatAmt(dif)}@{FormatAmt(bankBalance)}@{FormatAmt(bankBalance - dif)}";
            }
        }
        else if (amount < current)
        {
            var dif = current - amount;

            if (!Banker.Deposit(from, dif))
            {
                // Your attempt to decrease your Match Bid ... failed.
                cliloc = 1150590;
                args = $"@{FormatAmt(current)}@{FormatAmt(amount)}@{FormatAmt(dif)}@{FormatAmt(bankBalance)}";
            }
            else
            {
                MaginciaBazaar.DeductReserve(from, dif);

                // You have decreased your Match Bid ...
                cliloc = 1150591;
                args =
                    $"@{FormatAmt(current)}@{FormatAmt(amount)}@{FormatAmt(dif)}@{FormatAmt(bankBalance)}@{FormatAmt(bankBalance + dif)}";
            }
        }
        else
        {
            // You have not changed your match bid.
            cliloc = 1150592;
            args = null;
        }

        if (args == null)
        {
            AddHtmlLocalized(10, 100, 500, 120, cliloc, GreenColor16, false, false);
        }
        else
        {
            AddHtmlLocalized(10, 100, 500, 120, cliloc, args, GreenColor16, false, false);
        }

        AddHtmlLocalized(50, 490, 150, 18, 1150540, BlueColor16, false, false); // MY BID MATCHING
        AddButton(10, 490, 4014, 4016, 1);
    }

    public static void DisplayTo(Mobile from, int amount, MaginciaBazaarPlot plot)
    {
        if (from == null)
        {
            return;
        }

        from.CloseGump<ConfirmMatchBidGump>();
        from.SendGump(new ConfirmMatchBidGump(from, amount, plot));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        if (_plot != null && !_plot.IsOwner(from))
        {
            return;
        }

        if (info.ButtonID == 1)
        {
            MatchBidGump.DisplayTo(from, _plot);
        }
    }
}

public class BazaarInformationGump : BaseBazaarGump
{
    private readonly Gump _returnGump;

    private BazaarInformationGump(int title, int message, Gump returnGump) : base(520, 520)
    {
        _returnGump = returnGump;

        AddHtmlLocalized(10, 10, 500, 18, 1114513, "#1150385", RedColor16, false, false); // New Magincia Bazaar

        if (title > 0)
        {
            AddHtmlLocalized(10, 48, 500, 40, 1114513, $"#{title}", RedColor16, false, false);
        }

        AddHtmlLocalized(10, 95, 500, 375, 1114513, $"#{message}", RedColor16, true, true);

        if (returnGump != null)
        {
            AddButton(10, 490, 0xFAE, 0xFAF, 1);
            AddHtmlLocalized(50, 490, 210, 20, 1149777, BlueColor16, false, false); // MAIN MENU
        }
    }

    public static void DisplayTo(Mobile from, int title, int message, Gump returnGump = null)
    {
        if (from == null)
        {
            return;
        }

        from.CloseGump<BazaarInformationGump>();
        from.SendGump(new BazaarInformationGump(title, message, returnGump));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 1 && state.Mobile != null && _returnGump != null)
        {
            state.Mobile.SendGump(_returnGump);
        }
    }
}

public class ShopRecallRuneGump : BaseBazaarGump
{
    private readonly PlotSign _sign;

    private ShopRecallRuneGump(PlotSign sign) : base(520, 520)
    {
        _sign = sign;

        AddHtmlLocalized(195, 5, 150, 18, 1150385, RedColor16, false, false); // New Magincia Bazaar
        AddHtmlLocalized(217, 40, 150, 18, 1151508, RedColor16, false, false); // Shop Recall Rune

        // For a charge of 100gp, you will receive a recall rune marked for your bazaar stall.
        AddHtmlLocalized(10, 120, 500, 18, 1150458, RedColor16, false, false);

        AddButton(175, 150, 4005, 4007, 1);
        AddHtml(215, 150, 150, 18, Color("КУПИТЬ", OrangeColor), false, false);
    }

    public static void DisplayTo(Mobile from, PlotSign sign)
    {
        if (from == null || sign?.Deleted != false)
        {
            return;
        }

        from.CloseGump<ShopRecallRuneGump>();
        from.SendGump(new ShopRecallRuneGump(sign));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (info.ButtonID != 1 || from == null || _sign?.Deleted != false)
        {
            return;
        }

        if (!from.InRange(_sign.Location, 2))
        {
            from.SendMessage("Чтобы пометить руну, нужно стоять не дальше двух шагов от вывески.");
            return;
        }

        var pack = from.Backpack;
        var bank = from.BankBox;

        if (pack == null && bank == null)
        {
            from.SendMessage("Вам некуда положить руну.");
            return;
        }

        if (!Banker.Withdraw(from, PlotSign.RuneCost))
        {
            from.SendMessage($"Для покупки руны нужно {PlotSign.RuneCost} золота в банке.");
            return;
        }

        var location = _sign.Plot?.PlotDef?.Location ?? _sign.Location;

        var rune = new RecallRune
        {
            Target = location,
            TargetMap = _sign.Map,
            Description = $"Прилавок: {_sign.Plot?.PlotDef?.ID ?? "?"}",
            Marked = true
        };

        if (pack?.TryDropItem(from, rune, false) == true)
        {
            // An item has been placed in your backpack.
            from.SendLocalizedMessage(1153657);
            return;
        }

        if (bank != null)
        {
            bank.DropItem(rune);
            from.SendMessage("Предмет положен в ваш банк.");
            return;
        }

        rune.Delete();
        Banker.Deposit(from, PlotSign.RuneCost);
        from.SendMessage("В рюкзаке нет места, а банк недоступен — золото возвращено.");
    }
}
