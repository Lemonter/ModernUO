using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.Bots;

public sealed class BankGoal : BotGoal
{
    public override string Name => "В банк";

    public override double Score(BotBrain brain)
    {
        var gold = brain.Bot.Backpack?.GetAmount(typeof(Gold)) ?? 0;
        var excess = gold - DepositGoldAction.PocketMoney(brain);

        if (excess <= 0)
        {
            return 0;
        }

        // Cautious bots don't like walking around with a fat purse.
        var comfort = 2000 - BotBrain.Trait(brain.Caution) * 1500;
        return System.Math.Min(1.0, excess / comfort) * 0.9;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var city = WorldCatalog.FindNearest(bot.Map, bot.Location);
        var banker = city == null ? null : WorldCatalog.GetBanker(city);

        if (banker == null)
        {
            return null;
        }

        BotSpeech.Say(bot, BotTopic.Banking, chance: 0.5);
        return [new GoToAction(banker, 3, "в банк"), new DepositGoldAction(banker)];
    }
}
