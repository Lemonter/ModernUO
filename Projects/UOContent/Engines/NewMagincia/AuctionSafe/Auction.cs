/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/Time Of Legends/AuctionSafe/Auction.cs).
///
///     Аукцион на сейфе-аукционе: выставленный предмет, стартовая ставка, «купить сейчас», текущие ставки и их
///     история. Аукцион не является предметом и не имеет своего файла: его граф пишет
///     <see cref="AuctionSafePersistence" />, а живые объекты лежат в <see cref="AuctionStore" /> под Serial сейфа.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             Статический список <c>Auctions</c>, <c>Configure</c>/<c>Initialize</c> и минутный тик переехали в
///             <see cref="AuctionStore" />: он и есть единственный источник истины «сейф → аукцион», поэтому
///             второй список больше не нужен. <c>IDisposable</c>/финализатор и <c>ColUtility.Free</c> убраны —
///             в этом репозитории возвращать в пулы нечего.
///         </item>
///         <item>
///             <c>DateTime.Now</c>/<c>DateTime.UtcNow</c> → <see cref="Core.Now" /> (UTC); сроки аукциона и период
///             получения приза считаются от него же.
///         </item>
///         <item>
///             <c>Account</c> → <see cref="IAccount" /> (<c>Mobile.Account</c> здесь именно этого типа),
///             <c>Account.CurrencyThreshold</c> → <see cref="AccountGold.CurrencyThreshold" />. Списание и
///             зачисление разбиваются на платину и золото: <c>WithdrawGold/DepositGold</c> принимают только
///             <c>int</c> и не переходят через порог платины.
///         </item>
///         <item>
///             <c>[CommandProperty]</c>/<c>[PropertyObject]</c> на членах аукциона убраны: аукцион не поле предмета,
///             и <c>[props</c> его всё равно не увидит. <c>[CommandProperty] Auction</c> остался на самом сейфе.
///         </item>
///         <item>
///             <c>String.Format</c> → интерполяция, суммы — с культурой en-US, как в ServUO (разделитель тысяч не
///             зависит от локали сервера). <c>Utility.ToInt64</c> в этом репозитории нет — в окнах используется
///             <c>long.TryParse</c>.
///         </item>
///         <item>
///             Проверка средств по-прежнему идёт через <see cref="Banker.GetBalance" /> (счёт + банк), а списание —
///             только из валюты счёта: при выключенном <see cref="AccountGold.Enabled" /> ставка с золотом в
///             банке не пройдёт. Так же ведёт себя ServUO; это не менялось.
///         </item>
///         <item>
///             Карта аукциона (<see cref="AuctionMap" />) выдаётся в <see cref="TryPlaceBid" />, как в ServUO;
///             <see cref="CloseGumps" /> и <see cref="ResendGumps" /> работают с окнами из <c>Gumps.cs</c>.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using System.Globalization;
using Server.Accounting;
using Server.Engines.NewMagincia;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Auction;

public class Auction
{
    public const int DefaultDuration = 10080;

    private static readonly CultureInfo _amountCulture = CultureInfo.GetCultureInfo("en-US");

    public Auction(Mobile owner, AuctionSafe safe)
    {
        Safe = safe;
        Owner = owner;
        Bids = new List<BidEntry>();
        Duration = DefaultDuration;
    }

    public Mobile Owner { get; set; }

    public AuctionSafe Safe { get; set; }

    public Item AuctionItem { get; set; }

    public long CurrentBid { get; set; }

    public long StartBid { get; set; }

    public long Buyout { get; set; }

    public string Description { get; set; }

