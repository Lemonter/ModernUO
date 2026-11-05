/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/Time Of Legends/AuctionSafe/Gumps.cs).
///
///     Окна сейфа-аукциона: окно владельца (выставить предмет, описание, срок, стартовая ставка, «купить
///     сейчас»), окно участника (ставка/выкуп), справка и история ставок.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             <c>BaseGump</c> → обычный <see cref="Gump" /> с явным статическим <c>DisplayTo</c> у каждого окна
///             (стиль <c>Engines/NewMagincia/Bazaar/Gumps</c>); пустых рамок нет — фон, заголовок и строки
///             рисуются всегда, а если данных нет, вместо пустого места выводится строка-сообщение.
///         </item>
///         <item>
///             <c>AddGumpLayout</c> больше не вызывается из конструктора базового класса: виртуальный вызов из
///             конструктора собирал окно владельца до того, как выставлялся флаг владельца. Теперь
///             <c>Build</c> вызывается из конструктора наследника, когда все поля уже заданы.
///         </item>
///         <item>
///             <c>RelayInfo</c> → <c>in RelayInfo</c>, <c>TextRelay</c> → <c>info.GetTextEntry(id)</c> (возвращает
///             <c>string</c>), <c>Refresh</c> → <c>Reset</c> + повторная отправка того же окна.
///         </item>
///         <item>
///             <c>Account.CurrencyThreshold</c> → <see cref="AccountGold.CurrencyThreshold" />; строки золота и
///             платины берутся из <see cref="IAccount.TotalGold" />/<see cref="IAccount.TotalPlat" />, потому что
///             это две отдельные строки окна (а не «всего золота»).
///         </item>
///         <item>
///             <c>Utility.ToInt64</c> в этом репозитории нет → <c>long.TryParse</c>; пустая строка считается нулём,
///             отрицательное число — ошибкой ввода.
///         </item>
///         <item>
///             <c>Utility.FixHtml</c> вызывается как метод-расширение (<c>text.Trim().FixHtml()</c>);
///             <c>Guilds.BaseGuildGump.CheckProfanity</c> оставлен как есть. В ServUO описание принималось по
///             условию <c>str != null || CheckProfanity(...)</c>, то есть проверка не работала никогда — здесь
///             логическое И.
///         </item>
///         <item>
///             <c>String.Format</c> → интерполяция, суммы — с культурой en-US, как в ServUO; собственные строки
///             переведены на русский, cliloc-идентификаторы не тронуты.
///         </item>
///         <item>
///             <c>Auction.ResendGumps</c> рассылал участникам <c>AuctionOwnerGump</c> (окно владельца с правом
///             правки) — здесь участнику отправляется <see cref="AuctionBidGump" />.
///         </item>
///     </list>
/// </summary>
using System;
using System.Globalization;
using Server.Accounting;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.Auction;

public abstract class BaseAuctionGump : Gump
{
    protected const int Blue = 0x1FF;
    protected const int Yellow = 0x6B45;
    protected const int White = 0x7FFF;
    protected const int Gray = 0x4E73;
    protected const string HGray = "BFBFBF";

    protected const int Length = 400;
    protected const int Height = 600;

    private static readonly CultureInfo _amountCulture = CultureInfo.GetCultureInfo("en-US");

    protected readonly Mobile _user;
    protected readonly bool _owner;

    protected BaseAuctionGump(Mobile user, AuctionSafe safe, bool owner) : base(100, 100)
    {
        _user = user;
        Safe = safe;
        _owner = owner;
        Auction = AuctionStore.Get(safe);
    }

    public AuctionSafe Safe { get; }

    protected Auction Auction { get; set; }

    public void Refresh()
    {
        Auction = AuctionStore.Get(Safe);
        Build();
        _user.SendGump(this, true);
    }

    protected abstract void BuildContent();

