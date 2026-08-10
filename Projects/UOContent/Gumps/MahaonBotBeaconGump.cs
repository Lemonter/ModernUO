using Server.Network;

namespace Server.Gumps;

public class MahaonBotBeaconGump : StaticGump<MahaonBotBeaconGump>
{
    private readonly Items.MahaonBotBeacon _beacon;

    private static readonly Mobiles.BotArchetype[] Archetypes =
    {
        Mobiles.BotArchetype.Warrior, Mobiles.BotArchetype.Mage, Mobiles.BotArchetype.Archer,
        Mobiles.BotArchetype.Crafter, Mobiles.BotArchetype.Trader
    };

    private static readonly string[] ArchetypeNamesRu = { "Воины", "Маги", "Лучники", "Крафтеры", "Торговцы" };

    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonBotBeaconGump(Items.MahaonBotBeacon beacon) : base(80, 80) => _beacon = beacon;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 320, 335, 5054);
        builder.AddAlphaRegion(10, 10, 300, 315);

        builder.AddHtml(20, 15, 280, 20, "Маяк ботов — настройка");
        builder.AddHtml(20, 38, 280, 20, $"Сейчас: {_beacon.OwnedCount()} / {_beacon.TotalLimit}");
        builder.AddHtml(
            20, 58, 280, 20,
            $"Город: {(string.IsNullOrEmpty(_beacon.CityName) ? "не задан" : _beacon.CityName)}"
        );
        builder.AddButton(280, 58, 4011, 4013, 3); // cycle city

        builder.AddLabel(20, 85, 0x480, "Общий лимит:");
        builder.AddTextEntry(160, 85, 100, 20, 0x0, 0, _beacon.TotalLimit.ToString());

        var y = 115;
        for (var i = 0; i < Archetypes.Length; i++)
        {
            builder.AddLabel(20, y, 0x480, $"{ArchetypeNamesRu[i]} (сейчас {_beacon.OwnedCount(Archetypes[i])}):");
            builder.AddTextEntry(230, y, 60, 20, 0x0, i + 1, _beacon.GetTarget(Archetypes[i]).ToString());
            y += 25;
        }

        builder.AddLabel(20, y, 0x480, $"Воры (сейчас {_beacon.OwnedThiefCount()}):");
        builder.AddTextEntry(230, y, 60, 20, 0x0, Archetypes.Length + 1, _beacon.ThiefTarget.ToString());
        y += 25;

        builder.AddButton(20, y + 10, 4005, 4007, 1);
        builder.AddHtml(55, y + 12, 150, 20, "Сохранить");

        builder.AddButton(180, y + 10, 4011, 4013, 2);
        builder.AddHtml(215, y + 12, 100, 20, "Гильдии");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 2 && !_beacon.Deleted)
        {
            sender.Mobile?.SendGump(new MahaonBotGuildGump(_beacon));
            return;
        }

        if (info.ButtonID == 3 && !_beacon.Deleted)
        {
            var cities = new System.Collections.Generic.List<string>(Systems.MahaonCities.CityControlSystem.Cities.Keys);
            if (cities.Count > 0)
            {
                var currentIndex = string.IsNullOrEmpty(_beacon.CityName) ? -1 : cities.IndexOf(_beacon.CityName);
                var nextIndex = currentIndex + 1;
                _beacon.CityName = nextIndex >= cities.Count ? null : cities[nextIndex];
            }

            sender.Mobile?.SendGump(new MahaonBotBeaconGump(_beacon));
            return;
        }

        if (info.ButtonID != 1 || _beacon.Deleted)
        {
            return;
        }

        var limitText = info.GetTextEntry(0);
        if (limitText != null && int.TryParse(limitText, out var limit))
        {
            _beacon.TotalLimit = System.Math.Max(0, limit);
        }

        for (var i = 0; i < Archetypes.Length; i++)
        {
            var text = info.GetTextEntry(i + 1);
            if (text != null && int.TryParse(text, out var value))
            {
                _beacon.SetTarget(Archetypes[i], value);
            }
        }

        var thiefText = info.GetTextEntry(Archetypes.Length + 1);
        if (thiefText != null && int.TryParse(thiefText, out var thiefValue))
        {
            _beacon.ThiefTarget = System.Math.Max(0, thiefValue);
        }

        sender.Mobile?.SendMessage(0x59, "Маяк настроен.");
        sender.Mobile?.SendGump(new MahaonBotBeaconGump(_beacon));
    }
}
