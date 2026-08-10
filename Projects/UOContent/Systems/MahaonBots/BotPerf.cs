using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Gumps;

namespace Server.Systems.MahaonBots;

/// <summary>
///     Real, measured cost accounting for the bot system — wraps PollConveyor and
///     PollMovement with a Stopwatch and accumulates actual elapsed time, so "[BotPerf"
///     reports what your hardware is actually spending, not a guess read off the code.
/// </summary>
public static class BotPerf
{
    private static long _conveyorTicks;
    private static long _conveyorBotsTouched;
    private static long _conveyorCalls;

    private static long _movementTicks;
    private static long _movementBotsTouched;
    private static long _movementCalls;

    private static DateTime _windowStart = DateTime.UtcNow;

    // One live-refresh timer per mobile watching the gump — stopped automatically after a
    // while so this doesn't leak forever if someone just leaves it open.
    private static readonly Dictionary<Mobile, Timer> LiveTimers = new();
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan AutoStopAfter = TimeSpan.FromMinutes(15);

    public static void Configure()
    {
        CommandSystem.Register("BotPerf", AccessLevel.GameMaster, BotPerf_OnCommand);
        CommandSystem.Register("BotPerfReset", AccessLevel.GameMaster, BotPerfReset_OnCommand);
    }

    public static void RecordConveyor(long elapsedTicks, int botsTouched)
    {
        _conveyorTicks += elapsedTicks;
        _conveyorBotsTouched += botsTouched;
        _conveyorCalls++;
    }

    public static void RecordMovement(long elapsedTicks, int botsTouched)
    {
        _movementTicks += elapsedTicks;
        _movementBotsTouched += botsTouched;
        _movementCalls++;
    }

    public readonly struct Snapshot
    {
        public readonly int ActiveBots;
        public readonly double WindowSeconds;
        public readonly long ConveyorCalls;
        public readonly long ConveyorBotsTouched;
        public readonly double ConveyorUsPerBot;
        public readonly double ConveyorMsPerSec;
        public readonly long MovementCalls;
        public readonly long MovementBotsTouched;
        public readonly double MovementUsPerBot;
        public readonly double MovementMsPerSec;
        public readonly double TotalMsPerSec;
        public readonly double PercentOfOneCore;

        public Snapshot(
            int activeBots, double windowSeconds, long conveyorCalls, long conveyorBotsTouched,
            double conveyorUsPerBot, double conveyorMsPerSec, long movementCalls, long movementBotsTouched,
            double movementUsPerBot, double movementMsPerSec
        )
        {
            ActiveBots = activeBots;
            WindowSeconds = windowSeconds;
            ConveyorCalls = conveyorCalls;
            ConveyorBotsTouched = conveyorBotsTouched;
            ConveyorUsPerBot = conveyorUsPerBot;
            ConveyorMsPerSec = conveyorMsPerSec;
            MovementCalls = movementCalls;
            MovementBotsTouched = movementBotsTouched;
            MovementUsPerBot = movementUsPerBot;
            MovementMsPerSec = movementMsPerSec;
            TotalMsPerSec = conveyorMsPerSec + movementMsPerSec;
            PercentOfOneCore = TotalMsPerSec / 10.0; // 1000ms/sec = 100%, so ms/sec / 10 = %
        }
    }

    public static Snapshot GetSnapshot()
    {
        var windowSeconds = Math.Max(1.0, (DateTime.UtcNow - _windowStart).TotalSeconds);

        var conveyorMs = TicksToMs(_conveyorTicks);
        var movementMs = TicksToMs(_movementTicks);

        var conveyorUsPerBot = _conveyorBotsTouched > 0 ? conveyorMs * 1000.0 / _conveyorBotsTouched : 0;
        var movementUsPerBot = _movementBotsTouched > 0 ? movementMs * 1000.0 / _movementBotsTouched : 0;

        return new Snapshot(
            BotController.CountActive(), windowSeconds,
            _conveyorCalls, _conveyorBotsTouched, conveyorUsPerBot, conveyorMs / windowSeconds,
            _movementCalls, _movementBotsTouched, movementUsPerBot, movementMs / windowSeconds
        );
    }

    [Usage("BotPerf")]
    [Description("Opens a live bot-performance gump that refreshes every second.")]
    private static void BotPerf_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        StartLiveUpdates(from);
    }

    private static void StartLiveUpdates(Mobile from)
    {
        if (LiveTimers.TryGetValue(from, out var existing))
        {
            existing.Stop();
        }

        from.SendGump(new BotPerfGump());

        var stopAt = DateTime.UtcNow + AutoStopAfter;

        var timer = Timer.DelayCall(RefreshInterval, RefreshInterval, () =>
        {
            if (from.Deleted || from.NetState == null || DateTime.UtcNow >= stopAt)
            {
                StopLiveUpdates(from);
                return;
            }

            from.SendGump(new BotPerfGump());
        });

        LiveTimers[from] = timer;
    }

    private static void StopLiveUpdates(Mobile from)
    {
        if (LiveTimers.TryGetValue(from, out var timer))
        {
            timer.Stop();
            LiveTimers.Remove(from);
        }
    }

    [Usage("BotPerfReset")]
    [Description("Resets the BotPerf counters and starts a fresh measurement window.")]
    private static void BotPerfReset_OnCommand(CommandEventArgs e)
    {
        _conveyorTicks = 0;
        _conveyorBotsTouched = 0;
        _conveyorCalls = 0;
        _movementTicks = 0;
        _movementBotsTouched = 0;
        _movementCalls = 0;
        _windowStart = DateTime.UtcNow;

        e.Mobile.SendMessage(0x59, "Счётчики BotPerf сброшены.");
    }

    private static double TicksToMs(long ticks) => ticks / (double)TimeSpan.TicksPerMillisecond;
}