    protected void Build()
    {
        Reset();

        AddPage(0);
        AddBackground(0, 0, Length, Height, 39925);

        // <DIV ALIGN=CENTER>~1_TOKEN~</DIV>
        AddHtmlLocalized(15, 25, 360, 18, 1114513, "#1156371", White, false, false);
        AddHtmlLocalized(15, 52, 360, 18, 1114513, _owner ? "#1150328" : "#1156442", Blue, false, false);

        // Information / History
        AddHtmlLocalized(80, 88, 110, 22, 3000098, Yellow, false, false);
        AddButton(40, 88, 4005, 4007, 100, GumpButtonType.Reply, 0);

        AddHtmlLocalized(265, 88, 110, 22, 3010004, Yellow, false, false);
        AddButton(225, 88, 4005, 4007, 101, GumpButtonType.Reply, 0);

        var account = _user.Account;

        // Total Gold: / Total Platinum:
        AddHtmlLocalized(15, 117, 175, 18, 1114514, "#1156044", Yellow, false, false);
        AddHtml(200, 117, 175, 18, Colored(FormatAmount(account?.TotalGold ?? 0), HGray), false, false);

        AddHtmlLocalized(15, 137, 175, 18, 1114514, "#1156045", Yellow, false, false);
        AddHtml(200, 137, 175, 18, Colored(FormatAmount(account?.TotalPlat ?? 0), HGray), false, false);

        BuildContent();

        if (Auction != null && _user is PlayerMobile player)
        {
            Auction.AddViewer(player);
        }
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        switch (info.ButtonID)
        {
            case 100:
                {
                    Refresh();
                    AuctionInfoGump.DisplayTo(_user);
                    break;
                }
            case 101:
                {
                    Refresh();
                    BidHistoryGump.DisplayTo(_user, Auction);
                    break;
                }
        }

        if (Auction != null && _user is PlayerMobile player)
        {
            Auction.RemoveViewer(player);
        }
    }

    /// <summary>Строка «сколько осталось» для идущего аукциона (Lifespan: ~1_val~ days/hours/minutes).</summary>
    protected void AddRemainingTime(int x, int y)
    {
        var left = Auction.EndTime - Core.Now;

        if (left.TotalSeconds < 0 || Auction.InClaimPeriod)
        {
            // Auction Ended
            AddHtmlLocalized(x, y, 175, 18, 1114513, "#1156438", Gray, false, false);
            return;
        }

        int cliloc;
        double value;

        if (left.TotalDays >= 1)
        {
            cliloc = 1153091; // Lifespan: ~1_val~ days
            value = left.TotalDays;
        }
        else if (left.TotalHours >= 1)
        {
            cliloc = 1153090; // Lifespan: ~1_val~ hours
            value = left.TotalHours;
        }
        else
        {
            cliloc = 1153089; // Lifespan: ~1_val~ minutes
            value = left.TotalMinutes;
        }

        AddHtmlLocalized(x, y, 175, 18, cliloc, ((int)value).ToString(), Gray, false, false);
    }

    protected static string Colored(string text, string color) => $"<basefont color=#{color}>{text}</basefont>";

    protected static string FormatAmount(long amount) => amount.ToString("N0", _amountCulture);

    protected static bool TryParseAmount(string text, out long amount)
    {
        amount = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!long.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < 0)
        {
            return false;
        }

        amount = value;
        return true;
    }

    protected static string GetItemArg(Item item)
    {
        if (item?.LabelNumber > 0)
        {
            return $"#{item.LabelNumber}";
        }

        return item?.Name ?? "неизвестный предмет";
    }
}

public class AuctionOwnerGump : BaseAuctionGump
{
    private AuctionOwnerGump(Mobile user, AuctionSafe safe) : base(user, safe, true) => Build();

    public static void DisplayTo(Mobile from, AuctionSafe safe)
    {
        if (from == null || safe?.Deleted != false)
        {
            return;
        }

        from.CloseGump<AuctionOwnerGump>();
        from.SendGump(new AuctionOwnerGump(from, safe));
    }

