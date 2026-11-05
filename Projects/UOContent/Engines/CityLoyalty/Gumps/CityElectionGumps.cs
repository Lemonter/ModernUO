/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Gumps.cs, the election half). Plain
///     <see cref="Gump" /> subclasses instead of ServUO's <c>BaseGump</c> (see CityLoyaltyGumps.cs).
/// </summary>
using System;
using System.Collections.Generic;
using System.Globalization;
using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.CityLoyalty;

public class CityStoneGump : Gump
{
    public CityStoneGump(PlayerMobile pm, CityLoyaltySystem city) : base(50, 50)
    {
        User = pm;
        City = city;

        if (City.Election == null || !City.Election.Ongoing)
        {
            AddBackground(0, 0, 400, 150, 5054);
            AddHtmlLocalized(10, 12, 150, 20, 1155758, 0xFFFF); // City Stone

            var next = City.Election?.NextElection() ?? DateTime.MinValue;

            AddHtmlLocalized(
                20,
                45,
                360,
                60,
                1153898,
                $"{City.Definition.Name}\t{City.Governor?.Name ?? "нет"}\t{next}",
                0xFFFF,
                false,
                false
            );
            return;
        }

        AddBackground(0, 0, 500, 182, 5054);
        AddHtmlLocalized(10, 12, 150, 20, 1155758, 0xFFFF); // City Stone

        if (City.Election.CanNominate())
        {
            AddButton(10, 40, 4002, 4004, 1);
            AddHtmlLocalized(45, 40, 250, 20, 1153901, 0xFFFF); // Nominate Yourself for the Ballot
        }

        AddButton(10, 80, 4005, 4007, 2);
        AddHtmlLocalized(45, 80, 250, 20, 1153900, 0xFFFF); // View List of Current Nominees

        AddButton(10, 110, 4005, 4007, 3);
        AddHtmlLocalized(45, 110, 250, 20, 1153899, 0xFFFF); // View List of Current Candidates

        AddButton(10, 150, 4005, 4007, 0);
        AddHtmlLocalized(45, 150, 250, 20, 1060675, 0xFFFF); // CLOSE
    }

    public PlayerMobile User { get; }
    public CityLoyaltySystem City { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        switch (info.ButtonID)
        {
            case 1:
                City.Election?.TryNominate(User);
                break;
            case 2:
                User.SendGump(new NomineesGump(User, City));
                break;
            case 3:
                User.SendGump(new CandidatesGump(User, City));
                break;
        }
    }
}

public class NomineesGump : Gump
{
    public NomineesGump(PlayerMobile pm, CityLoyaltySystem city) : base(50, 50)
    {
        User = pm;
        City = city;

        AddBackground(0, 0, 600, 400, 5054);
        AddHtmlLocalized(10, 12, 200, 20, 1153906, 0xFFFF); // Current Nominees

        AddPage(0);

        AddHtmlLocalized(10, 40, 120, 20, 1153907, 0xFFFF); // Endorse
        AddHtmlLocalized(130, 40, 170, 20, 1153908, 0xFFFF); // Name
        AddHtmlLocalized(300, 40, 170, 20, 1153909, 0xFFFF); // Guild

        AddButton(10, 368, 4014, 4016, 1);
        AddHtmlLocalized(45, 368, 150, 16, 1149777, 0xFFFF); // MAIN MENU

        AddButton(540, 368, 4017, 4019, 2);
        AddHtmlLocalized(485, 368, 150, 16, 1153910, 0xFFFF); // Withdraw

        var page = 1;
        AddPage(page);
        var pageIndex = 0;

        var candidates = City.Election?.Candidates ?? new List<BallotEntry>();

        for (var i = 0; i < candidates.Count; i++)
        {
            var entry = candidates[i];
            var guild = entry.Player?.Guild as Guild;

            AddButton(10, 70 + pageIndex * 25, 4002, 4004, i + 100);
            AddHtml(130, 70 + pageIndex * 25, 200, 20, $"<basefont color=#EEE8AA>{entry.Player?.Name ?? "Неизвестно"}", false, false);
            AddHtml(300, 70 + pageIndex * 25, 200, 20, $"<basefont color=#EEE8AA>{guild?.Name ?? string.Empty}", false, false);
            pageIndex++;

            if (pageIndex >= 10 && i < candidates.Count - 1)
            {
                pageIndex = 0;
                page++;

                AddHtml(535, 346, 100, 16, "<basefont color=#FFFFFF>NEXT", false, false);
                AddButton(574, 348, 5601, 5605, 0, GumpButtonType.Page, page);

                AddPage(page);
            }

            if (i > 10)
            {
                AddHtml(32, 346, 100, 16, "<basefont color=#FFFFFF>BACK", false, false);
                AddButton(10, 348, 5603, 5607, 0, GumpButtonType.Page, page - 1);
            }
        }
    }

