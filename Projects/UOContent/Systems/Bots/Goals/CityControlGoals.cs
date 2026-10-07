using System.Collections.Generic;
using Server.Commands;
using Server.Guilds;
using Server.Mobiles;
using Server.Systems.MahaonBots;
using Server.Systems.MahaonCities;

namespace Server.Systems.Bots;

public partial class BotBrain
{
    // A claim the city system refused (a guard still alive somewhere out of sight): not retried
    // until this tick.
    internal string FailedClaimCity;
    internal long FailedClaimUntil;
}

/// <summary>
/// What bots know of the cities guilds fight over (<see cref="CityControlSystem"/>): who holds a
/// city, how many of its guards still stand and whether one is in a fight. Read with a spatial
/// query around the city centre, remembered for a few seconds so a crowd of bots asking costs one
/// scan.
/// </summary>
public static class BotCityControl
{
    // Guard posts sit up to ~22 tiles from the centre and patrol 20 further; the same reach the
    // website snapshot counts guards in.
    public const int GuardScanRadius = 48;

    private const long StateTtlMs = 5000;

    private static readonly Dictionary<string, (int guards, Mobile attacker, long at)> _state = new();

    public static bool Enabled { get; private set; } = true;

    /// <summary>How many cities one guild of bots may hold.</summary>
    public static int MaxPerGuild { get; private set; } = 1;

    /// <summary>Members a bot guild needs before it reaches for a city.</summary>
    public static int MinGuildSize { get; private set; } = 5;

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("bots.cityControl.enabled", true);
        MaxPerGuild = ServerConfiguration.GetOrUpdateSetting("bots.cityControl.maxPerGuild", 1);
        MinGuildSize = ServerConfiguration.GetOrUpdateSetting("bots.cityControl.minGuildSize", 5);
    }

    /// <summary>Living guards of the city and one mobile fighting them, if any.</summary>
    public static (int guards, Mobile attacker) StateOf(string city)
    {
        var now = Core.TickCount;
        if (_state.TryGetValue(city, out var known) && now - known.at < StateTtlMs)
        {
            return (known.guards, known.attacker);
        }

        var guards = 0;
        Mobile attacker = null;

        if (CityControlSystem.Cities.TryGetValue(city, out var info) && info.map != null && info.map != Map.Internal)
        {
            foreach (var guard in info.map.GetMobilesInRange<CityGuard>(info.spawn, GuardScanRadius))
            {
                if (guard.Deleted || !guard.Alive || guard.City != city)
                {
                    continue;
                }

                guards++;
                if (attacker == null && guard.Combatant is Mobile { Alive: true } foe and not CityGuard)
                {
                    attacker = foe;
                }
            }
        }

        _state[city] = (guards, attacker, now);
        return (guards, attacker);
    }

    /// <summary>Drops what is remembered of a city, after something changed its guards.</summary>
    public static void Invalidate(string city) => _state.Remove(city);

    public static int CitiesHeldBy(Guild guild)
    {
        var held = 0;
        foreach (var city in CityControlSystem.Cities.Keys)
        {
            if (CityControlSystem.GetController(city) == guild)
            {
                held++;
            }
        }

        return held;
    }

    /// <summary>Whether two guilds are at war, by the bot relation table or the engine's own.</summary>
    public static bool AtWar(Guild a, Guild b) =>
        a != null && b != null && a != b &&
        (BotGuilds.GetRelation(a.Name, b.Name) == BotGuildRelation.War || a.Enemies.Contains(b) || b.Enemies.Contains(a));

    /// <summary>The nearest city on this map, within range, that passes the filter.</summary>
    public static string Nearest(PlayerMobile bot, int range, System.Func<string, bool> filter)
    {
        string best = null;
        var bestDist = double.MaxValue;

        foreach (var (city, info) in CityControlSystem.Cities)
        {
            if (info.map != bot.Map)
            {
                continue;
            }

            var dist = bot.GetDistanceToSqrt(info.spawn);
            if (dist <= range && dist < bestDist && filter(city))
            {
                bestDist = dist;
                best = city;
            }
        }

        return best;
    }

    public static (Point3D center, Map map) CenterOf(string city) => CityControlSystem.Cities[city];

    // City names are typed as one quoted argument: "Skara Brae" has a space.
    public static bool Command(Mobile bot, string command, string city, string arg = null) =>
        CommandSystem.Handle(bot, arg == null ? $"{CommandSystem.Prefix}{command} \"{city}\"" : $"{CommandSystem.Prefix}{command} \"{city}\" {arg}");
}

