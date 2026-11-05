using System;
using Server.Engines.VvV;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.VoidPool;

/// <summary>
///     Vela's reward store, written against a plain <see cref="Gump" /> the same way the VvV port did —
///     ServUO's <c>BaseRewardGump</c> belongs to the reward-store framework this codebase lacks. The
///     balance is read from, and charged to, the player's <see cref="VoidPoolController" /> on whichever
///     facet they are standing on.
/// </summary>
public class VoidPoolRewardGump : Gump
{
    private const int PerPage = 8;

    public Mobile Owner { get; }
    public PlayerMobile User { get; }

    public VoidPoolRewardGump(Mobile owner, PlayerMobile user, int page = 0) : base(50, 50)
    {
        Owner = owner;
        User = user;

        var controller = GetController(user);
        var points = controller?.GetPoints(user) ?? 0;
        var rewards = VoidPoolRewards.Rewards;

        AddBackground(0, 0, 420, 500, 9380);
        AddHtmlLocalized(20, 15, 380, 20, 1152531, 0xFFFF); // The Void Pool
        AddHtml(20, 40, 380, 20, $"<basefont color=#FFFFFF>Очки Бассейна Пустоты: {points:N0}", false, false);

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

    private static VoidPoolController GetController(Mobile m) =>
        m.Map == Map.Felucca ? VoidPoolController.InstanceFel : VoidPoolController.InstanceTram;

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var id = info.ButtonID;

        if (id >= 1000)
        {
            User.SendGump(new VoidPoolRewardGump(Owner, User, id - 1000));
            return;
        }

        if (id < 1 || id > VoidPoolRewards.Rewards.Count)
        {
            return;
        }

        var entry = VoidPoolRewards.Rewards[id - 1];
        var controller = GetController(User);

        if (controller == null || controller.GetPoints(User) < entry.Price)
        {
            User.SendLocalizedMessage(1074360); // You do not have enough points to trade for this item.
            return;
        }

        // Talismans and the cauldron deed are built from a crafting resource, which the catalogue
        // carries in Hue (the same trick ServUO's reward store used).
        var takesResource = entry.Type == typeof(SmeltersTalisman) ||
                            entry.Type == typeof(WoodsmansTalisman) ||
                            entry.Type == typeof(CauldronOfTransmutationDeed) ||
                            entry.Type == typeof(HarvestMap);

        var item = takesResource
            ? Activator.CreateInstance(entry.Type, (CraftResource)entry.Hue) as Item
            : Activator.CreateInstance(entry.Type) as Item;

        if (item == null)
        {
            return;
        }

        if (!takesResource && entry.Hue != 0 && item.Hue == 0)
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

        User.SendGump(new VoidPoolRewardGump(Owner, User));
    }
}
