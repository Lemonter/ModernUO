/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Housing Lotto/Gumps/PlotWinnerGump.cs).
///
///     Поздравление победителя лотереи с кнопкой получения жетона аренды.
///
///     Замены: окно открывается через <see cref="DisplayTo" />, собственные строки переведены на русский.
/// </summary>
using Server.Gumps;
using Server.Network;

namespace Server.Engines.NewMagincia;

public class PlotWinnerGump : Gump
{
    private const int GreenColor = 0x7FFFD4;
    private const int BlueColor = 0x1E90FF;
    private const int EntryColor = 0xFF7F50;

    private readonly MaginciaHousingPlot _plot;

    private PlotWinnerGump(MaginciaHousingPlot plot) : base(75, 75)
    {
        _plot = plot;

        AddBackground(0, 0, 424, 351, 9500);
        AddImage(5, 10, 5411);

        AddHtmlLocalized(170, 13, 150, 16, 1150484, GreenColor, false, false); // WRIT OF LEASE

        var args = $"{plot.Identifier}\t{plot.Map}\t{plot.Bounds.X} {plot.Bounds.Y}";
        AddHtmlLocalized(10, 40, 404, 180, 1150499, args, BlueColor, true, true);

        AddButton(5, 235, 4005, 4007, 1);
        AddHtmlLocalized(50, 235, 150, 16, 1150498, EntryColor, false, false); // CLAIM DEED
    }

    /// <summary>Открывает окно, только если участок уже разыгран.</summary>
    public static void DisplayTo(Mobile from, MaginciaHousingPlot plot)
    {
        if (from == null || plot == null || plot.IsAvailable)
        {
            return;
        }

        from.CloseGump<PlotWinnerGump>();
        from.SendGump(new PlotWinnerGump(plot));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var winner = sender.Mobile;

        if (info.ButtonID != 1 || winner == null)
        {
            return;
        }

        var writ = new WritOfLease(_plot);
        _plot.Writ = writ;
        _plot.Winner = null;

        if (winner.Backpack == null || !winner.Backpack.TryDropItem(winner, writ, false))
        {
            // Your backpack is full, so the deed has been placed in your bank box.
            winner.SendLocalizedMessage(1150501);

            if (winner.BankBox != null)
            {
                winner.BankBox.DropItem(writ);
            }
            else
            {
                writ.Delete();
            }
        }
        else
        {
            winner.SendLocalizedMessage(1150500); // The deed has been placed in your backpack.
        }

        MaginciaLottoSystem.GetWinnerGump(winner);
    }
}
