using System;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Logging;

namespace Server.Systems.Bots;

/// <summary>
/// Wakes each bot's brain when it asked to be woken — never "every bot, every tick". A single
/// fast timer drains whatever is due, within a time budget per tick; whatever doesn't fit waits
/// for the next tick, so a burst of bots all wanting to think at once can't stall the loop.
/// </summary>
public static class BotScheduler
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotScheduler));

    private static readonly PriorityQueue<(BotBrain brain, int stamp), long> _queue = new();
    private static Timer _timer;

    // Priorities are offsets from this anchor, so they compare by plain subtraction from a real
    // tick and never depend on the tick counter's absolute value.
    private static long _anchor;

    public static double BudgetMs { get; set; } = 4.0;

    // ---- Stats for [BotStats -----------------------------------------------------------------
    public static long Thinks { get; private set; }
    public static double ThinkMsTotal { get; private set; }
    public static double ThinkMsMax { get; private set; }
    public static long BudgetOverruns { get; private set; }
    public static int QueueLength => _queue.Count;

    public static void ResetStats()
    {
        Thinks = 0;
        ThinkMsTotal = 0;
        ThinkMsMax = 0;
        BudgetOverruns = 0;
    }

    public static void Start()
    {
        _anchor = Core.TickCount;
        _timer ??= Timer.DelayCall(TimeSpan.FromMilliseconds(25), TimeSpan.FromMilliseconds(25), Drain);
    }

    public static void Schedule(BotBrain brain, int delayMs)
    {
        brain.DueTick = Core.TickCount + delayMs;
        brain.ScheduleStamp++;
        _queue.Enqueue((brain, brain.ScheduleStamp), brain.DueTick - _anchor);
    }

    private static void Drain()
    {
        var start = Stopwatch.GetTimestamp();
        var budgetTicks = (long)(BudgetMs * Stopwatch.Frequency / 1000.0);
        var now = Core.TickCount - _anchor;

        while (_queue.TryPeek(out var entry, out var due) && due - now <= 0)
        {
            if (Stopwatch.GetTimestamp() - start > budgetTicks)
            {
                BudgetOverruns++;
                return;
            }

            _queue.Dequeue();

            var (brain, stamp) = entry;
            if (!brain.Registered || stamp != brain.ScheduleStamp)
            {
                continue;
            }

            var t0 = Stopwatch.GetTimestamp();
            int delay;

            try
            {
                delay = brain.Think();
            }
            catch (Exception e)
            {
                // One broken brain must not take the scheduler down with it.
                logger.Error(e, "Bot {Bot} threw while thinking; its plan was dropped", brain.Bot);
                brain.ClearPlan();
                delay = 5000;
            }

            var ms = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;

            Thinks++;
            ThinkMsTotal += ms;
            if (ms > ThinkMsMax)
            {
                ThinkMsMax = ms;
            }

            if (delay < 0)
            {
                BotSystem.Unregister(brain.Bot);
            }
            else if (brain.Registered)
            {
                Schedule(brain, delay);
            }
        }
    }
}
