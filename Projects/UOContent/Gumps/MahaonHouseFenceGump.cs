using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Systems.MahaonWorld;

namespace Server.Items;

public class MahaonHouseFenceGump : StaticGump<MahaonHouseFenceGump>
{
    private readonly BaseHouse _house;

    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonHouseFenceGump(BaseHouse house) : base(80, 80) => _house = house;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var hasFence = MahaonHouseFenceSystem.HasFence(_house);
        var radius = MahaonHouseFenceSystem.GetFenceRadius(_house);

        builder.AddPage();
        builder.AddBackground(0, 0, 320, 260, 5054);
        builder.AddAlphaRegion(10, 10, 300, 240);

        builder.AddHtml(20, 15, 280, 20, "Ограда усадьбы");
        builder.AddHtml(
            20, 38, 280, 20,
            hasFence ? $"Сейчас: построена на {radius} тайлов" : "Сейчас: не построена"
        );

        var y = 70;

        foreach (var r in MahaonHouseFenceSystem.AllowedRadii)
        {
            var cost = MahaonHouseFenceSystem.GetCost(r);
            var isCurrent = hasFence && radius == r;

            builder.AddButton(20, y, isCurrent ? 4006 : 4005, isCurrent ? 4008 : 4007, 10 + r);
            builder.AddHtml(55, y + 2, 240, 20, $"{r} тайлов — {cost} золота" + (isCurrent ? " (текущая)" : ""));
            y += 28;
        }

        y += 6;

        if (hasFence)
        {
            builder.AddButton(20, y, 4017, 4019, 99);
            builder.AddHtml(55, y + 2, 240, 20, "Снести ограду (без возврата денег)");
            y += 28;
        }

        builder.AddButton(20, y, 4014, 4016, 0);
        builder.AddHtml(55, y + 2, 150, 20, "Назад");
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

        if (info.ButtonID == 99)
        {
            MahaonHouseFenceSystem.Remove(_house);
            from.SendMessage(0x59, "Ограда снесена.");
            from.SendGump(new MahaonHouseFenceGump(_house));
            return;
        }

        if (info.ButtonID is > 10 and < 20)
        {
            var radius = info.ButtonID - 10;
            var message = MahaonHouseFenceSystem.Build(from, _house, radius);
            from.SendMessage(0x59, message);
            from.SendGump(new MahaonHouseFenceGump(_house));
        }
    }
}
