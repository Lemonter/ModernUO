using System.Collections.Generic;
using Server.Commands;
using Server.Guilds;
using Server.Mobiles;
using Server.Systems.MahaonBots;
using Server.Systems.MahaonCities;

namespace Server.Systems.Bots;

public partial class BotBrain
{
    // A claim that failed — the banner was lost: not tried again until this tick.
    internal string FailedClaimCity;
    internal long FailedClaimUntil;
}

/// <summary>
/// What bots know of the cities guilds fight over (<see cref="CityControlSystem"/>): who holds a
/// city, how many of its guards still stand and whether one is in a fight. Read from the city's
/// guard roster, remembered for a few seconds so a crowd of bots asking costs one pass.
/// </summary>
public static class BotCityControl
{
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

        foreach (var guard in CityGuard.Of(city))
        {
            if (guard.Deleted || !guard.Alive)
            {
                continue;
            }

            guards++;
            if (attacker == null && guard.Combatant is Mobile { Alive: true } foe and not CityGuard)
            {
                attacker = foe;
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
/// A bot guild's leader lays claim to a city at its banner: an unheld city, or one held by a guild
/// it is at war with. Through the same command a player's guild leader uses, standing by the banner;
/// then it holds the banner with its guild for the ten minutes the claim takes.
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
            guild.Members.Count < BotCityControl.MinGuildSize || BotCityControl.CitiesHeldBy(guild) >= BotCityControl.MaxPerGuild ||
            bot.Hits < bot.HitsMax * 0.8)
        {
            return null;
        }

        var now = Core.TickCount;
        return BotCityControl.Nearest(
            bot,
            SearchRange,
            city =>
            {
                if (city == brain.FailedClaimCity && now - brain.FailedClaimUntil < 0 ||
                    Items.MahaonCityClaimPoint.Of(city) is not { Contested: false } point || point.Map != bot.Map)
                {
                    return false;
                }

                var holder = CityControlSystem.GetController(city);
                return holder == null || BotCityControl.AtWar(guild, holder);
            }
        );
    }

    public override double Score(BotBrain brain) =>
        Target(brain) == null ? 0 : 0.45 + BotBrain.Trait(brain.Greed) * 0.3 + BotBrain.Trait((byte)(100 - brain.Caution)) * 0.1;

    public override List<BotAction> Plan(BotBrain brain)
    {
        if (Target(brain) is not { } city || Items.MahaonCityClaimPoint.Of(city) is not { } point)
        {
            return null;
        }

        return
        [
            new GoToAction(point.Map, point.Location, 2, $"к знамени {city}"),
            new ClaimCityAction(city),
            new BannerFightAction(city, false)
        ];
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

        if (Items.MahaonCityClaimPoint.Of(_city) is not { Contested: true } point || point.Contender != guild)
        {
            brain.FailedClaimCity = _city;
            brain.FailedClaimUntil = Core.TickCount + RetryMs;
            return BotActionResult.Failed();
        }

        BotSpeech.SayText(bot, $"Этот город будет нашим! Гильдия {guild.Name}, к знамени!");
        return BotActionResult.Done(1000);
    }

    public override string Describe(BotBrain brain) => $"Заявляет права на {_city}";
}

/// <summary>
/// A claim laid by the bot's guild: its members come to the banner and hold it, fighting the
/// guards and the holders who come to take it back.
/// </summary>
public sealed class SiegeCityGoal : BotGoal
{
    private const int SearchRange = 400;
    private const int MinFightingSkill = 40;

    public override string Name => "Удержание знамени";

    public override string[] News => ["Держали знамя до последнего!", "Стража лезла со всех сторон, но мы выстояли."];

    private static string Target(PlayerMobile bot)
    {
        if (bot.Guild is not Guild guild || BotCombatStyles.FightingSkill(bot) < MinFightingSkill || bot.Hits < bot.HitsMax * 0.6)
        {
            return null;
        }

        return BotCityControl.Nearest(
            bot,
            SearchRange,
            city => Items.MahaonCityClaimPoint.Of(city) is { Contested: true } point && point.Contender == guild
        );
    }

    public override double Score(BotBrain brain) =>
        Target(brain.Bot) == null ? 0 : 0.8 + BotBrain.Trait((byte)(100 - brain.Caution)) * 0.3;

    public override List<BotAction> Plan(BotBrain brain)
    {
        if (Target(brain.Bot) is not { } city || Items.MahaonCityClaimPoint.Of(city) is not { } point)
        {
            return null;
        }

        BotSpeech.SayText(brain.Bot, $"Держим знамя в {city}!");
        return [new GoToAction(point.Map, point.Location, 4, $"к знамени {city}"), new BannerFightAction(city, false)];
    }
}

/// <summary>
/// The holding guild's members come running when their city is claimed at its banner or its guards
/// are attacked.
/// </summary>
public sealed class DefendCityGoal : BotGoal
{
    private const int CallRange = 300;

    public override string Name => "Защита своего города";

    public override string[] News => ["Отстояли наш город от чужаков.", "Сунулись к нам в город — пожалели."];

    private static string Target(PlayerMobile bot, out bool banner)
    {
        banner = false;
        if (bot.Guild is not Guild guild || BotCombatStyles.FightingSkill(bot) < 40 || bot.Hits < bot.HitsMax * 0.6)
        {
            return null;
        }

        var claimed = BotCityControl.Nearest(
            bot,
            CallRange,
            city => CityControlSystem.GetController(city) == guild && Items.MahaonCityClaimPoint.Of(city) is { Contested: true }
        );

        if (claimed != null)
        {
            banner = true;
            return claimed;
        }

        return BotCityControl.Nearest(
            bot,
            CallRange,
            city => CityControlSystem.GetController(city) == guild && BotCityControl.StateOf(city).attacker != null
        );
    }

