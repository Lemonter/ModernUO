/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Gumps.cs, the governor/admin half). Plain
///     <see cref="Gump" /> subclasses instead of ServUO's <c>BaseGump</c> (see CityLoyaltyGumps.cs).
///
///     Dropped: ServUO's <c>CityMessageBoardGump</c>/<c>CityMessageGump</c>, which nothing in the feature ever opened
///     (their only caller is commented out in ServUO itself); the board just shows the standard bulletin-board gump.
/// </summary>
using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.CityLoyalty;

public class ChooseTradeDealGump : Gump
{
    private static readonly TradeDeal[] _deals =
    {
        TradeDeal.GuildOfArcaneArts,
        TradeDeal.SocietyOfClothiers,
        TradeDeal.BardicCollegium,
        TradeDeal.OrderOfEngineers,
        TradeDeal.GuildOfHealers,
        TradeDeal.MaritimeGuild,
        TradeDeal.MerchantsAssociation,
        TradeDeal.MiningCooperative,
        TradeDeal.LeagueOfRangers,
        TradeDeal.GuildOfAssassins,
        TradeDeal.WarriorsGuild
    };

    public ChooseTradeDealGump(PlayerMobile pm, CityLoyaltySystem city) : base(75, 75)
    {
        User = pm;
        City = city;

        AddImage(0, 0, 8000);
        AddImage(20, 37, 8001);
        AddImage(20, 107, 8002);
        AddImage(20, 177, 8001);
        AddImage(20, 247, 8002);
        AddImage(20, 317, 8001);
        AddImage(20, 387, 8003);

        AddHtmlLocalized(0, 7, 345, 20, 1154645, "#1154103", 0, false, false); // City Loyalty Trade Deal

        AddHtmlLocalized(30, 40, 280, 100, 1154030, 0xFFFF);

        var y = 150;

        for (var i = 0; i < _deals.Length; i++)
        {
            AddButton(30, y + 5, 2103, 2104, i + 100);
            AddHtmlLocalized(50, y, 200, 20, (int)_deals[i] - 12, 0xFFFF);

            y += 20;
        }
    }

    public PlayerMobile User { get; }
    public CityLoyaltySystem City { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0)
        {
            return;
        }

        var id = info.ButtonID - 100;

        if (id < 0 || id >= _deals.Length)
        {
            return;
        }

        if (City.Treasury < CityLoyaltySystem.TradeDealCost)
        {
            // Begging thy pardon but the City Treasury doth nay have the funds available to make such a deal!
            City.HeraldMessage(User, 1154057);
        }
        else if (User.AccessLevel == AccessLevel.Player && City.TradeDealStart != DateTime.MinValue &&
                 City.TradeDealStart + TimeSpan.FromDays(CityLoyaltySystem.TradeDealCooldown) > Core.Now)
        {
            // You may only make a trade deal once per real world week!
            City.HeraldMessage(User, 1154056);
        }
        else
        {
            City.Treasury -= CityLoyaltySystem.TradeDealCost;

            City.OnNewTradeDeal(_deals[id]);
            City.HeraldMessage(1154058, $"{City.Definition.Name}\t#{(int)_deals[id] - 12}");
        }
    }
}

public class PlayerTitleGump : Gump
{
    public PlayerTitleGump(PlayerMobile pm, PlayerMobile citizen, CityLoyaltySystem sys) : base(50, 50)
    {
        User = pm;
        Citizen = citizen;
        City = sys;

        AddBackground(0, 0, 400, 400, 5054);
        AddHtmlLocalized(0, 15, 400, 20, 1154015, 0xFFFF); // <CENTER>City Title</CENTER>

        AddHtmlLocalized(15, 45, 370, 80, 1154016, 0xFFFF);

        AddImageTiled(50, 145, 250, 20, 3504);
        AddTextEntry(51, 145, 249, 20, 0, 1, "");
        AddButton(15, 145, 4023, 4024, 1);
    }

    public PlayerMobile User { get; }
    public PlayerMobile Citizen { get; }
    public CityLoyaltySystem City { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var text = info.GetTextEntry(1);
        var entry = City?.GetPlayerEntry(Citizen);

        if (entry == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(text))
        {
            entry.CustomTitle = null;

            if (User != Citizen)
            {
                User.SendMessage("Вы сняли их титул.");
                Citizen.SendMessage($"{User.Name} снял ваш городской титул.");
            }
            else
            {
                User.SendMessage("Вы сняли свой титул.");
            }

            return;
        }

        text = Utility.FixHtml(text);

        if (!Server.Guilds.BaseGuildGump.CheckProfanity(text) || text.Trim().Length <= 3)
        {
            // That title is disallowed.
            User.SendLocalizedMessage(501179);
            return;
        }

        if (!entry.IsCitizen)
        {
            return;
        }

        entry.CustomTitle = text.Trim();

        if (User != Citizen)
        {
            User.SendMessage($"Вы даровали {Citizen.Name} титул: {text} {City.Definition.Name}.");
        }

        // Thou hath been bestowed the title ~1_TITLE~! - ~1_TITLE~ of ~2_CITY~
        Citizen.SendLocalizedMessage(1155605, $"{text}\t{City.Definition.Name}");
    }
}

