/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Gumps.cs, the loyalty/citizenship half).
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             ServUO's <c>BaseGump</c> (with <c>AddGumpLayout</c>/<c>Refresh</c>/<c>BaseGump.SendGump</c>) has no
///             equivalent here, so each gump is a plain <see cref="Gump" /> that builds its layout in its
///             constructor and re-sends itself when it needs refreshing — the same shape the VvV gumps use.
///         </item>
///         <item><c>ConfirmCallbackGump</c> became <see cref="CityConfirmGump" />.</item>
///         <item>
///             ServUO's <c>LoyaltyRatingGump</c> lives outside this feature folder; a compact replacement is included
///             here so the "Loyalty Ratings" button still works.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.CityLoyalty;

/// <summary>Runs <paramref name="onConfirm" /> when the player accepts a cliloc-labelled confirmation.</summary>
public class CityConfirmGump : BaseConfirmGump
{
    private readonly int _label;
    private readonly Action<Mobile> _onConfirm;

    public CityConfirmGump(int label, Action<Mobile> onConfirm)
    {
        _label = label;
        _onConfirm = onConfirm;
    }

    public override int LabelNumber => _label;

    public override void Confirm(Mobile from) => _onConfirm(from);
}

/// <summary>Shared chrome for the city loyalty windows.</summary>
public abstract class BaseCityGump : Gump
{
    protected BaseCityGump(PlayerMobile pm, CityLoyaltySystem citizenship) : base(120, 120)
    {
        User = pm;
        Citizenship = citizenship ?? CityLoyaltySystem.GetCitizenship(pm, false);
    }

    protected PlayerMobile User { get; }

    protected CityLoyaltySystem Citizenship { get; }

    protected void AddCityChrome()
    {
        AddHtmlLocalized(0, 7, 345, 20, 1154645, "#1152190", 0, false, false); // City Loyalty

        AddImage(0, 0, 8000);
        AddImage(20, 37, 8001);
        AddImage(20, 107, 8002);
        AddImage(20, 177, 8001);
        AddImage(20, 247, 8002);
        AddImage(20, 317, 8001);
        AddImage(20, 387, 8003);

        AddHtmlLocalized(65, 395, 200, 16, 1152189, 0xFFFF);
        AddButton(50, 400, 2103, 2104, 500);

        AddHtmlLocalized(175, 395, 200, 16, 1157159, 0xFFFF);
        AddButton(160, 400, 2103, 2104, 501);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 500)
        {
            User.SendGump(new LoyaltyRatingGump(User));
            return;
        }

        if (info.ButtonID != 501)
        {
            return;
        }

        var entry = Citizenship?.GetPlayerEntry(User);

        if (entry == null)
        {
            return;
        }

        entry.ShowGainMessage = !entry.ShowGainMessage;

        // You will no longer/now receive City Loyalty gain messages.
        User.SendLocalizedMessage(entry.ShowGainMessage ? 1157161 : 1157160);

        User.SendGump(new CityLoyaltyGump(User));
    }
}

public class CityLoyaltyGump : BaseCityGump
{
    public CityLoyaltyGump(PlayerMobile pm) : base(pm, null)
    {
        if (!CityLoyaltySystem.Enabled || CityLoyaltySystem.Cities == null)
        {
            return;
        }

        AddCityChrome();

        var y = 40;

        for (var i = 0; i < CityLoyaltySystem.Cities.Count; i++)
        {
            var city = CityLoyaltySystem.Cities[i];

            if (city.CanUtilize && Citizenship == null && CityLoyaltySystem.CanAddCitizen(User))
            {
                AddButton(30, y + 3, 2103, 2104, 100 + i);
            }

            AddHtmlLocalized(50, y, 200, 16, CityLoyaltySystem.CityLocalization(city.City), 0xFFFF);
            AddHtmlLocalized(200, y, 200, 16, CityLoyaltySystem.RatingLocalization(city.GetLoyaltyRating(User)), 0xFFFF);

            y += 20;
        }

        y += 20;

        AddHtmlLocalized(70, y, 250, 16, 1152883, 0xFFFF); // Citizenship:
        AddHtmlLocalized(
            200,
            y,
            100,
            16,
            Citizenship != null ? CityLoyaltySystem.CityLocalization(Citizenship.City) : 1152884,
            0xFFFF
        );

        y += 40;

        if (!CityLoyaltySystem.IsSetup())
        {
            AddHtml(70, y, 250, 60, "Система лояльности городов ещё не включена владельцем сервера.", false, false);
        }
        else if (Citizenship != null)
        {
            AddHtmlLocalized(115, y - 5, 150, 16, 1152890, 0xFFFF); // Citizenship Titles
            AddButton(100, y, 2103, 2104, 1);

            y += 20;

            AddHtmlLocalized(115, y - 5, 150, 16, 1152888, 0xFFFF); // Renounce Citizenship
            AddButton(100, y, 2103, 2104, 2);
        }
        else if (CityLoyaltySystem.CanAddCitizen(User))
        {
            AddHtmlLocalized(30, y, 280, 90, 1152885, 0xFFFF);
        }
        else
        {
            AddHtmlLocalized(30, y, 285, 80, 1152886, CityLoyaltySystem.NextJoinCity(User).ToString(), 0, false, false);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID is 500 or 501)
        {
            base.OnResponse(sender, info);
            return;
        }

        if (!CityLoyaltySystem.IsSetup())
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                User.SendGump(new CityTitlesGump(User));
                break;
            case 2:
                User.SendGump(new RenounceCitizenshipGump(User));
                break;
            default:
                var id = info.ButtonID - 100;

                if (id >= 0 && id < CityLoyaltySystem.Cities.Count && Citizenship == null)
                {
                    User.SendGump(new DeclareCitizenshipGump(CityLoyaltySystem.Cities[id], User));
                }

                break;
        }
    }
}

