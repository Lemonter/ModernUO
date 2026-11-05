using System;
using Server.Engines.VvV;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/RewardGump.cs).
///     ServUO built it on <c>BaseRewardGump</c> from the reward-store framework this codebase does not have, so it
///     is a plain <see cref="Gumps.Gump" /> modelled on the Blackthorn reward gump, reading points from
///     <see cref="DoomData" />. The reward deeds are handed out as plain <see cref="TreasuresOfDoomRewardDeed" />
///     carrying the entry's tooltip cliloc.
/// </summary>
public class DoomRewardGump : Gumps.Gump
{
    private const int PerPage = 8;

    private readonly Mobile _owner;
    private readonly PlayerMobile _user;

    public DoomRewardGump(Mobile owner, PlayerMobile user, int page = 0) : base(50, 50)
    {
        _owner = owner;
        _user = user;

        var points = DoomData.GetPoints(user);
        var rewards = DoomRewards.Rewards;

        AddBackground(0, 0, 420, 500, 9380);
        AddHtmlLocalized(20, 15, 380, 20, 1155595, 0xFFFF); // Artifacts of Doom
        AddHtml(20, 40, 380, 20, $"<basefont color=#FFFFFF>Очки: {points:N0}", false, false);

        var start = page * PerPage;
        var y = 70;

        for (var i = start; i < Math.Min(start + PerPage, rewards.Count); i++)
        {
            var entry = rewards[i];

            AddItem(20, y, entry.ItemID, entry.Hue);
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
            _user.SendGump(new DoomRewardGump(_owner, _user, id - 1000));
            return;
        }

        if (id < 1 || id > DoomRewards.Rewards.Count)
        {
            return;
        }

        var entry = DoomRewards.Rewards[id - 1];

        if (DoomData.GetPoints(_user) < entry.Price)
        {
            _user.SendLocalizedMessage(1074360); // You do not have enough points to trade for this item.
            return;
        }

        var item = entry.Type == typeof(TreasuresOfDoomRewardDeed)
            ? new TreasuresOfDoomRewardDeed(entry.Tooltip)
            : Loot.Construct(entry.Type);

        if (item == null)
        {
            return;
        }

        if (item is LanternOfLight lantern)
        {
            lantern.OwnerName = _user.Name;
        }

        if (_user.Backpack == null || !_user.Backpack.TryDropItem(_user, item, false))
        {
            _user.SendLocalizedMessage(1074361); // The reward could not be given. Make sure you have room in your pack.
            item.Delete();
            return;
        }

        DoomData.DeductPoints(_user, entry.Price);

        _user.SendLocalizedMessage(1073621); // Your reward has been placed in your backpack.
        _user.PlaySound(0x5A7);

        _user.SendGump(new DoomRewardGump(_owner, _user));
    }
}
