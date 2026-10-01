using System.Collections.Generic;

namespace Server.Systems.Bots;

/// <summary>Earn a living from the land: mine, chop or fish near home. One instance per resource;
/// the bot's own skills decide which it prefers.</summary>
public sealed class GatherGoal : BotGoal
{
    private const int SearchRange = 160;

    private readonly ResourceKind _kind;

    public GatherGoal(ResourceKind kind) => _kind = kind;

    public override string Name => _kind switch
    {
        ResourceKind.Ore  => "Горное дело",
        ResourceKind.Wood => "Лесозаготовка",
        _                 => "Рыбалка"
    };

    public override string[] News => _kind switch
    {
        ResourceKind.Ore  => ["Весь день руду копал, спина отваливается.", "Нашёл неплохую жилу, между прочим.", "Кирка уже еле держится."],
        ResourceKind.Wood => ["Нарубил дров на целую зиму.", "В лесу сегодня тихо.", "Топор затупился, а деревья не кончаются."],
        _                 => ["Рыба сегодня клевала как бешеная.", "Наловил рыбы, кому продать?", "Сидел с удочкой с самого утра."]
    };

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (GatherAction.FindTool(bot, _kind) == null)
        {
            return 0;
        }

        var skill = bot.Skills[ResourceProbe.SkillFor(_kind)].Value / 100.0;
        var score = 0.15 + BotBrain.Trait(brain.Diligence) * 0.35 + BotBrain.Trait(brain.Greed) * 0.15 + skill * 0.35;

        score -= brain.Fatigue * 0.6;

        // Goods already in the pack want selling before more are gathered.
        if (BotGoods.ValueCarried(bot) > 1500)
        {
            score *= 0.3;
        }

        return score;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var center = bot.Map == brain.HomeMap && brain.HomeMap != null ? brain.Home : bot.Location;

        if (!ResourceAtlas.TryPick(bot.Map, center, SearchRange, _kind, out var spot) &&
            !ResourceAtlas.TryProspect(bot.Map, center, Utility.RandomMinMax(30, SearchRange), _kind, out spot))
        {
            return null;
        }

        var quota = 30 + brain.Diligence / 2;
        return [new GoToAction(bot.Map, spot, 0, Name.ToLowerInvariant()), new GatherAtSpotAction(_kind, quota, spot)];
    }
}

/// <summary>Gathers, and tells the atlas how the spot turned out.</summary>
public sealed class GatherAtSpotAction : BotAction
{
    private readonly GatherAction _gather;
    private readonly ResourceKind _kind;
    private readonly Point3D _spot;

    public GatherAtSpotAction(ResourceKind kind, int quota, Point3D spot)
    {
        _kind = kind;
        _spot = spot;
        _gather = new GatherAction(kind, quota);
    }

    public override void Start(BotBrain brain) => _gather.Start(brain);

    public override BotActionResult Tick(BotBrain brain)
    {
        var result = _gather.Tick(brain);

        if (result.Status == BotActionStatus.Done)
        {
            ResourceAtlas.Remember(brain.Bot.Map, _spot, _kind);
        }
        else if (result.Status == BotActionStatus.Failed)
        {
            ResourceAtlas.Bench(brain.Bot.Map, _spot, _kind);
        }

        return result;
    }

    public override void Stop(BotBrain brain) => _gather.Stop(brain);

    public override string Describe(BotBrain brain) => _gather.Describe(brain);
}