/// <summary>
/// A bot guild's leader claims a city for the guild: an unheld one, or an enemy's whose guards are
/// all down. Through the same command a player's guild leader uses, standing in the city.
/// </summary>
public sealed class ClaimCityGoal : BotGoal
{
    private const int SearchRange = 600;

    public override string Name => "Захват города";

    public override string[] News => ["Наша гильдия взяла город!", "Теперь в городе наши порядки."];

    private static string Target(BotBrain brain)
    {
        var bot = brain.Bot;
        if (!BotCityControl.Enabled || bot.Guild is not Guild guild || guild.Leader != bot ||
            guild.Members.Count < BotCityControl.MinGuildSize || BotCityControl.CitiesHeldBy(guild) >= BotCityControl.MaxPerGuild)
        {
            return null;
        }

        var now = Core.TickCount;
        return BotCityControl.Nearest(
            bot,
            SearchRange,
            city =>
            {
                if (city == brain.FailedClaimCity && now - brain.FailedClaimUntil < 0)
                {
                    return false;
                }

                var holder = CityControlSystem.GetController(city);
                return holder == null || BotCityControl.AtWar(guild, holder) && BotCityControl.StateOf(city).guards == 0;
            }
        );
    }

    public override double Score(BotBrain brain)
    {
        if (Target(brain) is not { } city)
        {
            return 0;
        }

        // An enemy city left without guards won't stay that way for long.
        return CityControlSystem.GetController(city) != null ? 0.95 : 0.45 + BotBrain.Trait(brain.Greed) * 0.3;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        if (Target(brain) is not { } city)
        {
            return null;
        }

        var (center, map) = BotCityControl.CenterOf(city);
        return [new GoToAction(map, center, 6, $"захватить {city}"), new ClaimCityAction(city)];
    }
}

public sealed class ClaimCityAction : BotAction
{
    private const long RetryMs = 30 * 60_000;

    private readonly string _city;

    public ClaimCityAction(string city) => _city = city;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        if (bot.Guild is not Guild guild)
        {
            return BotActionResult.Failed();
        }

        BotCityControl.Command(bot, "ClaimCity", _city);

        if (CityControlSystem.GetController(_city) != guild)
        {
            brain.FailedClaimCity = _city;
            brain.FailedClaimUntil = Core.TickCount + RetryMs;
            return BotActionResult.Failed();
        }

        // Capturing replaced the city's guards.
        BotCityControl.Invalidate(_city);

        // A greedy leader taxes harder.
        var tax = brain.Greed * 30 / 100;
        BotCityControl.Command(bot, "SetCityTax", _city, tax.ToString());

        BotSpeech.SayText(bot, $"Отныне {_city} под защитой гильдии {guild.Name}!");
        BotRumors.Spread($"Гильдия {guild.Name} взяла {_city}.");
        return BotActionResult.Done(2000);
    }

    public override string Describe(BotBrain brain) => $"Захватывает {_city}";
}

/// <summary>
/// Fighters of a guild at war with a city's holder go and cut its guards down, so their leader
/// can take it. Only a guild big enough to have a chance tries.
/// </summary>
public sealed class SiegeCityGoal : BotGoal
{
    private const int SearchRange = 400;
    private const int MinFightingSkill = 60;

    public override string Name => "Осада города";

    public override string[] News => ["Ходили на осаду, стража там крепкая.", "Ещё немного, и город будет наш."];

    private static string Target(PlayerMobile bot)
    {
        if (!BotCityControl.Enabled || bot.Guild is not Guild guild || guild.Members.Count < BotCityControl.MinGuildSize ||
            BotCombatStyles.FightingSkill(bot) < MinFightingSkill || bot.Hits < bot.HitsMax * 0.8 ||
            BotCityControl.CitiesHeldBy(guild) >= BotCityControl.MaxPerGuild)
        {
            return null;
        }

        return BotCityControl.Nearest(
            bot,
            SearchRange,
            city => BotCityControl.AtWar(guild, CityControlSystem.GetController(city)) && BotCityControl.StateOf(city).guards > 0
        );
    }

    public override double Score(BotBrain brain) =>
        Target(brain.Bot) == null ? 0 : 0.35 + BotBrain.Trait((byte)(100 - brain.Caution)) * 0.3;

