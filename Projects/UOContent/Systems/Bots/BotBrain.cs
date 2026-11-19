using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// Everything a bot wants, knows and is doing. The persistent part — home, character, needs, and
/// everything money and belongings depend on (loot marks, its own corpse, a paid hire) — is
/// serialized with the bot. The goal and plan are not: after a restart the brain picks a goal
/// afresh, because a half-walked route or a half-told sentence is not worth carrying across one.
/// </summary>
[SerializationGenerator(1)]
public partial class BotBrain
{
    [DirtyTrackingEntity]
    private PlayerMobile _bot;

    [SerializableField(0)]
    private Map _homeMap;

    [SerializableField(1)]
    private Point3D _home;

    [SerializableField(2)]
    private string _homeCity;

    // Character, 0-100 each. Rolled once; they weigh goals, not just thresholds.
    [SerializableField(3)]
    private byte _greed;

    [SerializableField(4)]
    private byte _caution;

    [SerializableField(5)]
    private byte _sociability;

    [SerializableField(6)]
    private byte _wanderlust;

    [SerializableField(7)]
    private byte _diligence;

    // Needs, 0 = satisfied, 1 = can't be ignored any longer.
    [SerializableField(8)]
    private double _fatigue;

    [SerializableField(9)]
    private double _loneliness;

    [SerializableField(10)]
    private double _restlessness;

    // The beacon that spawned this bot; it reclaims the bot after a restart so it doesn't spawn a
    // replacement on top of it.
    [SerializableField(11)]
    private Item _ownerBeacon;

    // Добыча с трупов продаётся только с меткой, а без сохранения метки после рестарта всё
    // взятое лежало в сумке «ничьим»: не продавалось и не выбрасывалось, только давило весом.
    [Tidy]
    [SerializableField(12, setter: "private")]
    private HashSet<Item> _loot = [];

    /// <summary>The bot's own corpse after a death, until its belongings are recovered. Saved: a
    /// restart must not leave everything the bot carried on the ground.</summary>
    [SerializableField(13)]
    private Items.Corpse _ownCorpse;

    // Наём оплачен золотом, поэтому переживает рестарт; отряд — нет, его игрок соберёт заново.
    [SerializableField(14)]
    private Mobile _hireEmployer;

    // Срок по часам мира; в работе бот сверяется с тиками (tick-counts.md), а при загрузке срок
    // переводится в тик заново.
    [SerializableField(15, setter: "private")]
    private DateTime _hireUntil;

