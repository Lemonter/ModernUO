using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Engines.Pathing.Nav;
using Server.Logging;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Systems.Bots;

/// <summary>
/// Entry point of the bots: registration, startup and the admin commands. Spawned bots
/// (<see cref="BotMobile"/>) carry their brain in their save; a player who hands their own
/// character to the AI with [BecomeBot gets a brain for as long as that lasts.
///
///   [BotInspect — a bot's needs, character, goal, plan and why.
///   [BotStats   — scheduler and population figures.
///   [ClearBots  — deletes every spawned bot (beacons spawn new ones).
/// </summary>
public static class BotSystem
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSystem));

    private static readonly HashSet<PlayerMobile> _bots = [];
    private static readonly Dictionary<PlayerMobile, BotBrain> _possessed = new();
    private static readonly List<BotMobile> _loaded = [];
    private static bool _initialized;

    public static int Count => _bots.Count;

    public static IReadOnlyCollection<PlayerMobile> Bots => _bots;

    /// <summary>The brain driving <paramref name="m"/>, if any: a spawned bot's own, or a possessed
    /// player's.</summary>
    public static BotBrain GetBrain(this Mobile m) =>
        m switch
        {
            BotMobile bot                                                  => bot.Brain,
            PlayerMobile pm when _possessed.TryGetValue(pm, out var brain) => brain,
            _                                                              => null
        };

    public static bool IsActive(Mobile m) => m.GetBrain() is { Registered: true };

    public static bool IsPossessed(Mobile m) => m is PlayerMobile pm && _possessed.ContainsKey(pm);

    public static void Configure()
    {
        BotScheduler.BudgetMs = ServerConfiguration.GetOrUpdateSetting("bots.thinkBudgetMs", 4.0);

        CommandSystem.Register("BotInspect", AccessLevel.GameMaster, OnBotInspect);
        CommandSystem.Register("BotStats", AccessLevel.GameMaster, OnBotStats);
        CommandSystem.Register("ClearBots", AccessLevel.Administrator, OnClearBots);
    }

    /// <summary>After the nav graphs (priority 60): bots plan routes from their first thought.</summary>
    [CallPriority(70)]
    public static void Initialize()
    {
        WorldCatalog.Rebuild();
        HuntingAtlas.Rebuild();
        ShrineAtlas.Rebuild();
        FarmAtlas.Rebuild();

        // Field seeding and crop rotation add and retire tiles over time.
        Timer.StartTimer(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10), FarmAtlas.Rebuild);
        BotScheduler.Start();
        _initialized = true;

        foreach (var bot in _loaded)
        {
            if (!bot.Deleted)
            {
                Register(bot, bot.Location, bot.Map, null);
            }
        }

        _loaded.Clear();

        logger.Information(
            "Bots: {Bots} bots, {Cities} towns known, {Shrines} ankh spots",
            _bots.Count,
            WorldCatalog.Cities.Count,
            ShrineAtlas.Count
        );
    }

    internal static void OnBotLoaded(BotMobile bot)
    {
        if (_initialized)
        {
            Register(bot, bot.Location, bot.Map, null);
        }
        else
        {
            _loaded.Add(bot);
        }
    }

    public static void Register(BotMobile bot, Point3D home, Map map, string homeCity)
    {
        if (bot.Deleted || !_bots.Add(bot))
        {
            return;
        }

        if (bot.Brain == null)
        {
            homeCity ??= WorldCatalog.FindNearest(map, home)?.Name;
            bot.Brain = new BotBrain(bot, home, map, homeCity);
        }

        bot.Brain.Registered = true;

        if (bot.Brain.OwnerBeacon is Items.MahaonBotBeacon { Deleted: false } beacon)
        {
            beacon.Claim(bot);
        }

        // Spread first thoughts so a freshly loaded world doesn't think all at once.
        BotScheduler.Schedule(bot.Brain, Utility.Random(3000));
    }

    /// <summary>Hands a player's own character to the AI. The brain lives in memory only: a
    /// restart gives the character back.</summary>
    public static BotBrain Possess(PlayerMobile player)
    {
        if (player is BotMobile || player.Deleted)
        {
            return null;
        }

        if (_possessed.TryGetValue(player, out var existing))
        {
            return existing;
        }

        var brain = new BotBrain(player, player.Location, player.Map, WorldCatalog.FindNearest(player.Map, player.Location)?.Name)
        {
            Registered = true
        };

        _possessed[player] = brain;
        _bots.Add(player);
        BotScheduler.Schedule(brain, 500);
        return brain;
    }

    public static void Unregister(PlayerMobile bot)
    {
        if (bot == null || !_bots.Remove(bot))
        {
            return;
        }

        var brain = bot.GetBrain();
        _possessed.Remove(bot);

        if (brain != null)
        {
            brain.Group?.Remove(bot);
            brain.ClearPlan();
            brain.Registered = false;
        }

        BotSpeech.Forget(bot);
    }

    [Usage("BotInspect")]
    [Description("Shows a bot's needs, character, goal scores and plan.")]
    private static void OnBotInspect(CommandEventArgs e)
    {
        e.Mobile.SendMessage("Target a bot.");
        e.Mobile.BeginTarget(
            -1,
            false,
            TargetFlags.None,
            (from, targeted) =>
            {
                if (targeted is PlayerMobile pm && pm.GetBrain() != null)
                {
                    BotInspectGump.DisplayTo(from, pm);
                }
                else
                {
                    from.SendMessage("That is not a bot.");
                }
            }
        );
    }

    [Usage("ClearBots")]
    [Description("Deletes every spawned bot. Possessed players are released, not deleted.")]
    private static void OnClearBots(CommandEventArgs e)
    {
        var deleted = 0;
        foreach (var bot in new List<PlayerMobile>(_bots))
        {
            if (bot is BotMobile)
            {
                bot.Delete();
                deleted++;
            }
            else
            {
                Unregister(bot);
            }
        }

        e.Mobile.SendMessage($"Deleted {deleted} bots.");
    }

    [Usage("BotStats")]
    [Description("Bot scheduler figures: population, thinks, time per think, budget overruns.")]
    private static void OnBotStats(CommandEventArgs e)
    {
        var thinks = BotScheduler.Thinks;
        var avg = thinks == 0 ? 0 : BotScheduler.ThinkMsTotal / thinks;
        var dead = 0;
        var goals = new Dictionary<string, int>();

        foreach (var bot in _bots)
        {
            if (!bot.Alive)
            {
                dead++;
            }

            var name = bot.GetBrain()?.Goal?.Name ?? "—";
            goals[name] = goals.GetValueOrDefault(name) + 1;
        }

        var m = e.Mobile;
        m.SendMessage($"Bots: {_bots.Count} ({dead} dead), queue {BotScheduler.QueueLength}.");
        m.SendMessage(
            $"Thinks: {thinks}, avg {avg:F3} ms, max {BotScheduler.ThinkMsMax:F2} ms, total {BotScheduler.ThinkMsTotal:F0} ms, budget overruns {BotScheduler.BudgetOverruns}."
        );

        foreach (var (goal, count) in goals)
        {
            m.SendMessage($"  {goal}: {count}");
        }

        m.SendMessage($"Nav links: {NavLinks.All.Count}. Stats reset.");
        BotScheduler.ResetStats();
    }
}
