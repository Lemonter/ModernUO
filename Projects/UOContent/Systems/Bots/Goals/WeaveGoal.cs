using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// Cotton and flax into cloth: spun into thread at a spinning wheel, woven into bolts at a loom,
/// through the fibres' and thread's own target cursors. Usable wheels are rare — most drawn into
/// shops are scenery — so a bot carries its fibre to the nearest town that has one, or else sells
/// it raw to a tailor (see <see cref="TradeGoal"/>).
/// </summary>
public sealed class WeaveGoal : BotGoal
{
    private const int MinFibre = 5;
    private const int MaxTrip = 600;

    public override string Name => "Ткачество";

    public override string[] News => ["Наткал полотна, руки гудят.", "Прялка скрипит, а нитки идут."];

    // Raw fibre sells nearly as well as cloth; only the diligent bother with the wheel.
    private const int MinDiligence = 50;

    private static bool Weaves(Mobile bot) => bot.GetBrain() is { Diligence: >= MinDiligence };

    /// <summary>A weaver holds its fibre and thread back from the market.</summary>
    public static bool KeepsForWeaving(Mobile bot, Item item) =>
        item is Cotton or Flax or SpoolOfThread && Weaves(bot) && NearestTextileTown(bot, out _, out _) != null;

    private static int Fibre(Mobile bot) =>
        (bot.Backpack?.GetAmount(typeof(Cotton)) ?? 0) + (bot.Backpack?.GetAmount(typeof(Flax)) ?? 0);

    internal static BotCity NearestTextileTown(Mobile bot, out Item wheel, out Item loom)
    {
        // A wheel and a loom at home come first; the town then only stands for where they are.
        if (BotHousing.HomeOf(bot) is { Map: { } homeMap } house && homeMap == bot.Map &&
            BotHousing.TryGetTextiles(house, out wheel, out loom))
        {
            return WorldCatalog.FindNearest(homeMap, house.Location);
        }

        wheel = null;
        loom = null;
        BotCity best = null;
        var bestDist = (double)MaxTrip;
        var outlaw = BotSocialRules.IsOutlaw(bot);

        foreach (var city in WorldCatalog.Cities)
        {
            if (city.Map != bot.Map || outlaw && BotSocialRules.IsGuarded(city.Region))
            {
                continue;
            }

            var dist = city.Center.GetDistanceToSqrt(bot.Location);
            if (dist < bestDist && WorldCatalog.TryGetTextileShop(city, out var w, out var l))
            {
                (best, bestDist, wheel, loom) = (city, dist, w, l);
            }
        }

        return best;
    }

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (!Weaves(bot) || Fibre(bot) < MinFibre || NearestTextileTown(bot, out _, out _) == null)
        {
            return 0;
        }

        return 0.45 + BotBrain.Trait(brain.Diligence) * 0.3 - brain.Fatigue * 0.4;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        if (NearestTextileTown(bot, out var wheel, out var loom) == null)
        {
            return null;
        }

        return
        [
            new UnloadPackAnimalAction(),
            new GoToAction(wheel.Map, wheel.Location, 2, "к прялке"),
            new SpinAction(wheel),
            new GoToAction(loom.Map, loom.Location, 2, "к ткацкому станку"),
            new WeaveAction(loom)
        ];
    }
}

/// <summary>Spins every bale of cotton and flax in the pack, one at a time as the wheel turns.</summary>
public sealed class SpinAction : BotAction
{
    private readonly Item _wheel;

    public SpinAction(Item wheel) => _wheel = wheel;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;

        if (_wheel.Deleted || !bot.InRange(_wheel.GetWorldLocation(), 3))
        {
            return BotActionResult.Failed();
        }

        if (_wheel is ISpinningWheel { Spinning: true })
        {
            return BotActionResult.Running(1000);
        }

        var fibre = (Item)bot.Backpack?.FindItemByType<Cotton>() ?? bot.Backpack?.FindItemByType<Flax>();
        if (fibre == null)
        {
            return BotActionResult.Done();
        }

        bot.Direction = bot.GetDirectionTo(_wheel);
        fibre.OnDoubleClick(bot);

        if (bot.Target is not { } target)
        {
            return BotActionResult.Failed();
        }

        target.Invoke(bot, _wheel);
        return BotActionResult.Running(3500);
    }

    public override string Describe(BotBrain brain) => "Прядёт нитки";
}

/// <summary>Weaves thread into bolts of cloth, five spools to a bolt.</summary>
public sealed class WeaveAction : BotAction
{
    private const int SpoolsPerBolt = 5;

    private readonly Item _loom;

    public WeaveAction(Item loom) => _loom = loom;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;

        if (_loom.Deleted || !bot.InRange(_loom.GetWorldLocation(), 3))
        {
            return BotActionResult.Failed();
        }

        // A loom half-threaded by someone else still finishes a bolt with fewer spools.
        var needed = _loom is ILoom loom ? SpoolsPerBolt - loom.Phase : SpoolsPerBolt;
        var thread = bot.Backpack?.FindItemByType<SpoolOfThread>();
        if (thread == null || bot.Backpack.GetAmount(typeof(SpoolOfThread)) < needed)
        {
            return BotActionResult.Done();
        }

        bot.Direction = bot.GetDirectionTo(_loom);
        thread.OnDoubleClick(bot);

        if (bot.Target is not { } target)
        {
            return BotActionResult.Failed();
        }

        target.Invoke(bot, _loom);
        return BotActionResult.Running(1200);
    }

    public override string Describe(BotBrain brain) => "Ткёт полотно";
}
