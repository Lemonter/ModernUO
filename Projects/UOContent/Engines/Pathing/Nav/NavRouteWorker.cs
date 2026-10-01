using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Server.Logging;

namespace Server.Engines.Pathing.Nav;

/// <summary>
/// Runs nav-graph route searches off the game loop. A search reads only a frozen
/// <see cref="NavSnapshot"/> — baked graph arrays and immutable links — and the loop resolves start
/// and goal regions before dispatch and turns the result into maps after, so no game state is read
/// or written on this thread.
///
/// One worker, FIFO, parked on a kernel wait while idle; it stands aside during world saves.
/// Results come back through <see cref="Core.LoopContext"/>.
/// </summary>
public static class NavRouteWorker
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(NavRouteWorker));

    /// <summary>
    /// Bounded by the bots upstream — each walker waits on at most one route — so this is a
    /// backstop; when full, the caller searches inline rather than lose the route.
    /// </summary>
    private const int MaxPending = 2048;

    // Nothing signals the worker when a save freeze ends, so it re-checks on this interval -- but
    // only while a save is in progress, never in steady state.
    private const int SaveGatePollMs = 50;

    private sealed class Job
    {
        public NavSnapshot Snapshot;
        public long StartNode;
        public long GoalNode;
        public Point3D Goal;
        public Action<NavRoute> OnDone;
    }

    private static readonly ConcurrentQueue<Job> _queue = [];
    private static readonly AutoResetEvent _work = new(false);
    private static readonly NavSearch _search = new();

    private static Thread _thread;
    private static int _pending;

    /// <summary>Off a host without a spare core to move the work to (see threading-model.md).</summary>
    public static bool Enabled { get; private set; }

    public static int MaxExpansions { get; set; } = 200_000;

    public static int Pending => Volatile.Read(ref _pending);

    public static void Configure()
    {
        Enabled = Environment.ProcessorCount >= 4 && ServerConfiguration.GetOrUpdateSetting("pathfinding.navWorker", true);

        if (Enabled)
        {
            _thread = new Thread(Execute) { IsBackground = true, Name = "Nav Route Worker" };
            _thread.Start();
        }
    }

    internal static bool TryEnqueue(NavSnapshot snapshot, long startNode, long goalNode, Point3D goal, Action<NavRoute> onDone)
    {
        if (!Enabled || Volatile.Read(ref _pending) >= MaxPending)
        {
            return false;
        }

        Interlocked.Increment(ref _pending);
        _queue.Enqueue(new Job { Snapshot = snapshot, StartNode = startNode, GoalNode = goalNode, Goal = goal, OnDone = onDone });
        _work.Set();
        return true;
    }

    private static bool CanRunNow() => World.WorldState is WorldState.Running or WorldState.WritingSave;

    private static void Execute()
    {
        while (true)
        {
            if (_queue.IsEmpty)
            {
                _work.WaitOne();
                continue;
            }

            if (!CanRunNow())
            {
                _work.WaitOne(SaveGatePollMs);
                continue;
            }

            if (!_queue.TryDequeue(out var job))
            {
                continue;
            }

            Interlocked.Decrement(ref _pending);

            NavSearchResult result;
            var t0 = Stopwatch.GetTimestamp();

            try
            {
                _search.MaxExpansions = MaxExpansions;
                result = _search.Run(job.Snapshot, job.StartNode, job.GoalNode, job.Goal);
            }
            catch (Exception e)
            {
                // A result must still come back, or the walker waits on it forever.
                logger.Error(e, "Nav route search failed");
                result = null;
            }

            var ms = NavStats.ElapsedMs(t0);
            Core.LoopContext.Post(() => Complete(job, result, ms));
        }
    }

    private static void Complete(Job job, NavSearchResult result, double workerMs)
    {
        var t0 = Stopwatch.GetTimestamp();
        var route = NavPathfinder.ToRoute(result);
        NavStats.RecordWorkerRoute(workerMs, NavStats.ElapsedMs(t0), result?.Expanded ?? 0, route != null);
        job.OnDone(route);
    }
}
