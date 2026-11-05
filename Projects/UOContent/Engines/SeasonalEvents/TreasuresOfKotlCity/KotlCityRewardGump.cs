using System;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/RewardGump.cs).
///     ServUO built it on <c>BaseRewardGump</c> (no such framework here), so this is a plain
///     <see cref="Gumps.Gump" /> in the same shape as the Doom and Khaldun reward gumps. The per-entry special
///     cases (reward deeds carry their tooltip cliloc, the six tribal banners pick their tribe by list index) are
///     kept; the recipe-scroll cases are gone with the Kotl recipes.
/// </summary>
public class KotlCityRewardGump : Gumps.Gump
{
    private const int PerPage = 8;

    private readonly Mobile _owner;
    private readonly PlayerMobile _user;

    public KotlCityRewardGump(Mobile owner, PlayerMobile user, int page = 0) : base(50, 50)
    {
        _owner = owner;
        _user = user;

        var points = KotlCityData.GetPoints(user);
        var rewards = KotlCityRewards.Rewards;

        AddBackground(0, 0, 420, 500, 9380);
        AddHtmlLocalized(20, 15, 380, 20, 1156988, 0xFFFF); // Artifacts of the Kotl City
        AddHtml(20, 40, 380, 20, $"<basefont color=#FFFFFF>Очки: {points:N0}", false, false);

        var start = page * PerPage;
        var y = 70;

        for (var i = start; i < Math.Min(start + PerPage, rewards.Count); i++)
        {
            var entry = rewards[i];

            AddItem(20, y + 15, entry.ItemID, entry.Hue);
            AddHtmlLocalized(70, y + 5, 250, 20, entry.Tooltip != 0 ? entry.Tooltip : 1015226, 0xFFFF);
            AddHtml(330, y + 5, 60, 20, $"<basefont color=#FFFFFF>{entry.Price:N0}", false, false);

            AddButton(390, y + 5, 4005, 4007, i + 1);

            y += 50;
        }

        if (start > 0)
        {
            AddButton(20, 470, 4014, 4016, 1000 + page - 1);
        }

        if (start + PerPage < rewards.Count)
        {
            AddButton(380, 470, 4005, 4007, 1000 + page + 1);
        }
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var id = info.ButtonID;

        if (id >= 1000)
        {
            _user.SendGump(new KotlCityRewardGump(_owner, _user, id - 1000));
            return;
        }

        if (id < 1 || id > KotlCityRewards.Rewards.Count)
        {
            return;
        }

        var index = id - 1;
        var entry = KotlCityRewards.Rewards[index];

        if (KotlCityData.GetPoints(_user) < entry.Price)
        {
            _user.SendLocalizedMessage(1074360); // You do not have enough points to trade for this item.
            return;
        }

        var item = entry.Type == typeof(TreasuresOfKotlRewardDeed)
            ? new TreasuresOfKotlRewardDeed(entry.Tooltip)
            : entry.Type == typeof(TribalBanner)
                ? CreateBanner(index)
                : Loot.Construct(entry.Type);

        if (item == null)
        {
            return;
        }

        if (_user.Backpack == null || !_user.Backpack.TryDropItem(_user, item, false))
        {
            _user.SendLocalizedMessage(1074361); // The reward could not be given. Make sure you have room in your pack.
            item.Delete();
            return;
        }

        KotlCityData.DeductPoints(_user, entry.Price);

        _user.SendLocalizedMessage(1073621); // Your reward has been placed in your backpack.
        _user.PlaySound(0x5A7);

        _user.SendGump(new KotlCityRewardGump(_owner, _user));
    }

    private static Item CreateBanner(int index) => index switch
    {
        7  => new TribalBanner(EodonTribe.Urali),
        8  => new TribalBanner(EodonTribe.Barrab),
        9  => new TribalBanner(EodonTribe.Sakkhra),
        10 => new TribalBanner(EodonTribe.Barako),
        11 => new TribalBanner(EodonTribe.Kurak),
        _  => new TribalBanner(EodonTribe.Jukari)
    };
}
