/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Housing Lotto/Gumps/LottoTrackingGump.cs).
///
///     Служебное окно администратора: список всех участков с числом билетов и переход к деталям по участку.
///
///     Замены:
///     <list type="bullet">
///         <item>Собственные подписи переведены на русский (ID/Facet/#bids → Лот/Фасет/Билеты).</item>
///         <item><c>String.Format</c> → интерполяция; окно строится только через <see cref="DisplayTo" />.</item>
///         <item><c>MaginciaLottoSystem.Instance</c> больше нет — система статическая.</item>
///     </list>
/// </summary>
using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.NewMagincia;

public class LottoTrackingGump : Gump
{
    private const int LabelColor = 0xFFFFFF;

    private readonly List<MaginciaHousingPlot> _list;

    private LottoTrackingGump() : base(50, 50)
    {
        AddBackground(0, 0, 410, 564, 9500);

        AddHtml(150, 10, 250, 20, "<DIV ALIGN=RIGHT><BASEFONT COLOR=#FFFFFF>Отслеживание лотереи Новой Маджинции</DIV>", false, false);
        AddHtml(10, 10, 205, 20, Colored($"Золотой сток: {MaginciaLottoSystem.GoldSink:#,0}", LabelColor), false, false);

        AddHeaders();

        _list = new List<MaginciaHousingPlot>(MaginciaLottoSystem.Plots);

        var y = 60;
        var x = 0;

        for (var i = 0; i < _list.Count; i++)
        {
            var plot = _list[i];

            if (plot == null)
            {
                continue;
            }

            var bids = 0;

            foreach (var bid in plot.Participants.Values)
            {
                bids += bid;
            }

            AddButton(10 + x, y, 4005, 4007, i + 5);
            AddHtml(45 + x, y, 40, 22, Colored(plot.Identifier, LabelColor), false, false);
            AddHtml(85 + x, y, 60, 22, Colored(plot.Map?.ToString() ?? "-", LabelColor), false, false);
            AddHtml(145 + x, y, 40, 22, Colored($"{bids}", LabelColor), false, false);

            if (i == 21)
            {
                y = 60;
                x = 200;
                AddHeaders(x);
            }
            else
            {
                y += 22;
            }
        }
    }

    public static void DisplayTo(Mobile from)
    {
        if (from == null)
        {
            return;
        }

        from.CloseGump<LottoTrackingGump>();
        from.SendGump(new LottoTrackingGump());
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || info.ButtonID < 5 || from.AccessLevel <= AccessLevel.Player)
        {
            return;
        }

        var index = info.ButtonID - 5;

        if (index < _list.Count && _list[index] != null)
        {
            PlotTrackingGump.DisplayTo(from, _list[index]);
        }
    }

    private void AddHeaders(int offset = 0)
    {
        AddHtml(45 + offset, 40, 40, 20, Colored("Лот", LabelColor), false, false);
        AddHtml(85 + offset, 40, 60, 20, Colored("Фасет", LabelColor), false, false);
        AddHtml(145 + offset, 40, 60, 20, Colored("Билеты", LabelColor), false, false);
    }

    private static string Colored(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";
}

/// <summary>
///     Детали одного участка: кто сколько билетов купил и на какую сумму.
/// </summary>
public class PlotTrackingGump : Gump
{
    private const int LabelColor = 0xFFFFFF;

    private readonly MaginciaHousingPlot _plot;

    private PlotTrackingGump(MaginciaHousingPlot plot) : base(50, 50)
    {
        _plot = plot;

        const int width = 600;
        const int height = 544;

        AddBackground(0, 0, width, height, 9500);

        AddHtml(10, 10, 580, 20, $"<CENTER><BASEFONT COLOR=#FFFFFF>Участок {plot.Identifier}</CENTER>", false, false);

        AddHtml(10, 10, 260, 20, Colored($"Золотой сток: {MaginciaLottoSystem.GoldSink:#,0}", LabelColor), false, false);

        var x = 0;
        var y = 60;
        var goldSink = 0;
        var index = 0;

        AddHeaders(x);

        foreach (var (mobile, amount) in plot.Participants)
        {
            var total = amount * plot.LottoPrice;
            goldSink += total;

            AddHtml(10 + x, y, 80, 22, Colored(mobile?.Name ?? "-", LabelColor), false, false);
            AddHtml(92 + x, y, 60, 22, Colored($"{amount}", LabelColor), false, false);
            AddHtml(154 + x, y, 60, 22, Colored($"{total}", LabelColor), false, false);

            index++;

            if (index == 22)
            {
                x = 200;
                y = 60;
                AddHeaders(x);
            }
            else if (index == 44)
            {
                x = 400;
                y = 60;
                AddHeaders(x);
            }
            else
            {
                y += 22;
            }
        }

        AddHtml(10, 10, 380, 20, Colored($"Золотой сток участка: {goldSink:#,0}", LabelColor), false, false);

        AddButton(10, height - 32, 4014, 4016, 1);
        AddHtml(45, height - 32, 150, 20, Colored("Назад", LabelColor), false, false);
    }

    public static void DisplayTo(Mobile from, MaginciaHousingPlot plot)
    {
        if (from == null || plot == null)
        {
            return;
        }

        from.CloseGump<PlotTrackingGump>();
        from.SendGump(new PlotTrackingGump(plot));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            LottoTrackingGump.DisplayTo(sender.Mobile);
        }
    }

    private void AddHeaders(int offset)
    {
        AddHtml(10 + offset, 40, 80, 20, Colored("Игрок", LabelColor), false, false);
        AddHtml(92 + offset, 40, 60, 20, Colored("Билеты", LabelColor), false, false);
        AddHtml(154 + offset, 40, 80, 20, Colored("Всего золота", LabelColor), false, false);
    }

    private static string Colored(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";
}
