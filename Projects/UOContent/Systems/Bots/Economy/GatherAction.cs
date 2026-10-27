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

        // Дошагать до следующего узла, а не заканчивать работу.
        //
        // Здесь и была та самая картина: «срубили одно дерево и оба стоят в лесу среди
        // деревьев». ResourceProbe смотрит на два тайла — это «до чего дотянусь», — и стоило
        // соседнему дереву кончиться, как действие завершалось. Дальше цель перепланировалась,
        // атлас возвращал ту же точку (её только что пометили удачной), бот приходил на неё
        // снова, снова ничего не находил, и так по кругу. Лес в десяти шагах его не касался.
        //
        // Теперь, не найдя ничего вблизи, он ищет в радиусе RelocateRange и идёт туда. Работа
        // кончается только когда во всём этом радиусе живых узлов нет — тогда уходить и правда
        // пора, и цель отправит его в другое место.
        if (!ResourceProbe.TryFind(bot.Map, bot.Location, _kind, out var target, out var graphic))
        {
            return Relocate(brain);
        }

        // Дошли до намеченного узла — переход больше не нужен.
        _walk?.Stop(brain);
        _walk = null;

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

    /// <summary>Насколько далеко бот готов отойти за следующим узлом, не бросая работу.</summary>
    private const int RelocateRange = 12;

    /// <summary>Сколько переходов подряд без единого узла значит, что место выработано.</summary>
    private const int MaxRelocates = 6;

    private GoToAction _walk;
    private int _relocates;

    /// <summary>
    ///     Шаги к ближайшему живому узлу в радиусе RelocateRange.
    ///
    ///     Счётчик переходов нужен, чтобы бот не ходил по выработанной роще вечно: узел может
    ///     оказаться недостижимым (за водой, в скале), и тогда переход завершится неудачей, а
    ///     поиск снова укажет на него же. Шесть попыток — и уходим насовсем.
    /// </summary>
    private BotActionResult Relocate(BotBrain brain)
    {
        var bot = brain.Bot;

        if (_walk != null)
        {
            var step = _walk.Tick(brain);

            if (step.Status == BotActionStatus.Running)
            {
                return BotActionResult.Running(step.DelayMs);
            }

            _walk.Stop(brain);
            _walk = null;

            // Пришёл — следующий тик найдёт узел вблизи и ударит. Не дошёл — считаем попытку.
            if (step.Status == BotActionStatus.Failed && ++_relocates >= MaxRelocates)
            {
                return Gathered > 0 ? BotActionResult.Done() : BotActionResult.Failed();
            }

            return BotActionResult.Running(400);
        }

        if (_relocates >= MaxRelocates ||
            !ResourceProbe.TryFind(bot.Map, bot.Location, _kind, RelocateRange, out var far, out _))
        {
            return Gathered > 0 ? BotActionResult.Done() : BotActionResult.Failed();
        }

        _relocates++;

        // Подходим на расстояние вытянутой руки, а не в саму клетку: дерево и скала
        // непроходимы, встать в них нельзя.
        _walk = new GoToAction(bot.Map, far, ResourceProbe.Reach, "к следующему узлу");
        _walk.Start(brain);

        return BotActionResult.Running(400);
    }

    private static bool IsOverloaded(Mobile bot) => Mobile.BodyWeight + bot.TotalWeight >= bot.MaxWeight - 10;

    public override void Stop(BotBrain brain)
    {
        _walk?.Stop(brain);
        _walk = null;
        brain.Bot.EndAction(SwingLock(_kind));
    }

    public override string Describe(BotBrain brain) => _kind switch
    {
        ResourceKind.Ore  => $"Копает руду: {Gathered}/{_quota}",
        ResourceKind.Wood => $"Рубит лес: {Gathered}/{_quota}",
        _                 => $"Рыбачит: {Gathered}/{_quota}"
    };
}
