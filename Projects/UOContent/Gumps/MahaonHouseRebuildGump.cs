using System;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items;

public class MahaonHouseRebuildGump : StaticGump<MahaonHouseRebuildGump>
{
    private readonly BaseHouse _house;
    private readonly int _page;

    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonHouseRebuildGump(BaseHouse house, int page = 0) : base(60, 60)
    {
        _house = house;
        _page = page;
    }

    private static HousePlacementEntry[] Entries =>
        Core.EJ ? HousePlacementEntry.HousesEJ : HousePlacementEntry.ClassicHouses;

    private const int PerPage = 10;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var entries = Entries;
        var totalPages = Math.Max(1, (entries.Length + PerPage - 1) / PerPage);
        var page = Math.Clamp(_page, 0, totalPages - 1);

        builder.AddPage();
        builder.AddBackground(0, 0, 420, 360, 5054);
        builder.AddAlphaRegion(10, 10, 400, 340);

        builder.AddHtml(20, 15, 380, 20, "Перестройка дома — выбери новый тип");
        builder.AddHtml(
            20, 38, 380, 40,
            "Внимание: старый дом будет снесён полностью, вместе со всем содержимым — сундуки, наковальня, любые предметы внутри. Это необратимо."
        );

        var y = 85;

        for (var i = page * PerPage; i < Math.Min(entries.Length, page * PerPage + PerPage); i++)
        {
            var entry = entries[i];
            builder.AddButton(20, y, 4005, 4007, 10 + i);
            builder.AddHtmlLocalized(55, y + 2, 250, 20, entry.Description);
            builder.AddLabel(310, y + 2, 0x480, entry.Cost.ToString());
            y += 24;
        }

        y += 6;

        if (page > 0)
        {
            builder.AddButton(20, y, 4014, 4016, 1);
            builder.AddHtml(55, y + 2, 100, 20, "Назад");
        }

        if (page < totalPages - 1)
        {
            builder.AddButton(150, y, 4005, 4007, 2);
            builder.AddHtml(185, y + 2, 100, 20, "Дальше");
        }

        y += 26;

        builder.AddButton(20, y, 4017, 4019, 0);
        builder.AddHtml(55, y + 2, 150, 20, "Закрыть меню усадьбы");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || _house?.Deleted != false || !_house.IsOwner(from))
        {
            return;
        }

        if (info.ButtonID == 0)
        {
            from.SendGump(new MahaonHouseMenuGump(_house));
            return;
        }

        var entries = Entries;
        var totalPages = Math.Max(1, (entries.Length + PerPage - 1) / PerPage);

        if (info.ButtonID == 1)
        {
            from.SendGump(new MahaonHouseRebuildGump(_house, Math.Max(0, _page - 1)));
            return;
        }

        if (info.ButtonID == 2)
        {
            from.SendGump(new MahaonHouseRebuildGump(_house, Math.Min(totalPages - 1, _page + 1)));
            return;
        }

        if (info.ButtonID < 10 || info.ButtonID - 10 >= entries.Length)
        {
            return;
        }

        var chosen = entries[info.ButtonID - 10];

        from.SendGump(
            new MahaonHouseRebuildConfirmGump(_house, chosen)
        );
    }
}

public class MahaonHouseRebuildConfirmGump : StaticGump<MahaonHouseRebuildConfirmGump>
{
    private readonly BaseHouse _house;
    private readonly HousePlacementEntry _entry;

    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonHouseRebuildConfirmGump(BaseHouse house, HousePlacementEntry entry) : base(100, 100)
    {
        _house = house;
        _entry = entry;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 380, 220, 5054);
        builder.AddAlphaRegion(10, 10, 360, 200);

        builder.AddHtml(20, 15, 340, 20, "Подтверждение перестройки");
        builder.AddHtmlLocalized(20, 45, 340, 20, _entry.Description);
        builder.AddHtml(20, 68, 340, 20, $"Стоимость: {_entry.Cost} золота");
        builder.AddHtml(
            20, 95, 340, 60,
            "Старый дом и всё, что в нём находится, будет уничтожено безвозвратно. Вынеси ценные вещи заранее, если это ещё возможно."
        );

        builder.AddButton(20, 170, 4005, 4007, 1);
        builder.AddHtml(55, 172, 200, 20, "Да, перестроить");

        builder.AddButton(20, 195, 4017, 4019, 0);
        builder.AddHtml(55, 197, 200, 20, "Отмена");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || _house?.Deleted != false)
        {
            return;
        }

        if (info.ButtonID == 1)
        {
            var message = MahaonHouseRebuildSystem.Rebuild(from, _house, _entry);
            from.SendMessage(0x59, message);
        }
    }
}