    private long _hireUntilTick;

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_hireEmployer != null)
        {
            var left = Math.Max(0, (long)(_hireUntil - Core.Now).TotalMilliseconds);
            _hireUntilTick = Core.TickCount + left;
        }
    }

    // Declared first: the generator picks the first matching constructor, and a deserialized brain
    // must know its bot to mark it dirty.
    public BotBrain(PlayerMobile bot) => _bot = bot;

    public BotBrain(PlayerMobile bot, Point3D home, Map homeMap, string homeCity) : this(bot)
    {
        _home = home;
        _homeMap = homeMap;
        _homeCity = homeCity;

        _greed = RollTrait();
        _caution = RollTrait();
        _sociability = RollTrait();
        _wanderlust = RollTrait();
        _diligence = RollTrait();

        _restlessness = Utility.RandomDouble() * 0.5;
        _loneliness = Utility.RandomDouble() * 0.5;
    }

    private void MigrateFrom(V0Content content)
    {
        _homeMap = content.HomeMap;
        _home = content.Home;
        _homeCity = content.HomeCity;
        _greed = content.Greed;
        _caution = content.Caution;
        _sociability = content.Sociability;
        _wanderlust = content.Wanderlust;
        _diligence = content.Diligence;
        _fatigue = content.Fatigue;
        _loneliness = content.Loneliness;
        _restlessness = content.Restlessness;
        _ownerBeacon = content.OwnerBeacon;
    }

    // Bell-ish: most characters are moderate, a few are extreme.
    private static byte RollTrait() => (byte)((Utility.Random(101) + Utility.Random(101) + Utility.Random(101)) / 3);

    public PlayerMobile Bot => _bot;

    public static double Trait(byte value) => value / 100.0;

    // ---- Transient state ----------------------------------------------------------------------

    /// <summary>Tick the scheduler expects to wake this brain next; stale queue entries carry an
    /// older stamp and are skipped.</summary>
    internal long DueTick;
    internal int ScheduleStamp;
    internal bool Registered;

    internal long LastThinkTick;
    internal long NextMountTick = Core.TickCount;
    private bool _hasThought;

    // Ghost state, see BotGhost.
    internal bool IsGhost;
    internal long DiedAt;
    internal GoToAction GhostWalk;
    internal bool GhostWalkStarted;
    internal long GhostArriveAt;
    internal IEntity GhostTarget;
    internal int GhostFailures;

    public BotCombatState Combat { get; } = new();

    // Who wronged this bot, until when it remembers. Bounded; a restart forgives.
    private readonly Dictionary<Mobile, long> _grudges = new();

    public void AddGrudge(Mobile m, long forMs)
    {
        if (m == null || m == _bot)
        {
            return;
        }

        if (_grudges.Count > 32)
        {
            _grudges.Clear();
        }

        _grudges[m] = Core.TickCount + forMs;
    }

    public bool HoldsGrudge(Mobile m) =>
        _grudges.TryGetValue(m, out var until) && Core.TickCount - until < 0;

    public int GrudgeCount => _grudges.Count;

    /// <summary>The hunting group this bot belongs to, if any. Transient.</summary>
    public BotGroup Group { get; internal set; }

    // Loot taken from kills, sold on the next market round; corpses already searched.
    private readonly HashSet<Serial> _lootedCorpses = [];

    public void MarkLoot(Item item)
    {
        // Без потолка метки копились бы вечно: проданное ушло, а запись осталась. Чистим то, что
        // удалено или уже не у бота (продано, выброшено, отдано).
        if (_loot.Count > 128)
        {
            _loot.RemoveWhere(i => i.Deleted || i.RootParent != _bot);
        }

        _loot.Add(item);
        this.MarkDirty();
    }

    public bool IsLoot(Item item) => _loot.Contains(item);

    public void ForgetLoot(Item item)
    {
        if (_loot.Remove(item))
        {
            this.MarkDirty();
        }
    }

    public void MarkLooted(Items.Corpse corpse)
    {
        if (_lootedCorpses.Count > 256)
        {
            _lootedCorpses.Clear();
        }

        _lootedCorpses.Add(corpse.Serial);
    }

    public bool HasLooted(Items.Corpse corpse) => _lootedCorpses.Contains(corpse.Serial);

    // ---- Possession ----------------------------------------------------------------------------

    // Что было у живого персонажа, когда его отдали ИИ ([BecomeBot). Мозг тот же, что у бота, а
    // бот продаёт сырьё, изделия и всё, что лежит в банке, — для игрока это потеря нажитого.
    // Поэтому прежнее имущество для торговли невидимо: продаётся только то, что мозг добыл,
    // сделал или снял с трупов сам. Только в памяти: одержимость рестарт не переживает.
    private HashSet<Serial> _ownersOwn;

    /// <summary>Remembers everything the character owns right now — worn, in the pack and in the
    /// bank, nested containers too — so the brain never sells or hauls it off.</summary>
    internal void RememberOwnersBelongings()
    {
        _ownersOwn = [];

        foreach (var item in _bot.Items)
        {
            AddOwned(item);
        }

        if (_bot.FindBankNoCreate() is { } bank)
        {
            AddOwned(bank);
        }
    }

    private void AddOwned(Item item)
    {
        _ownersOwn.Add(item.Serial);
        foreach (var child in item.Items)
        {
            AddOwned(child);
        }
    }

    /// <summary>Hands something over to the owner for good: banked surplus the brain must not haul
    /// back out to sell.</summary>
    internal void GiveToOwner(Item item) => _ownersOwn?.Add(item.Serial);

    /// <summary>The possessed character's own property from before the AI took over.</summary>
    public bool IsOwnersOwn(Item item) => _ownersOwn?.Contains(item.Serial) == true;

    /// <summary>Whether the character is someone's own, handed to the AI, rather than a bot.</summary>
    public bool IsPossessed => _ownersOwn != null;

    // ---- Hire ----------------------------------------------------------------------------------

    public bool IsHiredBy(Mobile employer) => _hireEmployer == employer && employer != null && Core.TickCount - _hireUntilTick < 0;

    public bool HasHire => _hireEmployer != null;

    public bool HireExpired => _hireEmployer != null && Core.TickCount - _hireUntilTick >= 0;

    public void Hire(Mobile employer, TimeSpan time)
    {
        var start = IsHiredBy(employer) ? _hireUntilTick : Core.TickCount;
        HireEmployer = employer;
        _hireUntilTick = start + (long)time.TotalMilliseconds;
        HireUntil = Core.Now + TimeSpan.FromMilliseconds(_hireUntilTick - Core.TickCount);
    }

    public void EndHire()
    {
        HireEmployer = null;
        HireUntil = DateTime.MinValue;
        _hireUntilTick = 0;
    }

    public BotGoal Goal { get; private set; }

    /// <summary>The last thing worth telling about the bot's own day, and when it happened.</summary>
    internal string LastNews;
    internal long LastNewsTick;

    /// <summary>A goal the owner of a possessed character asked for. While set, the brain weighs
    /// only it and the upkeep that keeps it going (supplies, selling, the bank, a corpse run).</summary>
    public BotGoal Focus { get; set; }

    public BotAction Action { get; private set; }

    private readonly Queue<BotAction> _plan = new();

    internal int PlanCount => _plan.Count;

    public IReadOnlyCollection<BotAction> Plan => _plan;

    /// <summary>Last evaluation's scores, for [BotInspect.</summary>
    public readonly List<(BotGoal goal, double score)> LastScores = [];

    private readonly Dictionary<BotGoal, long> _goalCooldownUntil = new();

    // Неудачи по цели в скользящем окне. Раньше счётчик был один и обнулялся любым удачным
    // шагом: план «дойти → сделать» давал «Done, Failed, Done, Failed…», и порог в три неудачи
    // не наступал никогда — бот перепланировал безнадёжную цель бесконечно.
    private readonly Dictionary<BotGoal, (int count, long since)> _goalFailures = new();
    private const int FailureWindowMs = 10 * 60_000;
    private const int FailuresToGiveUp = 3;

    private long _nextGoalReview;

    // ---- Thinking ------------------------------------------------------------------------------

    private const int GoalReviewMs = 45_000;

    /// <summary>Penalty a goal pays for interrupting the one in progress, so a bot finishes what it
    /// started instead of flipping between two near-equal wants.</summary>
    private const double SwitchCost = 0.2;

    /// <summary>One decision. Returns milliseconds until the next, or -1 to stop scheduling.</summary>
    internal int Think()
    {
        var bot = _bot;
        if (bot == null || bot.Deleted || bot.Map == null || bot.Map == Map.Internal)
        {
            return -1;
        }

        var now = Core.TickCount;
        var elapsedMs = _hasThought ? Math.Clamp(now - LastThinkTick, 0, 600_000) : 0;
        LastThinkTick = now;
        _hasThought = true;
        var t = BotProfile.Start();
        UpdateNeeds(elapsedMs);
        BotProfile.Stop("нужды", t);

        if (!bot.Alive)
        {
            ClearPlan();
            t = BotProfile.Start();
            var ghost = BotGhost.Think(this);
            BotProfile.Stop("призрак", t);
            return ghost;
        }

        // Brought back by something other than the ghost walk (a player's spell, a GM).
        if (IsGhost)
        {
            IsGhost = false;
            MahaonBots.BotReputation.RefreshTitle(bot);
        }

        // До боевого рефлекса намеренно: тот возвращается раньше, чем дело дойдёт до действий,
        // и всё, что стоит после него, в драке не исполняется никогда. Именно поэтому боты
        // молчали как раз там, где за ними смотрят. Свой счётчик внутри.
        t = BotProfile.Start();
        BotSpeech.Ambient(this);
        BotProfile.Stop("болтовня фоном", t);

        t = BotProfile.Start();
        var reflex = BotCombat.Think(this);
        BotProfile.Stop("бой: рефлекс", t);
        if (reflex >= 0)
        {
            // Бой — тоже дело: сторож не должен счесть дерущегося застрявшим.
            NoteProgress(now);
            return reflex;
        }

        if (Watchdog(now))
        {
            return 1000;
        }

        t = BotProfile.Start();
        BotStable.Upkeep(this);
        BotProfile.Stop("конюшня", t);

        // Без цели обзор нужен, только если и делать нечего: в LastResort план есть, а цели
        // нет, и прежняя проверка пересчитывала все цели на каждом шаге пути домой.
        if (now - _nextGoalReview >= 0 || Goal == null && Action == null && _plan.Count == 0)
        {
            ReviewGoal(now);
        }

        if (Action == null && !NextAction())
        {
            ReviewGoal(now, force: true);
            if (Action == null && !NextAction())
            {
                // Тупик: ни одна цель не смогла построить план. Раньше здесь просто стояли и
                // думали заново каждые две секунды — бесконечно, потому что обстановка сама
                // собой не менялась. Это и есть то «пришёл и стоит», за которым приходят НПС.
                //
                // Поэтому последний довод: уйти домой, а если и туда дороги нет — постоять и
                // пересмотреть цели. Разница с прежним поведением в том, что теперь это
                // ДЕЙСТВИЕ: оно закончится (успехом или отказом), и мозг получит новый повод
                // выбрать цель, а не будет возвращаться в ту же точку с тем же ответом.
                return LastResort(now);
            }
        }

        t = BotProfile.Start();
        var result = Action.Tick(this);
        BotProfile.Stop("шаг", Action.GetType().Name, t);

        switch (result.Status)
        {
            case BotActionStatus.Running:
                {
                    return result.DelayMs;
                }
            case BotActionStatus.Done:
                {
                    NoteProgress(now);
                    Action.Stop(this);
                    Action = null;

                    if (_plan.Count == 0)
                    {
                        if (Goal != null)
                        {
                            _goalFailures.Remove(Goal);
                        }

                        if (Goal?.News is { Length: > 0 } news)
                        {
                            LastNews = news[Utility.Random(news.Length)];
                            LastNewsTick = now;
                        }

                        NoteProgress(now);
                        Goal?.OnCompleted(this);
                        Goal = null;
                    }

                    return result.DelayMs;
                }
            default:
                {
                    Action.Stop(this);
                    Action = null;

                    // Replan the same goal from where the bot now stands; give up on it after
                    // repeated failures and let something else win for a while.
                    if (CountFailure(Goal, now) >= FailuresToGiveUp || !TryPlan(Goal))
                    {
                        if (Goal != null)
                        {
                            _goalCooldownUntil[Goal] = now + 5 * 60_000;
                            _goalFailures.Remove(Goal);
                        }

                        ClearPlan();
                        Goal = null;
                    }

                    return Math.Max(result.DelayMs, 500);
                }
        }
    }

    /// <summary>
    ///     Что делать, когда не придумалось вообще ничего.
    ///
    ///     Домой, если дом известен и бот не там: дорога сама по себе выводит его из места,
    ///     где ему нечем заняться, — а это чаще всего подземелье или глухой угол, где не
    ///     строится даже прогулка. Если дома нет или он здесь же — короткая передышка, после
    ///     которой цели пересматриваются заново.
    ///
    ///     Важно, что в обоих случаях это настоящее действие с концом. Прежний вариант
    ///     возвращал задержку, ничего не начав, и обстановка к следующему разу оставалась той
    ///     же — то есть бот стоял, пока его не убивали.
    /// </summary>
    private int LastResort(long now)
    {
        // Дом в охраняемом городе для красного или серого — верная смерть от стражи.
        var homeIsSafe = HomeMap == null || !BotSocialRules.IsOutlaw(_bot) || !BotSocialRules.IsGuarded(HomeMap, Home);

        if (HomeMap != null && homeIsSafe && (_bot.Map != HomeMap || !Utility.InRange(_bot.Location, Home, 12)))
        {
            _plan.Enqueue(new GoToAction(HomeMap, Home, 6, "домой, делать тут нечего"));
        }
        else
        {
            _plan.Enqueue(new RestAction(Utility.RandomMinMax(4_000, 12_000)));
        }

        // Пересмотреть цели пораньше: тупик — это обычно «обстановка сменится, если отойти».
        _nextGoalReview = now + 5_000;

        return NextAction() ? 500 : 2000;
    }

    // ---- Сторож -----------------------------------------------------------------------------------
    //
    // «Стоит столбом» чинили точечно не меньше пяти раз (LastResort, LoiterGoal, Relocate, речь,
    // сироты маяка). Общий механизм: бот не сдвинулся и не закончил ни одной цели десять минут —
    // мозг пишет о себе в лог и бросает всё, что делал; ещё десять минут без толку — переносится
    // домой. Бот в клетке вне навигационного графа иначе проваливал бы любой поход вечно.

    private const long StuckMs = 10 * 60_000;
    private const int StuckDistance = 4;

    private long _progressTick = Core.TickCount;
    private Point3D _progressAnchor;
    private Map _progressMap;
    private int _progressWeight;
    private int _stuckStrikes;

    private void NoteProgress(long now)
    {
        _progressTick = now;
        _progressAnchor = _bot.Location;
        _progressMap = _bot.Map;
        _progressWeight = _bot.TotalWeight;
        _stuckStrikes = 0;
    }

    /// <summary>True when it stepped in: the bot was stuck and has been shaken loose.</summary>
    private bool Watchdog(long now)
    {
        // Добыча и ремесло идут на месте, но сумка при этом меняется — это тоже дело.
        if (_progressMap != _bot.Map || !Utility.InRange(_bot.Location, _progressAnchor, StuckDistance) ||
            _progressWeight != _bot.TotalWeight || Action is RestAction)
        {
            NoteProgress(now);
            return false;
        }

        if (now - _progressTick < StuckMs)
        {
            return false;
        }

        _stuckStrikes++;
        logger.Warning(
            "Bot {Bot} stuck at {Location} {Map} for {Minutes} min: goal {Goal}, action {Action}, top scores {Scores}",
            _bot.Name, _bot.Location, _bot.Map, (now - _progressTick) / 60_000, Goal?.Name ?? "—", DescribeAction(), TopScores()
        );

        if (Goal != null)
        {
            _goalCooldownUntil[Goal] = now + StuckMs;
        }

        ClearPlan();
        Goal = null;

        if (_stuckStrikes >= 2 && HomeMap != null && HomeMap != Map.Internal)
        {
            // Последний довод: дорогу отсюда найти не удаётся, значит, вынести отсюда.
            _bot.MoveToWorld(Home, HomeMap);
            _stuckStrikes = 0;
        }

        _progressTick = now;
        return true;
    }

    private string TopScores()
    {
        var sorted = new List<(BotGoal goal, double score)>(LastScores);
        sorted.Sort((a, b) => b.score.CompareTo(a.score));

        var text = new System.Text.StringBuilder();
        for (var i = 0; i < sorted.Count && i < 5; i++)
        {
            text.Append(i == 0 ? "" : ", ").Append(sorted[i].goal.Name).Append('=').Append(sorted[i].score.ToString("0.00"));
        }

        return text.ToString();
    }

    private int CountFailure(BotGoal goal, long now)
    {
        if (goal == null)
        {
            return FailuresToGiveUp;
        }

        var (count, since) = _goalFailures.TryGetValue(goal, out var entry) && now - entry.since < FailureWindowMs
            ? entry
            : (0, now);

        _goalFailures[goal] = (++count, since);
        return count;
    }

    private bool NextAction()
    {
        if (!_plan.TryDequeue(out var next))
        {
            return false;
        }

        Action = next;
        Action.Start(this);
        return true;
    }

    private void ReviewGoal(long now, bool force = false)
    {
        _nextGoalReview = now + GoalReviewMs + Utility.Random(15_000);

        LastScores.Clear();
        BotGoal best = null;
        var bestScore = 0.0;

        foreach (var goal in BotGoals.All)
        {
            if (_goalCooldownUntil.TryGetValue(goal, out var until) && now - until < 0)
            {
                continue;
            }

            if (Focus != null && goal != Focus && !goal.IsUpkeep || IsPossessed && BotGoals.IsBotsOnly(goal))
            {
                continue;
            }

            double score;
            var scoreStart = BotProfile.Start();
            try
            {
                score = goal.Score(this);
                BotProfile.Stop("оценка", goal.Name, scoreStart);
            }
            catch (Exception e)
            {
                // Одна сломанная цель не должна парализовать бота: без этого обзор обрывался на
                // ней каждый раз, бот не делал ничего и раз в пять секунд писал ту же ошибку.
                Disable(goal, now, e);
                continue;
            }

            if (goal != Goal && Goal != null && !force)
            {
                score -= SwitchCost;
            }

            LastScores.Add((goal, score));

            if (score > bestScore)
            {
                bestScore = score;
                best = goal;
            }
        }

        if (best == null || best == Goal && !force && (Action != null || _plan.Count > 0))
        {
            return;
        }

        if (!TryPlan(best))
        {
            _goalCooldownUntil[best] = now + 2 * 60_000;
        }
    }

    private bool TryPlan(BotGoal goal)
    {
        if (goal == null)
        {
            return false;
        }

        List<BotAction> steps;
        var planStart = BotProfile.Start();
        try
        {
            steps = goal.Plan(this);
            BotProfile.Stop("план", goal.Name, planStart);
        }
        catch (Exception e)
        {
            Disable(goal, Core.TickCount, e);
            return false;
        }

        if (steps == null || steps.Count == 0)
        {
            return false;
        }

        ClearPlan();
        Goal = goal;

        foreach (var step in steps)
        {
            _plan.Enqueue(step);
        }

        return true;
    }

    private static readonly Logging.ILogger logger = Logging.LogFactory.GetLogger(typeof(BotBrain));

    // Одна запись на цель и вид ошибки: она повторится у каждого бота и забьёт лог.
    private static readonly HashSet<string> _reportedGoalErrors = [];

    private void Disable(BotGoal goal, long now, Exception e)
    {
        _goalCooldownUntil[goal] = now + 10 * 60_000;

        if (_reportedGoalErrors.Add($"{goal.GetType().Name}:{e.GetType().Name}"))
        {
            logger.Error(e, "Bot goal {Goal} threw for {Bot}; paused for this bot", goal.GetType().Name, _bot?.Name);
        }
    }

    /// <summary>Starts a goal as if the review had picked it. For tests and GM tools.</summary>
    internal bool Pursue(BotGoal goal)
    {
        _nextGoalReview = Core.TickCount + GoalReviewMs;
        return TryPlan(goal);
    }

    /// <summary>Whether the brain has put this goal aside for a while after it kept failing.</summary>
    internal bool IsPaused(BotGoal goal) =>
        _goalCooldownUntil.TryGetValue(goal, out var until) && Core.TickCount - until < 0;

    /// <summary>Drops the plan without stopping the action — for when Stop() itself is broken.</summary>
    internal void AbandonPlan()
    {
        Action = null;
        _plan.Clear();
    }

    public void ClearPlan()
    {
        Action?.Stop(this);
        Action = null;
        _plan.Clear();
    }

    // ---- Needs ---------------------------------------------------------------------------------

    private const double MsPerHour = 3_600_000.0;

    private void UpdateNeeds(long elapsedMs)
    {
        if (elapsedMs <= 0)
        {
            return;
        }

        var hours = elapsedMs / MsPerHour;
        var resting = Action is RestAction;

        // Fatigue builds over about three hours of activity and drains while resting.
        Fatigue = resting ? Math.Max(0, _fatigue - hours * 2.0) : Math.Min(1, _fatigue + hours / 3.0);

        // A sociable bot gets lonely within the hour; a recluse takes several.
        Loneliness = Math.Min(1, _loneliness + hours * (0.3 + Trait(_sociability) * 1.5));

        // The urge to see somewhere else builds over hours, faster for the wanderer.
        Restlessness = Math.Min(1, _restlessness + hours * (0.05 + Trait(_wanderlust) * 0.3));
    }

    public void OnRested(double amount) => Fatigue = Math.Max(0, _fatigue - amount);

    public void OnSocialized(double amount) => Loneliness = Math.Max(0, _loneliness - amount);

    public void OnTraveled() => Restlessness = 0;

    public string DescribeAction() => Action?.Describe(this) ?? "—";
}
