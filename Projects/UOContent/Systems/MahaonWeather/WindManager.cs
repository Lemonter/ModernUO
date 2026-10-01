using System;
using Server.Commands;
using Server.Network;

namespace Server.Systems.MahaonWeather;

/// <summary>
///     Global wind state — server is the source of truth (direction/strength/gusts,
///     smoothly transitioned), client (ClassicUO WindManager) does all the actual visual
///     work locally from this. Server never sends per-object animation, just this state —
///     see the "Направление" convention below, matches the spec exactly: 0=east, 90=north,
///     180=west, 270=south.
/// </summary>
public sealed class WindManager : GenericPersistence
{
    private static WindManager _instance;

    public WindManager() : base("MahaonWind", 1)
    {
    }

    public const float MaxStrength = 2f;
    public const float MaxGust = 1f;

    public static float Direction { get; private set; }
    public static float Strength { get; private set; }
    public static float GustStrength { get; private set; }

    // How long the CURRENT transition (the one already broadcast) takes to finish, and
    // when it started — sent to clients so they can interpolate on their own; the server
    // doesn't need to tick anything itself except one timer to flip its own "current"
    // values once the transition's really over server-side too (e.g. so a later relative
    // change starts from the right place instead of the old pre-transition value).
    private static TimeSpan _transitionDuration = TimeSpan.Zero;
    private static DateTime _transitionStart = Core.Now;
    private static Timer _transitionTimer;
    private static Timer _resyncTimer;

    private static readonly TimeSpan ResyncInterval = TimeSpan.FromMinutes(5);

    public static void Configure()
    {
        _instance = new WindManager();
        CommandSystem.Register("Wind", AccessLevel.GameMaster, Wind_OnCommand);
        _resyncTimer = Timer.DelayCall(ResyncInterval, ResyncInterval, ResyncAll);
    }

    /// <summary>Instant change — same as TransitionTo with zero duration.</summary>
    public static void SetWind(float direction, float strength, float gustStrength) =>
        TransitionTo(direction, strength, gustStrength, TimeSpan.Zero);

    /// <summary>Smoothly moves toward the given state over `duration` — the server
    /// broadcasts the target + duration once, every client interpolates on its own from
    /// there (see the ТЗ: no per-frame packets).</summary>
    public static void TransitionTo(float direction, float strength, float gustStrength, TimeSpan duration)
    {
        Direction = Normalize360(direction);
        Strength = Math.Clamp(strength, 0f, MaxStrength);
        GustStrength = Math.Clamp(gustStrength, 0f, MaxGust);

        _transitionStart = Core.Now;
        _transitionDuration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;

        foreach (var ns in NetState.Instances)
        {
            SendTo(ns);
        }

        _transitionTimer?.Stop();
        _transitionTimer = _transitionDuration > TimeSpan.Zero
            ? Timer.DelayCall(_transitionDuration, () => { }) // just marks the transition as "settled" server-side; nothing else to do
            : null;
    }

    private static void ResyncAll()
    {
        // Same reasoning as WeatherSystem's resync — a client-side desync (e.g. after a
        // DenyWalk reset, or just a dropped packet) shouldn't leave someone stuck with
        // stale wind until the next real change. Sends the CURRENT state with zero
        // remaining transition time (it's already settled by now for any real-world gap).
        foreach (var ns in NetState.Instances)
        {
            ns.SendWindState(Direction, Strength, GustStrength, 0);
        }
    }

    /// <summary>Sends the current (or in-progress) wind state to one NetState — call this
    /// on login too, same idea as WeatherSystem.SendTo/SeasonSystem.TellCurrentSeason.</summary>
    public static void SendTo(NetState ns)
    {
        var elapsed = Core.Now - _transitionStart;
        var remaining = _transitionDuration - elapsed;
        var remainingMs = remaining > TimeSpan.Zero ? (uint)remaining.TotalMilliseconds : 0;

        ns.SendWindState(Direction, Strength, GustStrength, remainingMs);
    }

    private static float Normalize360(float degrees)
    {
        var d = degrees % 360f;
        return d < 0 ? d + 360f : d;
    }

    [Usage("Wind <direction 0-360> <strength 0-2> <gust 0-1> [durationSeconds]")]
    [Description("GM tool: sets global wind state, optionally transitioning smoothly over time.")]
    private static void Wind_OnCommand(CommandEventArgs e)
    {
        if (e.Length < 3)
        {
            e.Mobile.SendMessage("Использование: [Wind <направление 0-360> <сила 0-2> <порывы 0-1> [длительность сек]]");
            return;
        }

        var direction = (float)e.GetDouble(0);
        var strength = (float)e.GetDouble(1);
        var gust = (float)e.GetDouble(2);
        var durationSeconds = e.Length >= 4 ? e.GetDouble(3) : 0;

        TransitionTo(direction, strength, gust, TimeSpan.FromSeconds(durationSeconds));

        e.Mobile.SendMessage(
            0x59,
            $"Ветер: направление {direction:0}°, сила {strength:0.00}, порывы {gust:0.00}" +
            (durationSeconds > 0 ? $", переход {durationSeconds:0} сек." : ".")
        );
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.Write(Direction);
        writer.Write(Strength);
        writer.Write(GustStrength);
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version
        Direction = reader.ReadFloat();
        Strength = reader.ReadFloat();
        GustStrength = reader.ReadFloat();
    }
}