public class DeclareCitizenshipGump : BaseCityGump
{
    public DeclareCitizenshipGump(CityLoyaltySystem city, PlayerMobile pm) : base(pm, null)
    {
        City = city;

        AddCityChrome();

        if (City == null)
        {
            return;
        }

        AddHtmlLocalized(
            30,
            40,
            285,
            200,
            1152891,
            $"#{CityLoyaltySystem.GetCityLocalization(City.City)}",
            1,
            false,
            true
        );

        AddHtmlLocalized(115, 280, 150, 16, 1152892, 0xFFFF); // Declare Citizenship
        AddButton(100, 285, 2103, 2104, 1);

        AddHtmlLocalized(115, 300, 150, 16, 1152889, 0xFFFF); // Cancel
        AddButton(100, 305, 2103, 2104, 2);
    }

    public CityLoyaltySystem City { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID is 500 or 501)
        {
            base.OnResponse(sender, info);
            return;
        }

        if (info.ButtonID == 1 && City != null && CityLoyaltySystem.CanAddCitizen(User))
        {
            City.DeclareCitizen(User);
            User.SendMessage($"Вы объявляете себя гражданином города {City.Definition.Name}!");
        }
        else if (info.ButtonID == 2)
        {
            User.SendGump(new CityLoyaltyGump(User));
        }
    }
}

public class RenounceCitizenshipGump : BaseCityGump
{
    public RenounceCitizenshipGump(PlayerMobile pm) : base(pm, null)
    {
        AddCityChrome();

        if (Citizenship == null)
        {
            return;
        }

        AddHtmlLocalized(45, 40, 250, 16, 1152883, 0xFFFF); // Citizenship:
        AddHtmlLocalized(200, 40, 100, 16, CityLoyaltySystem.CityLocalization(Citizenship.City), 0xFFFF);

        AddHtmlLocalized(
            30,
            70,
            280,
            200,
            1152887,
            $"#{CityLoyaltySystem.GetCityLocalization(Citizenship.City)}",
            1,
            false,
            true
        );

        AddHtmlLocalized(115, 280, 150, 16, 1152888, 0xFFFF); // Renounce Citizenship
        AddButton(100, 285, 2103, 2104, 1);

        AddHtmlLocalized(115, 300, 150, 16, 1152889, 0xFFFF); // Cancel
        AddButton(100, 305, 2103, 2104, 2);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID is 500 or 501)
        {
            base.OnResponse(sender, info);
            return;
        }

        if (info.ButtonID == 1 && Citizenship != null)
        {
            var name = Citizenship.Definition.Name;
            Citizenship.RenounceCitizenship(User);
            User.SendMessage($"Вы отказываетесь от гражданства города {name}!");
        }
        else if (info.ButtonID == 2)
        {
            User.SendGump(new CityLoyaltyGump(User));
        }
    }
}

public class CityTitlesGump : BaseCityGump
{
    public CityTitlesGump(PlayerMobile pm) : base(pm, null)
    {
        AddCityChrome();

        if (Citizenship == null)
        {
            return;
        }

        AddHtmlLocalized(60, 40, 200, 16, 1152894, 0xFFFF); // Your Titles

        var y = 60;

        foreach (var title in Enum.GetValues<CityTitle>())
        {
            if (title == CityTitle.None || !Citizenship.HasTitle(User, title))
            {
                continue;
            }

            AddHtmlLocalized(65, y, 300, 16, CityLoyaltySystem.GetTitleLocalization(User, title, Citizenship.City), 0xFFFF);
            y += 20;
        }

        y += 20;
        AddHtmlLocalized(60, y, 250, 16, 1152895, 0xFFFF); // Available Titles:
        y += 20;

        foreach (var title in Enum.GetValues<CityTitle>())
        {
            if (title == CityTitle.None || Citizenship.HasTitle(User, title))
            {
                continue;
            }

            AddButton(60, y + 5, 2103, 2104, (int)title + 1);
            AddHtmlLocalized(75, y, 300, 16, CityLoyaltySystem.GetTitleLocalization(User, title, Citizenship.City), 0xFFFF);
            y += 20;
        }

        y += 40;
        AddHtmlLocalized(30, y, 285, 60, 1152896, 0xFFFF);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID is 500 or 501)
        {
            base.OnResponse(sender, info);
            return;
        }

