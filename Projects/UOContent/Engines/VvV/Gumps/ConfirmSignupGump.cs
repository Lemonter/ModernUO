using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Gumps/ConfirmSignupGump.cs).
public class ConfirmSignupGump : Gump
{
    public PlayerMobile User { get; }

    public ConfirmSignupGump(PlayerMobile pm) : base(50, 50)
    {
        User = pm;

        AddBackground(0, 0, 360, 300, 83);

        AddHtmlLocalized(0, 25, 360, 20, 1154645, "#1155565", 0xFFFF); // Vice vs Virtue Signup

        if (ViceVsVirtueSystem.EnhancedRules)
        {
            AddHtml(10, 55, 340, 165, _enhancedRulesMessage, false, true);
        }
        else
        {
            AddHtmlLocalized(10, 55, 340, 210, 1155566, 0xFFFF);
        }

        AddButton(115, 230, 0x2622, 0x2623, 1);
        AddHtmlLocalized(140, 230, 150, 20, 1155567, 0xFFFF); // Learn more about VvV!

        AddButton(10, 268, 0xFA5, 0xFA7, 2);
        AddHtmlLocalized(45, 268, 100, 20, 1049011, 0xFFFF); // I Accept!

        AddButton(325, 268, 0xFB1, 0xFB3, 0);
        AddHtml(285, 268, 100, 20, "<basefont color=#FFFFFF>Cancel", false, false);
    }

    private readonly string _enhancedRulesMessage =
        "<basefont color=#FFFFFF>Greetings! You are about to join Vice vs Virtue! VvV is an exhilarating Player vs Player" +
        " experience that you can have fun with whether you have hours or only a few minutes to" +
        " jump into the action!  Be forewarned, once you join VvV you will be freely attackable" +
        " by other VvV participants in <b>any</b> facet.<br><br>Will you answer the call" +
        " and lead your guild to victory? Please note the slightly different enhanced rules that you may not be used to:<br><br>" +
        "- VvV Combat on any facet<br>- Reduced silver during town battles when uncontested<br>- Combat travel restrictions when in VvV Combat Zone";

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        switch (info.ButtonID)
        {
            case 1:
                User.SendGump(new ConfirmSignupGump(User));
                break;
            case 2:
                if (User.Guild is Guild)
                {
                    ViceVsVirtueSystem.Instance.AddPlayer(User);
                }

                break;
        }
    }
}

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Gumps/ExemptCityGump.cs).
public class ExemptCitiesGump : Gump
{
    public ExemptCitiesGump() : base(50, 50) => AddGumpLayout();

    private void AddGumpLayout()
    {
        AddBackground(0, 0, 250, 300, 83);
        AddHtml(
            0,
            15,
            250,
            60,
            "<basefont color=#FFFFFF><center>City Exempt List:<br>Check any cities that you do not want participating in VvV Battles.</center>",
            false,
            false
        );

        for (var i = 0; i < 8; i++)
        {
            var city = (VvVCity)i;
            var button = ViceVsVirtueSystem.Instance.ExemptCities.Contains(city) ? 211 : 210;

            AddButton(20, 80 + i * 23, button, button, i + 1);
            AddHtmlLocalized(44, 80 + i * 23, 200, 20, ViceVsVirtueSystem.GetCityLocalization(city), 0xFFFF);
        }
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var id = info.ButtonID;

        if (id == 0)
        {
            return;
        }

        var city = (VvVCity)(id - 1);

        if (!ViceVsVirtueSystem.Instance.ExemptCities.Remove(city))
        {
            ViceVsVirtueSystem.Instance.ExemptCities.Add(city);
        }

        state.Mobile.GetGumps().Close<ExemptCitiesGump>();
        state.Mobile.SendGump(new ExemptCitiesGump());
    }
}
