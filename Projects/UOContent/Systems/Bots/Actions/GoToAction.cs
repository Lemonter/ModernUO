using Server.Engines.Pathing.Nav;

namespace Server.Systems.Bots;

/// <summary>
/// Walks to a place, or to a mobile that may itself be moving, over the nav graph. Runs when the
/// way is long and walks the last few tiles, the way a player does.
/// </summary>
public sealed class GoToAction : BotAction
{
    private const int MaxReplans = 3;
    private const int RunDistance = 6;

    // A followed mobile that drifts this far from where the route was aimed gets a new route.
    private const int RetargetDistance = 6;

    private readonly Map _map;
    private Point3D _point;
    private readonly Mobile _mobile;
    private readonly int _range;
    private readonly string _label;

    private NavFollower _follower;
    private Point3D _aimedAt;
    private int _replans;

    // Ближе всего к цели, куда бот уже доходил. Лимит перепланировок — на заминки подряд, а не
    // на весь путь: четыре толпы в воротах на дороге через континент не должны проваливать поход.
    private double _closest = double.MaxValue;
    private const int ProgressStride = 5;

    // Как в NavFollower: этажом выше или ниже — ещё не пришёл (мост, второй этаж банка, подземный ход).
    private const int ArrivalZ = 16;

    // Точка с радиусом 0 — место работы (у кузницы, у жилы), общее для всех. Пришедший вторым
    // не лезет в ту же клетку, а встаёт на свободную рядом: из _spots, если место знает, где ещё
    // можно стоять (кузница — в двух шагах и от печи, и от наковальни), иначе по соседству.
    private readonly Point3D _origin;
    private readonly Point3D[] _spots;
    private int _shifts;
    private const int SpreadLook = 3;
    private const int MaxShifts = 4;

    public GoToAction(Map map, Point3D point, int range, string label, Point3D[] spots = null)
    {
        _map = map;
        _point = point;
        _origin = point;
        _range = range;
        _label = label;
        _spots = spots;
    }

    public GoToAction(Mobile mobile, int range, string label)
    {
        _mobile = mobile;
        _range = range;
        _label = label;
    }

    internal Point3D Destination => TargetPoint;

    private Map TargetMap => _mobile?.Map ?? _map;
    private Point3D TargetPoint => _mobile?.Location ?? _point;

    // A route is being searched (maybe on the route worker); ticks wait for it.
    private bool _routing;

    // Запрос в очереди: при новом запросе или остановке старый отзывается, чтобы поток поиска не
    // тратил время на маршрут, который уже никому не нужен.
    private NavRouteTicket _ticket;

    // Очередь поиска была полна: спросить снова не раньше этого тика (в главном потоке не ищем).
    private long _retryAt;
    private bool _routeFailed;
    private int _routeRequest;
    private bool _stopped;

    public override void Start(BotBrain brain) => RequestRoute(brain);

    private void RequestRoute(BotBrain brain)
    {
        var bot = brain.Bot;
        _aimedAt = TargetPoint;
        _routing = true;
        _routeFailed = false;

        _ticket?.Cancel();
        var ticket = _ticket = new NavRouteTicket();
        var request = ++_routeRequest;
        var queued = NavPathfinder.FindAsync(
            bot.Map,
            bot.Location,
            TargetMap,
            _aimedAt,
            route =>
            {
                // A newer request, a stopped action or a gone bot makes this answer stale.
                if (_stopped || request != _routeRequest || bot.Deleted)
                {
                    return;
                }

                _routing = false;
                _follower = route == null ? null : new NavFollower(bot, route, _range);
                _routeFailed = route == null;
            },
            NavPathfinder.AccessOf(bot),
            ticket,
            inlineWhenBusy: false
        );

        if (!queued)
        {
            _routing = false;
            _retryAt = Core.TickCount + BusyRetryMs;
        }
    }

    private const int BusyRetryMs = 1000;

