using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>A paladin low on tithing points tithes at the town ankh.</summary>
public sealed class TitheGoal : BotGoal
{
    private const int Low = 300;
    private const int Offer = 2000;

    public override string Name => "Десятина";

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (bot.Skills.Chivalry.Value < 30 || bot.TithingPoints >= Low)
        {
            return 0;
        }

        return Banker.GetBalance(bot) + (bot.Backpack?.GetAmount(typeof(Gold)) ?? 0) >= Offer ? 0.6 : 0;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var city = WorldCatalog.FindNearest(bot.Map, bot.Location);
        var ankh = city == null ? null : WorldCatalog.GetAnkh(city);
        if (ankh == null)
        {
            return null;
        }

        var steps = new List<BotAction>();
        if ((bot.Backpack?.GetAmount(typeof(Gold)) ?? 0) < Offer && WorldCatalog.GetBanker(city) is { } banker)
        {
            steps.Add(new GoToAction(banker, 3, "в банк"));
            steps.Add(new WithdrawGoldAction(banker, Offer));
        }

        steps.Add(new GoToAction(ankh.Map, ankh.Location, 2, "к анкху"));
        steps.Add(new TitheAction(ankh, Offer));
        return steps;
    }
}
