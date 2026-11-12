using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Engines.Pathing.Nav;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonCities;

namespace Server.Systems.MahaonRaids;

/// <summary>Чьи приспешники идут в набег. Сила у каждой стороны своя.</summary>
public enum RaidFaction
{
    Pirates,
    Necromancer,
    Horde
}

public enum RaidPhase
{
    /// <summary>Отряд идёт от лагеря к воротам.</summary>
    March,

    /// <summary>У ворот бой со стражей — пять минут.</summary>
    Battle,

    /// <summary>Стража не удержала: уцелевшие рвутся к банку.</summary>
    Breakthrough,

    Over
}

/// <summary>
///     Набег целиком: отряд появляется в лагере за городом, идёт по навигации к воротам, пять
///     минут бьётся там со стражей (урон в этой драке — единица и никого не добивает: решают
///     игроки), потом уцелевшие идут к банку и взрывают его. Отряд, выбитый до банка, —
///     набег отбит, сторона слабеет; взорванный банк — сторона крепнет и в следующий раз
///     приводит больше.
/// </summary>
public sealed class MahaonRaid
{
    // Сколько длится бой у ворот, прежде чем уцелевшие идут на прорыв.
    public static TimeSpan BattleDuration = TimeSpan.FromMinutes(5);

    // Отряд так и не дошёл до ворот — бой начинается всё равно, где бы он ни был.
    private static readonly TimeSpan MaxMarch = TimeSpan.FromMinutes(8);

    // Набег, который за это время ничем не кончился, распускается без последствий.
    private static readonly TimeSpan MaxDuration = TimeSpan.FromMinutes(30);

    private const int GateRange = 6;
    private const int BankRange = 3;

    public readonly RaidSite Site;
    public readonly RaidApproach Approach;
    public readonly RaidFaction Faction;
    public readonly Map Map;

    public RaidPhase Phase { get; private set; } = RaidPhase.March;

    /// <summary>Сколько длится бой у ворот этого набега (ГМ может укоротить для проверки).</summary>
    public TimeSpan Battle = BattleDuration;
    public DateTime StartedAt { get; } = Core.Now;
    public DateTime BattleAt { get; private set; }

    public readonly Dictionary<BaseCreature, Raider> Raiders = new();
    public readonly List<BaseCreature> Defenders = [];

    // Общий путь фазы: от лагеря к воротам, от ворот к банку. Отставшие ищут свой.
    private List<Point3D> _sharedRoute;
    private Point3D _sharedFrom;
    private int _routeRequest;

    public sealed class Raider
    {
        public List<Point3D> Route;
        public int Index;
        public IPoint3D Goal; // упакованная точка: PathFollower сравнивает цель по ссылке
        public int Stuck;
        public bool Routing;
        public bool Arrived;
    }

    public MahaonRaid(RaidSite site, RaidApproach approach, RaidFaction faction)
    {
        Site = site;
        Approach = approach;
        Faction = faction;
        Map = site.Map;
    }

    public Point3D Target => Phase == RaidPhase.Breakthrough ? Site.Bank : Approach.Gate;

    public int Alive
    {
        get
        {
            var n = 0;
            foreach (var bc in Raiders.Keys)
            {
                if (!bc.Deleted && bc.Alive)
                {
                    n++;
                }
            }

            return n;
        }
    }

    // ---- начало ---------------------------------------------------------------------------

    public int Spawn(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var creature = RaidDirector.CreateRaider(Faction, i);
            var at = Near(Approach.Spawn, 4);
            creature.Home = at;
            creature.RangeHome = 10;
            creature.MoveToWorld(at, Map);
            Raiders[creature] = new Raider();
        }

        // Стража встречает у ворот: защитники на время набега и городская стража, если город чей-то.
        var defenders = Math.Clamp(count * 2 / 3, 4, 12);
        for (var i = 0; i < defenders; i++)
        {
            BaseCreature d = i % 3 == 2 ? new RaidGateDefenderMage() : new RaidGateDefender();
            d.Home = Approach.Gate;
            d.RangeHome = 5;
            d.MoveToWorld(Near(Approach.Gate, 3), Map);
            Defenders.Add(d);
        }

        foreach (var guard in CityGuard.Of(Site.City))
        {
            if (guard.Alive && !guard.Deleted && guard.Map == Map)
            {
                guard.RallyTo(Approach.Gate);
            }
        }

