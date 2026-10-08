using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Systems.MahaonWorld;

namespace Server.Items;

public class MahaonHouseTerritoryGump : StaticGump<MahaonHouseTerritoryGump>
{
    private readonly BaseHouse _house;

    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonHouseTerritoryGump(BaseHouse house) : base(80, 80) => _house = house;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var hasFence = MahaonHouseFenceSystem.HasFence(_house);
        var radius = MahaonHouseFenceSystem.GetFenceRadius(_house);
        var level = MahaonHouseFenceSystem.GetLevel(radius);
        var bonus = (int)((MahaonHouseFenceSystem.GetBonusMultiplier(radius) - 1) * 100);

        builder.AddPage();
        builder.AddBackground(0, 0, 340, 220, 5054);
        builder.AddAlphaRegion(10, 10, 320, 200);

        builder.AddHtml(20, 15, 300, 20, "Территория усадьбы");

        builder.AddHtml(
            20, 45, 300, 60,
            hasFence
                ? $"Земля внутри ограды (радиус {radius} тайлов, уровень {level}) — особая: питомцы тренируются на +{bonus}% быстрее, а грядки, деревья и фрукты дают на +{bonus}% больше урожая. Следующий уровень ограды даёт ещё +50%."
                : "Бонус к территории доступен только после постройки ограды — сейчас у этого дома ограды нет."
        );

        builder.AddButton(20, 180, 4014, 4016, 0);
        builder.AddHtml(55, 182, 150, 20, "Назад");
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
        }
    }
}
