using System;
using Server.Network;

namespace Server.Systems.MahaonSeasons;

public enum MahaonSeason
{
    Spring,
    Summer,
    Autumn,
    Winter
}

/// <summary>
///     A real, ticking season cycle — not tied to the real-world calendar. Length is
///     adjustable (currently 30 minutes per season; bump to 1 hour once that's
///     confirmed working). Broadcasts a message to everyone online when the season
///     changes, and tells a player the current season on login.
/// </summary>
public static class SeasonSystem
{
    // 30 minutes per season for now, while testing tree growth — change to
    // TimeSpan.FromHours(1) once the fast cycle's confirmed working end to end.
    public static readonly TimeSpan SeasonLength = TimeSpan.FromMinutes(30);

    private static readonly string[] SeasonNamesRu = { "весна", "лето", "осень", "зима" };

    public static MahaonSeason CurrentSeason { get; private set; } = MahaonSeason.Spring;

    public static event Action<MahaonSeason> OnSeasonChanged;

    public static void Configure()
    {
        SyncNativeMapSeason();
        Timer.DelayCall(SeasonLength, SeasonLength, AdvanceSeason);
    }

    /// <summary>Directly sets the season (a GM gump click, not the natural cycle) — same
    /// broadcast/sync effects as a real tick, just not waiting for the timer.</summary>
    public static void SetSeason(MahaonSeason season)
    {
        CurrentSeason = season;

        var message = $"Наступил новый сезон: {SeasonNamesRu[(int)CurrentSeason]}.";

        foreach (var ns in NetState.Instances)
        {
            ns.Mobile?.SendMessage(0x59, message);
        }

        SyncNativeMapSeason();
        OnSeasonChanged?.Invoke(CurrentSeason);
    }

    private static void AdvanceSeason()
    {
        SetSeason((MahaonSeason)(((int)CurrentSeason + 1) % 4));
    }

    /// <summary>
    ///     The engine has its own, completely separate per-map Season value — sent to the
    ///     client as its own packet, and used by the client to decide how a lot of terrain
    ///     (including foliage) actually renders. Our season system never touched it before,
    ///     which meant every map just sat on whatever it defaulted to — keep it in sync with
    ///     ours instead, and push a live refresh to everyone currently online.
    /// </summary>
    private static void SyncNativeMapSeason()
    {
        // Real UO season values: 0=Spring, 1=Summer, 2=Fall, 3=Winter, 4=Desolation.
        var nativeSeason = CurrentSeason switch
        {
            MahaonSeason.Spring => 0,
            MahaonSeason.Summer => 1,
            MahaonSeason.Autumn => 2,
            _                   => 3
        };

        foreach (var map in Map.Maps)
        {
            if (map != null && map != Map.Internal)
            {
                map.Season = nativeSeason;
            }
        }

        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile != null)
            {
                ns.SendSeasonChange((byte)nativeSeason, true);
            }
        }
    }

    public static void TellCurrentSeason(Mobile m)
    {
        m.SendMessage(0x59, $"Сейчас сезон: {SeasonNamesRu[(int)CurrentSeason]}.");
    }

    public static string NameRu(MahaonSeason season) => SeasonNamesRu[(int)season];
}
