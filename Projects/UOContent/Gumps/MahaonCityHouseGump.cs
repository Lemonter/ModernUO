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

        var height = 320 + friendCount * 22 + banCount * 22;

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

        builder.AddHtml(20, y, 320, 20, $"Закреплено: {_house.LockdownCount} из {_house.MaxLockdowns}");
        y += 22;

        builder.AddButton(20, y, 4005, 4007, 4);
        builder.AddHtml(55, y + 2, 250, 20, "Закрепить вещь (цель)");
        y += 24;

        builder.AddButton(20, y, 4005, 4007, 5);
        builder.AddHtml(55, y + 2, 250, 20, "Сундук под замок (цель)");
        y += 24;

        builder.AddButton(20, y, 4005, 4007, 6);
        builder.AddHtml(55, y + 2, 250, 20, "Освободить вещь (цель)");
        y += 30;

        if (!_house.HasBasement)
        {
            builder.AddButton(20, y, 4005, 4007, 7);
            builder.AddHtml(55, y + 2, 290, 20, $"Выкопать подвал ({_house.BasementPrice} золота)");
            y += 30;
        }

        builder.AddButton(20, y, 4017, 4019, 3);
        builder.AddHtml(55, y + 2, 290, 20, $"Продать дом городу ({_house.SalePrice / 2} золота)");
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
                from.SendMessage(0x59, Systems.MahaonWorld.MahaonCityHouseSystem.SellBack(_house, _sign));
                break;

            case 7 when isOwnerOrGm:
                from.SendMessage(0x59, Systems.MahaonWorld.MahaonCityHouseSystem.TryBuyBasement(from, _house));
                break;

            case 4 or 5 or 6 when isOwnerOrGm:
                from.SendMessage(info.ButtonID == 6 ? "Укажи закреплённую вещь." : "Укажи вещь на полу дома.");
                from.Target = new LockdownTarget(_house, this, info.ButtonID);
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

            // Out the door, onto the street by the sign.
            if (_house.Contains(target.Location, target.Map) && _gump._sign is { Deleted: false } sign)
            {
                target.MoveToWorld(sign.Location, sign.Map);
            }

            from.SendGump(new MahaonCityHouseGump(_house, _gump._sign));
        }
    }

    private class LockdownTarget : Target
    {
        private readonly MahaonCityHouse _house;
        private readonly MahaonCityHouseGump _gump;
        private readonly int _mode;

        public LockdownTarget(MahaonCityHouse house, MahaonCityHouseGump gump, int mode) : base(12, false, TargetFlags.None)
        {
            _house = house;
            _gump = gump;
            _mode = mode;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Item item)
            {
                from.SendMessage(0x22, "Нужно указать вещь.");
                return;
            }

            var message = _mode == 6 ? _house.Release(from, item) : _house.LockDown(from, item, _mode == 5);
            from.SendMessage(0x59, message);
            from.SendGump(new MahaonCityHouseGump(_house, _gump._sign));
        }
    }
}