        RequestSharedRoute(Approach.Spawn);
        return Raiders.Count;
    }

    private Point3D Near(Point3D center, int radius)
    {
        for (var attempt = 0; attempt < 15; attempt++)
        {
            var x = center.X + Utility.RandomMinMax(-radius, radius);
            var y = center.Y + Utility.RandomMinMax(-radius, radius);
            var p = new Point3D(x, y, Map.GetAverageZ(x, y));
            if (Map.CanSpawnMobile(p))
            {
                return p;
            }
        }

        return center;
    }

    // ---- ход набега (раз в секунду) -------------------------------------------------------

    /// <summary>Возвращает false, когда набег кончился.</summary>
    public bool Tick()
    {
        if (Phase == RaidPhase.Over)
        {
            return false;
        }

        if (Alive == 0)
        {
            RaidDirector.Repelled(this);
            return false;
        }

        if (Core.Now - StartedAt > MaxDuration)
        {
            RaidDirector.Dissolved(this);
            return false;
        }

        LeashChases();

        switch (Phase)
        {
            case RaidPhase.March:
                {
                    // Бой — у ворот или там, где отряд первым сцепился с городской стражей.
                    if (AnyRaiderNear(Approach.Gate, GateRange) || AnyRaiderFightingGuards() || Core.Now - StartedAt > MaxMarch)
                    {
                        Phase = RaidPhase.Battle;
                        BattleAt = Core.Now;
                        RaidDirector.Announce(0x22, $"У ворот {Site.Genitive} бой! Стража сдерживает приспешников {RaidDirector.FactionGenitive(Faction)}.");
                    }

                    break;
                }
            case RaidPhase.Battle:
                {
                    if (Core.Now - BattleAt >= Battle)
                    {
                        StartBreakthrough();
                    }

                    break;
                }
            case RaidPhase.Breakthrough:
                {
                    if (AnyRaiderNear(Site.Bank, BankRange))
                    {
                        RaidDirector.BankDestroyed(this);
                        return false;
                    }

                    break;
                }
        }

        return true;
    }

    // За убегающим (горожане, боты) налётчик далеко не гонится: идёт он к воротам и к банку.
    private void LeashChases()
    {
        var leash = Phase == RaidPhase.Breakthrough ? 6 : 14;
        foreach (var bc in Raiders.Keys)
        {
            if (bc.Combatant is { } c && (c.Map != bc.Map || !bc.InRange(c, leash)))
            {
                bc.Combatant = null;
                bc.Warmode = false;
            }
        }
    }

    private bool AnyRaiderFightingGuards()
    {
        foreach (var bc in Raiders.Keys)
        {
            if (!bc.Deleted && bc.Alive && bc.Combatant is IMahaonTownDefender)
            {
                return true;
            }
        }

        return false;
    }

    private bool AnyRaiderNear(Point3D p, int range)
    {
        foreach (var bc in Raiders.Keys)
        {
            if (!bc.Deleted && bc.Alive && bc.Map == Map && bc.InRange(p, range))
            {
                return true;
            }
        }

        return false;
    }

    private void StartBreakthrough()
    {
        Phase = RaidPhase.Breakthrough;

        // Защитники у ворот смяты, городская стража уходит на посты; драка с ними кончилась.
        foreach (var d in Defenders)
        {
            if (!d.Deleted)
            {
                d.FixedParticles(0x3728, 10, 15, 5042, EffectLayer.Waist);
                d.Delete();
            }
        }

        Defenders.Clear();

        foreach (var guard in CityGuard.Of(Site.City))
        {
            guard.ReturnToPost(CityControlSystem.GuardPatrolRadius);
            if (guard.Combatant is { } c && MahaonRaidCombat.IsRaidRelated(c))
            {
                guard.Combatant = null;
            }
        }

        foreach (var (bc, state) in Raiders)
        {
            if (bc.Combatant is IMahaonTownDefender)
            {
                bc.Combatant = null;
            }

            // Постоянная стража города (на спаунерах) тоже отпускает его, и счёты между ними
            // забыты — иначе первый же удар снова записал бы их в агрессоры друг другу.
            ForgetGuards(bc);

            state.Route = null;
            state.Arrived = false;
            state.Stuck = 0;
        }

        _sharedRoute = null;
        RequestSharedRoute(Approach.Gate);
        RaidDirector.Announce(0x22, $"Стража {Site.Genitive} не удержала ворота — приспешники {RaidDirector.FactionGenitive(Faction)} рвутся к банку!");
    }

    private static void ForgetGuards(BaseCreature bc)
    {
        var guards = new List<Mobile>();
        foreach (var info in bc.Aggressors)
        {
            if (info.Attacker is IMahaonTownDefender)
            {
                guards.Add(info.Attacker);
            }
        }

        foreach (var info in bc.Aggressed)
        {
            if (info.Defender is IMahaonTownDefender)
            {
                guards.Add(info.Defender);
            }
        }

        if (bc.Map != null)
        {
            foreach (var m in bc.Map.GetMobilesInRange(bc.Location, 18))
            {
                if (m is IMahaonTownDefender && m.Combatant == bc)
                {
                    guards.Add(m);
                }
            }
        }

        foreach (var g in guards)
        {
            bc.RemoveAggressor(g);
            bc.RemoveAggressed(g);
            g.RemoveAggressor(bc);
            g.RemoveAggressed(bc);
            if (g.Combatant == bc)
            {
                g.Combatant = null;
                g.Warmode = false;
            }
        }
    }

    // ---- шаг налётчика --------------------------------------------------------------------

    private void RequestSharedRoute(Point3D from)
    {
        var request = ++_routeRequest;
        var target = Target;
        _sharedFrom = from;
        NavPathfinder.FindAsync(Map, from, Map, target, route =>
        {
            if (request == _routeRequest && Phase != RaidPhase.Over)
            {
                _sharedRoute = ToPoints(route, target);
            }
        });
    }

    private List<Point3D> ToPoints(NavRoute route, Point3D target)
    {
        var points = new List<Point3D>();
        if (route != null)
        {
            foreach (var w in route.Waypoints)
            {
                if (w.Map == Map)
                {
                    points.Add(w.Location);
                }
            }
        }

        if (points.Count == 0 || points[^1] != target)
        {
            points.Add(target);
        }

        return points;
    }

    /// <summary>Шаг налётчика, когда он ни с кем не дерётся (из OnActionWander).</summary>
    public void Step(BaseCreature bc)
    {
        if (Phase == RaidPhase.Over || !Raiders.TryGetValue(bc, out var state) || bc.AIObject is not { } ai)
        {
            return;
        }

        if (state.Arrived)
        {
            return; // стоит у цели: бродит у Home сам
        }

        if (state.Route == null)
        {
            if (_sharedRoute != null && bc.InRange(_sharedFrom, 12))
            {
                state.Route = _sharedRoute;
                state.Index = 0;
                state.Goal = null;
            }
            else if (!state.Routing)
            {
                RequestOwnRoute(bc, state);
                return;
            }
            else
            {
                return;
            }
        }

        if (!ai.CanMoveNow(out _))
        {
            return;
        }

        // Пропускаем точки пути, которые уже позади.
        while (state.Index < state.Route.Count && bc.InRange(state.Route[state.Index], 2))
        {
            state.Index++;
            state.Goal = null;
        }

        if (state.Index >= state.Route.Count)
        {
            state.Arrived = true;
            bc.Home = Target;
            bc.RangeHome = Phase == RaidPhase.Breakthrough ? 1 : 4;
            return;
        }

        var next = state.Route[state.Index];
        state.Goal ??= next;

        // Пока идёт — «дом» у него под ногами, чтобы обычное блуждание не тянуло назад к лагерю.
        bc.Home = bc.Location;
        bc.RangeHome = 2;

        var before = bc.Location;
        ai.MoveToPoint(state.Goal, 1);

        if (bc.Location == before)
        {
            if (++state.Stuck > 10)
            {
                state.Stuck = 0;
                state.Route = null;
                RequestOwnRoute(bc, state);
            }
        }
        else
        {
            state.Stuck = 0;
        }
    }

    private void RequestOwnRoute(BaseCreature bc, Raider state)
    {
        state.Routing = true;
        var target = Target;
        var phase = Phase;
        NavPathfinder.FindAsync(Map, bc.Location, Map, target, route =>
        {
            state.Routing = false;
            if (bc.Deleted || Phase != phase)
            {
                return;
            }

            state.Route = ToPoints(route, target);
            state.Index = 0;
            state.Goal = null;
        });
    }

    /// <summary>Набег кончился: налётчики и защитники уходят.</summary>
    public void End(bool vanishRaiders)
    {
        Phase = RaidPhase.Over;

        foreach (var d in Defenders)
        {
            d.Delete();
        }

        Defenders.Clear();

        foreach (var guard in CityGuard.Of(Site.City))
        {
            guard.ReturnToPost(CityControlSystem.GuardPatrolRadius);
        }

        if (vanishRaiders)
        {
            foreach (var bc in Raiders.Keys)
            {
                if (!bc.Deleted)
                {
                    Effects.SendLocationParticles(
                        EffectItem.Create(bc.Location, bc.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023
                    );
                    bc.Delete();
                }
            }
        }
    }
}

