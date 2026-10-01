using Server.Mobiles;
using Server.Systems.MahaonCities;

namespace Server.Systems.Bots;

/// <summary>
/// A market round in town: walk to a vendor who buys something the bot carries, sell, repeat;
/// whatever no vendor wants goes up on the auction. Decided step by step, since what is left to
/// sell only shows after each sale.
/// </summary>
public sealed class SellGoodsAction : BotAction
{
    private const int MaxVendorVisits = 6;

    private readonly BotCity _city;

    private BotAction _step;
    private int _visits;
    private bool _listed;

    public SellGoodsAction(BotCity city) => _city = city;

    public override BotActionResult Tick(BotBrain brain)
    {
        if (_step != null)
        {
            var result = _step.Tick(brain);
            if (result.Status == BotActionStatus.Running)
            {
                return result;
            }

            _step.Stop(brain);
            _step = null;

            if (result.Status == BotActionStatus.Failed && _listed)
            {
                return BotActionResult.Failed();
            }
        }

        var bot = brain.Bot;

        if (BotGoods.ValueCarried(bot) <= 0)
        {
            return BotActionResult.Done();
        }

        if (_visits < MaxVendorVisits && NextBuyer(bot) is { } vendor)
        {
            _visits++;
            _step = new Sequence(new GoToAction(vendor, 2, $"к торговцу {vendor.Name}"), new SellToVendorAction(vendor));
            _step.Start(brain);
            return BotActionResult.Running(250);
        }

        if (_listed)
        {
            return BotActionResult.Done();
        }

        _listed = true;

        _step = CityMarkers.TryGetMarker(_city.Name, "auctionstone", out var stone, out var stoneMap) && stoneMap == bot.Map
            ? new Sequence(new GoToAction(stoneMap, stone, 2, "к аукциону"), new AuctionListAction(_city.Name))
            : new AuctionListAction(_city.Name);
        _step.Start(brain);
        return BotActionResult.Running(250);
    }

    private BaseVendor NextBuyer(Mobile bot)
    {
        foreach (var item in bot.Backpack.Items)
        {
            if (BotGoods.IsForSale(bot, item) && WorldCatalog.FindBuyerFor(_city, item) is { } vendor)
            {
                return vendor;
            }
        }

        return null;
    }

    public override void Stop(BotBrain brain) => _step?.Stop(brain);

    public override string Describe(BotBrain brain) => _step?.Describe(brain) ?? "Торгует";
}

/// <summary>Runs actions one after another as a single action.</summary>
public sealed class Sequence : BotAction
{
    private readonly BotAction[] _steps;
    private int _index;

    public Sequence(params BotAction[] steps) => _steps = steps;

    public override void Start(BotBrain brain) => _steps[0].Start(brain);

    public override BotActionResult Tick(BotBrain brain)
    {
        var result = _steps[_index].Tick(brain);

        if (result.Status != BotActionStatus.Done)
        {
            return result;
        }

        _steps[_index].Stop(brain);
        if (++_index >= _steps.Length)
        {
            return result;
        }

        _steps[_index].Start(brain);
        return BotActionResult.Running(result.DelayMs);
    }

    public override void Stop(BotBrain brain)
    {
        if (_index < _steps.Length)
        {
            _steps[_index].Stop(brain);
        }
    }

    public override string Describe(BotBrain brain) => _index < _steps.Length ? _steps[_index].Describe(brain) : "—";
}
