using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>Puts all but some pocket money in the bank, standing at a banker, and tidies the box:
/// goods that came back from the auction come out, loose gold becomes cheques.</summary>
public sealed class DepositGoldAction : BotAction
{
    private const int BankerRange = 12;

    private readonly Banker _banker;

    public DepositGoldAction(Banker banker) => _banker = banker;

    /// <summary>What a bot keeps on it for the day: enough for reagents, bandages and a meal.</summary>
    public static int PocketMoney(BotBrain brain) => 200 + brain.Greed * 3;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var pack = bot.Backpack;

        if (pack == null || _banker.Deleted || !bot.InRange(_banker, BankerRange))
        {
            return BotActionResult.Failed();
        }

        BotBank.TakeOut(bot);
        BotBank.Consolidate(bot);

        var amount = pack.GetAmount(typeof(Gold)) - PocketMoney(brain);
        if (amount <= 0)
        {
            return BotActionResult.Done();
        }

        bot.Say("банк");

        if (pack.ConsumeTotal(typeof(Gold), amount) && !Banker.Deposit(bot, amount))
        {
            // The bank box refused (full); put the gold back rather than lose it.
            pack.DropItem(new Gold(amount));
            return BotActionResult.Failed();
        }

        return BotActionResult.Done(1500);
    }

    public override string Describe(BotBrain brain) => "Кладёт золото в банк";
}
