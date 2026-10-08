using System;
using System.Collections.Generic;
using System.Linq;
using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.VvV;

public enum Filter
{
    Score,
    Kills,
    ReturnedSigils
}

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Gumps/LeaderboardGump.cs).
// `ViceVsVirtueSystem.Instance.PlayerTable` (a `PointsSystem` collection) doesn't exist here —
// replaced with `PlayerEntries.Values`.
public class ViceVsVirtueLeaderboardGump : Gump
{
    public const int PerPage = 10;

    public PlayerMobile User { get; }
    public Filter Filter { get; }

    public ViceVsVirtueLeaderboardGump(PlayerMobile pm, Filter filter = Filter.Score) : base(50, 50)
    {
        User = pm;
        Filter = filter;

        AddPage(0);
        AddBackground(0, 0, 920, 320, 5054);
        AddImageTiled(10, 10, 900, 300, 2624);

        AddHtmlLocalized(0, 12, 920, 20, 1114921, 0xFFFF); // Vice Vs Virtue - Player Rankings

        AddHtmlLocalized(10, 55, 65, 20, 1114981, 0xFFFF); // #:
        AddHtmlLocalized(70, 55, 160, 20, 1114966, 0xFFFF); // Name:
        AddHtmlLocalized(230, 55, 70, 20, 1114978, 0xFFFF); // Guild:
        AddHtmlLocalized(300, 55, 100, 20, 1114977, Filter == Filter.Score ? GumpColor.Convert32To16(0x00FA9A) : 0xFFFF); // Score:
        AddHtmlLocalized(420, 55, 55, 20, 1114975, Filter == Filter.Kills ? GumpColor.Convert32To16(0x00FA9A) : 0xFFFF); // Kills:
        AddHtmlLocalized(480, 55, 55, 20, 1114893, 0xFFFF); // Deaths:
        AddHtmlLocalized(540, 55, 55, 20, 1155572, 0xFFFF); // Assists:
        AddHtmlLocalized(610, 55, 90, 20, 1155575, Filter == Filter.ReturnedSigils ? GumpColor.Convert32To16(0x00FA9A) : 0xFFFF); // Returned Sigil:
        AddHtmlLocalized(710, 55, 100, 20, 1155574, 0xFFFF); // Disarmed Traps:
        AddHtmlLocalized(810, 55, 80, 20, 1155573, 0xFFFF); // Stolen Sigil:

        if (Filter != Filter.Score)
        {
            AddButton(400, 55, 2437, 2438, 1);
        }
        else
        {
            AddImage(400, 55, 10006);
        }

        if (Filter != Filter.Kills)
        {
            AddButton(475, 55, 2437, 2438, 2);
        }
        else
        {
            AddImage(475, 55, 10006);
        }

        if (Filter != Filter.ReturnedSigils)
        {
            AddButton(700, 55, 2437, 2438, 3);
        }
        else
        {
            AddImage(700, 55, 10006);
        }

        AddButton(280, 290, 4005, 4007, 4);
        AddHtmlLocalized(315, 290, 150, 20, 1114923, 0xFFFF); // Guild Rankings

        var list = ViceVsVirtueSystem.Instance.PlayerEntries.Values.ToList();

        list = Filter switch
        {
            Filter.Kills          => list.OrderByDescending(e => e.Kills).ToList(),
            Filter.ReturnedSigils => list.OrderByDescending(e => e.ReturnedSigils).ToList(),
            _                     => list.OrderByDescending(e => e.Score).ToList()
        };

        var pages = Math.Max(1, (int)Math.Ceiling((double)list.Count / PerPage));
        var y = 75;
        var page = 1;
        var pageIndex = 0;

        AddPage(page);
        AddHtmlLocalized(60, 290, 150, 20, 1153561, $"{page}\t{pages}", 0xFFFF); // Page ~1_CUR~ of ~2_MAX~

        for (var i = 0; i < list.Count; i++)
        {
            var entry = list[i];

            if (entry.Player == null)
            {
                continue;
            }

            var g = entry.Player.Guild as Guild;

            AddHtml(10, y, 65, 20, CenterGray($"{i + 1}."), false, false);
            AddHtml(70, y, 160, 20, LeftGray(entry.Player.Name), false, false);
            AddHtml(230, y, 70, 20, CenterGray(g == null ? "None" : g.Abbreviation), false, false);
            AddHtml(300, y, 100, 20, Filter == Filter.Score ? RightGreen(entry.Score.ToString()) : RightGray(entry.Score.ToString()), false, false);
            AddHtml(420, y, 55, 20, Filter == Filter.Kills ? RightGreen(entry.Kills.ToString()) : RightGray(entry.Kills.ToString()), false, false);
            AddHtml(480, y, 55, 20, RightGray(entry.Deaths.ToString()), false, false);
            AddHtml(540, y, 55, 20, RightGray(entry.Assists.ToString()), false, false);
            AddHtml(610, y, 90, 20, Filter == Filter.ReturnedSigils ? RightGreen(entry.ReturnedSigils.ToString()) : RightGray(entry.ReturnedSigils.ToString()), false, false);
            AddHtml(710, y, 100, 20, RightGray(entry.DisarmedTraps.ToString()), false, false);
            AddHtml(810, y, 80, 20, RightGray(entry.StolenSigils.ToString()), false, false);

            y += 20;
            pageIndex++;

            if (pageIndex != PerPage)
            {
                continue;
            }

            AddHtmlLocalized(60, 290, 150, 20, 1153561, $"{page}\t{pages}", 0xFFFF);

            if (i > 0 && i < list.Count - 1)
            {
                AddButton(200, 290, 4005, 4007, 0, GumpButtonType.Page, page + 1);

                page++;
                y = 75;
                pageIndex = 0;
                AddPage(page);

                AddButton(170, 290, 4014, 4016, 0, GumpButtonType.Page, page - 1);
            }
        }
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        switch (info.ButtonID)
        {
            case 1:
            case 2:
            case 3:
                User.SendGump(new ViceVsVirtueLeaderboardGump(User, (Filter)(info.ButtonID - 1)));
                break;
            case 4:
                User.SendGump(new GuildLeaderboardGump(User));
                break;
        }
    }

