using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonSeasons;

namespace Server.Systems.Bots;

/// <summary>
/// Picking fruit in spring and summer, harvesting the fields in autumn — through the trees' and
/// crop tiles' own double-click, like a player. The harvest feeds the bot's animals and sells:
/// a tailor pays well for cotton and flax, a farmer for fruit and vegetables.
/// </summary>
public sealed class FarmGoal : BotGoal
{
    private const int SearchRange = 200;

    public override string Name => "Сбор урожая";

    public override string[] News => SeasonSystem.CurrentSeason == MahaonSeason.Autumn
        ? ["Убирал урожай, руки в земле.", "Поля в этом году щедрые.", "Хлопок нынче в цене, говорят."]
        : ["Набрал фруктов, угощайся.", "Яблоки в этом году сладкие.", "Обтряс пару деревьев."];

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (SeasonSystem.CurrentSeason == MahaonSeason.Winter ||
            BotGoods.ValueCarried(bot) > 1500 * BotGoods.TripCapacity(bot))
        {
            return 0;
        }

        var hungryAnimals = BotStable.TryNeededFood(bot, out _, out _);
        var target = SeasonSystem.CurrentSeason == MahaonSeason.Autumn
            ? (object)FarmAtlas.NearestRipeField(bot.Map, bot.Location, SearchRange)
            : FarmAtlas.NearestFruitTree(bot.Map, bot.Location, SearchRange);

        if (target == null)
        {
            return 0;
        }

        if (hungryAnimals)
        {
            return 0.8;
        }

        // Field work suits the diligent and those who don't live by the sword.
        var score = 0.15 + BotBrain.Trait(brain.Diligence) * 0.3 - brain.Fatigue * 0.5;
        if (BotCombatStyles.FightingSkill(bot) < 40)
        {
            score += 0.15;
        }

        // Autumn's cotton and flax are worth the walk.
        if (SeasonSystem.CurrentSeason == MahaonSeason.Autumn)
        {
            score += BotBrain.Trait(brain.Greed) * 0.15;
        }

        return score;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;

        if (SeasonSystem.CurrentSeason == MahaonSeason.Autumn)
        {
            var field = FarmAtlas.NearestRipeField(bot.Map, bot.Location, SearchRange);
            return field == null
                ? null
                : [new GoToAction(field.Map, field.Center, 3, "на поле"), new HarvestFieldAction(field, Utility.RandomMinMax(6, 12))];
        }

        var tree = FarmAtlas.NearestFruitTree(bot.Map, bot.Location, SearchRange);
        return tree == null
            ? null
            : [new GoToAction(tree.Map, tree.Location, 1, "к фруктовому дереву"), new PickFruitAction(Utility.RandomMinMax(3, 6))];
    }
}

/// <summary>Works through a field patch tile by tile: the nearest ripe tile, walk up, harvest.</summary>
public sealed class HarvestFieldAction : BotAction
{
    private const int ReachRange = 2;

    private readonly FieldPatch _patch;
    private readonly int _quota;
    private int _done;
    private MahaonCropTile _tile;
    private GoToAction _walk;

    public HarvestFieldAction(FieldPatch patch, int quota)
    {
        _patch = patch;
        _quota = quota;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;

        if (_walk != null)
        {
            var step = _walk.Tick(brain);
            if (step.Status == BotActionStatus.Running)
            {
                return step;
            }

            _walk.Stop(brain);
            _walk = null;
        }

        if (_tile is { Deleted: false, Harvested: false } && bot.InRange(_tile.GetWorldLocation(), ReachRange))
        {
            bot.Direction = bot.GetDirectionTo(_tile);
            _tile.OnDoubleClick(bot);
            _done++;
            _tile = null;
            return BotActionResult.Running(1500);
        }

        // A harvest that doesn't fit would fall to the ground.
        if (Mobile.BodyWeight + bot.TotalWeight > bot.MaxWeight - 30 && !BotStable.TryOffload(bot))
        {
            return _done > 0 ? BotActionResult.Done() : BotActionResult.Failed();
        }

        if (_done >= _quota || (_tile = NearestRipe(bot)) == null)
        {
            return _done > 0 ? BotActionResult.Done() : BotActionResult.Failed();
        }

        if (!bot.InRange(_tile.GetWorldLocation(), ReachRange))
        {
            _walk = new GoToAction(_tile.Map, _tile.Location, 1, "к грядке");
            _walk.Start(brain);
        }

        return BotActionResult.Running(300);
    }

    private MahaonCropTile NearestRipe(Mobile bot)
    {
        MahaonCropTile best = null;
        var bestDist = double.MaxValue;

        foreach (var tile in _patch.Tiles)
        {
            if (tile.Deleted || tile.Harvested)
            {
                continue;
            }

            var dist = bot.GetDistanceToSqrt(tile);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = tile;
            }
        }

        return best;
    }

    public override void Stop(BotBrain brain) => _walk?.Stop(brain);

    public override string Describe(BotBrain brain) => $"Убирает урожай: {_done}/{_quota}";
}

/// <summary>Picks fruit tree after tree nearby: a handful per pick, as a player would.</summary>
public sealed class PickFruitAction : BotAction
{
    private const int NextTreeRange = 24;

    private readonly int _trees;
    private int _visited;
    private MahaonTreeFoliage _tree;
    private GoToAction _walk;

    public PickFruitAction(int trees) => _trees = trees;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;

        if (_walk != null)
        {
            var step = _walk.Tick(brain);
            if (step.Status == BotActionStatus.Running)
            {
                return step;
            }

            _walk.Stop(brain);
            _walk = null;
        }

        if (_tree != null && FarmAtlas.HasFruit(_tree) && bot.InRange(_tree.GetWorldLocation(), 2))
        {
            bot.Direction = bot.GetDirectionTo(_tree);
            _tree.OnDoubleClick(bot);
            return BotActionResult.Running(1200);
        }

        if (_tree != null)
        {
            _visited++;
        }

        if (_visited >= _trees || (_tree = FarmAtlas.NearestFruitTree(bot.Map, bot.Location, NextTreeRange)) == null)
        {
            return _visited > 0 ? BotActionResult.Done() : BotActionResult.Failed();
        }

        if (!bot.InRange(_tree.GetWorldLocation(), 2))
        {
            _walk = new GoToAction(_tree.Map, _tree.Location, 1, "к дереву");
            _walk.Start(brain);
        }

        return BotActionResult.Running(300);
    }

    public override void Stop(BotBrain brain) => _walk?.Stop(brain);

    public override string Describe(BotBrain brain) => $"Собирает фрукты: дерево {_visited + 1}/{_trees}";
}
