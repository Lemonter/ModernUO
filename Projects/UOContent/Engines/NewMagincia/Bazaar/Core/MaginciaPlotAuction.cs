/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Core/MaginciaPlotAuction.cs).
///
///     Аукцион аренды прилавка: ставки, срок окончания, определение победителя и раздача проигравшим их
///     залогов. Состояние живёт в памяти и пишется <see cref="BazaarPersistence" /> вместе с участком
///     (<see cref="MaginciaBazaar.SerializeState" />).
///
///     Замены:
///     <list type="bullet">
///         <item><c>[PropertyObject]</c> в этом репозитории нет — атрибут снят.</item>
///         <item><c>DateTime.UtcNow</c> → <see cref="Core.Now" />.</item>
///         <item>
///             <c>String.Format</c> в аргументах cliloc заменён интерполяцией с явным приведением к <c>string</c>:
///             в ModernUO число в дыре подставилось бы с настройками культуры, а клиент ждёт готовый текст.
///         </item>
///         <item>
///             В ServUO слияние ставок «на конкретный прилавок» и «на первый свободный» делалось через
///             <c>Dictionary.Add</c> и падало с исключением, если игрок держал обе ставки. Здесь вторая ставка
///             перезаписывает первую — краша нет, и это ровно то, что предотвращает <c>ConfirmBidGump</c>.
///         </item>
///         <item>
///             <c>m_Plot.HasTempMulti()</c> / <c>RemoveTempPlot()</c> остались, но временный мульти теперь
///             отменяется токеном <see cref="TimerExecutionToken" />.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.NewMagincia;

public class MaginciaPlotAuction
{
    private MaginciaBazaarPlot _plot;
    private DateTime _auctionEnd;

    public MaginciaPlotAuction(MaginciaBazaarPlot plot) : this(plot, MaginciaBazaar.GetShortAuctionTime)
    {
    }

    public MaginciaPlotAuction(MaginciaBazaarPlot plot, TimeSpan auctionDuration)
    {
        _plot = plot;
        _auctionEnd = Core.Now + auctionDuration;
    }

    public Dictionary<Mobile, BidEntry> Auctioners { get; } = new();

    /// <summary>Участок, за который торгуются. Проставляется при десериализации участка.</summary>
    public MaginciaBazaarPlot Plot => _plot;

    public DateTime AuctionEnd => _auctionEnd;

    /// <summary>Свойство только для команды <c>[props</c>: установка значения завершает аукцион.</summary>
    public bool EndCurrentAuction
    {
        get => false;
        set => EndAuction();
    }

    public override string ToString() => $"{_plot?.PlotDef?.ID ?? "?"} ({_auctionEnd})";

    public void MakeBid(Mobile bidder, int amount) => Auctioners[bidder] = new BidEntry(bidder, amount, BidType.Specific);

    public bool RetractBid(Mobile from)
    {
        if (from?.Account is not Account acct)
        {
            return false;
        }

        for (var i = 0; i < acct.Length; i++)
        {
            var mobile = acct[i];

            if (mobile == null)
            {
                continue;
            }

            if (Auctioners.TryGetValue(mobile, out var entry) && entry != null && Banker.Deposit(mobile, entry.Amount))
            {
                Auctioners.Remove(mobile);
                return true;
            }
        }

        return false;
    }

    public void RemoveBid(Mobile from) => Auctioners.Remove(from);

    public int GetHighestBid()
    {
        var highest = -1;

        foreach (var entry in Auctioners.Values)
        {
            if (entry.Amount >= highest)
            {
                highest = entry.Amount;
            }
        }

        return highest;
    }

    public void OnTick()
    {
        if (_auctionEnd < Core.Now)
        {
            EndAuction();
        }
    }

