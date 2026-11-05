using System;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.CleanUpBritannia;

/// <summary>
///     Витрина наград Clean Up Britannia. В ServUO это был <c>BaseRewardGump</c> из фреймворка
///     <c>Server.Engines.Points</c>, которого здесь нет, поэтому окно написано на
///     <see cref="DynamicGump" /> (наследник <see cref="BaseGump" />).
/// </summary>
public class CleanUpBritanniaRewardGump : DynamicGump
{
    private const int PerPage = 8;

    private readonly Mobile _owner;
    private readonly PlayerMobile _user;
    private readonly int _page;

    public CleanUpBritanniaRewardGump(Mobile owner, PlayerMobile user, int page = 0) : base(50, 50)
    {
        _owner = owner;
        _user = user;
        _page = page;
    }

    public override bool Singleton => true;

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var buttonId = info.ButtonID;

        if (buttonId >= 1000)
        {
            _user.SendGump(new CleanUpBritanniaRewardGump(_owner, _user, buttonId - 1000));
            return;
        }

        if (buttonId < 1 || buttonId > CleanUpBritanniaRewards.Rewards.Count)
        {
            return;
        }

        var entry = CleanUpBritanniaRewards.Rewards[buttonId - 1];

        if (CleanUpBritanniaData.GetPoints(_user) < entry.Price)
        {
            _user.SendLocalizedMessage(1074360); // You do not have enough points to trade for this item.
            return;
        }

        var item = CreateReward(entry.Type);

        if (item == null)
        {
            return;
        }

        if (entry.Hue != 0)
        {
            item.Hue = entry.Hue;
        }

        item.InvalidateProperties();

        if (_user.Backpack == null || !_user.Backpack.TryDropItem(_user, item, false))
        {
            _user.SendLocalizedMessage(1074361); // The reward could not be given. Make sure you have room in your pack.
            item.Delete();
            return;
        }

        CleanUpBritanniaData.DeductPoints(_user, entry.Price);

        _user.SendLocalizedMessage(1073621); // Your reward has been placed in your backpack.
        _user.PlaySound(0x5A7);

        _user.SendGump(new CleanUpBritanniaRewardGump(_owner, _user, _page));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var points = CleanUpBritanniaData.GetPoints(_user);
        var rewards = CleanUpBritanniaRewards.Rewards;

        builder.AddPage();

        builder.AddBackground(0, 0, 420, 500, 9380);
        builder.AddHtmlLocalized(20, 15, 380, 20, 1151316, 0xFFFF); // Clean Up Britannia Rewards
        builder.AddHtml(20, 40, 380, 20, $"<basefont color=#FFFFFF>Ваши очки: {points:N0}");

        var start = _page * PerPage;
        var y = 70;

        for (var i = start; i < Math.Min(start + PerPage, rewards.Count); i++)
        {
            var entry = rewards[i];

            builder.AddItem(20, y, entry.ItemID, entry.Hue);
            builder.AddHtmlLocalized(70, y + 5, 250, 20, entry.Tooltip != 0 ? entry.Tooltip : 1015226, 0xFFFF);
            builder.AddHtml(330, y + 5, 60, 20, $"<basefont color=#FFFFFF>{entry.Price:N0}");
            builder.AddButton(390, y + 5, 4005, 4007, i + 1);

            y += 50;
        }

        if (start > 0)
        {
            builder.AddButton(20, 470, 4014, 4016, 1000 + _page - 1);
        }

        if (start + PerPage < rewards.Count)
        {
            builder.AddButton(380, 470, 4005, 4007, 1000 + _page + 1);
        }
    }

    private static Item CreateReward(Type type)
    {
        // Свиток проворства требует навык в конструкторе, остальные награды создаются без параметров.
        if (type == typeof(ScrollofAlacrity))
        {
            return new ScrollofAlacrity((SkillName)Utility.Random(SkillInfo.Table.Length));
        }

        return Activator.CreateInstance(type) as Item;
    }
}