    public override double Score(BotBrain brain) =>
        Target(brain.Bot, out _) == null ? 0 : 0.85 + BotBrain.Trait((byte)(100 - brain.Caution)) * 0.3;

    public override List<BotAction> Plan(BotBrain brain)
    {
        if (Target(brain.Bot, out var banner) is not { } city)
        {
            return null;
        }

        if (banner && Items.MahaonCityClaimPoint.Of(city) is { } point)
        {
            BotSpeech.SayText(brain.Bot, $"Наш {city} оспаривают! Все к знамени!");
            return [new GoToAction(point.Map, point.Location, 6, $"к знамени {city}"), new BannerFightAction(city, true)];
        }

        var (center, map) = BotCityControl.CenterOf(city);
        BotSpeech.SayText(brain.Bot, $"На наш {city} напали! Все туда!");
        return [new GoToAction(map, center, 10, $"на защиту {city}"), new CityFightAction(city)];
    }
}

/// <summary>
/// Fights at a city's banner for as long as the claim lasts: claimants fight the guards and the
/// holders, holders fight the claimants. A claimant that walks away from the banner loses it, so
/// the bot stays near and only steps out to a foe close by. When the claim ends in the guild's
/// favour, the leader sets the city's tax.
/// </summary>
public sealed class BannerFightAction : BotAction
{
    private const long MaxMs = 15 * 60_000;

    private readonly string _city;
    private readonly bool _defending;
    private BotAction _step;
    private long _until;

    public BannerFightAction(string city, bool defending)
    {
        _city = city;
        _defending = defending;
    }

    public override void Start(BotBrain brain) => _until = Core.TickCount + MaxMs;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;
        var point = Items.MahaonCityClaimPoint.Of(_city);

        if (point is not { Contested: true } || Core.TickCount - _until >= 0)
        {
            Settle(brain);
            return BotActionResult.Done(1000);
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

        if (brain.Combat.Opponent is { Alive: true, Deleted: false } opponent &&
            opponent.InRange(point.Location, Items.MahaonCityClaimPoint.HoldRange + 4))
        {
            return BotActionResult.Running(1000);
        }

        if (!bot.InRange(point.Location, Items.MahaonCityClaimPoint.HoldRange - 4))
        {
            _step = new GoToAction(point.Map, point.Location, 3, "к знамени");
            _step.Start(brain);
            return BotActionResult.Running(250);
        }

        if (Foe(bot, point) is { } foe)
        {
            BotCombat.Engage(brain, foe);
        }

        return BotActionResult.Running(1000);
    }

    private Mobile Foe(PlayerMobile bot, Items.MahaonCityClaimPoint point)
    {
        Mobile best = null;
        var bestDist = double.MaxValue;
        var holder = CityControlSystem.GetController(_city);

        foreach (var m in point.Map.GetMobilesInRange<Mobile>(point.Location, Items.MahaonCityClaimPoint.HoldRange + 4))
        {
            if (!m.Alive || m == bot || m.Hidden)
            {
                continue;
            }

            var isFoe = _defending
                ? m is PlayerMobile && m.Guild == point.Contender
                : m is CityGuard guard && guard.City == _city || m is PlayerMobile && holder != null && m.Guild == holder;

            if (!isFoe)
            {
                continue;
            }

            var dist = bot.GetDistanceToSqrt(m);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = m;
            }
        }

        return best;
    }

    // A won claim: the leader sets the tax, as greedy as it is. A lost one is not tried again soon.
    private void Settle(BotBrain brain)
    {
        var bot = brain.Bot;
        if (_defending || bot.Guild is not Guild guild || guild.Leader != bot)
        {
            return;
        }

        if (CityControlSystem.GetController(_city) != guild)
        {
            brain.FailedClaimCity = _city;
            brain.FailedClaimUntil = Core.TickCount + 30 * 60_000;
            return;
        }

        BotCityControl.Invalidate(_city);
        BotCityControl.Command(bot, "SetCityTax", _city, (brain.Greed * 30 / 100).ToString());
        BotSpeech.SayText(bot, $"Отныне {_city} под защитой гильдии {guild.Name}!");
    }

    public override void Stop(BotBrain brain) => _step?.Stop(brain);

    public override string Describe(BotBrain brain) => _defending ? $"Защищает знамя {_city}" : $"Держит знамя {_city}";
}

/// <summary>Fights whoever attacks the city's guards, until no one does.</summary>
public sealed class CityFightAction : BotAction
{
    private const long MaxMs = 20 * 60_000;
    private const int Reach = 20;

    private readonly string _city;
    private long _until;
    private BotAction _step;

    public CityFightAction(string city) => _city = city;

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
        if (BotCityControl.StateOf(_city).attacker is not { } foe)
        {
            return BotActionResult.Done(2000);
        }

        // The reflexes only take on a foe within reach; walk up to one further off first.
        if (!bot.InRange(foe, Reach))
        {
            _step = new GoToAction(foe, 4, "к нападающим");
            _step.Start(brain);
            return BotActionResult.Running(250);
        }

        BotCombat.Engage(brain, foe);
        return BotActionResult.Running(1000);
    }

    public override void Stop(BotBrain brain) => _step?.Stop(brain);

    public override string Describe(BotBrain brain) => $"Защищает {_city}";
}
