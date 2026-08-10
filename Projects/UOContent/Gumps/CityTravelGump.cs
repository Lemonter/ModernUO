using Server.Items;
using Server.Network;
using Server.Systems.MahaonCities;

namespace Server.Gumps;

public class CityTravelGump : StaticGump<CityTravelGump>
{
    private const long TravelCost = 50; // copper

    private readonly Mobile _player;
    private static readonly string[] CityNames;

    public override bool Singleton => true;

    static CityTravelGump()
    {
        CityNames = new string[CityControlSystem.Cities.Count];
        CityControlSystem.Cities.Keys.CopyTo(CityNames, 0);
    }

    public CityTravelGump(Mobile player) : base(50, 50) => _player = player;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var height = 60 + CityNames.Length * 25;

        builder.AddPage();
        builder.AddBackground(0, 0, 260, height, 5054);
        builder.AddAlphaRegion(10, 10, 240, height - 20);
        builder.AddHtml(15, 15, 230, 20, "Отправиться в:");

        for (var i = 0; i < CityNames.Length; i++)
        {
            var y = 45 + i * 25;
            builder.AddButton(15, y, 4005, 4007, i + 1);
            builder.AddLabelPlaceholder(45, y, 0x480, $"city{i}");
        }
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        for (var i = 0; i < CityNames.Length; i++)
        {
            builder.SetStringSlot($"city{i}", CityNames[i]);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var index = info.ButtonID - 1;
        if (index < 0 || index >= CityNames.Length)
        {
            return;
        }

        var cityName = CityNames[index];
        if (!CityControlSystem.Cities.TryGetValue(cityName, out var info2))
        {
            return;
        }

        if (_player.Map == info2.map && _player.Location == info2.spawn)
        {
            _player.SendMessage("Ты уже там.");
            return;
        }

        var backpack = _player.Backpack;
        if (backpack == null || !CurrencyHelper.TryWithdrawCopperValue(backpack, TravelCost))
        {
            _player.SendMessage($"Использование камня телепорта стоит {TravelCost} меди.");
            return;
        }

        _player.MoveToWorld(info2.spawn, info2.map);
        _player.SendMessage(0x59, $"Ты телепортируешься в {cityName}.");
    }
}
