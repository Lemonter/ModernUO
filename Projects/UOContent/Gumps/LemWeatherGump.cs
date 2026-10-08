using System;
using Server.Network;
using Server.Systems.MahaonSeasons;
using Server.Systems.MahaonWeather;

namespace Server.Gumps;

public enum LemWeatherCategory
{
    Main,
    Season,
    Weather,
    DayNight,
    Wind
}

/// <summary>GM tool — one click sets season, weather, or the day/night light level,
/// broadcast to everyone online immediately. Split into categories (own sub-page each)
/// instead of one long page, so adding more later doesn't push the gump past a screen's
/// worth of buttons.</summary>
public class LemWeatherGump : StaticGump<LemWeatherGump>
{
    private static readonly MahaonSeason[] Seasons =
    {
        MahaonSeason.Spring, MahaonSeason.Summer, MahaonSeason.Autumn, MahaonSeason.Winter
    };

    private static readonly string[] SeasonNamesRu = { "Весна", "Лето", "Осень", "Зима" };

    private static readonly MahaonWeather[] Weathers =
    {
        MahaonWeather.Clear, MahaonWeather.Rain, MahaonWeather.Snow, MahaonWeather.Storm
    };

    private static readonly string[] WeatherNamesRu = { "Ясно", "Дождь", "Снег", "Гроза" };

    // LightCycle.DayLevel/NightLevel are the real values the natural cycle itself already
    // uses (see Misc/LightCycle.cs) — reusing them here instead of picking our own numbers
    // keeps "Day"/"Night" here meaning exactly the same brightness the cycle would pick.
    private static readonly int[] DayNightValues = { LightCycle.DayLevel, LightCycle.NightLevel, int.MinValue };
    private static readonly string[] DayNightNamesRu = { "День", "Ночь", "Естественный цикл" };

    private readonly LemWeatherCategory _category;

    public override bool Singleton => true;
    protected override bool Cached => false;