    private static string CenterGray(string text) => $"<basefont color=#A9A9A9><DIV ALIGN=CENTER>{text}</DIV>";
    private static string RightGray(string text) => $"<basefont color=#A9A9A9><DIV ALIGN=RIGHT>{text}</DIV>";
    private static string LeftGray(string text) => $"<basefont color=#A9A9A9><DIV ALIGN=LEFT>{text}</DIV>";
    private static string RightGreen(string text) => $"<basefont color=#00FA9A><DIV ALIGN=RIGHT>{text}</DIV>";
}

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Gumps/GuildLeaderboardGump.cs).
public class GuildLeaderboardGump : Gump
{
    public const int PerPage = 10;

    public PlayerMobile User { get; }
    public Filter Filter { get; }

    public GuildLeaderboardGump(PlayerMobile pm, Filter filter = Filter.Score) : base(50, 50)
    {
        User = pm;
        Filter = filter;

        AddPage(0);
        AddBackground(0, 0, 560, 320, 5054);
        AddImageTiled(10, 10, 540, 300, 2624);

        AddHtmlLocalized(0, 12, 560, 20, 1114922, 0xFFFF); // Vice Vs Virtue - Guild Rankings

        AddHtmlLocalized(10, 55, 60, 20, 1114981, 0xFFFF); // #:
        AddHtmlLocalized(50, 55, 180, 20, 1114978, 0xFFFF); // Guild:
        AddHtmlLocalized(230, 55, 100, 20, 1114977, Filter == Filter.Score ? GumpColor.Convert32To16(0x00FA9A) : 0xFFFF); // Score:
        AddHtmlLocalized(330, 55, 85, 20, 1114975, Filter == Filter.Kills ? GumpColor.Convert32To16(0x00FA9A) : 0xFFFF); // Kills:
        AddHtmlLocalized(425, 55, 95, 20, 1155575, Filter == Filter.ReturnedSigils ? GumpColor.Convert32To16(0x00FA9A) : 0xFFFF); // Returned Sigil:

        if (Filter != Filter.Score)
        {
            AddButton(330, 55, 2437, 2438, 1);
        }
        else
        {
            AddImage(330, 55, 10006);
        }

        if (Filter != Filter.Kills)
        {
            AddButton(415, 55, 2437, 2438, 2);
        }
        else
        {
            AddImage(415, 55, 10006);
        }

        if (Filter != Filter.ReturnedSigils)
        {
            AddButton(520, 55, 2437, 2438, 3);
        }
        else
        {
            AddImage(520, 55, 10006);
        }

        AddButton(280, 290, 4005, 4007, 4);
        AddHtmlLocalized(315, 290, 150, 20, 1114924, 0xFFFF); // Guild Rankings

        var list = ViceVsVirtueSystem.Instance.GuildStats.Values.ToList();

        list = Filter switch
        {
            Filter.Kills          => list.OrderByDescending(e => e.Kills).ToList(),
            Filter.ReturnedSigils => list.OrderByDescending(e => e.ReturnedSigils).ToList(),
            _                     => list.OrderByDescending(e => e.Score).ToList()
        };

        var pages = Math.Max(1, (int)Math.Ceiling((double)list.Count / PerPage));
        var y = 75;
        var page = 1;
        var pageIndex = 0;

        AddPage(page);
        AddHtmlLocalized(60, 290, 150, 20, 1153561, $"{page}\t{pages}", 0xFFFF); // Page ~1_CUR~ of ~2_MAX~

        for (var i = 0; i < list.Count; i++)
        {
            var entry = list[i];

            AddHtml(10, y, 65, 20, CenterGray($"{i + 1}."), false, false);
            AddHtml(50, y, 180, 20, CenterGray(entry.Guild == null ? "" : entry.Guild.Name), false, false);
            AddHtml(230, y, 100, 20, Filter == Filter.Score ? RightGreen(entry.Score.ToString()) : RightGray(entry.Score.ToString()), false, false);
            AddHtml(330, y, 85, 20, Filter == Filter.Kills ? RightGreen(entry.Kills.ToString()) : RightGray(entry.Kills.ToString()), false, false);
            AddHtml(425, y, 95, 20, Filter == Filter.ReturnedSigils ? RightGreen(entry.ReturnedSigils.ToString()) : RightGray(entry.ReturnedSigils.ToString()), false, false);

            y += 20;
            pageIndex++;

            if (pageIndex != PerPage)
            {
                continue;
            }

            AddHtmlLocalized(60, 290, 150, 20, 1153561, $"{page}\t{pages}", 0xFFFF);

            if (i > 0 && i < list.Count - 1)
            {
                AddButton(200, 290, 4005, 4007, 0, GumpButtonType.Page, page + 1);

                page++;
                y = 75;
                pageIndex = 0;
                AddPage(page);

                AddButton(170, 290, 4014, 4016, 0, GumpButtonType.Page, page - 1);
            }
        }
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        switch (info.ButtonID)
        {
            case 1:
            case 2:
            case 3:
                User.SendGump(new GuildLeaderboardGump(User, (Filter)(info.ButtonID - 1)));
                break;
            case 4:
                User.SendGump(new ViceVsVirtueLeaderboardGump(User));
                break;
        }
    }

    private static string CenterGray(string text) => $"<basefont color=#A9A9A9><DIV ALIGN=CENTER>{text}</DIV>";
    private static string RightGray(string text) => $"<basefont color=#A9A9A9><DIV ALIGN=RIGHT>{text}</DIV>";
    private static string RightGreen(string text) => $"<basefont color=#00FA9A><DIV ALIGN=RIGHT>{text}</DIV>";
}
