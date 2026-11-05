using System;
using Server.Engines.VvV;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Blackthorn;

/// <summary>
///     Ported from ServUO's Blackthorn Revamped dungeon (Gumps.cs) — the reward store. ServUO built it on
///     <c>BaseRewardGump</c> from the reward-store framework this codebase does not have, so it is a plain
///     <see cref="Gump" /> modelled on the VvV and void pool reward gumps; the balance comes from the
///     invasion controller's own points tally.
/// </summary>
public class BlackthornRewardGump : Gump
{
    private const int PerPage = 8;

    public Mobile Owner { get; }
    public PlayerMobile User { get; }

    public BlackthornRewardGump(Mobile owner, PlayerMobile user, int page = 0) : base(50, 50)
    {
        Owner = owner;
        User = user;

        var controller = GetController(user);
        var points = controller?.GetPoints(user) ?? 0;
        var rewards = BlackthornRewards.Rewards;

        AddBackground(0, 0, 420, 500, 9380);
        AddHtmlLocalized(20, 15, 380, 20, 1154516, 0xFFFF); // Blackthorn's Rewards
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

    private static InvasionController GetController(Mobile m) =>
        m.Map == Map.Felucca ? InvasionController.FelInstance : InvasionController.TramInstance;

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var id = info.ButtonID;

        if (id >= 1000)
        {
            User.SendGump(new BlackthornRewardGump(Owner, User, id - 1000));
            return;
        }

        if (id < 1 || id > BlackthornRewards.Rewards.Count)
        {
            return;
        }

        var entry = BlackthornRewards.Rewards[id - 1];
        var controller = GetController(User);

        if (controller == null || controller.GetPoints(User) < entry.Price)
        {
            User.SendLocalizedMessage(1074360); // You do not have enough points to trade for this item.
            return;
        }

        if (Activator.CreateInstance(entry.Type) is not Item item)
        {
            return;
        }

        if (entry.Hue != 0 && item.Hue == 0)
        {
            item.Hue = entry.Hue;
        }

        if (User.Backpack == null || !User.Backpack.TryDropItem(User, item, false))
        {
            User.SendLocalizedMessage(1074361); // The reward could not be given. Make sure you have room in your pack.
            item.Delete();
            return;
        }

        controller.SpendPoints(User, entry.Price);

        User.SendLocalizedMessage(1073621); // Your reward has been placed in your backpack.
        User.PlaySound(0x5A7);

        User.SendGump(new BlackthornRewardGump(Owner, User));
    }
}