/// <summary>
///     Расписание и исход набегов: кто идёт, на какой город, сколько их, и что стало с силой
///     стороны после. Сила хранится в <see cref="RaidEventSystem"/> вместе с расписанием.
/// </summary>
public static class RaidDirector
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(RaidDirector));

    // Отряд на нулевой силе и прибавка за каждую ступень силы.
    private const int BaseCount = 8;
    private const int PerStrength = 2;
    public const int MaxStrength = 10;

    private static readonly List<MahaonRaid> _active = [];
    private static readonly Dictionary<BaseCreature, MahaonRaid> _byRaider = new();
    private static Timer _timer;

    public static readonly int[] Strength = new int[3];

    public static IReadOnlyList<MahaonRaid> Active => _active;

    public static void Configure()
    {
        CommandSystem.Register("RaidStart", AccessLevel.GameMaster, RaidStart_OnCommand);
        CommandSystem.Register("RaidStop", AccessLevel.GameMaster, RaidStop_OnCommand);
        CommandSystem.Register("RaidStatus", AccessLevel.GameMaster, RaidStatus_OnCommand);
        CommandSystem.Register("RaidStrength", AccessLevel.GameMaster, RaidStrength_OnCommand);
    }

    public static void Initialize()
    {
        // Набег не переживает перезапуск: отряд и защитники от прошлого запуска — вон.
        var stale = new List<Mobile>();
        foreach (var m in World.Mobiles.Values)
        {
            if (m is IRaidSpawn or RaidGateDefender or RaidGateDefenderMage)
            {
                stale.Add(m);
            }
        }

        foreach (var m in stale)
        {
            m.Delete();
        }

        if (stale.Count > 0)
        {
            logger.Information("Raids: removed {Count} raiders and gate defenders left from the last run", stale.Count);
        }
    }

    public static string FactionGenitive(RaidFaction f) =>
        f switch { RaidFaction.Pirates => "пиратских баронов", RaidFaction.Necromancer => "некроманта", _ => "орды" };

    public static RaidFaction? ParseFaction(string s) =>
        s?.ToLowerInvariant() switch
        {
            "pirates" or "pirate" or "пираты" => RaidFaction.Pirates,
            "necro" or "necromancer" or "некромант" => RaidFaction.Necromancer,
            "horde" or "orcs" or "орда" => RaidFaction.Horde,
            _ => null
        };

    public static int CountFor(RaidFaction f) => BaseCount + PerStrength * Strength[(int)f];

    /// <summary>Налётчик своей стороны: у некроманта на каждого восьмого — лич, у орды — орки.</summary>
    public static BaseCreature CreateRaider(RaidFaction f, int i) =>
        f switch
        {
            RaidFaction.Pirates => i % 4 == 3 ? new RaidBandit { Title = "абордажник" } : new RaidPirate(),
            RaidFaction.Necromancer => i % 8 == 7 ? new RaidLich() : i % 2 == 0 ? new RaidSkeleton() : new RaidZombie(),
            _ => new RaidOrc()
        };

    public static bool IsBusy(RaidSite site) => _active.Exists(r => r.Site == site);

    /// <summary>Начинает набег. Город — с готовыми подходами; сторона и подход — случайные, если не заданы.</summary>
    public static MahaonRaid Start(RaidSite site, RaidFaction? faction = null, int approach = -1, Point3D? camp = null)
    {
        if (site == null || site.Map == null || IsBusy(site) || site.Approaches.Count == 0 && camp == null)
        {
            return null;
        }

        var f = faction ?? (RaidFaction)Utility.Random(3);
        var a = camp is { } c
            ? new RaidApproach { Spawn = c, Gate = site.Approaches.Count > 0 ? site.Approaches[0].Gate : site.Bank }
            : site.Approaches[approach >= 0 && approach < site.Approaches.Count ? approach : Utility.Random(site.Approaches.Count)];

        var raid = new MahaonRaid(site, a, f);
        var count = raid.Spawn(CountFor(f));
        foreach (var bc in raid.Raiders.Keys)
        {
            _byRaider[bc] = raid;
        }

        _active.Add(raid);
        _timer ??= Timer.DelayCall(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TickAll);

        RaidAlarm.Raise(site.Map, a.Gate, site.City);
        Announce(
            0x22,
            $"Дозорные {site.Genitive} трубят тревогу: с {SideFrom(site, a)} идут приспешники {FactionGenitive(f)} — {count} налётчиков!"
        );
        MahaonBots.BotRumors.Spread($"Набег на {site.City}! {count} налётчиков у стен.");
        MahaonAi.MahaonForumBridge.OnRaidTriggered(site.City, count);

        logger.Information(
            "Raid on {City}: {Faction} x{Count} from {Camp} via gate {Gate}",
            site.City, f, count, a.Spawn, a.Gate
        );
        return raid;
    }

    // «с севера», «с юго-запада»
    private static string SideFrom(RaidSite site, RaidApproach a) => RaidSites.SideOf(site.Bank, a.Spawn) + "а";

    public static MahaonRaid RaidOf(Mobile m) => m is BaseCreature bc && _byRaider.TryGetValue(bc, out var r) ? r : null;

    /// <summary>Налётчик прорывается к банку: стража его больше не держит, а он — её.</summary>
    public static bool IsBreakingThrough(Mobile m) => RaidOf(m) is { Phase: RaidPhase.Breakthrough };

    /// <summary>Из OnActionWander налётчика.</summary>
    public static void OnRaiderWander(BaseCreature bc) => RaidOf(bc)?.Step(bc);

    public static void OnRaiderDeleted(BaseCreature bc) => _byRaider.Remove(bc);

    private static void TickAll()
    {
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            if (!_active[i].Tick())
            {
                Forget(_active[i]);
            }
        }

        if (_active.Count == 0)
        {
            _timer?.Stop();
            _timer = null;
        }
    }

    private static void Forget(MahaonRaid raid)
    {
        _active.Remove(raid);
        foreach (var bc in raid.Raiders.Keys)
        {
            _byRaider.Remove(bc);
        }
    }

    // ---- исходы ---------------------------------------------------------------------------

    public static void Repelled(MahaonRaid raid)
    {
        raid.End(false);
        var f = (int)raid.Faction;
        var weakened = Strength[f] > 0;
        Strength[f] = Math.Max(0, Strength[f] - 1);

        Announce(
            0x59,
            weakened
                ? $"Защитники {raid.Site.Genitive} отбили набег! Приспешники {FactionGenitive(raid.Faction)} ослабли."
                : $"Защитники {raid.Site.Genitive} отбили набег! Приспешники {FactionGenitive(raid.Faction)} отступили."
        );
        logger.Information("Raid on {City} repelled; {Faction} strength {Strength}", raid.Site.City, raid.Faction, Strength[f]);
    }

    public static void BankDestroyed(MahaonRaid raid)
    {
        var bank = raid.Site.Bank;
        var map = raid.Map;

        // Взрыв банка: огонь и взрывы по кругу, дым после.
        Effects.PlaySound(bank, map, 0x307);
        for (var i = 0; i < 10; i++)
        {
            var p = new Point3D(bank.X + Utility.RandomMinMax(-4, 4), bank.Y + Utility.RandomMinMax(-4, 4), bank.Z);
            Timer.DelayCall(
                TimeSpan.FromMilliseconds(150 * i),
                () =>
                {
                    Effects.SendLocationEffect(p, map, 0x36BD, 20, 10);
                    Effects.PlaySound(p, map, 0x11D);
                }
            );
            Effects.SendLocationEffect(new Point3D(p.X, p.Y, p.Z + 5), map, 0x3709, 30, 10);
        }

        raid.End(true);
        var f = (int)raid.Faction;
        Strength[f] = Math.Min(MaxStrength, Strength[f] + 1);

        Announce(
            0x22,
            $"Защита {raid.Site.Genitive} провалена! Банк взорван. Приспешники {FactionGenitive(raid.Faction)} стали сильнее."
        );
        logger.Information("Raid on {City}: bank destroyed; {Faction} strength {Strength}", raid.Site.City, raid.Faction, Strength[f]);
    }

    public static void Dissolved(MahaonRaid raid)
    {
        raid.End(true);
        Announce(0x3B2, $"Приспешники {FactionGenitive(raid.Faction)} отошли от {raid.Site.Genitive}.");
        logger.Information("Raid on {City} dissolved after timeout", raid.Site.City);
    }

    public static void Announce(int hue, string text)
    {
        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile is PlayerMobile player)
            {
                player.SendMessage(hue, text);
            }
        }
    }

    // ---- команды --------------------------------------------------------------------------

    [Usage("RaidStart [here] [город] [pirates|necro|horde] [подход] [fast]")]
    [Description("Начинает набег на город (по умолчанию — ближайший). «here» — лагерь там, где стоишь.")]
    private static void RaidStart_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var args = new List<string>(e.Arguments);
        var here = args.Count > 0 && args[0].Equals("here", StringComparison.OrdinalIgnoreCase);
        if (here)
        {
            args.RemoveAt(0);
        }

        // «fast» — бой у ворот минуту вместо пяти, для проверки.
        var fast = args.RemoveAll(a => a.Equals("fast", StringComparison.OrdinalIgnoreCase)) > 0;

        RaidSite site = null;
        if (args.Count > 0 && RaidSites.Get(args[0]) is { } named)
        {
            site = named;
            args.RemoveAt(0);
        }

        site ??= RaidSites.Nearest(from.Map, from.Location);
        RaidFaction? faction = args.Count > 0 ? ParseFaction(args[0]) : null;
        var approach = args.Count > 1 && int.TryParse(args[1], out var n) ? n : -1;

        var raid = Start(site, faction, approach, here ? from.Location : null);
        if (raid != null && fast)
        {
            raid.Battle = TimeSpan.FromMinutes(1);
        }
        from.SendMessage(
            raid != null ? 0x59 : 0x22,
            raid != null
                ? $"Набег на {site.City}: {raid.Raiders.Count} ({raid.Faction}), лагерь {raid.Approach.Spawn}, ворота {raid.Approach.Gate}, банк {site.Bank}."
                : "Набег не начат: нет города с подходами рядом, или на него уже идёт набег."
        );
    }

    [Usage("RaidStop")]
    [Description("Распускает все идущие набеги без последствий для силы сторон.")]
    private static void RaidStop_OnCommand(CommandEventArgs e)
    {
        foreach (var raid in _active.ToArray())
        {
            raid.End(true);
            Forget(raid);
        }

        e.Mobile.SendMessage(0x59, "Набеги распущены.");
    }

    [Usage("RaidStatus")]
    [Description("Идущие набеги и сила сторон.")]
    private static void RaidStatus_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        foreach (RaidFaction f in Enum.GetValues(typeof(RaidFaction)))
        {
            from.SendMessage($"{f}: сила {Strength[(int)f]}, отряд {CountFor(f)}");
        }

        foreach (var raid in _active)
        {
            var since = raid.Phase == RaidPhase.Battle ? $", бой {(int)(Core.Now - raid.BattleAt).TotalSeconds} с" : "";
            from.SendMessage(0x59, $"{raid.Site.City}: {raid.Phase}{since}, живых {raid.Alive}/{raid.Raiders.Count}, защитников {raid.Defenders.Count}");
            foreach (var (bc, state) in raid.Raiders)
            {
                if (!bc.Deleted && bc.Alive)
                {
                    from.SendMessage(
                        $"  {bc.Serial} {bc.GetType().Name} {bc.Location} hp {bc.Hits}/{bc.HitsMax} " +
                        $"{(bc.Combatant != null ? "бьётся с " + bc.Combatant.Name : state.Arrived ? "у цели" : $"идёт {state.Index}/{state.Route?.Count ?? 0}")}"
                    );
                }
            }
        }

        var next = RaidEventSystem.GetTimeUntilNextRaid();
        from.SendMessage($"Следующий набег по расписанию через {(int)next.TotalHours} ч {next.Minutes} мин.");
    }

    [Usage("RaidStrength <pirates|necro|horde> <0-10>")]
    [Description("Задаёт силу стороны: отряд = 8 + 2 × сила.")]
    private static void RaidStrength_OnCommand(CommandEventArgs e)
    {
        if (e.Length < 2 || ParseFaction(e.GetString(0)) is not { } f)
        {
            e.Mobile.SendMessage("[RaidStrength <pirates|necro|horde> <0-10>");
            return;
        }

        Strength[(int)f] = Math.Clamp(e.GetInt32(1), 0, MaxStrength);
        e.Mobile.SendMessage(0x59, $"{f}: сила {Strength[(int)f]}, отряд {CountFor(f)}.");
    }
}
