using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonSeasons;
using Server.Systems.MahaonWorld;
using Server.Targeting;

namespace Server.Systems.Bots;

/// <summary>
/// Sowing in spring and summer: a small field on open ground near home — never in a town or a
/// house — ploughed first when the bot owns a plough (that doubles the autumn harvest), seeded
/// from what the last harvest gave or from the farmer's stock. A diligent bot now and then plants
/// a fruit tree beside it. Everything goes through the plough's, seeds' and sapling's own target
/// cursors.
/// </summary>
public sealed class SowGoal : BotGoal
{
    private const int PlotsPerTrip = 8;
    private const int SeedsToBuy = 8;

    // Far enough from home to be outside the town, near enough to walk.
    private const int MinDistance = 20;
    private const int MaxDistance = 90;

    // A plough pays for itself only to someone who works the land often.
    private const int PloughDiligence = 60;
    private const int TreeDiligence = 70;

    public override string Name => "Посев";

    public override string[] News => ["Посеял кое-что, осенью соберу.", "Землю вспахал, теперь бы дождя.", "Посадил деревце у поля."];

    private static bool Season => SeasonSystem.CurrentSeason is MahaonSeason.Spring or MahaonSeason.Summer;

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (!Season || BotSocialRules.IsOutlaw(bot) || brain.HomeMap != bot.Map)
        {
            return 0;
        }

        var hasSeeds = bot.Backpack?.FindItemByType<MahaonCropSeed>() != null;
        if (!hasSeeds && BotShopping.Funds(bot) < 300)
        {
            return 0;
        }

        // Seeds already in the pack ask to be sown; otherwise it is the diligent who bother.
        var score = (hasSeeds ? 0.35 : 0.05) + BotBrain.Trait(brain.Diligence) * 0.35 - brain.Fatigue * 0.5;
        if (BotCombatStyles.FightingSkill(bot) < 40)
        {
            score += 0.1;
        }

        return score;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        // A fenced yard yields more: sow inside the fence first.
        var plots = BotHousing.HomeOf(bot) is { } house && house.Map == bot.Map &&
                    Systems.MahaonWorld.MahaonHouseFenceSystem.HasFence(house)
            ? FarmAtlas.FindPlot(bot.Map, house.BanLocation, 1, 4, PlotsPerTrip)
            : [];

        if (plots.Count == 0)
        {
            plots = FarmAtlas.FindPlot(bot.Map, brain.Home, MinDistance, MaxDistance, PlotsPerTrip);
        }

        if (plots.Count == 0)
        {
            return null;
        }

        var steps = new List<BotAction>();
        var city = WorldCatalog.FindNearest(bot.Map, bot.Location);

        if (bot.Backpack?.FindItemByType<MahaonCropSeed>() == null)
        {
            var crop = ChooseCrop(brain);
            var buy = BotShopping.PlanPurchase(
                bot, city, typeof(MahaonCropSeed), SeedsToBuy,
                info => info.Args is [MahaonCropType c] && c == crop
            );

            if (buy == null)
            {
                return null;
            }

            steps.AddRange(buy);
        }

        if (brain.Diligence >= PloughDiligence && bot.Backpack?.FindItemByType<MahaonPlough>() == null &&
            BotShopping.Funds(bot) >= 600 && BotShopping.PlanPurchase(bot, city, typeof(MahaonPlough), 1) is { } plough)
        {
            steps.AddRange(plough);
        }

        var treeSpot = brain.Diligence >= TreeDiligence && BotShopping.Funds(bot) >= 1000 && Utility.RandomDouble() < 0.3
            ? FarmAtlas.FindTreeSpot(bot.Map, plots[0])
            : null;

        if (treeSpot != null &&
            BotShopping.PlanPurchase(bot, city, typeof(MahaonSapling), 1, info => info.Args is [MahaonTreeSpecies s] && IsFruit(s)) is { } sapling)
        {
            steps.AddRange(sapling);
        }
        else
        {
            treeSpot = null;
        }

        steps.Add(new GoToAction(bot.Map, plots[0], 2, "на поле"));
        steps.Add(new SowAction(plots));

        if (treeSpot is { } spot)
        {
            steps.Add(new GoToAction(bot.Map, spot, 2, "сажать дерево"));
            steps.Add(new PlantTreeAction(spot));
        }

        return steps;
    }

    private static bool IsFruit(MahaonTreeSpecies species) => MahaonTreeSpeciesTable.Get(species).BearsFruit;

    /// <summary>What to grow: fibre for a tailor's money if greedy, food otherwise.</summary>
    private static MahaonCropType ChooseCrop(BotBrain brain) =>
        Utility.RandomDouble() < 0.3 + BotBrain.Trait(brain.Greed) * 0.5
            ? Utility.RandomBool() ? MahaonCropType.Cotton : MahaonCropType.Flax
            : Utility.RandomList(MahaonCropType.Wheat, MahaonCropType.Carrot, MahaonCropType.Cabbage, MahaonCropType.Pumpkin, MahaonCropType.Corn);
}

/// <summary>Works down a list of plots: plough if it can, then sow one seed in each.</summary>
public sealed class SowAction : BotAction
{
    private const int Reach = 2;

    private readonly List<Point3D> _plots;
    private int _next;
    private int _sown;
    private GoToAction _walk;

    public SowAction(List<Point3D> plots) => _plots = plots;

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

        if (_next >= _plots.Count || bot.Backpack?.FindItemByType<MahaonCropSeed>() is not { } seeds)
        {
            return _sown > 0 ? BotActionResult.Done() : BotActionResult.Failed();
        }

        var plot = _plots[_next];
        if (!bot.InRange(plot, Reach))
        {
            _walk = new GoToAction(bot.Map, plot, 1, "к пашне");
            _walk.Start(brain);
            return BotActionResult.Running(300);
        }

        bot.Direction = bot.GetDirectionTo(plot);

        // Plough first: a separate tick, as the plough's own target takes it.
        if (MahaonTilledEarth.Find(plot, bot.Map) == null && bot.Backpack.FindItemByType<MahaonPlough>() is { } plough)
        {
            plough.OnDoubleClick(bot);
            bot.Target?.Invoke(bot, new LandTarget(plot, bot.Map));

            if (MahaonTilledEarth.Find(plot, bot.Map) != null)
            {
                return BotActionResult.Running(1500);
            }
        }

        var before = seeds.Amount;
        seeds.OnDoubleClick(bot);
        bot.Target?.Invoke(bot, new LandTarget(plot, bot.Map));

        if (seeds.Deleted || seeds.Amount < before)
        {
            _sown++;
        }

        _next++;
        return BotActionResult.Running(1500);
    }

    public override void Stop(BotBrain brain) => _walk?.Stop(brain);

    public override string Describe(BotBrain brain) => $"Сеет: {_sown}/{_plots.Count}";
}

/// <summary>Plants a sapling from the pack at a spot.</summary>
public sealed class PlantTreeAction : BotAction
{
    private readonly Point3D _spot;

    public PlantTreeAction(Point3D spot) => _spot = spot;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (bot.Backpack?.FindItemByType<MahaonSapling>() is not { } sapling || !bot.InRange(_spot, 2))
        {
            return BotActionResult.Failed();
        }

        bot.Direction = bot.GetDirectionTo(_spot);
        sapling.OnDoubleClick(bot);
        bot.Target?.Invoke(bot, new LandTarget(_spot, bot.Map));

        return sapling.Deleted ? BotActionResult.Done(1500) : BotActionResult.Failed();
    }

    public override string Describe(BotBrain brain) => "Сажает дерево";
}
