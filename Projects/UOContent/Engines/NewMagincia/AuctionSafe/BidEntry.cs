/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/Time Of Legends/AuctionSafe/BidEntry.cs).
///
///     Ставка одного участника аукциона: кто поставил и сколько. Сумма хранится в «золотом эквиваленте»
///     (платина пересчитана по <see cref="AccountGold.CurrencyThreshold" />), а свойства ниже разбивают её
///     обратно на платину и золото для отображения.
///
///     Замены:
///     <list type="bullet">
///         <item><c>Account.CurrencyThreshold</c> → <see cref="AccountGold.CurrencyThreshold" />.</item>
///         <item>
///             <c>Serialize</c>/<c>Deserialize(GenericReader)</c> убраны: граф аукциона пишется полями в
///             <see cref="AuctionStore" />, отдельный формат записи ставке не нужен.
///         </item>
///         <item><c>CompareTo</c> защищён от <c>null</c>: сортировка больше не падает на пустом элементе.</item>
///     </list>
/// </summary>
using System;
using Server.Accounting;
using Server.Mobiles;

namespace Server.Engines.Auction;

public class BidEntry : IComparable<BidEntry>
{
    public BidEntry(PlayerMobile mobile, long bid = 0)
    {
        Mobile = mobile;
        CurrentBid = bid;
    }

    public PlayerMobile Mobile { get; set; }

    public long CurrentBid { get; set; }

    public int TotalGoldBid => (int)(CurrentBid >= AccountGold.CurrencyThreshold
        ? CurrentBid - TotalPlatBid * AccountGold.CurrencyThreshold
        : CurrentBid);

    public int TotalPlatBid => (int)(CurrentBid >= AccountGold.CurrencyThreshold
        ? CurrentBid / AccountGold.CurrencyThreshold
        : 0);

    public int CompareTo(BidEntry other)
    {
        if (other == null)
        {
            return 1;
        }

        return CurrentBid > other.CurrentBid ? 1 : 0;
    }
}
