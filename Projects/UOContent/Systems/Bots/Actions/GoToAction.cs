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
    private readonly Point3D _point;
    private readonly Mobile _mobile;
    private readonly int _range;
    private readonly string _label;

    private NavFollower _follower;
    private Point3D _aimedAt;
    private int _replans;

    public GoToAction(Map map, Point3D point, int range, string label)
    {
        _map = map;
        _point = point;
        _range = range;
        _label = label;
    }

    public GoToAction(Mobile mobile, int range, string label)
    {
        _mobile = mobile;
        _range = range;
        _label = label;
    }

    private Map TargetMap => _mobile?.Map ?? _map;
    private Point3D TargetPoint => _mobile?.Location ?? _point;

    public override void Start(BotBrain brain) => Route(brain);

    private bool Route(BotBrain brain)
    {
        var bot = brain.Bot;
        _aimedAt = TargetPoint;

        var route = NavPathfinder.Find(bot.Map, bot.Location, TargetMap, _aimedAt);
        _follower = route == null ? null : new NavFollower(bot, route, _range);
        return _follower != null;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;

        if (_mobile != null && (_mobile.Deleted || _mobile.Map == null || _mobile.Map == Map.Internal))
        {
            return BotActionResult.Failed();
        }

        if (bot.Map == TargetMap && Utility.InRange(bot.Location, TargetPoint, _range))
        {
            return BotActionResult.Done();
        }

        if (_follower == null || _mobile != null && !Utility.InRange(_aimedAt, _mobile.Location, RetargetDistance))
        {
            if (!Route(brain))
            {
                return ++_replans > MaxReplans ? BotActionResult.Failed() : BotActionResult.Running(1000);
            }
        }

        var run = bot.Map != TargetMap || !Utility.InRange(bot.Location, TargetPoint, RunDistance);

        switch (_follower.Step(run))
        {
            case NavStepResult.Moved:
                {
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
                    return ++_replans > MaxReplans ? BotActionResult.Failed() : BotActionResult.Running(500);
                }
        }
    }

    public override string Describe(BotBrain brain)
    {
        var where = _mobile != null ? _mobile.Name : _point.ToString();
        var progress = _follower == null ? "" : $" [{_follower.Index}/{_follower.Route.Waypoints.Count}]";
        return $"Идёт: {_label} ({where}){progress}";
    }
}