    public LemWeatherGump(LemWeatherCategory category = LemWeatherCategory.Main) : base(80, 80) =>
        _category = category;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        switch (_category)
        {
            case LemWeatherCategory.Season:
                BuildSeasonPage(ref builder);
                break;
            case LemWeatherCategory.Weather:
                BuildWeatherPage(ref builder);
                break;
            case LemWeatherCategory.DayNight:
                BuildDayNightPage(ref builder);
                break;
            case LemWeatherCategory.Wind:
                BuildWindPage(ref builder);
                break;
            default:
                BuildMainPage(ref builder);
                break;
        }
    }

    private static void BuildMainPage(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 260, 210, 5054);
        builder.AddAlphaRegion(10, 10, 240, 190);

        builder.AddHtml(20, 15, 220, 20, "LemWeather");

        var y = 45;

        builder.AddButton(20, y, 4005, 4007, 200);
        builder.AddHtml(55, y + 2, 180, 20, $"Сезон ({SeasonSystem.NameRu(SeasonSystem.CurrentSeason)})");
        y += 30;

        builder.AddButton(20, y, 4005, 4007, 201);
        builder.AddHtml(55, y + 2, 180, 20, $"Погода ({WeatherSystem.NameRu(WeatherSystem.Current)})");
        y += 30;

        builder.AddButton(20, y, 4005, 4007, 202);
        builder.AddHtml(55, y + 2, 180, 20, "День/Ночь");
        y += 30;

        builder.AddButton(20, y, 4005, 4007, 203);
        builder.AddHtml(
            55, y + 2, 180, 20,
            $"Ветер ({WindManager.Direction:0}°, {WindManager.Strength:0.00})"
        );
    }

    private static void BuildSeasonPage(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 300, 220, 5054);
        builder.AddAlphaRegion(10, 10, 280, 200);

        builder.AddHtml(20, 15, 260, 20, "LemWeather — сезон");
        builder.AddHtml(20, 40, 260, 20, $"Сейчас: {SeasonSystem.NameRu(SeasonSystem.CurrentSeason)}");

        var y = 65;
        for (var i = 0; i < Seasons.Length; i++)
        {
            var isCurrent = Seasons[i] == SeasonSystem.CurrentSeason;
            builder.AddButton(20, y, isCurrent ? 4006 : 4005, isCurrent ? 4008 : 4007, 10 + i);
            builder.AddHtml(55, y + 2, 200, 20, SeasonNamesRu[i]);
            y += 24;
        }

        AddBackButton(ref builder, y + 6);
    }

    private static void BuildWeatherPage(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 300, 220, 5054);
        builder.AddAlphaRegion(10, 10, 280, 200);

        builder.AddHtml(20, 15, 260, 20, "LemWeather — погода");
        builder.AddHtml(20, 40, 260, 20, $"Сейчас: {WeatherSystem.NameRu(WeatherSystem.Current)}");

        var y = 65;
        for (var i = 0; i < Weathers.Length; i++)
        {
            var isCurrent = Weathers[i] == WeatherSystem.Current;
            builder.AddButton(20, y, isCurrent ? 4006 : 4005, isCurrent ? 4008 : 4007, 20 + i);
            builder.AddHtml(55, y + 2, 200, 20, WeatherNamesRu[i]);
            y += 24;
        }

        AddBackButton(ref builder, y + 6);
    }

    private static void BuildWindPage(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 320, 240, 5054);
        builder.AddAlphaRegion(10, 10, 300, 220);

        builder.AddHtml(20, 15, 280, 20, "LemWeather — ветер");
        builder.AddHtml(
            20, 40, 280, 20,
            $"Сейчас: {WindManager.Direction:0}°, сила {WindManager.Strength:0.00}, порывы {WindManager.GustStrength:0.00}"
        );

        builder.AddHtml(20, 70, 100, 20, "Направление (0-360):");
        builder.AddTextEntry(200, 70, 80, 20, 0x0, 0, WindManager.Direction.ToString("0"));

        builder.AddHtml(20, 95, 100, 20, "Сила (0-2):");
        builder.AddTextEntry(200, 95, 80, 20, 0x0, 1, WindManager.Strength.ToString("0.00"));

        builder.AddHtml(20, 120, 100, 20, "Порывы (0-1):");
        builder.AddTextEntry(200, 120, 80, 20, 0x0, 2, WindManager.GustStrength.ToString("0.00"));

        builder.AddHtml(20, 145, 180, 20, "Переход, сек (0 = мгновенно):");
        builder.AddTextEntry(200, 145, 80, 20, 0x0, 3, "0");

        builder.AddButton(20, 175, 4023, 4025, 300);
        builder.AddHtml(55, 177, 150, 20, "Применить");

        AddBackButton(ref builder, 205);
    }

    private static void BuildDayNightPage(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 300, 200, 5054);
        builder.AddAlphaRegion(10, 10, 280, 180);

        builder.AddHtml(20, 15, 260, 20, "LemWeather — день/ночь");

        var current = LightCycle.LevelOverride;
        var currentLabel = current == int.MinValue ? "Естественный цикл" :
            current == LightCycle.DayLevel ? "День" :
            current == LightCycle.NightLevel ? "Ночь" : $"Вручную ({current})";

        builder.AddHtml(20, 40, 260, 20, $"Сейчас: {currentLabel}");

        var y = 65;
        for (var i = 0; i < DayNightValues.Length; i++)
        {
            var isCurrent = DayNightValues[i] == current;
            builder.AddButton(20, y, isCurrent ? 4006 : 4005, isCurrent ? 4008 : 4007, 30 + i);
            builder.AddHtml(55, y + 2, 200, 20, DayNightNamesRu[i]);
            y += 24;
        }

        AddBackButton(ref builder, y + 6);
    }

    private static void AddBackButton(ref StaticGumpBuilder builder, int y)
    {
        builder.AddButton(20, y, 4014, 4016, 1);
        builder.AddHtml(55, y + 2, 150, 20, "Назад");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (sender.Mobile is not { AccessLevel: >= AccessLevel.GameMaster } gm)
        {
            return;
        }

        if (info.ButtonID == 1)
        {
            gm.SendGump(new LemWeatherGump());
            return;
        }

        if (info.ButtonID is >= 200 and < 200 + 4)
        {
            gm.SendGump(new LemWeatherGump((LemWeatherCategory)(info.ButtonID - 200 + 1)));
            return;
        }

        if (info.ButtonID is >= 10 and < 10 + 4)
        {
            SeasonSystem.SetSeason(Seasons[info.ButtonID - 10]);
            gm.SendGump(new LemWeatherGump(LemWeatherCategory.Season));
            return;
        }

        if (info.ButtonID is >= 20 and < 20 + 4)
        {
            WeatherSystem.SetWeather(Weathers[info.ButtonID - 20]);
            gm.SendGump(new LemWeatherGump(LemWeatherCategory.Weather));
            return;
        }

        if (info.ButtonID is >= 30 and < 30 + 3)
        {
            LightCycle.LevelOverride = DayNightValues[info.ButtonID - 30];
            gm.SendGump(new LemWeatherGump(LemWeatherCategory.DayNight));
            return;
        }

        if (info.ButtonID == 300)
        {
            var directionText = info.GetTextEntry(0);
            var strengthText = info.GetTextEntry(1);
            var gustText = info.GetTextEntry(2);
            var durationText = info.GetTextEntry(3);

            var direction = double.TryParse(directionText, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : WindManager.Direction;
            var strength = double.TryParse(strengthText, System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : WindManager.Strength;
            var gust = double.TryParse(gustText, System.Globalization.CultureInfo.InvariantCulture, out var g) ? g : WindManager.GustStrength;
            var duration = double.TryParse(durationText, System.Globalization.CultureInfo.InvariantCulture, out var dur) ? dur : 0;

            WindManager.TransitionTo((float)direction, (float)strength, (float)gust, TimeSpan.FromSeconds(duration));
            gm.SendGump(new LemWeatherGump(LemWeatherCategory.Wind));
        }
    }
}
