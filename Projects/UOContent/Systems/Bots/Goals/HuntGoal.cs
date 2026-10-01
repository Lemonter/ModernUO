using System.Collections.Generic;

namespace Server.Systems.Bots;

/// <summary>Earn by the sword: hunt creatures as tough as the bot can take, near home.</summary>
public sealed class HuntGoal : BotGoal
{
    private const int SearchRange = 300;

    public override string Name => "Охота";

    public override string[] News =>
        ["Только с охоты, еле ноги унёс.", "Набил тварей, добыча неплохая.", "Чуть не сожрали меня там."];

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        var skill = BotCombatStyles.FightingSkill(bot) / 100.0;
        if (skill < 0.2 || bot.Hits < bot.HitsMax * 0.8)
        {
            return 0;
        }

        var score = 0.1 + skill * 0.55 + BotBrain.Trait((byte)(100 - brain.Caution)) * 0.2 + BotBrain.Trait(brain.Greed) * 0.1;
        score -= brain.Fatigue * 0.6;

        // Loot and goods in the pack want selling first.
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
        var spot = HuntingAtlas.Pick(bot.Map, center, SearchRange, BotCombatStyles.MaxPreyFame(brain));

        if (spot == null)
        {
            return null;
        }

        return [new GoToAction(spot.Map, spot.Center, 4, "на охоту"), new HuntAction(spot, 6 + brain.Diligence / 10)];
    }
}

/// <summary>After a death: go back for the belongings left on the corpse.</summary>
public sealed class CorpseRunGoal : BotGoal
{
    public override string Name => "За своим трупом";

    public override bool IsUpkeep => true;

    public override double Score(BotBrain brain) =>
        brain.OwnCorpse is { Deleted: false } corpse && corpse.Items.Count > 0 && corpse.Map == brain.Bot.Map ? 1.1 : 0;

    public override List<BotAction> Plan(BotBrain brain)
    {
        var corpse = brain.OwnCorpse;
        if (corpse == null || corpse.Deleted)
        {
            return null;
        }

        return [new GoToAction(corpse.Map, corpse.Location, 1, "к своему трупу"), new LootCorpseAction(corpse)];
    }
}
