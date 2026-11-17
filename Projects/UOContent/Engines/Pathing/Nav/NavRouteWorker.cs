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
        public NavRouteTicket Ticket;
        public NavSnapshot Snapshot;
        public long StartNode;
        public long GoalNode;
        public Point3D Goal;
        public NavAccess Access;
        public Action<NavRoute> OnDone;
    }

    private static readonly ConcurrentQueue<Job> _queue = [];
    private static readonly AutoResetEvent _work = new(false);

    // Несколько потоков поиска, у каждого свой NavSearch: снимок графа неизменяем, делить нечего.
    // Один поток не успевал за тысячей ботов: очередь доходила до предела, и маршруты начинали
    // искаться прямо в главном потоке — по секунде на маршрут.
    private static Thread[] _threads;
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
            var count = Math.Clamp(
                ServerConfiguration.GetOrUpdateSetting("pathfinding.navWorkers", Math.Max(1, Environment.ProcessorCount - 2)),
                1,
                Environment.ProcessorCount
            );
            _threads = new Thread[count];
            for (var i = 0; i < count; i++)
            {
                var search = new NavSearch();
                _threads[i] = new Thread(() => Execute(search)) { IsBackground = true, Name = $"Nav Route Worker {i + 1}" };
                _threads[i].Start();
            }
        }
    }

    internal static bool TryEnqueue(
        NavSnapshot snapshot, long startNode, long goalNode, Point3D goal, NavAccess access, Action<NavRoute> onDone,
        NavRouteTicket ticket = null
    )
    {
        if (!Enabled || Volatile.Read(ref _pending) >= MaxPending)
        {
            return false;
        }

        Interlocked.Increment(ref _pending);
        _queue.Enqueue(
            new Job
            {
                Ticket = ticket, Snapshot = snapshot, StartNode = startNode, GoalNode = goalNode, Goal = goal, Access = access,
                OnDone = onDone
            }
        );
        _work.Set();
        return true;
    }

    private static bool CanRunNow() => World.WorldState is WorldState.Running or WorldState.WritingSave;

    public static int Workers => _threads?.Length ?? 0;

    private static void Execute(NavSearch search)
    {
        while (true)
        {
            if (_queue.IsEmpty)
            {
                // Ждём с тайм-аутом: событие одно на всех, и сигнал может достаться соседу.
                _work.WaitOne(100);
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

            // Ходок уже передумал (новая цель, новый маршрут, остановка): искать незачем.
            if (job.Ticket?.Cancelled == true)
            {
                continue;
            }

            if (!_queue.IsEmpty)
            {
                _work.Set();
            }

            NavSearchResult result;
            var t0 = Stopwatch.GetTimestamp();

            try
            {
                search.MaxExpansions = MaxExpansions;
                result = search.Run(job.Snapshot, job.StartNode, job.GoalNode, job.Goal, job.Access);
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
        if (route == null)
        {
            NavPathfinder.RememberFailure(job.StartNode, job.GoalNode);
        }
        job.OnDone(route);
    }
}

/// <summary>Запрос маршрута, который ходок может отозвать, пока тот ждёт в очереди.</summary>
public sealed class NavRouteTicket
{
    private volatile bool _cancelled;

    public bool Cancelled => _cancelled;

    public void Cancel() => _cancelled = true;
}
