using Server.Items;

namespace Server.Systems.Bots;

/// <summary>Tithes gold at an ankh for Chivalry, as the ankh's tithing gump does.</summary>
public sealed class TitheAction : BotAction
{
    private const int MaxTithe = 100_000;

    private readonly Item _ankh;
    private readonly int _amount;

    public TitheAction(Item ankh, int amount)
    {
        _ankh = ankh;
        _amount = amount;
    }

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (_ankh.Deleted || !bot.InRange(_ankh.GetWorldLocation(), 3))
        {
            return BotActionResult.Failed();
        }

        var offer = System.Math.Min(_amount, MaxTithe - bot.TithingPoints);
        offer = System.Math.Min(offer, bot.Backpack?.GetAmount(typeof(Gold)) ?? 0);

        if (offer <= 0 || bot.Backpack?.ConsumeTotal(typeof(Gold), offer) != true)
        {
            return BotActionResult.Failed();
        }

        bot.TithingPoints += offer;
        return BotActionResult.Done(1500);
    }

    public override string Describe(BotBrain brain) => "Жертвует у анкха";
}
