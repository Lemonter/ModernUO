using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// A dead bot walks as a ghost to the nearest place that will bring it back, as a player would:
/// a healer who accepts it, or a shrine ankh. Healers refuse murderers and criminals, so reds
/// head for shrines (the Chaos shrine, the virtue shrines) or an evil healer, keeping out of
/// guarded towns. Healers and ankhs offer resurrection through a gump, which a client-less bot
/// can't answer, so arriving in range stands in for accepting it.
/// </summary>
public static class BotGhost
{
    private const int HealerRange = 3;

    // Wandering and evil healers have no town to be catalogued under; a ghost notices the ones nearby.
    private const int NearbyHealerRange = 48;

    // Nowhere to go (nothing on this facet, or every route failed): wait it out, then get up where it fell.
    private const long NoTargetWaitMs = 120_000;
    private const int MaxWalkFailures = 3;

    internal static int Think(BotBrain brain)
    {
        var bot = brain.Bot;
        var now = Core.TickCount;

        if (!brain.IsGhost)
        {
            brain.IsGhost = true;
            brain.DiedAt = now;
            brain.OwnCorpse = bot.Corpse as Corpse;
            brain.GhostFailures = 0;
            ClearTarget(brain);

            // The killer is remembered for a good while.
            if (bot.LastKiller is { } killer)
            {
                brain.AddGrudge(killer is BaseCreature { ControlMaster: { } master } ? master : killer, 2 * 60 * 60_000);
            }

            BotSpeech.Say(bot, BotTopic.Dead, chance: 0.5);
        }

        if (!IsValid(brain.GhostTarget, bot) && brain.GhostFailures < MaxWalkFailures)
        {
            ClearTarget(brain);
            brain.GhostTarget = ChooseTarget(bot);
        }

        var target = brain.GhostTarget;
        if (target == null)
        {
            if (now - brain.DiedAt >= NoTargetWaitMs)
            {
                Resurrect(brain);
            }

            return 5000;
        }

        if (TryResurrectAt(brain, target))
        {
            return 1000;
        }

        if (brain.GhostTarget == null)
        {
            return 2000;
        }

        brain.GhostWalk ??= target switch
        {
            BaseHealer healer => new GoToAction(healer, HealerRange, "к лекарю"),
            _                 => new GoToAction(target.Map, ((Item)target).GetWorldLocation(), 1, "к анкху")
        };

        if (!brain.GhostWalkStarted)
        {
            brain.GhostWalk.Start(brain);
            brain.GhostWalkStarted = true;
        }

        var result = brain.GhostWalk.Tick(brain);
        if (result.Status == BotActionStatus.Failed)
        {
            brain.GhostFailures++;
            ClearTarget(brain);
            return 3000;
        }

        return result.DelayMs;
    }

    /// <summary>
    /// The nearest healer that would accept this bot or shrine ankh it may use. Outlaws skip
    /// anything inside guards, where the town defenders would cut them down as they rise.
    /// </summary>
    internal static IEntity ChooseTarget(Mobile bot)
    {
        var map = bot.Map;
        var outlaw = BotSocialRules.IsOutlaw(bot);
        IEntity best = null;
        var bestDist = double.MaxValue;

        void Consider(IEntity e, Point3D at)
        {
            if (outlaw && BotSocialRules.IsGuarded(map, at))
            {
                return;
            }

            var dist = bot.GetDistanceToSqrt(at);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = e;
            }
        }

        if (BotSocialRules.TownFor(bot) is { } city && WorldCatalog.GetHealer(city) is { } townHealer &&
            Accepts(townHealer, bot))
        {
            Consider(townHealer, townHealer.Location);
        }

        foreach (var healer in map.GetMobilesInRange<BaseHealer>(bot.Location, NearbyHealerRange))
        {
            if (healer.Alive && Accepts(healer, bot))
            {
                Consider(healer, healer.Location);
            }
        }

        var ankh = ShrineAtlas.FindNearest(
            map,
            bot.Location,
            a => !outlaw || !BotSocialRules.IsGuarded(a.Map, a.Location)
        );

        if (ankh != null)
        {
            Consider(ankh, ankh.GetWorldLocation());
        }

        return best;
    }

    /// <summary>
    /// Whether a healer would resurrect <paramref name="m"/>, mirroring the healers' own
    /// CheckResurrect without asking them: that refuses out loud, and a ghost weighing its
    /// options shouldn't make every healer in range speak.
    /// </summary>
    public static bool Accepts(BaseHealer healer, Mobile m) =>
        healer switch
        {
            EvilHealer or EvilWanderingHealer => !(Core.AOS && m.Criminal),
            Healer or WanderingHealer         => !m.Criminal && !m.Murderer,
            _                                 => true
        };

    private static bool IsValid(IEntity target, Mobile bot) =>
        target switch
        {
            BaseHealer healer => !healer.Deleted && healer.Alive && healer.Map == bot.Map && Accepts(healer, bot),
            Item ankh         => !ankh.Deleted && ankh.Map == bot.Map,
            _                 => false
        };

    private static bool TryResurrectAt(BotBrain brain, IEntity target)
    {
        var bot = brain.Bot;

        if (target is BaseHealer healer)
        {
            if (!bot.InRange(healer, HealerRange))
            {
                return false;
            }

            // The healer has the last word (and says why when it refuses).
            if (!healer.CheckResurrect(bot))
            {
                brain.GhostFailures++;
                ClearTarget(brain);
                return false;
            }
        }
        else if (target is Item ankh)
        {
            if (!bot.InRange(ankh.GetWorldLocation(), Ankhs.ResurrectRange) ||
                bot.Map?.CanFit(bot.Location, 16, false, false) != true)
            {
                return false;
            }
        }

        Resurrect(brain);
        return true;
    }

    private static void ClearTarget(BotBrain brain)
    {
        brain.GhostWalk?.Stop(brain);
        brain.GhostWalk = null;
        brain.GhostWalkStarted = false;
        brain.GhostTarget = null;
    }

    private static void Resurrect(BotBrain brain)
    {
        var bot = brain.Bot;
        bot.Resurrect();
        bot.Hits = bot.HitsMax;
        bot.Stam = bot.StamMax;
        bot.Mana = bot.ManaMax;

        brain.IsGhost = false;
        brain.GhostFailures = 0;
        ClearTarget(brain);
        BotSpeech.Say(bot, BotTopic.Resurrected, chance: 0.6);
    }
}