public class OpenInventoryGump : Gump
{
    public OpenInventoryGump(PlayerMobile pm, CityLoyaltySystem city) : base(50, 50)
    {
        User = pm;
        City = city;

        var boxes = city.Stone?.Boxes;

        if (boxes == null)
        {
            AddBackground(0, 0, 250, 90, 5054);
            AddHtml(0, 15, 250, 20, "<center>Инвентарь</center>", false, false);
            return;
        }

        boxes.RemoveAll(b => b == null || b.Deleted);

        AddBackground(0, 0, 250, boxes.Count * 25 + 90, 5054);
        AddHtml(0, 15, 250, 20, $"<center>Инвентарь — {city.Definition.Name}</center>", false, false);

        AddLabel(10, 40, 0, "Место");
        AddLabel(150, 40, 0, "Открыть");
        AddLabel(200, 40, 0, "Убрать");

        for (var i = 0; i < boxes.Count; i++)
        {
            var box = boxes[i];

            AddLabel(10, 70 + i * 25, 0, box == null || box.Deleted ? "Удалено" : box.Location.ToString());
            AddButton(150, 70 + i * 25, 4011, 4013, i + 100);
            AddButton(200, 70 + i * 25, 4017, 4019, i + 200);
        }
    }

    public PlayerMobile User { get; }
    public CityLoyaltySystem City { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var boxes = City.Stone?.Boxes;

        if (info.ButtonID == 0 || boxes == null)
        {
            return;
        }

        if (info.ButtonID < 200)
        {
            var id = info.ButtonID - 100;

            if (id >= 0 && id < boxes.Count && boxes[id] != null && !boxes[id].Deleted)
            {
                User.SendGump(new OpenInventoryGump(User, City));
            }

            return;
        }

        var removeId = info.ButtonID - 200;

        if (removeId < 0 || removeId >= boxes.Count)
        {
            return;
        }

        var box = boxes[removeId];

        if (box == null || box.Deleted)
        {
            return;
        }

        box.Delete();
        boxes.Remove(box);
        User.SendMessage("Баллотировочная урна удалена.");
        User.SendGump(new OpenInventoryGump(User, City));
    }
}

public class SystemInfoGump : Gump
{
    public SystemInfoGump(PlayerMobile pm) : base(50, 50)
    {
        User = pm;

        AddBackground(0, 0, 400, CityLoyaltySystem.Cities.Count * 25 + 100, 5054);

        AddHtml(0, 15, 400, 20, "<center>Сведения о системе лояльности городов</center>", false, false);

        AddLabel(10, 35, 0, "Город");
        AddLabel(150, 35, 0, "Граждан");
        AddLabel(250, 35, 0, "Казна");
        AddLabel(350, 35, 0, "Просмотр");

        for (var i = 0; i < CityLoyaltySystem.Cities.Count; i++)
        {
            var city = CityLoyaltySystem.Cities[i];

            AddHtml(10, 60 + i * 25, 190, 20, city.Definition.Name, false, false);
            AddHtml(150, 60 + i * 25, 100, 20, city.GetCitizenCount().ToString(), false, false);
            AddHtml(250, 60 + i * 25, 100, 20, city.Treasury.ToString("N0"), false, false);
            AddButton(350, 60 + i * 25, 4005, 4007, i + 100);
        }
    }

    public PlayerMobile User { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0)
        {
            return;
        }

        var id = info.ButtonID - 100;

        if (id >= 0 && id < CityLoyaltySystem.Cities.Count)
        {
            User.SendGump(new CityInfoGump(User, CityLoyaltySystem.Cities[id]));
        }
    }
}

public class CityInfoGump : Gump
{
    public CityInfoGump(PlayerMobile pm, CityLoyaltySystem city) : base(50, 50)
    {
        User = pm;
        City = city;

        AddPage(0);

        AddBackground(0, 0, 500, 400, 5054);
        AddHtml(0, 15, 400, 20, $"<center>Сведения о городе — {city.Definition.Name}</center>", false, false);

        AddLabel(10, 35, 0, "Игрок");
        AddLabel(150, 35, 0, "Любовь");
        AddLabel(225, 35, 0, "Ненависть");
        AddLabel(300, 35, 0, "Титул");
        AddLabel(450, 35, 0, "Инфо");

        var page = 1;
        AddPage(page);

        AddButton(10, 370, 4014, 4016, 1);
        AddLabel(45, 370, 0, "НАЗАД");

        var entries = new System.Collections.Generic.List<CityLoyaltyEntry>(City.PlayerTable.Values);
        var pageIndex = 0;

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            pageIndex++;

            AddHtml(10, 60 + pageIndex * 25, 140, 20, $"{entry.Player?.Name ?? "Неизвестно"}{(entry.IsCitizen ? "(гражданин)" : string.Empty)}", false, false);
            AddHtml(150, 60 + pageIndex * 25, 75, 20, entry.Love.ToString(), false, false);
            AddHtml(225, 60 + pageIndex * 25, 75, 20, entry.Hate.ToString(), false, false);
            AddHtml(300, 60 + pageIndex * 25, 150, 20, string.IsNullOrEmpty(entry.CustomTitle) ? string.Empty : entry.CustomTitle, false, false);
            AddButton(450, 60 + pageIndex * 25, 4005, 4007, i + 100);

            if (pageIndex >= 11 && i < entries.Count - 1)
            {
                pageIndex = 0;
                page++;

                AddButton(252, 370, 4005, 4007, 0, GumpButtonType.Page, page);
                AddPage(page);
            }

            if (i > 12)
            {
                AddButton(228, 370, 4014, 4016, 0, GumpButtonType.Page, page - 1);
            }
        }
    }

    public PlayerMobile User { get; }
    public CityLoyaltySystem City { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0)
        {
            return;
        }

        if (info.ButtonID == 1)
        {
            User.SendGump(new SystemInfoGump(User));
            return;
        }

        var entries = new System.Collections.Generic.List<CityLoyaltyEntry>(City.PlayerTable.Values);
        var id = info.ButtonID - 100;

        if (id >= 0 && id < entries.Count)
        {
            User.SendGump(new PropertiesGump(User, entries[id]));
        }
    }
}