    public PlayerMobile User { get; }
    public CityLoyaltySystem City { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            User.SendGump(new CityStoneGump(User, City));
            return;
        }

        if (info.ButtonID == 2)
        {
            City.Election?.TryWithdraw(User);
            return;
        }

        if (info.ButtonID < 100)
        {
            return;
        }

        var id = info.ButtonID - 100;
        var candidates = City.Election?.Candidates;

        if (candidates != null && id >= 0 && id < candidates.Count && candidates[id].Player != null)
        {
            City.Election.TryEndorse(User, candidates[id].Player);
        }
    }
}

public class CandidatesGump : Gump
{
    private readonly List<BallotEntry> _candidates;

    public CandidatesGump(PlayerMobile pm, CityLoyaltySystem city) : base(50, 50)
    {
        User = pm;
        City = city;

        AddBackground(0, 0, 600, 400, 5054);
        AddHtmlLocalized(10, 12, 200, 20, 1153914, 0xFFFF); // Candidate List

        AddPage(0);

        AddHtmlLocalized(10, 40, 120, 20, 1153915, 0xFFFF); // Cast Vote
        AddHtmlLocalized(130, 40, 170, 20, 1153908, 0xFFFF); // Name
        AddHtmlLocalized(300, 40, 170, 20, 1153909, 0xFFFF); // Guild
        AddHtmlLocalized(470, 40, 80, 20, 1153916, 0xFFFF); // Standing

        AddButton(10, 368, 4014, 4016, 1);
        AddHtmlLocalized(45, 368, 150, 16, 1149777, 0xFFFF); // MAIN MENU

        AddButton(540, 368, 4017, 4019, 2);
        AddHtmlLocalized(485, 368, 150, 16, 1153910, 0xFFFF); // Withdraw

        var page = 1;
        AddPage(page);
        var pageIndex = 0;

        _candidates = City.Election?.Candidates.FindAll(e => e.Endorsements.Count > 0) ?? new List<BallotEntry>();

        for (var i = 0; i < _candidates.Count; i++)
        {
            var entry = _candidates[i];
            var guild = entry.Player?.Guild as Guild;

            AddButton(10, 70 + pageIndex * 25, 4002, 4004, i + 100);
            AddHtml(130, 70 + pageIndex * 25, 200, 20, $"<basefont color=#EEE8AA>{entry.Player?.Name ?? "Неизвестно"}", false, false);
            AddHtml(300, 70 + pageIndex * 25, 200, 20, $"<basefont color=#EEE8AA>{guild?.Name ?? string.Empty}", false, false);

            var standing = City.Election?.GetStanding(entry) ?? 0.0;
            AddHtml(470, 70 + pageIndex * 25, 200, 20, $"<basefont color=#EEE8AA>{standing.ToString("F1", CultureInfo.InvariantCulture)}%", false, false);

            pageIndex++;

            if (pageIndex >= 10 && i < _candidates.Count - 1)
            {
                pageIndex = 0;
                page++;

                AddHtml(535, 346, 100, 16, "<basefont color=#FFFFFF>NEXT", false, false);
                AddButton(574, 348, 5601, 5605, 0, GumpButtonType.Page, page);

                AddPage(page);
            }

            if (i > 10)
            {
                AddHtml(32, 346, 100, 16, "<basefont color=#FFFFFF>BACK", false, false);
                AddButton(10, 348, 5603, 5607, 0, GumpButtonType.Page, page - 1);
            }
        }
    }

    public PlayerMobile User { get; }
    public CityLoyaltySystem City { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            User.SendGump(new CityStoneGump(User, City));
            return;
        }

        if (info.ButtonID == 2)
        {
            City.Election?.TryWithdraw(User);
            return;
        }

        if (info.ButtonID < 100)
        {
            return;
        }

        var id = info.ButtonID - 100;

        if (id >= 0 && id < _candidates.Count && _candidates[id].Player != null)
        {
            City.Election?.TryVote(User, _candidates[id].Player);
        }
    }
}

