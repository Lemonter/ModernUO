using System;

namespace Server.Systems.MahaonBots;

/// <summary>
///     Guild relations (BotGuilds.Relations) used to be purely GM-set through the beacon
///     gump and never changed on their own. This nudges them periodically — every few
///     hours, a small chance per guild pair to escalate (Neutral to War/Ally) or cool down
///     (War/Ally back to Neutral), so the world's alliance map actually shifts over time
///     instead of staying exactly however a GM last configured it. GM-set relations still
///     work exactly as before; this just adds drift on top, same "world only moves while
///     someone's online" rule the raid system uses.
/// </summary>
public static class DynamicGuildRelations
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(2);
    private const double ShiftChancePerPair = 0.08; // checked once per pair, per poll

    private static Timer _pollTimer;

    public static void Initialize()
    {
        _pollTimer = Timer.DelayCall(PollInterval, PollInterval, Poll);
    }

    private static void Poll()
    {
        if (Network.NetState.Instances.Count == 0)
        {
            return; // world "paused" — no drift while no one's online to see it
        }

        var names = BotGuilds.NamePool;

        for (var i = 0; i < names.Length; i++)
        {
            for (var j = i + 1; j < names.Length; j++)
            {
                if (Utility.RandomDouble() >= ShiftChancePerPair)
                {
                    continue;
                }

                var current = BotGuilds.GetRelation(names[i], names[j]);
                var next = ShiftFrom(current);

                if (next != current)
                {
                    BotGuilds.SetRelation(names[i], names[j], next);
                }
            }
        }
    }

    private static BotGuildRelation ShiftFrom(BotGuildRelation current) => current switch
    {
        // Neutral tips toward War slightly more often than Ally — a bit more chaos than
        // peace, reads as a livelier world than a 50/50 coin flip would.
        BotGuildRelation.Neutral => Utility.RandomDouble() < 0.6 ? BotGuildRelation.War : BotGuildRelation.Ally,
        // War and Ally both cool back down to Neutral eventually rather than flipping
        // straight to the opposite extreme.
        BotGuildRelation.War  => BotGuildRelation.Neutral,
        BotGuildRelation.Ally => BotGuildRelation.Neutral,
        _                     => current
    };
}
