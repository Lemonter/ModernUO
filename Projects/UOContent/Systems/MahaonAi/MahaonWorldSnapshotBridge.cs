using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Server.Logging;
using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonBots;
using Server.Systems.MahaonCities;
using Server.Systems.MahaonRaids;
using Server.Systems.MahaonSeasons;
using Server.Systems.MahaonWeather;

namespace Server.Systems.MahaonAi;

/// <summary>
///     Pushes a periodic snapshot of world *state* to the website — city control, season,
///     weather, population, raids. Sits next to <see cref="MahaonForumBridge"/> and works
///     the same way on purpose: same one-way game → site direction, same "disabled cleanly
///     if unconfigured" shape, same fire-and-forget send.
///
///     The split between the two is events vs. state. The forum bridge posts when
///     something happens and the site keeps it forever; this posts what the world looks
///     like right now and the site keeps only the latest. Which is why this one is a
///     snapshot on a timer rather than a stream of deltas — nothing here needs history,
///     and a missed push costs nothing but freshness.
///
///     Nothing the website says back ever reaches the game.
/// </summary>
public static class MahaonWorldSnapshotBridge
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(MahaonWorldSnapshotBridge));
    private static HttpClient _httpClient;
    private static string _postUrl;
    private static TimeSpan _interval;

    // Guard posts sit up to ~22 tiles from a city center and patrol up to 20 further out
    // (CityControlSystem.GuardPatrolRadius), so this covers a city's whole guard ring with
    // room to spare. A spatial query rather than a World.Mobiles sweep — rule #4.
    private const int GuardScanRadius = 48;

    // Bumped when the payload shape changes in a way the site has to care about. The site
    // renders whatever it gets, so this is for diagnosing a stale pairing, not gating.
    private const int SchemaVersion = 1;

    // Long enough that the world is fully loaded and settled, short enough that the site
    // isn't showing a blank map for the first full interval after a restart.
    private static readonly TimeSpan FirstPushDelay = TimeSpan.FromSeconds(30);

    public static void Configure()
    {
        // Empty by default, same convention as MahaonForumBridge. Example value:
        // "http://localhost:3000/mahaon/api/world/snapshot".
        _postUrl = ServerConfiguration.GetOrUpdateSetting("mahaonSnapshot.postUrl", "");
        _interval = ServerConfiguration.GetOrUpdateSetting("mahaonSnapshot.interval", TimeSpan.FromMinutes(20));

        if (IsEnabled)
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _logger.Information($"MahaonWorldSnapshotBridge enabled — posting to {_postUrl} every {_interval}.");
        }
        else
        {
            _logger.Information("MahaonWorldSnapshotBridge disabled (no mahaonSnapshot.postUrl configured).");
        }
    }

    public static void Initialize()
    {
        if (!IsEnabled)
        {
            return;
        }

        Timer.DelayCall(FirstPushDelay, Push);
        Timer.DelayCall(_interval, _interval, Push);
    }

    public static bool IsEnabled => !string.IsNullOrEmpty(_postUrl);

    /// <summary>
    ///     Pushes a snapshot immediately, off-cycle. For the handful of events that would
    ///     otherwise leave the site visibly wrong for up to a full interval — a city
    ///     changing hands, mainly. Cheap enough that a burst of captures doesn't matter.
    /// </summary>
    public static void PushNow()
    {
        if (IsEnabled)
        {
            Push();
        }
    }

    private static void Push()
    {
        // Runs on the game loop (Timer callback), which is the only safe place to read
        // World/Map/guild state. BuildSnapshot copies everything into a DTO of primitives
        // and strings, so nothing that touches game state ever crosses to the send.
        var snapshot = BuildSnapshot();
        _ = SendAsync(snapshot); // fire and forget — errors just get logged
    }

    private static WorldSnapshot BuildSnapshot()
    {
        var cities = new List<CitySnapshot>(CityControlSystem.Cities.Count);

        foreach (var (city, info) in CityControlSystem.Cities)
        {
            var controller = CityControlSystem.GetController(city);

            cities.Add(
                new CitySnapshot
                {
                    Name = city,
                    Map = info.map?.Name,
                    Guild = controller?.Name,
                    GuildAbbreviation = controller?.Abbreviation,
                    TaxRate = CityControlSystem.GetTaxRate(city),
                    Guards = CountLivingGuards(city, info.spawn, info.map)
                }
            );
        }

        var season = SeasonSystem.CurrentSeason;
        var weather = WeatherSystem.Current;

        return new WorldSnapshot
        {
            SchemaVersion = SchemaVersion,
            CapturedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UptimeSeconds = Core.Uptime / 1000,
            Season = season.ToString(),
            SeasonRu = SeasonSystem.NameRu(season),
            SeasonLengthSeconds = (long)SeasonSystem.SeasonLength.TotalSeconds,
            Weather = weather.ToString(),
            WeatherRu = WeatherSystem.NameRu(weather),
            WindDirection = WindManager.Direction,
            WindStrength = WindManager.Strength,
            WindGustStrength = WindManager.GustStrength,
            PlayersOnline = CountOnlinePlayers(),
            Bots = Systems.Bots.BotSystem.Count,
            ActiveRaiders = RaidEventSystem.GetActiveRaiderCount(),
            SecondsUntilNextRaid = (long)RaidEventSystem.GetTimeUntilNextRaid().TotalSeconds,
            Cities = cities
        };
    }

    private static int CountLivingGuards(string city, Point3D center, Map map)
    {
        if (map == null || map == Map.Internal)
        {
            return 0;
        }

        var count = 0;

        foreach (var guard in map.GetMobilesInRange<CityGuard>(center, GuardScanRadius))
        {
            if (!guard.Deleted && guard.Alive && guard.City == city)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    ///     Real people, not bots: bots are PlayerMobiles too, but they hold no NetState, so
    ///     walking connections rather than the world counts exactly the humans logged in.
    /// </summary>
    private static int CountOnlinePlayers()
    {
        var count = 0;

        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile is PlayerMobile { Deleted: false })
            {
                count++;
            }
        }

        return count;
    }

    private static async Task SendAsync(WorldSnapshot snapshot)
    {
        try
        {
            // ConfigureAwait(false): the game loop installs itself as the ambient
            // SynchronizationContext (Main.cs), so a bare await would drag this
            // continuation back onto the main thread for work that never touches game
            // state. Same reasoning as MahaonForumBridge.
            var response = await _httpClient.PostAsJsonAsync(_postUrl, snapshot).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning($"MahaonWorldSnapshotBridge: site returned {(int)response.StatusCode}.");
            }
        }
        catch (Exception ex)
        {
            // Website down/unreachable — never let this affect the game, just log and move
            // on. The next tick will carry the current state anyway.
            _logger.Warning($"MahaonWorldSnapshotBridge: failed to push snapshot — {ex.Message}");
        }
    }

    private sealed class WorldSnapshot
    {
        [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; }
        [JsonPropertyName("capturedAt")] public long CapturedAt { get; set; }
        [JsonPropertyName("uptimeSeconds")] public long UptimeSeconds { get; set; }
        [JsonPropertyName("season")] public string Season { get; set; }
        [JsonPropertyName("seasonRu")] public string SeasonRu { get; set; }
        [JsonPropertyName("seasonLengthSeconds")] public long SeasonLengthSeconds { get; set; }
        [JsonPropertyName("weather")] public string Weather { get; set; }
        [JsonPropertyName("weatherRu")] public string WeatherRu { get; set; }
        [JsonPropertyName("windDirection")] public float WindDirection { get; set; }
        [JsonPropertyName("windStrength")] public float WindStrength { get; set; }
        [JsonPropertyName("windGustStrength")] public float WindGustStrength { get; set; }
        [JsonPropertyName("playersOnline")] public int PlayersOnline { get; set; }
        [JsonPropertyName("bots")] public int Bots { get; set; }
        [JsonPropertyName("activeRaiders")] public int ActiveRaiders { get; set; }
        [JsonPropertyName("secondsUntilNextRaid")] public long SecondsUntilNextRaid { get; set; }
        [JsonPropertyName("cities")] public List<CitySnapshot> Cities { get; set; }
    }

    private sealed class CitySnapshot
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("map")] public string Map { get; set; }
        [JsonPropertyName("guild")] public string Guild { get; set; }
        [JsonPropertyName("guildAbbreviation")] public string GuildAbbreviation { get; set; }
        [JsonPropertyName("taxRate")] public int TaxRate { get; set; }
        [JsonPropertyName("guards")] public int Guards { get; set; }
    }
}
