using System;
using Server.Commands;
using Server.Gumps;
using Server.Network;

namespace Server.Systems.MahaonWeather;

public enum MahaonWeather
{
    Clear,
    Rain,
    Snow,
    Storm
}

/// <summary>
///     GM-driven weather, sent through the real UO weather packet (0x65 — the same one
///     every classic client already knows how to render: rain/snow particles, the works).
///     Not on a timer like SeasonSystem — purely a button push from LemWeatherGump. Global
///     across every map/facet; no per-region weather here.
/// </summary>
public sealed class WeatherSystem : GenericPersistence
{
    private static WeatherSystem _instance;

    public WeatherSystem() : base("MahaonWeather", 1)
    {
    }

    // Real UO WeatherType byte values (ClassicUO's Game/Weather.cs WT_* enum) — 0xFF isn't
    // a real weather type, so sending it just never matches a render case client-side,
    // which is the conventional "turn it off" signal every classic-era server has used.
    private const byte WtRain = 0;
    private const byte WtSnow = 2;
    private const byte WtStorm = 3;
    private const byte WtOff = 0xFF;

    private static readonly string[] NamesRu = { "ясно", "дождь", "снег", "гроза" };

    public static MahaonWeather Current { get; private set; } = MahaonWeather.Clear;

    public static event Action<MahaonWeather> OnWeatherChanged;

    // The vanilla client resets its own local weather state (world.Weather.Reset()) any
    // time the server denies a move (routine — happens on any bit of lag/desync), with
    // nothing forcing a resync afterward. Left alone, a single DenyWalk during a storm
    // would silently kill the rain/snow visuals client-side until a GM happens to toggle
    // weather again — not just cosmetic for the glint layer, real rain/snow stops
    // rendering too. Resending periodically is a safe, cheap fix: the client's own
    // Generate() treats "same type sent again" as just extending the existing effect, not
    // a reset, so this doesn't cause any flicker on its own.
    private static readonly TimeSpan ResyncInterval = TimeSpan.FromSeconds(25);
    private static Timer _resyncTimer;

    public static void Configure()
    {
        _instance = new WeatherSystem();
        CommandSystem.Register("LemWeather", AccessLevel.GameMaster, LemWeather_OnCommand);
        _resyncTimer = Timer.DelayCall(ResyncInterval, ResyncInterval, ResyncAll);
    }

    private static void ResyncAll()
    {
        foreach (var ns in NetState.Instances)
        {
            SendTo(ns);
        }
    }

    [Usage("LemWeather")]
    [Description("GM tool: opens the season/weather control gump.")]
    private static void LemWeather_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendGump(new LemWeatherGump());
    }

    public static void SetWeather(MahaonWeather weather)
    {
        Current = weather;

        foreach (var ns in NetState.Instances)
        {
            SendTo(ns);
        }

        OnWeatherChanged?.Invoke(weather);
    }

    /// <summary>Sends the current weather to one NetState — call this on login too, same
    /// idea as SeasonSystem.TellCurrentSeason, so a player who logs in mid-storm actually
    /// sees it instead of a default clear sky until the next GM toggle.</summary>
    public static void SendTo(NetState ns)
    {
        var (type, density, temp) = Current switch
        {
            MahaonWeather.Rain  => (WtRain, (byte)70, (byte)0),
            MahaonWeather.Snow  => (WtSnow, (byte)70, (byte)0x1E),
            MahaonWeather.Storm => (WtStorm, (byte)90, (byte)0),
            _                   => (WtOff, (byte)0, (byte)0)
        };

        ns.SendWeather(type, density, temp);
    }

    public static string NameRu(MahaonWeather weather) => NamesRu[(int)weather];

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt((int)Current);
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version
        Current = (MahaonWeather)reader.ReadEncodedInt();
    }
}
