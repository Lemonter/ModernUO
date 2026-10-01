using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// Everything a bot wants, knows and is doing. The persistent part — home, character, needs — is
/// serialized with the bot; the plan and current action are rebuilt from the goal after a restart,
/// because a half-walked route or a half-told sentence is not worth carrying across one.
/// </summary>
[SerializationGenerator(0)]
public partial class BotBrain
{
    [DirtyTrackingEntity]
    private BotMobile _bot;

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

    // Declared first: the generator picks the first matching constructor, and a deserialized brain
    // must know its bot to mark it dirty.
    public BotBrain(BotMobile bot) => _bot = bot;

    public BotBrain(BotMobile bot, Point3D home, Map homeMap, string homeCity) : this(bot)
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

    // Bell-ish: most characters are moderate, a few are extreme.
    private static byte RollTrait() => (byte)((Utility.Random(101) + Utility.Random(101) + Utility.Random(101)) / 3);

    public BotMobile Bot => _bot;

    public static double Trait(byte value) => value / 100.0;

    // ---- Transient state ----------------------------------------------------------------------

    /// <summary>Tick the scheduler expects to wake this brain next; stale queue entries carry an
    /// older stamp and are skipped.</summary>
    internal long DueTick;
    internal int ScheduleStamp;
    internal bool Registered;

    internal long LastThinkTick;
    private bool _hasThought;

    // Ghost state, see BotGhost.
    internal bool IsGhost;
    internal long DiedAt;
    internal GoToAction GhostWalk;
    internal bool GhostWalkStarted;

    public BotCombatState Combat { get; } = new();

    // Loot taken from kills, sold on the next market round; corpses already searched.
    private readonly HashSet<Serial> _loot = [];
    private readonly HashSet<Serial> _lootedCorpses = [];

    public void MarkLoot(Item item) => _loot.Add(item.Serial);

    public bool IsLoot(Item item) => _loot.Contains(item.Serial);

    public void ForgetLoot(Item item) => _loot.Remove(item.Serial);

    public void MarkLooted(Items.Corpse corpse)
    {
        if (_lootedCorpses.Count > 256)
        {
            _lootedCorpses.Clear();
        }

        _lootedCorpses.Add(corpse.Serial);
    }

    public bool HasLooted(Items.Corpse corpse) => _lootedCorpses.Contains(corpse.Serial);

    /// <summary>The bot's own corpse after a death, until its belongings are recovered.</summary>
    internal Items.Corpse OwnCorpse;

    public BotGoal Goal { get; private set; }

    public BotAction Action { get; private set; }

    private readonly Queue<BotAction> _plan = new();

    public IReadOnlyCollection<BotAction> Plan => _plan;

    /// <summary>Last evaluation's scores, for [BotInspect.</summary>
    public readonly List<(BotGoal goal, double score)> LastScores = [];

    private readonly Dictionary<BotGoal, long> _goalCooldownUntil = new();
    private int _goalFailures;
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
        UpdateNeeds(elapsedMs);

        if (!bot.Alive)
        {
            ClearPlan();
            return BotGhost.Think(this);
        }

        // Brought back by something other than the ghost walk (a player's spell, a GM).
        IsGhost = false;

        var reflex = BotCombat.Think(this);
        if (reflex >= 0)
        {
            return reflex;
        }

        if (now - _nextGoalReview >= 0 || Goal == null)
        {
            ReviewGoal(now);
        }

        if (Action == null && !NextAction())
        {
            ReviewGoal(now, force: true);
            if (Action == null && !NextAction())
            {
                return 2000;
            }
        }

        var result = Action.Tick(this);

        switch (result.Status)
        {
            case BotActionStatus.Running:
                {
                    return result.DelayMs;
                }
            case BotActionStatus.Done:
                {
                    _goalFailures = 0;
                    Action.Stop(this);
                    Action = null;

                    if (_plan.Count == 0)
                    {
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
                    if (++_goalFailures >= 3 || !TryPlan(Goal))
                    {
                        if (Goal != null)
                        {
                            _goalCooldownUntil[Goal] = now + 5 * 60_000;
                        }

                        _goalFailures = 0;
                        ClearPlan();
                        Goal = null;
                    }

                    return Math.Max(result.DelayMs, 500);
                }
        }
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

            var score = goal.Score(this);
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

        var steps = goal.Plan(this);
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
