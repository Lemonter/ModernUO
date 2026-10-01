using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Engines.Pathing.Nav;
using Server.Logging;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Systems.Bots;

public enum BotEngine
{
    V1,
    V2
}

/// <summary>
/// Entry point of the v2 bots: which engine drives bots, registration, startup and the admin
/// commands. While v2 grows to cover everything v1 does, both engines exist side by side and
/// [BotEngine switches every bot between them live.
///
///   [BotEngine v1|v2 — which engine drives the bots (persisted).
///   [BotInspect      — a bot's needs, character, goal, plan and why.
///   [BotStats        — scheduler and population figures.
/// </summary>
public static class BotSystem
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSystem));

    private const string EngineSetting = "bots.engine";

    private static readonly HashSet<BotMobile> _bots = [];
    private static readonly List<BotMobile> _loaded = [];
    private static bool _initialized;

    public static BotEngine Engine { get; private set; } = BotEngine.V1;

    public static int Count => _bots.Count;

    public static IReadOnlyCollection<BotMobile> Bots => _bots;

    public static bool IsV2(BotMobile bot) => bot.Brain is { Registered: true };

    public static void Configure()
    {
        Engine = ServerConfiguration.GetOrUpdateSetting(EngineSetting, "v1")
            .Equals("v2", StringComparison.OrdinalIgnoreCase)
            ? BotEngine.V2
            : BotEngine.V1;

        BotScheduler.BudgetMs = ServerConfiguration.GetOrUpdateSetting("bots.thinkBudgetMs", 4.0);

        CommandSystem.Register("BotEngine", AccessLevel.Administrator, OnBotEngine);
        CommandSystem.Register("BotInspect", AccessLevel.GameMaster, OnBotInspect);
        CommandSystem.Register("BotStats", AccessLevel.GameMaster, OnBotStats);
    }

    /// <summary>After the nav graphs (priority 60): bots plan routes from their first thought.</summary>
    [CallPriority(70)]
    public static void Initialize()
    {
        WorldCatalog.Rebuild();
        HuntingAtlas.Rebuild();
        BotScheduler.Start();
        _initialized = true;

        if (Engine == BotEngine.V2)
        {
            foreach (var bot in _loaded)
            {
                if (!bot.Deleted)
                {
                    Register(bot, bot.Location, bot.Map, null);
                }
            }
        }

        _loaded.Clear();

        logger.Information(
            "Bots: engine {Engine}, {Cities} towns known, {Bots} bots on v2",
            Engine,
            WorldCatalog.Cities.Count,
            _bots.Count
        );
    }

    internal static void OnBotLoaded(BotMobile bot)
    {
        if (!_initialized)
        {
            _loaded.Add(bot);
        }
    }

    /// <summary>The one place new bots join an engine: the beacon and any other spawner call this
    /// instead of an engine directly.</summary>
    public static void RegisterNew(BotMobile bot, Point3D home, Map map, string homeCity)
    {
        if (Engine == BotEngine.V2)
        {
            Register(bot, home, map, homeCity);
        }
        else
        {
            MahaonBots.BotController.RegisterBot(bot, home, map, homeCity);
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

    public static void Unregister(BotMobile bot)
    {
        if (bot == null || !_bots.Remove(bot))
        {
            return;
        }

        if (bot.Brain != null)
        {
            bot.Brain.Group?.Remove(bot);
            bot.Brain.ClearPlan();
            bot.Brain.Registered = false;
        }

        BotSpeech.Forget(bot);
    }

    private static void SwitchEngine(BotEngine engine)
    {
        if (engine == Engine)
        {
            return;
        }

        Engine = engine;
        ServerConfiguration.SetSetting(EngineSetting, engine == BotEngine.V2 ? "v2" : "v1");

        // Every bot the beacons own, whichever engine had it, moves to the new one.
        var all = new List<BotMobile>(_bots);
        foreach (var bot in MahaonBots.BotController.RegisteredBotMobiles())
        {
            if (!_bots.Contains(bot))
            {
                all.Add(bot);
            }
        }

        foreach (var bot in all)
        {
            var home = bot.Brain?.Home ?? bot.Location;
            var map = bot.Brain?.HomeMap ?? bot.Map;

            if (engine == BotEngine.V2)
            {
                MahaonBots.BotController.UnregisterBot(bot);
                Register(bot, home, map, bot.Brain?.HomeCity);
            }
            else
            {
                Unregister(bot);
                MahaonBots.BotController.RegisterBot(bot, home, map, bot.Brain?.HomeCity);
            }
        }
    }

    [Usage("BotEngine [v1|v2]")]
    [Description("Shows or switches the engine that drives the bots.")]
    private static void OnBotEngine(CommandEventArgs e)
    {
        if (e.Length > 0)
        {
            var arg = e.GetString(0);
            if (arg.Equals("v2", StringComparison.OrdinalIgnoreCase))
            {
                SwitchEngine(BotEngine.V2);
            }
            else if (arg.Equals("v1", StringComparison.OrdinalIgnoreCase))
            {
                SwitchEngine(BotEngine.V1);
            }
        }

        e.Mobile.SendMessage($"Bot engine: {Engine}. Bots on v2: {_bots.Count}.");
    }

    [Usage("BotInspect")]
    [Description("Shows a v2 bot's needs, character, goal scores and plan.")]
    private static void OnBotInspect(CommandEventArgs e)
    {
        e.Mobile.SendMessage("Target a bot.");
        e.Mobile.BeginTarget(
            -1,
            false,
            TargetFlags.None,
            (from, targeted) =>
            {
                if (targeted is BotMobile { Brain: not null } bot)
                {
                    BotInspectGump.DisplayTo(from, bot);
                }
                else
                {
                    from.SendMessage("That is not a v2 bot.");
                }
            }
        );
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

            var name = bot.Brain?.Goal?.Name ?? "—";
            goals[name] = goals.GetValueOrDefault(name) + 1;
        }

        var m = e.Mobile;
        m.SendMessage($"Bots v2: {_bots.Count} ({dead} dead), queue {BotScheduler.QueueLength}.");
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
