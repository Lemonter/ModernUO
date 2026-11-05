/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Housing Lotto/Core/MaginciaHousingPlot.cs).
///
///     Участок под дом на Новой Маджинции: границы, камень-указатель, лотерея и выданный жетон аренды.
///     В ServUO участки сериализовались внутри предмета-контроллера; здесь их пишет
///     <see cref="MaginciaLottoPersistence" /> через <see cref="MaginciaLottoSystem" />.
///
///     Замены:
///     <list type="bullet">
///         <item><c>[PropertyObject]</c> в этом репозитории нет — атрибут снят.</item>
///         <item><c>DateTime.UtcNow</c> → <see cref="Core.Now" />.</item>
///         <item>Таймерный обратный вызов <c>SendMessage_Callback</c> не нужен: сообщение отправляется напрямую.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Accounting;
using Server.Gumps;

namespace Server.Engines.NewMagincia;

public class MaginciaHousingPlot
{
    private readonly List<Mobile> _raffle = new();

    public MaginciaHousingPlot(string identifier, Rectangle2D bounds, bool prime, Map map)
    {
        Identifier = identifier;
        Bounds = bounds;
        IsPrimeSpot = prime;
        Map = map;
        Expires = DateTime.MinValue;
        Participants = new Dictionary<Mobile, int>();
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public string Identifier { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public WritOfLease Writ { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Rectangle2D Bounds { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public MaginciaPlotStone Stone { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsPrimeSpot { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Complete { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile Winner { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Map Map { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime Expires { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D RecallLoc => new(Bounds.X, Bounds.Y, Map.GetAverageZ(Bounds.X, Bounds.Y));

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsAvailable => !Complete;

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime LottoEnds { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int LottoPrice => IsPrimeSpot ? 10000 : 2000;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool LottoOngoing => IsAvailable && LottoEnds > Core.Now && LottoEnds != DateTime.MinValue;

    public Dictionary<Mobile, int> Participants { get; }

    public override string ToString() => Identifier;

    public void AddPlotStone() => AddPlotStone(MaginciaLottoSystem.GetPlotStoneLoc(this));

    public void AddPlotStone(Point3D location)
    {
        Stone = new MaginciaPlotStone { Plot = this };
        Stone.MoveToWorld(location, Map);
    }

    public bool CanPurchaseLottoTicket(Mobile from)
    {
        if (!IsPrimeSpot)
        {
            return true;
        }

        return from.Account is Account account && CheckAccount(account);
    }

    /// <summary>На премиальный участок — не больше одного билета с аккаунта.</summary>
    private static bool CheckAccount(Account account)
    {
        for (var i = 0; i < account.Length; i++)
        {
            var mobile = account[i];

            if (mobile == null)
            {
                continue;
            }

            foreach (var plot in MaginciaLottoSystem.Plots)
            {
                if (plot.IsPrimeSpot && plot.Participants.ContainsKey(mobile))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void PurchaseLottoTicket(Mobile from, int amount)
    {
        Participants[from] = Participants.TryGetValue(from, out var current) ? current + amount : amount;
    }

    public void EndLotto()
    {
        if (Participants.Count == 0)
        {
            ResetLotto();
            return;
        }

        _raffle.Clear();

        foreach (var (mobile, tickets) in Participants)
        {
            for (var i = 0; i < tickets; i++)
            {
                _raffle.Add(mobile);
            }
        }

        if (_raffle.Count == 0)
        {
            ResetLotto();
        }
        else
        {
            OnLottoComplete(_raffle[Utility.Random(_raffle.Count)]);
        }

        _raffle.Clear();
        Participants.Clear();
    }

    public void OnLottoComplete(Mobile winner)
    {
        Complete = true;
        Winner = winner;
        LottoEnds = DateTime.MinValue;
        Expires = Core.Now + TimeSpan.FromDays(MaginciaLottoSystem.WritExpirePeriod);

        if (winner == null || winner.Account is not Account account)
        {
            return;
        }

        for (var i = 0; i < account.Length; i++)
        {
            var mobile = account[i];

            if (mobile?.NetState != null)
            {
                PlotWinnerGump.DisplayTo(winner, this);
                break;
            }
        }
    }

    public void ResetLotto()
    {
        LottoEnds = MaginciaLottoSystem.AutoResetLotto
            ? Core.Now + MaginciaLottoSystem.LottoDuration
            : DateTime.MinValue;
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия

        writer.Write(Identifier);
        writer.Write(Writ);
        writer.Write(Stone);
        writer.Write(LottoEnds);
        writer.Write(Bounds);
        writer.Write(Map);
        writer.Write(IsPrimeSpot);
        writer.Write(Complete);
        writer.Write(Winner);
        writer.Write(Expires);

        writer.WriteEncodedInt(Participants.Count);

        foreach (var (mobile, tickets) in Participants)
        {
            writer.Write(mobile);
            writer.Write(tickets);
        }
    }

    public static MaginciaHousingPlot Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // версия

        var identifier = reader.ReadString();
        var writ = reader.ReadEntity<WritOfLease>();
        var stone = reader.ReadEntity<MaginciaPlotStone>();
        var lottoEnds = reader.ReadDateTime();
        var bounds = reader.ReadRect2D();
        var map = reader.ReadMap();
        var prime = reader.ReadBool();
        var complete = reader.ReadBool();
        var winner = reader.ReadEntity<Mobile>();
        var expires = reader.ReadDateTime();

        var plot = new MaginciaHousingPlot(identifier, bounds, prime, map)
        {
            Writ = writ,
            Stone = stone,
            LottoEnds = lottoEnds,
            Complete = complete,
            Winner = winner,
            Expires = expires
        };

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var tickets = reader.ReadInt();

            if (mobile != null)
            {
                plot.Participants[mobile] = tickets;
            }
        }

        if (writ != null)
        {
            writ.Plot = plot;
        }

        if (stone != null && !stone.Deleted)
        {
            stone.Plot = plot;
        }
        else if (plot.LottoOngoing)
        {
            plot.AddPlotStone();
        }

        return plot;
    }
}
