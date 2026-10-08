using System;
using Server.Mobiles;
using Server.Items;
using Server.Systems.MahaonMetals;
using Server.Systems.MahaonMining;

namespace Server.Systems.Bots;

/// <summary>
/// Works a gathering spot through the same swing chains a player's click starts: one call
/// swings until the vein, tree or water runs dry, then the bot looks for the next target within
/// reach. Ends when the quota is met, the pack is heavy, the time is up, or nothing within reach
/// is left — the goal then sends it elsewhere.
/// </summary>
public sealed class GatherAction : BotAction
{
    private const long MaxDurationMs = 20 * 60_000;

    // A started chain that yields nothing this many times running means the spot is no good
    // for this bot (skill too low, target not actually workable).
    private const int MaxBarrenChains = 4;

    private readonly ResourceKind _kind;
    private readonly int _quota;

    private long _deadline;
    private int _startCount;
    private int _lastCount;
    private int _barrenChains;
    private bool _chainRunning;

    public GatherAction(ResourceKind kind, int quota)
    {
        _kind = kind;
        _quota = quota;
    }

    public int Gathered { get; private set; }

    public static Type ProductType(ResourceKind kind) => kind switch
    {
        ResourceKind.Ore  => typeof(MahaonOre),
        ResourceKind.Wood => typeof(Log),
        _                 => typeof(Fish)
    };

    private static Type SwingLock(ResourceKind kind) => kind switch
    {
        ResourceKind.Ore  => typeof(MahaonMiningSwings),
        ResourceKind.Wood => typeof(MahaonLumberjackingSwings),
        _                 => typeof(MahaonFishingSwings)
    };

    public static Item FindTool(Mobile bot, ResourceKind kind)
    {
        var pack = bot.Backpack;
        return kind switch
        {
            ResourceKind.Ore  => pack?.FindItemByType<Pickaxe>() ?? (Item)pack?.FindItemByType<Shovel>(),
            ResourceKind.Wood => bot.FindItemOnLayer(Layer.TwoHanded) as BaseAxe ?? pack?.FindItemByType<BaseAxe>(),
            _                 => pack?.FindItemByType<FishingPole>()
        };
    }

    // The pack animal's load counts too: the bot hands goods over as it works.
    private int Count(PlayerMobile bot) =>
        (bot.Backpack?.GetAmount(ProductType(_kind)) ?? 0) + (BotStable.ReachablePack(bot, 12)?.GetAmount(ProductType(_kind)) ?? 0);

    public override void Start(BotBrain brain)
    {
        _deadline = Core.TickCount + MaxDurationMs;
        _startCount = _lastCount = Count(brain.Bot);
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var count = Count(bot);
        Gathered = Math.Max(0, count - _startCount);

        if (IsOverloaded(bot))
        {
            BotStable.TryOffload(bot);
        }

        if (Gathered >= _quota || Core.TickCount - _deadline >= 0 || IsOverloaded(bot))
        {
            return Gathered > 0 ? BotActionResult.Done() : BotActionResult.Failed();
        }

        if (!bot.CanBeginAction(SwingLock(_kind)))
        {
            _chainRunning = true;
            return BotActionResult.Running(2000);
        }

        // A chain just ended: did it yield anything?
        if (_chainRunning)
        {
            _chainRunning = false;
            _barrenChains = count > _lastCount ? 0 : _barrenChains + 1;
            _lastCount = count;

            if (_barrenChains >= MaxBarrenChains)
            {
                return Gathered > 0 ? BotActionResult.Done() : BotActionResult.Failed();
            }
        }

        if (!ResourceProbe.TryFind(bot.Map, bot.Location, _kind, out var target, out var graphic))
        {
            return Gathered > 0 ? BotActionResult.Done() : BotActionResult.Failed();
        }

        var tool = FindTool(bot, _kind);
        if (tool == null)
        {
            return BotActionResult.Failed();
        }

        bot.Direction = bot.GetDirectionTo(target);

        switch (_kind)
        {
            case ResourceKind.Ore:
                {
                    MahaonMiningSwings.StartSurfaceSwings(bot, target, bot.Map, tool, allowMineOffer: false);
                    break;
                }
            case ResourceKind.Wood:
                {
                    MahaonLumberjackingSwings.StartTreeSwings(bot, target, graphic, bot.Map, tool);
                    break;
                }
            default:
                {
                    MahaonFishingSwings.StartFishing(bot, target, bot.Map, tool);
                    break;
                }
        }

        // A chain that refused to start (vein still respawning) counts as barren too.
        if (bot.CanBeginAction(SwingLock(_kind)))
        {
            _chainRunning = true;
        }

        return BotActionResult.Running(1800);
    }

    private static bool IsOverloaded(Mobile bot) => Mobile.BodyWeight + bot.TotalWeight >= bot.MaxWeight - 10;

    public override void Stop(BotBrain brain) => brain.Bot.EndAction(SwingLock(_kind));

    public override string Describe(BotBrain brain) => _kind switch
    {
        ResourceKind.Ore  => $"Копает руду: {Gathered}/{_quota}",
        ResourceKind.Wood => $"Рубит лес: {Gathered}/{_quota}",
        _                 => $"Рыбачит: {Gathered}/{_quota}"
    };
}
