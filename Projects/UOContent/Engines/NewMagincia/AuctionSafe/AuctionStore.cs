/// <summary>
///     Хранилище аукционов на сейфах: единственное место, где живёт связь «сейф → аукцион», и единственное,
///     что пишет граф аукциона. В ServUO аукцион висел полем на самом предмете-сейфе и сериализовался внутри
///     него; здесь <see cref="AuctionSafePersistence" /> (тот же приём, что в
///     <c>Engines/NewMagincia/Bazaar/Core/BazaarPersistence.cs</c> и
///     <c>Engines/CityLoyalty/CityLoyaltyPersistence.cs</c>) пишет его полями, а ключом служит Serial сейфа.
///
///     Формат файла (версия 0):
///     <code>
///     encoded int  версия
///     encoded int  количество аукционов
///     для каждого:
///         AuctionSafe  сейф (сущность; -1, если удалён)
///         Mobile       владелец
///         Item         выставленный предмет (сущность)
///         long         текущая ставка
///         long         стартовая ставка
///         long         «купить сейчас»
///         string       описание
///         int          длительность, минут
///         DateTime     время начала
///         DateTime     конец периода получения приза
///         bool         идут торги
///         encoded int  количество ставок
///             Mobile + long на каждую
///         encoded int  количество записей истории
///             Mobile + long + bool + DateTime на каждую
///     </code>
///
///     Замены:
///     <list type="bullet">
///         <item>
///             <c>Auction.Configure/Initialize</c> и статический список <c>Auctions</c> заменены этим классом:
///             минутный тик обходит словарь, поэтому отдельный список не нужен.
///         </item>
///         <item>
///             <c>HighestBid</c> не пишется: как и в ServUO, он восстанавливается из ставок с
///             <c>CurrentBid &gt; 0</c>.
///         </item>
///         <item><c>ColUtility.Free</c> в этом репозитории нет — буфер тика просто переиспользуется.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.Auction;

public static class AuctionStore
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(AuctionStore));

    private static readonly Dictionary<Serial, Auction> _auctions = new();
    private static readonly List<Auction> _tickBuffer = new();

    private static AuctionSafePersistence _persistence;
    private static TimerExecutionToken _tickToken;

    public static int Count => _auctions.Count;

    public static void Configure() => _persistence = new AuctionSafePersistence();

    public static void Initialize() =>
        Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), Tick, out _tickToken);

    public static Auction Get(AuctionSafe safe) =>
        safe != null && _auctions.TryGetValue(safe.Serial, out var auction) ? auction : null;

    public static Auction GetOrCreate(AuctionSafe safe, Mobile owner)
    {
        var auction = Get(safe);

        if (auction != null || safe == null || safe.Deleted)
        {
            return auction;
        }

        auction = new Auction(owner, safe);
        _auctions[safe.Serial] = auction;

        return auction;
    }

    public static void Set(AuctionSafe safe, Auction auction)
    {
        if (safe == null)
        {
            return;
        }

        if (auction == null)
        {
            _auctions.Remove(safe.Serial);
            return;
        }

        auction.Safe = safe;
        _auctions[safe.Serial] = auction;
    }

    public static void Remove(AuctionSafe safe)
    {
        if (safe != null)
        {
            _auctions.Remove(safe.Serial);
        }
    }

    public static void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия
        writer.WriteEncodedInt(_auctions.Count);

        foreach (var auction in _auctions.Values)
        {
            writer.Write(auction.Safe);
            writer.Write(auction.Owner);
            writer.Write(auction.AuctionItem);
            writer.Write(auction.CurrentBid);
            writer.Write(auction.StartBid);
            writer.Write(auction.Buyout);
            writer.Write(auction.Description);
            writer.Write(auction.Duration);
            writer.Write(auction.StartTime);
            writer.Write(auction.ClaimPeriod);
            writer.Write(auction.OnGoing);

            var bids = auction.Bids;
            writer.WriteEncodedInt(bids?.Count ?? 0);

            if (bids != null)
            {
                for (var i = 0; i < bids.Count; i++)
                {
                    writer.Write(bids[i].Mobile);
                    writer.Write(bids[i].CurrentBid);
                }
            }

            var history = auction.BidHistory;
            writer.WriteEncodedInt(history?.Count ?? 0);

            if (history != null)
            {
                for (var i = 0; i < history.Count; i++)
                {
                    writer.Write(history[i].Mobile);
                    writer.Write(history[i].Bid);
                    writer.Write(history[i].ShowRealBid);
                    writer.Write(history[i].BidTime);
                }
            }
        }
    }

    public static void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // версия

        _auctions.Clear();

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var safe = reader.ReadEntity<AuctionSafe>();
            var owner = reader.ReadEntity<Mobile>();
            var auctionItem = reader.ReadEntity<Item>();

            var auction = new Auction(owner, safe)
            {
                AuctionItem = auctionItem
            };

            auction.CurrentBid = reader.ReadLong();
            auction.StartBid = reader.ReadLong();
            auction.Buyout = reader.ReadLong();
            auction.Description = reader.ReadString();
            auction.Duration = reader.ReadInt();
            auction.StartTime = reader.ReadDateTime();
            auction.ClaimPeriod = reader.ReadDateTime();
            auction.OnGoing = reader.ReadBool();

            var bidCount = reader.ReadEncodedInt();

            for (var j = 0; j < bidCount; j++)
            {
                var mobile = reader.ReadEntity<Mobile>();
                var bid = reader.ReadLong();

                if (mobile is not PlayerMobile player)
                {
                    continue;
                }

                var entry = new BidEntry(player, bid);
                auction.Bids.Add(entry);

                if (bid > 0 && (auction.HighestBid == null || bid > auction.HighestBid.CurrentBid))
                {
                    auction.HighestBid = entry;
                }
            }

            var historyCount = reader.ReadEncodedInt();

            for (var j = 0; j < historyCount; j++)
            {
                var mobile = reader.ReadEntity<Mobile>();
                var bid = reader.ReadLong();
                var showRealBid = reader.ReadBool();
                var bidTime = reader.ReadDateTime();

                if (mobile == null)
                {
                    continue;
                }

                auction.BidHistory ??= new List<HistoryEntry>();

                auction.BidHistory.Add(
                    new HistoryEntry(mobile, bid)
                    {
                        ShowRealBid = showRealBid,
                        BidTime = bidTime
                    }
                );
            }

            if (safe == null || safe.Deleted)
            {
                _logger.Warning("Auction for a missing auction safe was dropped during load");
                auction.AuctionItem?.Delete();
                continue;
            }

            safe.Auction = auction;
        }
    }

    private static void Remove(Auction auction)
    {
        foreach (var pair in _auctions)
        {
            if (pair.Value == auction)
            {
                _auctions.Remove(pair.Key);
                return;
            }
        }
    }

    private static void Tick()
    {
        if (_auctions.Count == 0)
        {
            return;
        }

        _tickBuffer.Clear();
        _tickBuffer.AddRange(_auctions.Values);

        for (var i = 0; i < _tickBuffer.Count; i++)
        {
            var auction = _tickBuffer[i];

            if (auction.Safe == null || auction.Safe.Deleted)
            {
                Remove(auction);
            }
            else if (auction.OnGoing && !auction.InClaimPeriod && auction.EndTime < Core.Now)
            {
                auction.EndAuction();
            }
            else if (auction.InClaimPeriod && Core.Now > auction.ClaimPeriod)
            {
                auction.EndClaimPeriod();
            }
        }

        _tickBuffer.Clear();
    }
}