        if (Citizenship == null || info.ButtonID <= 0 || info.ButtonID >= 500)
        {
            return;
        }

        var title = (CityTitle)(info.ButtonID - 1);

        if (!Citizenship.HasTitle(User, title))
        {
            User.SendGump(new CityTitlesInfoGump(User, title));
        }
    }
}

public class CityTitlesInfoGump : BaseCityGump
{
    public CityTitlesInfoGump(PlayerMobile pm, CityTitle title) : base(pm, null)
    {
        Title = title;

        AddCityChrome();

        if (Citizenship == null)
        {
            return;
        }

        var gold = Citizenship.GetTitleCost(Title);
        var rating = Citizenship.GetMinimumRating(Title);

        AddHtmlLocalized(60, 40, 300, 20, 1152901, 0xFFFF); // Obtain Title
        AddHtmlLocalized(75, 60, 200, 16, CityLoyaltySystem.GetTitleLocalization(User, Title, Citizenship.City), 0xFFFF);

        AddHtmlLocalized(60, 120, 200, 16, 1152899, 0xFFFF); // Loyalty Required:
        AddHtmlLocalized(60, 140, 200, 16, 1152900, 0xFFFF); // Donation Required:

        AddHtmlLocalized(200, 120, 150, 16, CityLoyaltySystem.RatingLocalization(rating), 0xFFFF);
        AddHtml(200, 140, 150, 16, gold.ToString("N0"), false, false);

        if (gold > Banker.GetBalance(User))
        {
            AddHtmlLocalized(30, 180, 280, 80, 1152902, 0xFFFF);
        }
        else if (rating > Citizenship.GetLoyaltyRating(User))
        {
            AddHtmlLocalized(30, 180, 280, 80, 1152903, 0xFFFF);
        }
        else
        {
            AddHtmlLocalized(60, 345, 150, 16, 1152904, 0xFFFF); // Gain Title
            AddButton(40, 350, 2103, 2104, 1);
        }

        AddHtmlLocalized(60, 365, 150, 16, 1152889, 0xFFFF); // Cancel
        AddButton(40, 370, 2103, 2104, 2);
    }

    public CityTitle Title { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID is 500 or 501)
        {
            base.OnResponse(sender, info);
            return;
        }

        if (Citizenship == null)
        {
            return;
        }

        if (info.ButtonID == 1)
        {
            var cost = Citizenship.GetTitleCost(Title);

            if (Banker.Withdraw(User, cost))
            {
                Citizenship.AddTitle(User, Title);
                Citizenship.AddToTreasury(User, cost);
                User.SendGump(new CityTitlesGump(User));
            }

            return;
        }

        if (info.ButtonID == 2)
        {
            User.SendGump(new CityTitlesGump(User));
        }
    }
}

/// <summary>
///     Replacement for ServUO's external <c>LoyaltyRatingGump</c>: shows the rating ladder with the player's own
///     standing highlighted.
/// </summary>
public class LoyaltyRatingGump : Gump
{
    public LoyaltyRatingGump(PlayerMobile pm) : base(100, 100)
    {
        User = pm;

        AddBackground(0, 0, 420, 460, 5054);
        AddHtmlLocalized(0, 15, 420, 20, 1152189, 0xFFFF); // Loyalty Ratings

        var city = CityLoyaltySystem.GetCitizenship(pm, false);
        var current = city?.GetLoyaltyRating(pm) ?? LoyaltyRating.Unknown;

        var y = 45;

        foreach (var rating in Enum.GetValues<LoyaltyRating>())
        {
            var hue = rating == current ? 0x35 : 0xFFFF;

            AddHtmlLocalized(20, y, 250, 16, CityLoyaltySystem.RatingLocalization(rating), hue);
            y += 17;
        }

        AddButton(20, 430, 4014, 4016, 1);
        AddHtmlLocalized(55, 430, 150, 16, 1149777, 0xFFFF); // MAIN MENU
    }

    public PlayerMobile User { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            User.SendGump(new CityLoyaltyGump(User));
        }
    }
}