    public void EndAuction()
    {
        if (_plot == null)
        {
            return;
        }

        if (_plot.HasTempMulti())
        {
            _plot.RemoveTempPlot();
        }

        Mobile winner = null;
        var highest = 0;

        var combined = new Dictionary<Mobile, BidEntry>(Auctioners);

        // Ставки «на первый свободный прилавок» участвуют в том же розыгрыше, что и ставки на конкретный.
        foreach (var (mobile, entry) in MaginciaBazaar.NextAvailableBids)
        {
            combined[mobile] = entry;
        }

        foreach (var entry in combined.Values)
        {
            if (entry.Amount > highest)
            {
                highest = entry.Amount;
            }
        }

        // Текущий арендатор может перебить чужую ставку своей «ответной» (match bid), если он не торгуется
        // за другой прилавок.
        if (_plot.Owner != null && MaginciaBazaar.HasReserve(_plot.Owner) && MaginciaBazaar.Enabled
            && !MaginciaBazaar.HasActiveBid(_plot.Owner))
        {
            var matching = MaginciaBazaar.GetBidMatching(_plot.Owner);

            if (matching >= highest)
            {
                MaginciaBazaar.DeductReserve(_plot.Owner, highest);
                var newReserve = MaginciaBazaar.GetBidMatching(_plot.Owner);
                winner = _plot.Owner;

                MaginciaLottoSystem.SendMessageTo(
                    _plot.Owner,
                    new NewMaginciaMessage(
                        null,
                        TextDefinition.Of(1150427),
                        $"@{_plot.PlotDef.ID}@{_plot.PlotDef.Map}@{highest:N0}@{matching:N0}@{newReserve:N0}"
                    )
                );
            }
            else
            {
                MaginciaLottoSystem.SendMessageTo(
                    _plot.Owner,
                    new NewMaginciaMessage(
                        null,
                        TextDefinition.Of(1150528),
                        $"@{_plot.PlotDef.ID}@{_plot.PlotDef.Map}@{matching:N0}"
                    )
                );
            }
        }
        else if (_plot.Owner != null)
        {
            MaginciaLottoSystem.SendMessageTo(
                _plot.Owner,
                new NewMaginciaMessage(
                    null,
                    TextDefinition.Of(1150430),
                    $"@{_plot.PlotDef.ID}@{_plot.PlotDef.Map}"
                )
            );
        }

        if (winner == null)
        {
            var winners = new List<BidEntry>();

            foreach (var entry in combined.Values)
            {
                if (entry.Amount >= highest)
                {
                    winners.Add(entry);
                }
            }

            if (winners.Count == 1)
            {
                winner = winners[0].Bidder;
            }
            else
            {
                var specifics = new List<BidEntry>();

                foreach (var bid in winners)
                {
                    if (bid.BidType == BidType.Specific)
                    {
                        specifics.Add(bid);
                    }
                }

                if (specifics.Count == 1)
                {
                    winner = specifics[0].Bidder;
                }
                else if (specifics.Count > 1)
                {
                    winner = GetOldest(specifics).Bidder;
                }
                else
                {
                    winner = GetOldest(winners)?.Bidder;
                }
            }
        }

        // Проигравшим возвращается залог: сначала в банк, а если он переполнен — банковскими чеками в рюкзак.
        foreach (var (mobile, entry) in Auctioners)
        {
            if (mobile == winner)
            {
                continue;
            }

            if (!Banker.Deposit(mobile, entry.Amount, true) && mobile.Backpack != null)
            {
                var total = entry.Amount;

                while (total > 60000)
                {
                    mobile.Backpack.DropItem(new BankCheck(60000));
                    total -= 60000;
                }

                if (total > 0)
                {
                    mobile.Backpack.DropItem(new BankCheck(total));
                }
            }
        }

        if (winner != null)
        {
            MaginciaBazaar.AwardPlot(this, winner, highest);
        }
        else
        {
            _plot.Reset();
            _plot.NewAuction(MaginciaBazaar.GetShortAuctionTime);
        }
    }

    private static BidEntry GetOldest(List<BidEntry> entries)
    {
        BidEntry oldest = null;

        foreach (var entry in entries)
        {
            if (oldest == null || entry.DatePlaced < oldest.DatePlaced)
            {
                oldest = entry;
            }
        }

        return oldest;
    }

    public int GetBidAmount(Mobile from) => Auctioners.TryGetValue(from, out var entry) ? entry.Amount : 0;

    public void ChangeAuctionTime(TimeSpan ts)
    {
        _auctionEnd = Core.Now + ts;
        _plot?.Sign?.InvalidateProperties();
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия

        writer.Write(_auctionEnd);
        writer.WriteEncodedInt(Auctioners.Count);

        foreach (var (mobile, entry) in Auctioners)
        {
            writer.Write(mobile);
            entry.Serialize(writer);
        }
    }

    public static MaginciaPlotAuction Deserialize(IGenericReader reader, MaginciaBazaarPlot plot)
    {
        reader.ReadEncodedInt(); // версия

        var auction = new MaginciaPlotAuction(plot) { _auctionEnd = reader.ReadDateTime() };

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var entry = BidEntry.Deserialize(reader);

            if (mobile != null)
            {
                auction.Auctioners[mobile] = entry;
            }
        }

        return auction;
    }
}

public enum BidType
{
    Specific,
    NextAvailable
}

public class BidEntry : IComparable
{
    public BidEntry(Mobile bidder, int amount, BidType type)
    {
        Bidder = bidder;
        Amount = amount;
        BidType = type;
        DatePlaced = Core.Now;
    }

    public Mobile Bidder { get; }

    public int Amount { get; }

    public BidType BidType { get; }

    public DateTime DatePlaced { get; private set; }

    public int CompareTo(object obj) => ((BidEntry)obj).Amount - Amount;

    public void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия

        writer.Write(Bidder);
        writer.Write(Amount);
        writer.Write((int)BidType);
        writer.Write(DatePlaced);
    }

    public static BidEntry Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // версия

        var bidder = reader.ReadEntity<Mobile>();
        var amount = reader.ReadInt();
        var type = (BidType)reader.ReadInt();

        return new BidEntry(bidder, amount, type) { DatePlaced = reader.ReadDateTime() };
    }
}