    protected override void BuildContent()
    {
        if (Auction == null)
        {
            Auction = AuctionStore.GetOrCreate(Safe, _user);
        }

        if (Auction == null)
        {
            AddHtml(15, 166, 360, 40, Colored("Сейф аукциона недоступен.", HGray), false, false);
            return;
        }

        var y = 166;

        // Select New Auction Item
        AddHtmlLocalized(200, y, 175, 22, 1156421, Yellow, false, false);
        AddButton(160, y, 4005, 4007, 1, GumpButtonType.Reply, 0);

        y += 24;

        // Description:
        AddHtmlLocalized(15, y, 175, 110, 1114514, "#1156400", Yellow, false, false);
        AddButton(345, y, 4014, 4016, 2, GumpButtonType.Reply, 0);

        AddBackground(200, y, 140, 110, 9350);
        AddTextEntry(202, y + 2, 136, 106, 0, 1, Auction.Description ?? "", 140);

        if (Auction.AuctionItem != null)
        {
            var item = Auction.AuctionItem;
            AddImageTiledButton(102, 212, 0x918, 0x918, 0x0, GumpButtonType.Page, 0, item.ItemID, item.Hue, 23, 5);
            AddItemProperty(item.Serial);
        }

        y += 112;

        // Time Remaining:
        AddHtmlLocalized(15, y, 175, 18, 1114514, "#1156404", Yellow, false, false);

        if (Auction.HasBegun)
        {
            AddRemainingTime(200, y);
        }
        else
        {
            var span = TimeSpan.FromMinutes(Auction.Duration);

            if (span.TotalMinutes > 60)
            {
                AddHtmlLocalized(200, y, 175, 18, 1153091, $"{span.TotalDays}", Gray, false, false);
            }
            else
            {
                AddHtmlLocalized(200, y, 175, 18, 1153089, $"{span.TotalMinutes}", Gray, false, false);
            }
        }

        y += 20;

        // One Hour / Three Days / Five Days / Seven Days
        AddHtmlLocalized(200, y, 140, 20, 1114514, "#1156455", Yellow, false, false);
        AddButton(345, y, 4014, 4016, 3, GumpButtonType.Reply, 0);

        y += 20;

        AddHtmlLocalized(200, y, 140, 20, 1114514, "#1156418", Yellow, false, false);
        AddButton(345, y, 4014, 4016, 4, GumpButtonType.Reply, 0);

        y += 20;

        AddHtmlLocalized(Length / 2, y, 140, 20, 1114514, "#1156419", Yellow, false, false);
        AddButton(345, y, 4014, 4016, 5, GumpButtonType.Reply, 0);

        y += 20;

        AddHtmlLocalized(200, y, 140, 20, 1114514, "#1156420", Yellow, false, false);
        AddButton(Length - 55, y, 4014, 4016, 6, GumpButtonType.Reply, 0);

        y += 24;

        var startBid = GetPlatGold(Auction.StartBid);

        // Item Starting Bid Plat:
        AddHtmlLocalized(15, y, 175, 22, 1114514, "#1156410", Yellow, false, false);
        AddBackground(200, y, 175, 22, 9350);
        AddTextEntry(202, y, 171, 18, 0, 2, startBid[0] > 0 ? startBid[0].ToString() : "", 9);

        y += 24;

        // Item Starting Bid Gold:
        AddHtmlLocalized(15, y, 175, 22, 1114514, "#1156411", Yellow, false, false);
        AddBackground(200, y, 175, 22, 9350);
        AddTextEntry(202, y, 171, 18, 0, 3, startBid[1] > 0 ? startBid[1].ToString() : "", 9);

        y += 24;

        // Set Starting Bids
        AddHtmlLocalized(200, y, 175, 22, 1156416, Yellow, false, false);
        AddButton(160, y, 4005, 4007, 7, GumpButtonType.Reply, 0);

        y += 26;

        // Buy Now Plat Price: / Buy Now Gold Price:
        AddHtmlLocalized(15, y, 175, 22, 1114514, "#1156413", Yellow, false, false);
        AddBackground(200, y, 175, 22, 9350);
        AddTextEntry(202, y + 2, 171, 18, 0, 4, Auction.BuyoutPlat > 0 ? Auction.BuyoutPlat.ToString() : "", 9);

        y += 26;

        AddHtmlLocalized(15, y, 175, 22, 1114514, "#1156412", Yellow, false, false);
        AddBackground(200, y, 175, 22, 9350);
        AddTextEntry(202, y, 171, 18, 0, 5, Auction.BuyoutGold > 0 ? Auction.BuyoutGold.ToString() : "", 9);

        y += 24;

        // Set Buy Now Price
        AddHtmlLocalized(200, y, 175, 22, 1156417, Yellow, false, false);
        AddButton(160, y, 4005, 4007, 8, GumpButtonType.Reply, 0);

        y += 24;

        if (Auction.AuctionItemOnDisplay() && !Auction.OnGoing)
        {
            // Start Auction
            AddHtmlLocalized(200, y, 175, 22, 1156414, Yellow, false, false);
            AddButton(160, y, 4005, 4007, 9, GumpButtonType.Reply, 0);
        }

        if (Auction.OnGoing && Auction.HighestBid == null)
        {
            // Stop Auction
            AddHtmlLocalized(200, y, 175, 22, 1156415, Yellow, false, false);
            AddButton(160, y, 4005, 4007, 23, GumpButtonType.Reply, 0);
        }
    }

