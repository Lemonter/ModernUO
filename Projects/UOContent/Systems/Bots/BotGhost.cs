namespace Server.Systems.Bots;

/// <summary>
/// A dead bot walks to the nearest town healer as a ghost and is resurrected there, as a player
/// would be. Healers offer resurrection through a gump, which a client-less bot can't answer, so
/// arriving in range stands in for accepting it.
/// </summary>
public static class BotGhost
{
    private const int HealerRange = 3;

    // Nowhere to go (no town, no healer on this facet): wait it out, then get up where it fell.
    private const long NoHealerWaitMs = 120_000;

    internal static int Think(BotBrain brain)
    {
        var bot = brain.Bot;
        var now = Core.TickCount;

        if (!brain.IsGhost)
        {
            brain.IsGhost = true;
            brain.DiedAt = now;
            brain.GhostWalk = null;
            BotSpeech.Say(bot, BotTopic.Dead, chance: 0.5);
        }

        var city = WorldCatalog.FindNearest(bot.Map, bot.Location);
        var healer = city == null ? null : WorldCatalog.GetHealer(city);

        if (healer == null)
        {
            if (now - brain.DiedAt >= NoHealerWaitMs)
            {
                Resurrect(brain);
            }

            return 5000;
        }

        if (bot.Map == healer.Map && bot.InRange(healer, HealerRange))
        {
            Resurrect(brain);
            return 1000;
        }

        brain.GhostWalk ??= new GoToAction(healer, HealerRange, "к лекарю");
        if (!brain.GhostWalkStarted)
        {
            brain.GhostWalk.Start(brain);
            brain.GhostWalkStarted = true;
        }

        var result = brain.GhostWalk.Tick(brain);
        if (result.Status == BotActionStatus.Failed)
        {
            brain.GhostWalk = null;
            brain.GhostWalkStarted = false;
            return 3000;
        }

        return result.DelayMs;
    }

    private static void Resurrect(BotBrain brain)
    {
        var bot = brain.Bot;
        bot.Resurrect();
        bot.Hits = bot.HitsMax;
        bot.Stam = bot.StamMax;
        bot.Mana = bot.ManaMax;

        brain.IsGhost = false;
        brain.GhostWalk = null;
        brain.GhostWalkStarted = false;
        BotSpeech.Say(bot, BotTopic.Resurrected, chance: 0.6);
    }
}
