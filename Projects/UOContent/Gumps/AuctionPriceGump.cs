using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonAuction;

namespace Server.Gumps;

public class AuctionPriceGump : DynamicGump
{
    private readonly PlayerMobile _player;
    private readonly Item _item;
    private readonly string _city;

    public override bool Singleton => true;

    public AuctionPriceGump(PlayerMobile player, Item item, string city) : base(50, 50)
    {
        _player = player;
        _item = item;
        _city = city;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 300, 160, 5054);
        builder.AddAlphaRegion(10, 10, 280, 140);

        builder.AddItem(15, 15, _item.ItemID, _item.Hue);
        builder.AddLabel(55, 15, 0x480, _item.Name ?? _item.GetType().Name);

        builder.AddHtml(15, 50, 270, 20, "Цена (золото):");
        builder.AddTextEntry(15, 75, 150, 20, 0x480, 0, "100");

        builder.AddButton(15, 120, 4005, 4007, 1);
        builder.AddLabel(50, 120, 0x480, "Выставить лот (на месяц)");

        builder.AddButton(200, 120, 4017, 4019, 0);
        builder.AddLabel(235, 120, 0x480, "Отмена");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID != 1 || _item.Deleted)
        {
            return;
        }

        var text = info.GetTextEntry(0)?.Trim();

        if (string.IsNullOrEmpty(text) || !long.TryParse(text, out var price) || price <= 0)
        {
            _player.SendMessage("Введи корректную положительную цену.");
            return;
        }

        if (!_item.IsChildOf(_player.Backpack))
        {
            _player.SendMessage("Предмет должен всё ещё быть у тебя.");
            return;
        }

        AuctionHouseSystem.CreateListing(_player, _item, price, _city);
        _player.SendMessage(0x59, $"Лот выставлен: {_item.Name ?? _item.GetType().Name} за {price} золота.");
    }
}