    public int[] GetPlatGold(long amount)
    {
        if (amount < AccountGold.CurrencyThreshold)
        {
            return new[] { 0, (int)amount };
        }

        var plat = (int)(amount / AccountGold.CurrencyThreshold);
        var gold = (int)(amount - plat * (long)AccountGold.CurrencyThreshold);

        return new[] { plat, gold };
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        base.OnResponse(state, info);

        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                {
                    if (Auction == null || !Auction.CheckModifyAuction(from, true))
                    {
                        Refresh();
                        break;
                    }

                    if (Auction.AuctionItem != null)
                    {
                        var item = Auction.AuctionItem;

                        // A reward of ~1_ITEM~ has been placed in your backpack.
                        from.SendLocalizedMessage(1152339, GetItemArg(item));

                        item.Movable = true;
                        from.AddToBackpack(item);
                        Auction.AuctionItem = null;
                    }

                    from.Target = new InternalTarget(Auction, this);
                    break;
                }
            case 2:
                {
                    if (Auction != null && Auction.CheckModifyAuction(from))
                    {
                        var text = info.GetTextEntry(1);

                        if (text != null && BaseGuildGump.CheckProfanity(text, 140))
                        {
                            Auction.Description = text.Trim().FixHtml();
                        }
                        else
                        {
                            from.SendLocalizedMessage(1150315); // That text is unacceptable.
                        }
                    }

                    Refresh();
                    break;
                }
            case 3:
                {
                    if (Auction != null && Auction.CheckModifyAuction(from))
                    {
                        Auction.Duration = 60;
                    }

                    Refresh();
                    break;
                }
            case 4:
                {
                    if (Auction != null && Auction.CheckModifyAuction(from))
                    {
                        Auction.Duration = 4320;
                    }

                    Refresh();
                    break;
                }
            case 5:
                {
                    if (Auction != null && Auction.CheckModifyAuction(from))
                    {
                        Auction.Duration = 7200;
                    }

                    Refresh();
                    break;
                }
            case 6:
                {
                    if (Auction != null && Auction.CheckModifyAuction(from))
                    {
                        Auction.Duration = 10080;
                    }

                    Refresh();
                    break;
                }
            case 7:
                {
                    if (Auction != null && Auction.CheckModifyAuction(from))
                    {
                        var platOk = TryParseAmount(info.GetTextEntry(2), out var plat);
                        var goldOk = TryParseAmount(info.GetTextEntry(3), out var gold);

                        if (platOk && goldOk)
                        {
                            var tempBid = plat * AccountGold.CurrencyThreshold + gold;

                            if (Auction.OnGoing && Auction.BidHistory == null)
                            {
                                Auction.CurrentBid = tempBid;
                            }

                            Auction.StartBid = tempBid;
                        }
                        else
                        {
                            from.SendLocalizedMessage(1150315); // That text is unacceptable.
                        }
                    }

                    Refresh();
                    break;
                }
            case 8:
                {
                    if (Auction != null && Auction.CheckModifyAuction(from))
                    {
                        var platOk = TryParseAmount(info.GetTextEntry(4), out var plat);
                        var goldOk = TryParseAmount(info.GetTextEntry(5), out var gold);

                        if (platOk && goldOk)
                        {
                            Auction.Buyout = plat * AccountGold.CurrencyThreshold + gold;
                        }
                        else
                        {
                            from.SendLocalizedMessage(1150315); // That text is unacceptable.
                        }
                    }

                    Refresh();
                    break;
                }
            case 9:
                {
                    if (Auction == null)
                    {
                        Refresh();
                        break;
                    }

                    if (Auction.StartBid <= 0)
                    {
                        from.SendLocalizedMessage(1156434); // You must set a starting bid.
                    }
                    else
                    {
                        Auction.OnBegin();
                    }

                    Refresh();
                    break;
                }
            case 23:
                {
                    if (Auction is { OnGoing: true, HighestBid: null })
                    {
                        Auction.ClaimPrize(from);
                    }

                    Refresh();
                    break;
                }
        }
    }

    private class InternalTarget : Target
    {
        private readonly Auction _auction;
        private readonly AuctionOwnerGump _gump;

        public InternalTarget(Auction auction, AuctionOwnerGump gump) : base(-1, false, TargetFlags.None)
        {
            _auction = auction;
            _gump = gump;
        }

        private static bool IsBadItem(Item item) =>
            item == null || item.Weight > 300 || item is Container and not BaseQuiver || item is Gold or BankCheck ||
            !item.Movable || item.Items.Count > 0;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_auction.Safe == null || _auction.Safe.Deleted)
            {
                return;
            }

            if (targeted is not Item item || IsBadItem(item))
            {
                from.Target = new InternalTarget(_auction, _gump);
                return;
            }

            if (!item.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                from.Target = new InternalTarget(_auction, _gump);
                return;
            }

            _auction.AuctionItem = item;
            item.Movable = false;
            item.MoveToWorld(new Point3D(_gump.Safe.X, _gump.Safe.Y, _gump.Safe.Z + 7), _gump.Safe.Map);

            _gump.Refresh();
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            _gump.Refresh();
            from.SendLocalizedMessage(1149667); // Invalid target.
        }
    }
}

