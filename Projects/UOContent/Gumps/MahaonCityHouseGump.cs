using Server.Gumps;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

public class MahaonCityHouseGump : StaticGump<MahaonCityHouseGump>
{
    private readonly MahaonCityHouse _house;
    private readonly MahaonCityHouseSign _sign;

    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonCityHouseGump(MahaonCityHouse house, MahaonCityHouseSign sign) : base(80, 80)
    {
        _house = house;
        _sign = sign;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var friendCount = _house.Friends.Count;
        var banCount = _house.Bans.Count;

        var height = 200 + friendCount * 22 + banCount * 22;

        builder.AddPage();
        builder.AddBackground(0, 0, 360, height, 5054);
        builder.AddAlphaRegion(10, 10, 340, height - 20);

        builder.AddHtml(20, 15, 320, 20, $"«{_house.Label}» — владелец: {_house.Owner?.Name ?? "никто"}");

        var y = 42;

        builder.AddHtml(20, y, 320, 20, $"Друзья ({friendCount}):");
        y += 22;

        for (var i = 0; i < friendCount; i++)
        {
            builder.AddHtml(30, y, 250, 20, _house.Friends[i].Name);
            builder.AddButton(290, y, 4017, 4019, 200 + i);
            y += 22;
        }

        builder.AddButton(20, y, 4005, 4007, 1);
        builder.AddHtml(55, y + 2, 250, 20, "Добавить друга (цель)");
        y += 28;

        builder.AddHtml(20, y, 320, 20, $"Забаненные ({banCount}):");
        y += 22;

        for (var i = 0; i < banCount; i++)
        {
            builder.AddHtml(30, y, 250, 20, _house.Bans[i].Name);
            builder.AddButton(290, y, 4017, 4019, 300 + i);
            y += 22;
        }

        builder.AddButton(20, y, 4005, 4007, 2);
        builder.AddHtml(55, y + 2, 250, 20, "Забанить игрока (цель)");
        y += 34;

        builder.AddButton(20, y, 4017, 4019, 3);
        builder.AddHtml(55, y + 2, 250, 20, "Отказаться от дома");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || _house?.Deleted != false || !_house.IsFriend(from))
        {
            return;
        }

        var isOwnerOrGm = _house.IsOwner(from);

        switch (info.ButtonID)
        {
            case 1 when isOwnerOrGm:
                from.SendMessage("Целься в игрока, которого хочешь добавить в друзья.");
                from.Target = new AddFriendTarget(_house, this);
                break;

            case 2 when isOwnerOrGm:
                from.SendMessage("Целься в игрока, которого хочешь забанить.");
                from.Target = new BanTarget(_house, this);
                break;

            case 3 when isOwnerOrGm:
                _house.Owner = null;
                _house.Friends.Clear();
                _sign.RefreshName();
                from.SendMessage(0x59, $"Ты больше не владелец «{_house.Label}».");
                break;

            default:
                if (isOwnerOrGm && info.ButtonID is >= 200 and < 300)
                {
                    var index = info.ButtonID - 200;
                    if (index >= 0 && index < _house.Friends.Count)
                    {
                        _house.Friends.RemoveAt(index);
                    }

                    from.SendGump(new MahaonCityHouseGump(_house, _sign));
                }
                else if (isOwnerOrGm && info.ButtonID is >= 300 and < 400)
                {
                    var index = info.ButtonID - 300;
                    if (index >= 0 && index < _house.Bans.Count)
                    {
                        _house.Bans.RemoveAt(index);
                    }

                    from.SendGump(new MahaonCityHouseGump(_house, _sign));
                }

                break;
        }
    }

    private class AddFriendTarget : Target
    {
        private readonly MahaonCityHouse _house;
        private readonly MahaonCityHouseGump _gump;

        public AddFriendTarget(MahaonCityHouse house, MahaonCityHouseGump gump) : base(12, false, TargetFlags.None)
        {
            _house = house;
            _gump = gump;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Mobile target || target == from)
            {
                from.SendMessage(0x22, "Нужно выбрать другого игрока.");
                return;
            }

            if (!_house.Friends.Contains(target))
            {
                _house.Friends.Add(target);
                from.SendMessage(0x59, $"{target.Name} добавлен в друзья дома.");
            }

            from.SendGump(new MahaonCityHouseGump(_house, _gump._sign));
        }
    }

    private class BanTarget : Target
    {
        private readonly MahaonCityHouse _house;
        private readonly MahaonCityHouseGump _gump;

        public BanTarget(MahaonCityHouse house, MahaonCityHouseGump gump) : base(12, false, TargetFlags.None)
        {
            _house = house;
            _gump = gump;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Mobile target || target == from)
            {
                from.SendMessage(0x22, "Нужно выбрать другого игрока.");
                return;
            }

            _house.Friends.Remove(target);

            if (!_house.Bans.Contains(target))
            {
                _house.Bans.Add(target);
                from.SendMessage(0x59, $"{target.Name} забанен.");
            }

            from.SendGump(new MahaonCityHouseGump(_house, _gump._sign));
        }
    }
}
