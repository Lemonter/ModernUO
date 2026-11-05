/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/Time Of Legends/AuctionSafe/BidHistory.cs).
///
///     Строка истории ставок: кто, сколько и когда поставил. <see cref="ShowRealBid" /> отличает ставку,
///     которую можно показать как есть, от скрытой (текущая максимальная ставка в истории маскируется).
///
///     Замены:
///     <list type="bullet">
///         <item>
///             <c>DateTime.Now</c> → <see cref="Core.Now" />, поэтому <see cref="BidTime" /> хранится в UTC
///             (окно истории показывает его как есть).
///         </item>
///         <item>
///             <c>Serialize</c>/<c>Deserialize(GenericReader)</c> убраны: историю пишет полями
///             <see cref="AuctionStore" />.
///         </item>
///     </list>
/// </summary>
using System;

namespace Server.Engines.Auction;

public class HistoryEntry
{
    public HistoryEntry(Mobile mobile, long bid)
    {
        Mobile = mobile;
        Bid = bid;
        BidTime = Core.Now;
    }

    public Mobile Mobile { get; set; }

    public long Bid { get; set; }

    public bool ShowRealBid { get; set; }

    public DateTime BidTime { get; set; }
}