    public override void Stop(BotBrain brain)
    {
        _stopped = true;
        _ticket?.Cancel();
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;

        if (_mobile != null && (_mobile.Deleted || _mobile.Map == null || _mobile.Map == Map.Internal))
        {
            return BotActionResult.Failed();
        }

        if (_range == 0 && _mobile == null && ShiftIfTaken(bot))
        {
            return BotActionResult.Running(200);
        }

        if (bot.Map == TargetMap && Utility.InRange(bot.Location, TargetPoint, _range) &&
            System.Math.Abs(bot.Z - TargetPoint.Z) < ArrivalZ)
        {
            return BotActionResult.Done();
        }

        if (_routing)
        {
            return BotActionResult.Running(200);
        }

        // Очередь поиска была занята: ждём и спрашиваем снова, не считая это неудачей.
        if (_retryAt != 0)
        {
            if (Core.TickCount - _retryAt < 0)
            {
                return BotActionResult.Running(BusyRetryMs);
            }

            _retryAt = 0;
            RequestRoute(brain);
            return BotActionResult.Running(200);
        }

        if (_routeFailed)
        {
            if (++_replans > MaxReplans)
            {
                return BotActionResult.Failed();
            }

            RequestRoute(brain);
            return BotActionResult.Running(1000);
        }

        if (_follower == null || _mobile != null && !Utility.InRange(_aimedAt, _mobile.Location, RetargetDistance))
        {
            RequestRoute(brain);
            return BotActionResult.Running(200);
        }

        var run = bot.Map != TargetMap || !Utility.InRange(bot.Location, TargetPoint, RunDistance);

        switch (_follower.Step(run))
        {
            case NavStepResult.Moved:
                {
                    NoteProgress(bot);
                    return BotActionResult.Running(BotMovement.StepDelay(bot, run));
                }
            case NavStepResult.Waiting:
                {
                    return BotActionResult.Running(250);
                }
            case NavStepResult.Arrived:
                {
                    return BotActionResult.Done();
                }
            default:
                {
                    _follower = null;
                    if (++_replans > MaxReplans)
                    {
                        return BotActionResult.Failed();
                    }

                    RequestRoute(brain);
                    return BotActionResult.Running(500);
                }
        }
    }

    /// <summary>Подойдя к занятой чужим клетке, перенацеливается на свободную рядом.</summary>
    private bool ShiftIfTaken(Mobile bot)
    {
        if (_shifts >= MaxShifts || bot.Map != _map || bot.Location == _point ||
            !Utility.InRange(bot.Location, _point, SpreadLook) || !IsTaken(_map, _point, bot))
        {
            return false;
        }

        if (FreeSpot(bot) is not { } free)
        {
            return false;
        }

        _shifts++;
        _point = free;
        _follower = null;
        return true;
    }

    internal static bool IsTaken(Map map, Point3D p, Mobile except)
    {
        foreach (var m in map.GetMobilesAt(p))
        {
            if (m != except && !m.Deleted && System.Math.Abs(m.Z - p.Z) < ArrivalZ)
            {
                return true;
            }
        }

        return false;
    }

    private Point3D? FreeSpot(Mobile bot)
    {
        Point3D? best = null;
        var bestDistance = double.MaxValue;

        if (_spots != null)
        {
            foreach (var p in _spots)
            {
                Consider(bot, p, ref best, ref bestDistance);
            }

            return best;
        }

        var graph = NavSystem.GetGraph(_map);
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                var p = new Point3D(_origin.X + dx, _origin.Y + dy, _origin.Z);
                if ((dx != 0 || dy != 0) && graph?.Locate(p.X, p.Y, p.Z, 0) >= 0 && _map.CanFit(p, 16, false, false))
                {
                    Consider(bot, p, ref best, ref bestDistance);
                }
            }
        }

        return best;
    }

    private void Consider(Mobile bot, Point3D p, ref Point3D? best, ref double bestDistance)
    {
        if (p == _point || IsTaken(_map, p, bot))
        {
            return;
        }

        var distance = bot.GetDistanceToSqrt(p);
        if (distance < bestDistance)
        {
            bestDistance = distance;
            best = p;
        }
    }

    private void NoteProgress(Mobile bot)
    {
        if (bot.Map != TargetMap)
        {
            return;
        }

        var distance = bot.GetDistanceToSqrt(TargetPoint);
        if (distance <= _closest - ProgressStride)
        {
            _closest = distance;
            _replans = 0;
        }
    }

    public override string Describe(BotBrain brain)
    {
        var where = _mobile != null ? _mobile.Name : _point.ToString();
        var progress = _follower == null ? "" : $" [{_follower.Index}/{_follower.Route.Waypoints.Count}]";
        return $"Идёт: {_label} ({where}){progress}";
    }
}
