/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Housing Lotto/Gumps/MaginciaLottoGump.cs).
///
///     Окно покупки билетов лотереи конкретного участка.
///
///     Замены:
///     <list type="bullet">
///         <item>Окно строится только через <see cref="DisplayTo" /> — пустых рамок не бывает.</item>
///         <item><c>String.Format</c> заменён интерполяцией, <c>DateTime.UtcNow</c> → <see cref="Core.Now" />.</item>
///         <item>Собственные строки переведены на русский; cliloc-идентификаторы не тронуты.</item>
///     </list>
/// </summary>
using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.NewMagincia;

public class MaginciaLottoGump : Gump
{
    private const int BlueColor = 0x00BFFF;
    private const int LabelColor = 0xFFFFFF;
    private const int EntryColor = 0xE9967A;

    private readonly Mobile _from;
    private readonly MaginciaHousingPlot _plot;

    private MaginciaLottoGump(Mobile from, MaginciaHousingPlot plot) : base(75, 75)
    {
        _plot = plot;
        _from = from;

        var prime = plot.IsPrimeSpot;

        var ticketsBought = plot.Participants.TryGetValue(from, out var bought) ? bought : 0;

        var totalTicketsSold = 0;

        foreach (var amount in plot.Participants.Values)
        {
            totalTicketsSold += amount;
        }

        AddBackground(0, 0, 350, 380, 9500);

        AddHtmlLocalized(10, 10, 200, 20, 1150460, BlueColor, false, false); // New Magincia Housing Lottery

        AddHtmlLocalized(10, 50, 75, 20, 1150461, BlueColor, false, false); // This Facet:
        AddHtml(170, 50, 100, 16, Colored(plot.Map.ToString(), LabelColor), false, false);

        AddHtmlLocalized(10, 70, 75, 20, 1150462, BlueColor, false, false); // This Plot:
        AddHtml(170, 70, 100, 16, Colored(plot.Identifier, EntryColor), false, false);

        AddHtmlLocalized(10, 95, 130, 20, 1150463, BlueColor, false, false); // Total Tickets Sold:
        AddHtml(170, 95, 100, 16, Colored($"{totalTicketsSold}", EntryColor), false, false);

        AddHtmlLocalized(10, 110, 320, 40, prime ? 1150464 : 1150465, LabelColor, false, false);

        AddHtmlLocalized(10, 160, 90, 20, 1150466, BlueColor, false, false); // Your Tickets:
        AddHtml(170, 160, 100, 20, Colored($"{ticketsBought}", EntryColor), false, false);

        if (ticketsBought == 0)
        {
            // You have not bought a ticket, so you have no chance of winning this plot.
            AddHtmlLocalized(10, 175, 320, 40, 1150467, LabelColor, false, false);
        }
        else
        {
            var odds = totalTicketsSold / ticketsBought;

            // Your chances of winning this plot are currently about 1 in ~1_ODDS~
            AddHtmlLocalized(10, 175, 320, 40, 1150468, $"{odds}", LabelColor, false, false);
        }

        AddHtmlLocalized(10, 225, 115, 20, 1150472, BlueColor, false, false); // Price Per Ticket:
        AddHtml(170, 225, 100, 20, Colored($"{plot.LottoPrice:#,0}", EntryColor), false, false);

        if (plot.LottoOngoing)
        {
            if (!prime)
            {
                AddButton(310, 245, 4005, 4007, 1);
                AddImageTiled(170, 245, 130, 20, 3004);
                AddTextEntry(172, 245, 126, 16, 0, 0, "");
                AddHtmlLocalized(10, 245, 100, 20, 1150477, BlueColor, false, false); // Buy Tickets
            }
            else if (plot.CanPurchaseLottoTicket(from))
            {
                AddButton(125, 245, 4014, 4007, 2);
                AddHtmlLocalized(165, 242, 100, 20, 1150469, BlueColor, false, false); // Buy Ticket
            }
            else
            {
                // You may not purchase another ticket for this plot's lottery.
                AddHtmlLocalized(10, 240, 320, 40, 1150475, LabelColor, false, false);
            }
        }
        else
        {
            AddHtml(10, 240, 320, 40, Colored("Лотерея этого участка сейчас отключена.", LabelColor), false, false);
        }

        var left = plot.LottoEnds - Core.Now;

        // Ticket purchases are NONREFUNDABLE. Odds of winning may vary.
        AddHtmlLocalized(10, 300, 320, 40, 1150476, LabelColor, false, false);

        if (left.Days > 0)
        {
            // There are ~1_DAYS~ days left before the drawing.
            AddHtmlLocalized(10, 340, 320, 20, 1150504, $"{left.Days}", LabelColor, false, false);
        }
        else
        {
            // The lottery drawing will happen in less than 1 day.
            AddHtmlLocalized(10, 340, 320, 20, 1150503, LabelColor, false, false);
        }
    }

    /// <summary>Открывает окно, только если участок существует и лотерея по нему ещё идёт.</summary>
    public static void DisplayTo(Mobile from, MaginciaHousingPlot plot)
    {
        if (from == null || plot == null)
        {
            return;
        }

        from.CloseGump<MaginciaLottoGump>();
        from.SendGump(new MaginciaLottoGump(from, plot));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0)
        {
            return;
        }

        if ((_plot.IsPrimeSpot && !_plot.CanPurchaseLottoTicket(_from)) || !_plot.LottoOngoing)
        {
            return;
        }

        var pricePer = _plot.LottoPrice;
        var total = pricePer;
        var toBuy = 1;

        if (!_plot.IsPrimeSpot && info.ButtonID == 1)
        {
            toBuy = 0;

            var text = info.GetTextEntry(0);

            if (!int.TryParse(text, out toBuy))
            {
                toBuy = 0;
            }

            if (toBuy <= 0)
            {
                return;
            }
        }

        if (toBuy > 1)
        {
            total = toBuy * pricePer;
        }

        if (Banker.Withdraw(_from, total))
        {
            MaginciaLottoSystem.GoldSink += total;
            // Purchase of ~1_COUNT~ ticket(s) at ~2_PRICE~gp each costs a total of ~3_TOTAL~...
            _from.SendLocalizedMessage(1150480, $"{toBuy}\t{pricePer}\t{total}");
            _plot.PurchaseLottoTicket(_from, toBuy);
        }
        else
        {
            // ... You do not have the required funds in your bank box to make the purchase.
            _from.SendLocalizedMessage(1150479, $"{toBuy}\t{pricePer}\t{total}");
        }
    }

    private static string Colored(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";
}