public class AuctionBidGump : BaseAuctionGump
{
    private long _tempBid;

    private AuctionBidGump(Mobile user, AuctionSafe safe) : base(user, safe, false) => Build();

    public static void DisplayTo(Mobile from, AuctionSafe safe)
    {
        if (from == null || safe?.Deleted != false)
        {
            return;
        }

        if (AuctionStore.Get(safe) == null)
        {
            from.SendLocalizedMessage(1156432); // There is no active auction to complete this action.
            return;
        }

        from.CloseGump<AuctionBidGump>();
        from.SendGump(new AuctionBidGump(from, safe));
    }

    protected override void BuildContent()
    {
        _tempBid = 0;

        if (Auction == null)
        {
            AddHtml(15, 166, 360, 60, Colored("Аукцион на этом сейфе больше не идёт.", HGray), false, false);
            return;
        }

        if (Auction.AuctionItem != null)
        {
            var item = Auction.AuctionItem;
            AddImageTiledButton(200, 166, 0x918, 0x918, 0x0, GumpButtonType.Page, 0, item.ItemID, item.Hue, 23, 5);
            AddItemProperty(item.Serial);
        }

        // Description:
        AddHtmlLocalized(15, 238, 175, 90, 1114514, "#1156400", Yellow, false, false);

        if (string.IsNullOrEmpty(Auction.Description))
        {
            AddHtml(200, 238, 175, 90, Colored("Без описания.", HGray), true, true);
        }
        else
        {
            AddHtml(200, 238, 175, 90, Auction.Description, true, true);
        }

        // Time Remaining:
        AddHtmlLocalized(15, 330, 175, 18, 1114514, "#1156404", Yellow, false, false);

        if (Auction.HasBegun)
        {
            AddRemainingTime(200, 330);
        }
        else
        {
            // Auction Pending
            AddHtmlLocalized(200, 330, 175, 18, 1114513, "#1156440", Gray, false, false);
        }

        // Current Platinum Bid: / Current Gold Bid:
        AddHtmlLocalized(15, 350, 175, 18, 1114514, "#1156436", Yellow, false, false);
        AddHtml(200, 350, 175, 18, Colored(FormatAmount(Auction.CurrentPlatBid), HGray), false, false);

        AddHtmlLocalized(15, 370, 175, 18, 1114514, "#1156435", Yellow, false, false);
        AddHtml(200, 370, 175, 18, Colored(FormatAmount(Auction.CurrentGoldBid), HGray), false, false);

        // Your Current Platinum Bid: / Your Current Gold Bid:
        AddHtmlLocalized(15, 392, 175, 22, 1114514, "#1156406", Yellow, false, false);
        AddBackground(200, 392, 175, 22, 9350);
        AddTextEntry(202, 394, 171, 18, 0, 1, "", 9);

        AddHtmlLocalized(15, 418, 175, 22, 1114514, "#1156405", Yellow, false, false);
        AddBackground(200, 418, 175, 22, 9350);
        AddTextEntry(202, 420, 171, 18, 0, 2, "", 9);

        // Place Bid
        AddHtmlLocalized(200, 442, 175, 22, 1156407, Yellow, false, false);
        AddButton(160, 442, 4005, 4007, 1, GumpButtonType.Reply, 0);

        if (Auction.Buyout > 0 && Auction.HighestBid?.Mobile != _user)
        {
            // Buy Now Plat Price: / Buy Now Gold Price:
            AddHtmlLocalized(15, 484, 175, 18, 1114514, "#1156413", Yellow, false, false);
            AddHtml(200, 484, 175, 18, Colored(FormatAmount(Auction.BuyoutPlat), HGray), false, false);

            AddHtmlLocalized(15, 502, 175, 18, 1114514, "#1156412", Yellow, false, false);
            AddHtml(200, 502, 175, 18, Colored(FormatAmount(Auction.BuyoutGold), HGray), false, false);

            // Buy Now
            AddHtmlLocalized(200, 520, 175, 22, 1156409, Yellow, false, false);
            AddButton(160, 520, 4005, 4007, 2, GumpButtonType.Reply, 0);
        }
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        base.OnResponse(state, info);

        var from = state.Mobile;

        if (from == null || Auction == null)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                {
                    var platOk = TryParseAmount(info.GetTextEntry(1), out var plat);
                    var goldOk = TryParseAmount(info.GetTextEntry(2), out var gold);

                    if (!platOk || !goldOk)
                    {
                        from.SendLocalizedMessage(1150315); // That text is unacceptable.
                        Refresh();
                        break;
                    }

                    _tempBid = plat * AccountGold.CurrencyThreshold + gold;

                    if (Auction.TryPlaceBid(from, _tempBid))
                    {
                        Refresh();

                        if (from is PlayerMobile player)
                        {
                            Auction.ResendGumps(player);
                        }
                    }
                    else
                    {
                        Refresh();
                    }

                    break;
                }
            case 2:
                {
                    Auction.TryBuyout(from);
                    DisplayTo(from, Safe);
                    break;
                }
        }
    }
}

