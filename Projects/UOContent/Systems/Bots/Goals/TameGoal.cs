using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>A tamer with room for a follower goes out and tames the strongest creature it can
/// control, near home.</summary>
public sealed class TameGoal : BotGoal
{
    private const int SearchRange = 40;

    public override string Name => "Приручение";

    public override string[] News => ["Приручил себе зверушку.", "Зверь теперь за мной ходит, представляешь?"];

    private static bool IsTamer(Mobile bot) => bot.Skills.AnimalTaming.Value >= 40 && bot.Skills.AnimalLore.Value >= 30;

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        return IsTamer(bot) && bot.Followers < bot.FollowersMax ? 0.55 + bot.Skills.AnimalTaming.Value / 400 - brain.Fatigue * 0.4 : 0;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;
        var skill = bot.Skills.AnimalTaming.Value;
        var room = bot.FollowersMax - bot.Followers;

        BaseCreature best = null;
        var bestSkill = -1.0;

        foreach (var c in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, SearchRange))
        {
            if (!c.Tamable || c.Controlled || !c.Alive || c.ControlSlots > room || c.MinTameSkill > skill - 5 ||
                c.MinTameSkill <= bestSkill)
            {
                continue;
            }

            best = c;
            bestSkill = c.MinTameSkill;
        }

        return best == null ? null : [new GoToAction(best, 2, "к зверю"), new TameAction(best)];
    }
}