    public int Duration { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime => StartTime + TimeSpan.FromMinutes(Duration);

    public DateTime ClaimPeriod { get; set; }

    public bool OnGoing { get; set; }

    public List<BidEntry> Bids { get; set; }

    public BidEntry HighestBid { get; set; }

    public List<HistoryEntry> BidHistory { get; set; }

    public List<PlayerMobile> Viewers { get; set; }

    public bool HasBegun => StartTime != DateTime.MinValue;

    public bool InClaimPeriod => HasBegun && ClaimPeriod != DateTime.MinValue;

    public bool CanModify => !HasBegun;

    public int CurrentGoldBid => (int)(CurrentBid >= AccountGold.CurrencyThreshold
        ? CurrentBid - CurrentPlatBid * AccountGold.CurrencyThreshold
        : CurrentBid);

    public int CurrentPlatBid => (int)(CurrentBid >= AccountGold.CurrencyThreshold
        ? CurrentBid / AccountGold.CurrencyThreshold
        : 0);

    public int BuyoutGold => (int)(Buyout >= AccountGold.CurrencyThreshold
        ? Buyout - BuyoutPlat * AccountGold.CurrencyThreshold
        : Buyout);

    public int BuyoutPlat => (int)(Buyout >= AccountGold.CurrencyThreshold
        ? Buyout / AccountGold.CurrencyThreshold
        : 0);

    public static string FormatAmount(long amount) => amount.ToString("N0", _amountCulture);

    public bool AuctionItemOnDisplay()
    {
        if (AuctionItem == null || AuctionItem.Deleted || Safe == null)
        {
            return false;
        }

        return !AuctionItem.Movable && AuctionItem.X == Safe.X && AuctionItem.Y == Safe.Y &&
               AuctionItem.Z == Safe.Z + 7;
    }

    public void OnBegin()
    {
        if (Safe == null || Safe.Deleted)
        {
            return;
        }

        if (!OnGoing)
        {
            BidHistory = null;
        }

        StartTime = Core.Now;
        OnGoing = true;
        CurrentBid = StartBid;
    }

    public bool CheckModifyAuction(Mobile m, bool checkingItem = false)
    {
        if (Safe == null)
        {
            return false;
        }

        if (AuctionItem != null && InClaimPeriod)
        {
            // You must wait for your auctioned item to be claimed before modifying this auction safe /
            // before adding a new item.
            m.SendLocalizedMessage(checkingItem ? 1156429 : 1156430);
            return false;
        }

        if (!CanModify)
        {
            m.SendLocalizedMessage(1156431); // You cannot modify this while an auction is in progress.
            return false;
        }

        return true;
    }

    public bool TryPlaceBid(Mobile m, long bidTotal)
    {
        if (!OnGoing || InClaimPeriod)
        {
            m.SendLocalizedMessage(1156432); // There is no active auction to complete this action.
            return false;
        }

        var entry = GetBidEntry(m);

        if (entry == null)
        {
            return false;
        }

        var highestBid = HighestBid?.CurrentBid ?? CurrentBid;

        if (Banker.GetBalance(m) < bidTotal)
        {
            // The amount entered is invalid. Verify that there are sufficient funds.
            m.SendLocalizedMessage(1155867);
            return false;
        }

        if (bidTotal < entry.CurrentBid || entry == HighestBid || bidTotal <= highestBid)
        {
            m.SendLocalizedMessage(1156445); // You have been out bid.
            return false;
        }

        if (!Withdraw(m, bidTotal))
        {
            m.SendLocalizedMessage(1155867);
            return false;
        }

        entry.CurrentBid = bidTotal;
        CurrentBid = highestBid + 1;

        if (HighestBid != null)
        {
            // You have been out bid in an auction for ~1_ITEMNAME~. The current winning bid amount is
            // ~2_BIDAMT~plat and ~3_BIDAMT~gp.
            var message = new NewMaginciaMessage(
                null,
                TextDefinition.Of(1156427),
                $"{GetItemName()}\t{FormatAmount(CurrentPlatBid)}\t{FormatAmount(CurrentGoldBid)}"
            );

            MaginciaLottoSystem.SendMessageTo(HighestBid.Mobile, message);

            Deposit(HighestBid.Mobile, HighestBid.CurrentBid);
            HighestBid.CurrentBid = 0;
        }

        m.SendLocalizedMessage(1156433); // Your bid has been placed.

        var map = new AuctionMap(Safe);

        if (m.Backpack == null || !m.Backpack.TryDropItem(m, map, false))
        {
            map.Delete();
        }
        else
        {
            m.SendLocalizedMessage(1156478); // The auction safe map has been placed in your backpack.
        }

        HighestBid = entry;
        AddToHistory(m, entry.CurrentBid);

        return true;
    }

    public bool TryBuyout(Mobile m)
    {
        if (!OnGoing || InClaimPeriod || Buyout <= 0)
        {
            return false;
        }

        if (!Withdraw(m, Buyout))
        {
            // The amount entered is invalid. Verify that there are sufficient funds.
            m.SendLocalizedMessage(1155867);
            return false;
        }

        HighestBid = GetBidEntry(m, true);

        if (HighestBid == null)
        {
            Deposit(m, Buyout);
            return false;
        }

        HighestBid.CurrentBid = Buyout - (long)((double)Buyout * .05);
        CurrentBid = Buyout;

        EndAuction();
        ClaimPrize(m);

        return true;
    }

    public BidEntry GetBidEntry(Mobile m, bool create = true)
    {
        Bids ??= new List<BidEntry>();

        BidEntry entry = null;

        for (var i = 0; i < Bids.Count; i++)
        {
            if (Bids[i].Mobile == m)
            {
                entry = Bids[i];
                break;
            }
        }

        if (entry == null && create && m is PlayerMobile player)
        {
            entry = new BidEntry(player);
            Bids.Add(entry);
        }

        return entry;
    }

    public void EndAuction()
    {
        if (HighestBid?.Mobile != null)
        {
            var name = GetItemName();

            // You have won an auction for ~1_ITEMNAME~! Your bid amount of ~2_BIDAMT~plat and ~3_BIDAMT~gp
            // won the auction.
            var message = new NewMaginciaMessage(
                null,
                TextDefinition.Of(1156426),
                TimeSpan.FromHours(24),
                $"{name}\t{FormatAmount(CurrentPlatBid)}\t{FormatAmount(CurrentGoldBid)}"
            );

            MaginciaLottoSystem.SendMessageTo(HighestBid.Mobile, message);

            var difference = HighestBid.CurrentBid - CurrentBid;

            if (difference > 0)
            {
                Deposit(HighestBid.Mobile, difference);
            }

            Deposit(Owner, HighestBid.CurrentBid);

            // Your auction for ~1_ITEMNAME~ has ended with a winning bid of ~2_BIDAMT~plat and ~3_BIDAMT~gp.
            message = new NewMaginciaMessage(
                null,
                TextDefinition.Of(1156428),
                TimeSpan.FromHours(24),
                $"{name}\t{FormatAmount(CurrentPlatBid)}\t{FormatAmount(CurrentGoldBid)}"
            );

            MaginciaLottoSystem.SendMessageTo(Owner, message);

            ClaimPeriod = Core.Now + TimeSpan.FromDays(3);
        }
        else
        {
            TrayAuction();
        }

        CloseGumps();
    }

    public void CloseGumps()
    {
        if (Viewers == null)
        {
            return;
        }

        for (var i = 0; i < Viewers.Count; i++)
        {
            var viewer = Viewers[i];

            if (viewer == null)
            {
                continue;
            }

            viewer.CloseGump<AuctionBidGump>();
            viewer.CloseGump<AuctionOwnerGump>();
        }

        Viewers.Clear();
    }

    public void ResendGumps(PlayerMobile player)
    {
        if (Viewers == null)
        {
            return;
        }

        for (var i = 0; i < Viewers.Count; i++)
        {
            var viewer = Viewers[i];

            if (viewer == null || viewer == player || viewer.NetState == null)
            {
                continue;
            }

            var gump = viewer.FindGump<AuctionBidGump>();

            if (gump == null)
            {
                AuctionBidGump.DisplayTo(viewer, Safe);
            }
            else
            {
                gump.Refresh();
            }
        }
    }

    public void AddViewer(PlayerMobile player)
    {
        Viewers ??= new List<PlayerMobile>();

        if (!Viewers.Contains(player))
        {
            Viewers.Add(player);
        }
    }

    public void RemoveViewer(PlayerMobile player)
    {
        if (Viewers == null)
        {
            return;
        }

        Viewers.Remove(player);

        if (Viewers.Count == 0)
        {
            Viewers = null;
        }
    }

    public void HouseCollapse()
    {
        var item = AuctionItem;

        if (Bids != null)
        {
            for (var i = 0; i < Bids.Count; i++)
            {
                var bid = Bids[i];

                // Your winning bid amount of ~1_BIDAMT~plat and ~2_BIDAMT~gp for ~3_ITEMNAME~ has been
                // refunded to you due to house collapse.
                var message = new NewMaginciaMessage(
                    null,
                    TextDefinition.Of(1156454),
                    $"{FormatAmount(CurrentPlatBid)}\t{FormatAmount(CurrentGoldBid)}\t{GetItemName()}"
                );

                MaginciaLottoSystem.SendMessageTo(bid.Mobile, message);
                Deposit(bid.Mobile, bid.CurrentBid);
            }

            RemoveAuction();
        }

        if (item != null)
        {
            item.Movable = true;
            Owner?.BankBox?.DropItem(item);
        }
    }

    public void ClaimPrize(Mobile m)
    {
        if (AuctionItem != null)
        {
            AuctionItem.Movable = true;

            if (m.Backpack == null || !m.Backpack.TryDropItem(m, AuctionItem, false))
            {
                m.BankBox.DropItem(AuctionItem);

                // A reward of ~1_ITEM~ has been placed in your bank.
                m.SendLocalizedMessage(1156322, GetClaimArg());
            }
            else
            {
                // A reward of ~1_ITEM~ has been placed in your backpack.
                m.SendLocalizedMessage(1152339, GetClaimArg());
            }

            AuctionItem = null;
        }

        RemoveAuction();

        if (Safe != null && !Safe.Deleted)
        {
            Safe.Auction = new Auction(Owner, Safe);
        }
    }

    public void EndClaimPeriod()
    {
        if (AuctionItem != null)
        {
            TrayAuction();
        }
    }

    public void TrayAuction()
    {
        OnGoing = false;
        ClaimPeriod = DateTime.MinValue;
        StartTime = DateTime.MinValue;

        Bids = new List<BidEntry>();
        HighestBid = null;
        CurrentBid = 0;
        StartBid = 0;
        Buyout = 0;
        Duration = DefaultDuration;
    }

    public void RemoveAuction()
    {
        OnGoing = false;
        AuctionItem = null;
        ClaimPeriod = DateTime.MinValue;

        AuctionStore.Remove(Safe);
    }

    private string GetClaimArg()
    {
        if (AuctionItem?.LabelNumber > 0)
        {
            return $"#{AuctionItem.LabelNumber}";
        }

        return AuctionItem?.Name ?? "Неизвестный предмет";
    }

    private string GetItemName()
    {
        if (AuctionItem == null || AuctionItem.Deleted)
        {
            return "Неизвестный предмет";
        }

        if (!string.IsNullOrEmpty(AuctionItem.Name))
        {
            return AuctionItem.Name;
        }

        return AuctionItem.LabelNumber > 0 ? $"#{AuctionItem.LabelNumber}" : "Неизвестный предмет";
    }

    private void AddToHistory(Mobile m, long highBid)
    {
        BidHistory ??= new List<HistoryEntry>();

        BidHistory.Add(new HistoryEntry(m, highBid));
    }

    /// <summary>Списывает суммы в золотом эквиваленте, разбивая их на платину и золото.</summary>
    private static bool Withdraw(Mobile m, long amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (m?.Account is not { } account)
        {
            return false;
        }

        var plat = (int)(amount / AccountGold.CurrencyThreshold);
        var gold = (int)(amount % AccountGold.CurrencyThreshold);

        if (plat > 0 && !account.WithdrawPlat(plat))
        {
            return false;
        }

        if (gold > 0 && !account.WithdrawGold(gold))
        {
            if (plat > 0)
            {
                account.DepositPlat(plat);
            }

            return false;
        }

        return true;
    }

    /// <summary>Зачисляет суммы в золотом эквиваленте, разбивая их на платину и золото.</summary>
    private static void Deposit(Mobile m, long amount)
    {
        if (amount <= 0 || m?.Account is not { } account)
        {
            return;
        }

        var plat = (int)(amount / AccountGold.CurrencyThreshold);
        var gold = (int)(amount % AccountGold.CurrencyThreshold);

        if (plat > 0)
        {
            account.DepositPlat(plat);
        }

        if (gold > 0)
        {
            account.DepositGold(gold);
        }
    }
}
