using System.Diagnostics;

namespace Server.Engines.Pathing.Nav;

/// <summary>
/// On-loop cost of path planning, for deciding with numbers whether any of it should leave the
/// loop (see dev-docs/threading-model.md, "prove the need"). Graph routes are pure reads of baked
/// data and the only candidate for a worker; leg searches read live items and mobiles and stay.
/// </summary>
public static class NavStats
{
    public static long Routes { get; private set; }
    public static double RouteMsTotal { get; private set; }
    public static double RouteMsMax { get; private set; }
    public static long RouteExpanded { get; private set; }
    public static long RoutesFailed { get; private set; }

    // Searches run on the route worker: worker time is off the loop; only dispatch and the
    // hand-back conversion are paid on it.
    public static long WorkerRoutes { get; private set; }
    public static double WorkerMsTotal { get; private set; }
    public static double WorkerMsMax { get; private set; }
    public static double DispatchMsTotal { get; private set; }

    public static long Legs { get; private set; }
    public static double LegMsTotal { get; private set; }
    public static double LegMsMax { get; private set; }
    public static long LegsFailed { get; private set; }

    public static double ElapsedMs(long startTimestamp) =>
        (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;

    internal static void RecordRoute(double ms, int expanded, bool found)
    {
        Routes++;
        RouteMsTotal += ms;
        RouteExpanded += expanded;
        if (ms > RouteMsMax)
        {
            RouteMsMax = ms;
        }

        if (!found)
        {
            RoutesFailed++;
        }
    }

    internal static void RecordDispatch(double loopMs) => DispatchMsTotal += loopMs;

    internal static void RecordWorkerRoute(double workerMs, double loopMs, int expanded, bool found)
    {
        WorkerRoutes++;
        WorkerMsTotal += workerMs;
        DispatchMsTotal += loopMs;
        RouteExpanded += expanded;
        if (workerMs > WorkerMsMax)
        {
            WorkerMsMax = workerMs;
        }

        if (!found)
        {
            RoutesFailed++;
        }
    }

    internal static void RecordLeg(double ms, bool found)
    {
        Legs++;
        LegMsTotal += ms;
        if (ms > LegMsMax)
        {
            LegMsMax = ms;
        }

        if (!found)
        {
            LegsFailed++;
        }
    }

    public static void Reset()
    {
        Routes = 0;
        RouteMsTotal = 0;
        RouteMsMax = 0;
        RouteExpanded = 0;
        RoutesFailed = 0;
        WorkerRoutes = 0;
        WorkerMsTotal = 0;
        WorkerMsMax = 0;
        DispatchMsTotal = 0;
        Legs = 0;
        LegMsTotal = 0;
        LegMsMax = 0;
        LegsFailed = 0;
    }
}
