using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonAuction;
using Server.Targeting;

namespace Server.Gumps;

public class AuctionBrowseGump : DynamicGump
{
    private const int PageSize = 10;

    private readonly PlayerMobile _player;
    private readonly string _city;
    private readonly int _page;
    private readonly List<AuctionListing> _pageListings = new();
    private readonly int _totalCount;

    public override bool Singleton => true;

    public AuctionBrowseGump(PlayerMobile player, string city, int page) : base(50, 50)
    {
        _player = player;
        _city = city;
        _page = page;
        _totalCount = AuctionHouseSystem.ActiveCount(city);

        var start = page * PageSize;
        var i = 0;

        foreach (var listing in AuctionHouseSystem.ActiveListings(city))
        {
            if (i >= start && _pageListings.Count < PageSize)
            {
                _pageListings.Add(listing);
            }

            i++;
        }
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 420, 420, 5054);
        builder.AddAlphaRegion(10, 10, 400, 400);

        var totalPages = System.Math.Max(1, (_totalCount + PageSize - 1) / PageSize);
        builder.AddHtml(15, 15, 390, 20, $"Аукцион {_city} — стр. {_page + 1}/{totalPages} (лотов: {_totalCount})");

        for (var i = 0; i < _pageListings.Count; i++)
        {
            var y = 45 + i * 32;
            var listing = _pageListings[i];

            builder.AddItem(15, y, listing.Item.ItemID, listing.Item.Hue);
            builder.AddLabel(55, y, 0x480, listing.Item.Name ?? listing.Item.GetType().Name);
            builder.AddLabel(230, y, 0x59, $"{listing.Price} мон.");

            if (listing.Seller == _player)
            {
                builder.AddLabel(300, y, 0x480, "(твой)");
                builder.AddButton(370, y, 4017, 4019, 2000 + i); // cancel
            }
            else
            {
                builder.AddButton(370, y, 4005, 4007, 1000 + i); // buy
            }
        }

        if (_page > 0)
        {
            builder.AddButton(15, 375, 4014, 4016, 9001);
            builder.AddLabel(50, 375, 0x480, "Назад");
        }

        if ((_page + 1) * PageSize < _totalCount)
        {
            builder.AddButton(150, 375, 4005, 4007, 9002);
            builder.AddLabel(185, 375, 0x480, "Далее");
        }

        builder.AddButton(15, 400, 4005, 4007, 9003);
        builder.AddLabel(50, 400, 0x480, "Продать предмет");

        builder.AddButton(300, 400, 4017, 4019, 9000);
        builder.AddLabel(335, 400, 0x480, "Закрыть");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        switch (info.ButtonID)
        {
            case 9000:
                return;
            case 9001:
                _player.SendGump(new AuctionBrowseGump(_player, _city, _page - 1));
                return;
            case 9002:
                _player.SendGump(new AuctionBrowseGump(_player, _city, _page + 1));
                return;
            case 9003:
                _player.SendMessage("Укажи предмет в рюкзаке, который хочешь продать.");
                _player.Target = new AuctionSellTarget(_city);
                return;
        }

        if (info.ButtonID >= 2000)
        {
            var index = info.ButtonID - 2000;
            if (index >= 0 && index < _pageListings.Count)
            {
                AuctionHouseSystem.TryCancel(_player, _pageListings[index].Id);
                _player.SendMessage(0x59, "Лот отменён, предмет возвращён в банк.");
            }

            _player.SendGump(new AuctionBrowseGump(_player, _city, _page));
            return;
        }

        if (info.ButtonID >= 1000)
        {
            var index = info.ButtonID - 1000;
            if (index >= 0 && index < _pageListings.Count)
            {
                var result = AuctionHouseSystem.TryBuy(_player, _pageListings[index].Id);

                _player.SendMessage(result switch
                {
                    AuctionBuyResult.Success    => "Покупка совершена.",
                    AuctionBuyResult.CantAfford => "Тебе не хватает золота.",
                    AuctionBuyResult.OwnListing => "Нельзя купить свой же лот.",
                    _                           => "Этот лот больше недоступен."
                });
            }

            _player.SendGump(new AuctionBrowseGump(_player, _city, _page));
        }
    }
}

public class AuctionSellTarget : Target
{
    private readonly string _city;

    public AuctionSellTarget(string city) : base(2, false, TargetFlags.None) => _city = city;

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (from is not PlayerMobile pm)
        {
            return;
        }

        if (targeted is not Item item || !item.IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должен быть предмет в твоём рюкзаке.");
            return;
        }

        from.SendGump(new AuctionPriceGump(pm, item, _city));
    }
}