    public override List<BotAction> Plan(BotBrain brain)
    {
        if (Target(brain.Bot) is not { } city)
        {
            return null;
        }

        var (center, map) = BotCityControl.CenterOf(city);
        BotSpeech.SayText(brain.Bot, $"На {city}! Возьмём его!");
        return [new GoToAction(map, center, 10, $"на осаду {city}"), new CityFightAction(city, true)];
    }
}

/// <summary>
/// The holding guild's members come running when their city's guards are attacked.
/// </summary>
public sealed class DefendCityGoal : BotGoal
{
    private const int CallRange = 300;

    public override string Name => "Защита своего города";

    public override string[] News => ["Отстояли наш город от чужаков.", "Сунулись к нам в город — пожалели."];

    private static string Target(PlayerMobile bot)
    {
        if (bot.Guild is not Guild guild || BotCombatStyles.FightingSkill(bot) < 40 || bot.Hits < bot.HitsMax * 0.6)
        {
            return null;
        }

        return BotCityControl.Nearest(
            bot,
            CallRange,
            city => CityControlSystem.GetController(city) == guild && BotCityControl.StateOf(city).attacker != null
        );
    }

    public override double Score(BotBrain brain) =>
        Target(brain.Bot) == null ? 0 : 0.85 + BotBrain.Trait((byte)(100 - brain.Caution)) * 0.3;

    public override List<BotAction> Plan(BotBrain brain)
    {
        if (Target(brain.Bot) is not { } city)
        {
            return null;
        }

        var (center, map) = BotCityControl.CenterOf(city);
        BotSpeech.SayText(brain.Bot, $"На наш {city} напали! Все туда!");
        return [new GoToAction(map, center, 10, $"на защиту {city}"), new CityFightAction(city, false)];
    }
}

/// <summary>Fights at a city: its guards when besieging, whoever attacks them when defending.
/// Ends when there is no one left to fight there.</summary>
public sealed class CityFightAction : BotAction
{
    private const long MaxMs = 20 * 60_000;
    private const int GuardSight = 20;

    private readonly string _city;
    private readonly bool _siege;
    private long _until;
    private BotAction _step;

    public CityFightAction(string city, bool siege)
    {
        _city = city;
        _siege = siege;
    }

    public override void Start(BotBrain brain) => _until = Core.TickCount + MaxMs;

    public override BotActionResult Tick(BotBrain brain)
    {
        if (Core.TickCount - _until >= 0)
        {
            return BotActionResult.Done();
        }

        if (_step != null)
        {
            var result = _step.Tick(brain);
            if (result.Status == BotActionStatus.Running)
            {
                return result;
            }

            _step.Stop(brain);
            _step = null;
        }

        if (brain.Combat.Opponent is { Alive: true, Deleted: false })
        {
            return BotActionResult.Running(1000);
        }

        var bot = brain.Bot;
        var foe = _siege ? NearestGuard(bot) : BotCityControl.StateOf(_city).attacker;
        if (foe == null)
        {
            return BotActionResult.Done(2000);
        }

        // The reflexes only take on a foe within reach; walk up to one further off first.
        if (!bot.InRange(foe, GuardSight))
        {
            _step = new GoToAction(foe, 4, _siege ? "к страже" : "к нападающим");
            _step.Start(brain);
            return BotActionResult.Running(250);
        }

        BotCombat.Engage(brain, foe);
        return BotActionResult.Running(1000);
    }

    public override void Stop(BotBrain brain) => _step?.Stop(brain);

    private Mobile NearestGuard(PlayerMobile bot)
    {
        Mobile best = null;
        var bestDist = double.MaxValue;

        foreach (var guard in bot.Map.GetMobilesInRange<CityGuard>(bot.Location, GuardSight))
        {
            if (guard.Alive && !guard.Deleted && guard.City == _city)
            {
                var dist = bot.GetDistanceToSqrt(guard);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = guard;
                }
            }
        }

        return best ?? FindAnyGuard();
    }

    private Mobile FindAnyGuard()
    {
        var (center, map) = BotCityControl.CenterOf(_city);
        foreach (var guard in map.GetMobilesInRange<CityGuard>(center, BotCityControl.GuardScanRadius))
        {
            if (guard.Alive && !guard.Deleted && guard.City == _city)
            {
                return guard;
            }
        }

        return null;
    }

    public override string Describe(BotBrain brain) => _siege ? $"Осаждает {_city}" : $"Защищает {_city}";
}
