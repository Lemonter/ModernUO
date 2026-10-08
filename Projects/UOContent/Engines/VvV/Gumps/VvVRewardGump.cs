using System;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Gumps/RewardGump.cs), but
// rewritten from scratch against a plain `Gump` instead of ServUO's `BaseRewardGump` — that's
// part of the same `Server.Engines.Points` reward-store framework this codebase doesn't have
// (see ViceVsVirtueSystem.cs header). Silver balance/deduction now reads directly from
// `VvVPlayerEntry.Points` instead of `PointsSystem.ViceVsVirtue`.
public class VvVRewardGump : Gump
{
    public Mobile Owner { get; }
    public PlayerMobile User { get; }

    private const int PerPage = 8;

    public VvVRewardGump(Mobile owner, PlayerMobile user, int page = 0) : base(50, 50)
    {
        Owner = owner;
        User = user;

        var entry = ViceVsVirtueSystem.Instance.GetPlayerEntry(user, true);
        var rewards = VvVRewards.Rewards;

        AddBackground(0, 0, 420, 500, 9380);
        AddHtmlLocalized(20, 15, 380, 20, 1155512, 0xFFFF); // Vice vs Virtue Rewards
        AddHtml(20, 40, 380, 20, $"<basefont color=#FFFFFF>Silver: {entry.Points:N0}", false, false);

        var start = page * PerPage;
        var y = 70;

        for (var i = start; i < Math.Min(start + PerPage, rewards.Count); i++)
        {
            var item = rewards[i];

            AddItem(20, y, item.ItemID, item.Hue);
            AddHtmlLocalized(70, y + 5, 250, 20, item.Tooltip != 0 ? item.Tooltip : 1015226, item.Hue == 0 ? 0xFFFF : GumpColor.Convert32To16(0xFFFFFF));
            AddHtml(330, y + 5, 60, 20, $"<basefont color=#FFFFFF>{item.Price:N0}", false, false);

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
            User.SendGump(new VvVRewardGump(Owner, User, id - 1000));
            return;
        }

        if (id < 1)
        {
            return;
        }

        var index = id - 1;

        if (index < 0 || index >= VvVRewards.Rewards.Count)
        {
            return;
        }

        var citem = VvVRewards.Rewards[index];
        var entry = ViceVsVirtueSystem.Instance.GetPlayerEntry(User, true);

        if (entry.Points < citem.Price)
        {
            User.SendLocalizedMessage(1074360); // You do not have enough points to trade for this item.
            return;
        }

        Item item;

        if (citem.Type == typeof(VvVPotionKeg))
        {
            var type = index switch
            {
                1 => PotionType.Supernova,
                2 => PotionType.StatLossRemoval,
                3 => PotionType.AntiParalysis,
                _ => PotionType.GreaterStamina
            };

            item = new VvVPotionKeg(type);
        }
        else if (citem.Type == typeof(VvVSteedStatuette))
        {
            var type = index == 5 || index == 6 ? SteedType.WarHorse : SteedType.Ostard;
            item = new VvVSteedStatuette(type, citem.Hue);
        }
        else if (citem.Type == typeof(VvVTrapKit))
        {
            var type = index - 11 switch
            {
                1 => VvVTrapType.Cold,
                2 => VvVTrapType.Energy,
                3 => VvVTrapType.Blade,
                4 => VvVTrapType.Explosion,
                _ => VvVTrapType.Poison
            };

            item = new VvVTrapKit(type);
        }
        else if (citem.Type == typeof(VvVRobe) || citem.Type == typeof(VvVHairDye))
        {
            item = Activator.CreateInstance(citem.Type, citem.Hue) as Item;
        }
        else if (citem.Type == typeof(ScrollofTranscendence))
        {
            item = ScrollofTranscendence.CreateRandom(10, 10);
        }
        else
        {
            item = Activator.CreateInstance(citem.Type) as Item;
        }

        if (item == null)
        {
            return;
        }

        VvVRewards.OnRewardItemCreated(User, item);

        if (User.Backpack == null || !User.Backpack.TryDropItem(User, item, false))
        {
            User.SendLocalizedMessage(1074361); // The reward could not be given. Make sure you have room in your pack.
            item.Delete();
        }
        else
        {
            if (User.AccessLevel == AccessLevel.Player)
            {
                entry.Points -= citem.Price;
            }

            User.SendLocalizedMessage(1073621); // Your reward has been placed in your backpack.
            User.PlaySound(0x5A7);
        }

        User.SendGump(new VvVRewardGump(Owner, User));
    }
}
