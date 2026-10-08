using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Systems.MahaonWorld;

namespace Server.Items;

/// <summary>
///     The owner-only entry point for the three Mahaon estate features (fence, territory
///     info, rebuild) — shown from HouseSign.ShowSign in place of jumping straight to the
///     vanilla HouseGumpAOS/HouseGump, but only for the real owner (IsOwner, not just a
///     friend/co-owner). A button on this gump still opens the normal vanilla management
///     gump unchanged — this is purely an extra stop along the way, not a replacement for
///     any existing house UI.
/// </summary>
public class MahaonHouseMenuGump : StaticGump<MahaonHouseMenuGump>
{
    private readonly BaseHouse _house;

    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonHouseMenuGump(BaseHouse house) : base(80, 80) => _house = house;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var hasFence = MahaonHouseFenceSystem.HasFence(_house);
        var radius = MahaonHouseFenceSystem.GetFenceRadius(_house);

        builder.AddPage();
        builder.AddBackground(0, 0, 300, 230, 5054);
        builder.AddAlphaRegion(10, 10, 280, 210);

        builder.AddHtml(20, 15, 260, 20, "Управление усадьбой");

        builder.AddHtml(
            20, 40, 260, 20,
            hasFence ? $"Ограда: построена ({radius} тайлов)" : "Ограда: не построена"
        );

        builder.AddButton(20, 70, 4005, 4007, 1);
        builder.AddHtml(55, 72, 220, 20, "Ограда");

        builder.AddButton(20, 100, 4005, 4007, 2);
        builder.AddHtml(55, 102, 220, 20, "Территория");

        builder.AddButton(20, 130, 4005, 4007, 3);
        builder.AddHtml(55, 132, 220, 20, "Перестройка");

        builder.AddButton(20, 170, 4005, 4007, 4);
        builder.AddHtml(55, 172, 220, 20, "Открыть управление домом (обычное)");

        builder.AddButton(20, 200, 4017, 4019, 0);
        builder.AddHtml(55, 200, 150, 20, "Закрыть");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || _house?.Deleted != false || !_house.IsOwner(from))
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                from.SendGump(new MahaonHouseFenceGump(_house));
                break;

            case 2:
                from.SendGump(new MahaonHouseTerritoryGump(_house));
                break;

            case 3:
                from.SendGump(new MahaonHouseRebuildGump(_house));
                break;

            case 4:
                if (_house.IsAosRules)
                {
                    HouseGumpAOS.DisplayTo(from, _house, HouseGumpPageAOS.Information);
                }
                else
                {
                    HouseGump.DisplayTo(from, _house);
                }

                break;
        }
    }
}