public class AuctionInfoGump : Gump
{
    private AuctionInfoGump() : base(100, 200)
    {
        AddPage(0);

        AddBackground(0, 0, 600, 400, 0x9BF5);

        // <DIV ALIGN=CENTER>~1_TOKEN~</DIV>
        AddHtmlLocalized(50, 10, 500, 18, 1114513, "#1156371", 0x7FFF, false, false);
        AddHtmlLocalized(50, 37, 500, 313, 1156441, 0x4100, true, true);
    }

    public static void DisplayTo(Mobile from)
    {
        if (from == null)
        {
            return;
        }

        from.CloseGump<AuctionInfoGump>();
        from.SendGump(new AuctionInfoGump());
    }
}

public class BidHistoryGump : Gump
{
    private const int Green = 0x208;
    private const string HGray = "BFBFBF";

    private static readonly CultureInfo _amountCulture = CultureInfo.GetCultureInfo("en-US");

    private BidHistoryGump(Auction auction) : base(100, 200)
    {
        AddPage(0);

        AddBackground(0, 0, 600, 400, 0x9BF5);

        // <DIV ALIGN=CENTER>~1_TOKEN~</DIV>
        AddHtmlLocalized(50, 10, 500, 18, 1114513, "#1156422", Green, false, false);

        AddHtmlLocalized(50, 46, 58, 22, 1078924, Green, false, false); // Name:
        AddHtmlLocalized(118, 46, 117, 22, 1156423, Green, false, false); // Platinum Bid:
        AddHtmlLocalized(245, 46, 117, 22, 1156424, Green, false, false); // Gold Bid:
        AddHtmlLocalized(372, 46, 176, 22, 1156425, Green, false, false); // Bid Time:

        var history = auction?.BidHistory;

        if (history == null || history.Count == 0)
        {
            AddHtml(50, 70, 500, 22, Colored("История ставок пока пуста.", HGray), false, false);
            return;
        }

        var y = 70;

        for (var i = history.Count - 1; i >= 0; i--)
        {
            if (i < history.Count - 13)
            {
                break;
            }

            var entry = history[i];
            var bid = i != history.Count - 1 || entry.ShowRealBid ? entry.Bid : auction.CurrentBid;

            var plat = bid >= AccountGold.CurrencyThreshold ? bid / AccountGold.CurrencyThreshold : 0;
            var gold = bid >= AccountGold.CurrencyThreshold
                ? bid - bid / AccountGold.CurrencyThreshold * AccountGold.CurrencyThreshold
                : bid;

            AddHtml(50, y, 58, 22, Colored($"{GetMaskedName(entry.Mobile)}*****", HGray), false, false);
            AddHtml(118, y, 117, 22, Colored(plat.ToString("N0", _amountCulture), HGray), false, false);
            AddHtml(245, y, 117, 22, Colored(gold.ToString("N0", _amountCulture), HGray), false, false);
            AddHtml(
                372,
                y,
                176,
                22,
                Colored(
                    $"{entry.BidTime.Year}-{entry.BidTime.Month}-{entry.BidTime.Day} {entry.BidTime.ToShortTimeString()}",
                    HGray
                ),
                false,
                false
            );

            y += 24;
        }
    }

    public static void DisplayTo(Mobile from, Auction auction)
    {
        if (from == null)
        {
            return;
        }

        from.CloseGump<BidHistoryGump>();
        from.SendGump(new BidHistoryGump(auction));
    }

    private static string GetMaskedName(Mobile mobile)
    {
        var name = mobile?.Name;

        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        return name.Trim()[0].ToString();
    }

    private static string Colored(string text, string color) => $"<basefont color=#{color}>{text}</basefont>";
}
