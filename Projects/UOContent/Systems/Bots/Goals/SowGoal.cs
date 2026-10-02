using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonSeasons;
using Server.Systems.MahaonWorld;
using Server.Targeting;

namespace Server.Systems.Bots;

/// <summary>
/// Sowing in spring and summer: the bare farmland beside a field, ploughed first when the bot owns
/// a plough (that doubles the autumn harvest), seeded from what the last harvest gave or from the
/// farmer's stock. Now and then a diligent bot plants a fruit tree at the field's edge. Everything
/// goes through the plough's, seeds' and sapling's own target cursors.
/// </summary>
public sealed class SowGoal : BotGoal
{
    private const int SearchRange = 150;
    private const int PlotsPerTrip = 8;
    private const int SeedsToBuy = 8;

    // A plough pays for itself only to someone who works the land often.
    private const int PloughDiligence = 60;

    // Orchards grow slowly and stay put: a tree only where few stand yet.
    private const int TreeDiligence = 70;
    private const int MaxTreesNearby = 4;
    private const int TreeCrowding = 12;

    public override string Name => "Посев";

    public override string[] News => ["Посеял кое-что, осенью соберу.", "Землю вспахал, теперь бы дождя.", "Посадил деревце у поля."];

    private static bool Season => SeasonSystem.CurrentSeason is MahaonSeason.Spring or MahaonSeason.Summer;

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (!Season || BotSocialRules.IsOutlaw(bot))
        {
            return 0;
        }

        var hasSeeds = bot.Backpack?.FindItemByType<MahaonCropSeed>() != null;
        if (!hasSeeds && BotShopping.Funds(bot) < 300)
        {
            return 0;
        }

        if (FarmAtlas.NearestField(bot.Map, bot.Location, SearchRange) is not { } patch ||
            FarmAtlas.FreeFarmland(patch, 1).Count == 0)
        {
            return 0;
        }

        // Seeds already in the pack ask to be sown; otherwise it is the diligent who bother.
        var score = (hasSeeds ? 0.35 : 0.1) + BotBrain.Trait(brain.Diligence) * 0.35 - brain.Fatigue * 0.5;
        if (BotCombatStyles.FightingSkill(bot) < 40)
        {
            score += 0.1;
        }

        return score;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        if (FarmAtlas.NearestField(bot.Map, bot.Location, SearchRange) is not { } patch)
        {
            return null;
        }

        var plots = FarmAtlas.FreeFarmland(patch, PlotsPerTrip);
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

        steps.Add(new GoToAction(patch.Map, plots[0], 2, "на поле"));
        steps.Add(new SowAction(plots));

        if (brain.Diligence >= TreeDiligence && BotShopping.Funds(bot) >= 1000 && TreeSpot(patch) is { } spot &&
            BotShopping.PlanPurchase(bot, city, typeof(MahaonSapling), 1, info => info.Args is [MahaonTreeSpecies s] && IsFruit(s)) is { } sapling)
        {
            // Bought after the sowing would mean a second walk to town; buy it on the way out.
            steps.InsertRange(0, sapling);
            steps.Add(new GoToAction(patch.Map, spot, 2, "сажать дерево"));
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

    /// <summary>A free spot just outside the field for a tree, unless the area has trees enough.</summary>
    private static Point3D? TreeSpot(FieldPatch patch)
    {
        var map = patch.Map;
        var trees = 0;
        foreach (var item in map.GetItemsInRange(patch.Center, TreeCrowding))
        {
            if (item is MahaonTreeFoliage or MahaonPlantedSapling && ++trees >= MaxTreesNearby)
            {
                return null;
            }
        }

        var b = patch.Bounds;
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var x = Utility.RandomBool() ? b.Start.X - 2 : b.End.X + 1;
            var y = Utility.RandomMinMax(b.Start.Y, b.End.Y);
            if (Utility.RandomBool())
            {
                (x, y) = (Utility.RandomMinMax(b.Start.X, b.End.X), Utility.RandomBool() ? b.Start.Y - 2 : b.End.Y + 1);
            }

            var z = map.GetAverageZ(x, y);
            if (map.CanFit(x, y, z, 16) && !Systems.MahaonFarming.MahaonFieldPlots.IsPlotGround(map.Tiles.GetLandTile(x, y).ID))
            {
                return new Point3D(x, y, z);
            }
        }

        return null;
    }
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

        // One seed, not the stack: the seed's planting deletes the item it was used from.
        var single = seeds.Amount > 1 ? new MahaonCropSeed(seeds.CropType) : seeds;
        if (single != seeds)
        {
            seeds.Consume(1);
            bot.Backpack.DropItem(single);
        }

        single.OnDoubleClick(bot);
        bot.Target?.Invoke(bot, new LandTarget(plot, bot.Map));

        if (single.Deleted)
        {
            _sown++;
        }
        else
        {
            // Refused (no room after all): the seed goes back on the stack.
            bot.Backpack.TryDropItem(bot, single, false);
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