public class AcceptOfficeGump : Gump
{
    public AcceptOfficeGump(PlayerMobile pm, CityLoyaltySystem city) : base(50, 50)
    {
        User = pm;
        City = city;

        AddBackground(0, 0, 600, 600, 9380);

        AddHtmlLocalized(50, 50, 500, 400, 1154069, City.Definition.Name, 0, false, false);

        AddButton(50, 548, 4005, 4007, 1);
        AddHtmlLocalized(90, 548, 150, 20, 3000372, 0xFFFF); // Accept

        AddButton(510, 548, 4017, 4019, 2);
        AddHtmlLocalized(450, 548, 150, 20, 1115372, 0xFFFF); // Decline
    }

    public PlayerMobile User { get; }
    public CityLoyaltySystem City { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            City.Governor = User;
            return;
        }

        if (info.ButtonID != 2)
        {
            return;
        }

        // Are you sure you wish to reject the Office of Governor? This is permanent and cannot be undone.
        User.SendGump(
            new CityConfirmGump(
                1154092,
                _ =>
                {
                    City.PendingGovernor = true;
                    City.Election?.OnRejectOffice(User);
                }
            )
        );
    }
}

public class ElectionStartTimeGump : Gump
{
    public ElectionStartTimeGump(PlayerMobile pm) : base(50, 50)
    {
        User = pm;

        AddBackground(0, 0, 400, 400, 5054);
        AddHtml(0, 15, 400, 20, "<center>Время начала выборов</center>", false, false);

        var britain = CityLoyaltySystem.Britain;

        if (britain?.Election != null && britain.Election.Ongoing)
        {
            AddHtml(
                15,
                35,
                370,
                40,
                "Нужно дождаться окончания текущих выборов, прежде чем начинать новые.",
                false,
                true
            );
            return;
        }

        AddHtml(
            15,
            35,
            370,
            120,
            "Можно задать до 4 дат начала выборов. По умолчанию это март, июнь, сентябрь и декабрь " +
            "(3, 6, 9, 12 — система использует номер месяца). Даты должны быть не ближе месяца друг к другу, " +
            "и каждая начинается первого числа своего месяца.",
            false,
            false
        );

        AddLabel(15, 160, 0, "Начало");
        AddLabel(150, 160, 0, "Месяц (номер)");

        for (var i = 0; i < 4; i++)
        {
            var start = string.Empty;

            if (britain?.Election?.StartTimes is { } times && times.Length > i && times[i] != DateTime.MinValue)
            {
                start = times[i].Month.ToString();
            }

            AddLabel(15, 180 + i * 25, 0, $"{i + 1}.");
            AddImageTiled(150, 180 + i * 25, 50, 20, 5058);
            AddAlphaRegion(150, 180 + i * 25, 50, 20);
            AddTextEntry(151, 180 + i * 25, 49, 20, 0, i + 1, start);
        }

        AddHtml(55, 346, 150, 20, "ПО УМОЛЧАНИЮ", false, false);
        AddButton(15, 346, 4005, 4007, 2);

        AddHtml(55, 368, 150, 20, "ВЫБРАТЬ", false, false);
        AddButton(15, 368, 4005, 4007, 1);
    }

    public PlayerMobile User { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var britain = CityLoyaltySystem.Britain;

        if (info.ButtonID < 1 || (britain?.Election != null && britain.Election.Ongoing))
        {
            return;
        }

        if (info.ButtonID == 2)
        {
            foreach (var sys in CityLoyaltySystem.Cities)
            {
                sys.Election?.GetDefaultStartTimes();
            }

            User.SendMessage("Время начала выборов сброшено к значениям по умолчанию.");
            return;
        }

        var times = new List<int>();

        for (var i = 1; i <= 4; i++)
        {
            var text = info.GetTextEntry(i);

            if (text == null)
            {
                continue;
            }

            var time = Utility.ToInt32(text);

            if (time > 0 && time < 13)
            {
                times.Add(time);
            }
        }

        if (times.Count == 0)
        {
            return;
        }

        var startTimes = CityElection.ValidateStartTimes(User, times.ToArray());

        if (startTimes == null)
        {
            return;
        }

        foreach (var sys in CityLoyaltySystem.Cities)
        {
            if (sys.Election != null)
            {
                sys.Election.StartTimes = startTimes;
            }
        }

        User.SendMessage("Время начала выборов сброшено.");
    }
}
