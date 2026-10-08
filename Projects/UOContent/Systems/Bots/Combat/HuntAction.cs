using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// Hunts a spot: picks prey around it the bot can take, starts the fight (the reflexes carry it),
/// searches the corpses, and wanders the spot when nothing is in sight. Ends after enough kills,
/// when time is up, or when the pack is heavy.
/// </summary>
public sealed class HuntAction : BotAction
{
    private const long MaxDurationMs = 25 * 60_000;
    private const int SightRange = 12;
    private const int CorpseRange = 10;

    private readonly HuntSpot _spot;
    private readonly int _quota;
    private readonly int _maxFame;
    private readonly string _preyType;

    private const int NotableFame = 15_000;

    private long _deadline;
    private int _kills;
    private Mobile _prey;
    private BotAction _step;

    /// <param name="maxFame">The toughest prey to pick; 0 means the bot's own limit.</param>
    /// <param name="preyType">Only this kind of prey (by type name), for a quest; null hunts anything.</param>
    public HuntAction(HuntSpot spot, int quota, int maxFame = 0, string preyType = null)
    {
        _spot = spot;
        _quota = quota;
        _maxFame = maxFame;
        _preyType = preyType;
    }

    public override void Start(BotBrain brain) => _deadline = Core.TickCount + MaxDurationMs;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;

        if (_prey != null && (_prey.Deleted || !_prey.Alive))
        {
            _kills++;

            // A kill like this gets talked about.
            if (_prey is BaseCreature { Fame: >= NotableFame } notable && !notable.Deleted)
            {
                MahaonBots.BotRumors.Spread(
                    $"Говорят, {bot.Name} {(brain.Group != null ? "со своим отрядом" : "в одиночку")} одолел {notable.Name}!"
                );
            }

            _prey = null;
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

        BotStable.TryOffload(bot);

        if (_kills >= _quota || Core.TickCount - _deadline >= 0 || Mobile.BodyWeight + bot.TotalWeight >= bot.MaxWeight - 20)
        {
            return _kills > 0 ? BotActionResult.Done() : BotActionResult.Failed();
        }

        if (FindCorpse(brain) is { } corpse)
        {
            _step = new Sequence(new GoToAction(corpse.Map, corpse.Location, 1, "к трупу"), new LootCorpseAction(corpse));
            _step.Start(brain);
            return BotActionResult.Running(250);
        }

        if (FindPrey(brain, _maxFame, _preyType) is { } prey)
        {
            _prey = prey;
            BotCombat.Engage(brain, prey);
            return BotActionResult.Running(300);
        }

        if (BotMovement.TryRandomSpot(_spot.Map, _spot.Center, _spot.Range, out var spot))
        {
            _step = new GoToAction(_spot.Map, spot, 1, "выслеживает добычу");
            _step.Start(brain);
            return BotActionResult.Running(250);
        }

        return BotActionResult.Running(2000);
    }

    private Corpse FindCorpse(BotBrain brain)
    {
        var bot = brain.Bot;
        foreach (var corpse in bot.Map.GetItemsInRange<Corpse>(bot.Location, CorpseRange))
        {
            if (corpse.Owner is BaseCreature && !brain.HasLooted(corpse) && LootCorpseAction.MayLoot(bot, corpse) && corpse.Items.Count > 0)
            {
                return corpse;
            }
        }

        return null;
    }

    private static Mobile FindPrey(BotBrain brain, int maxFameOverride, string preyType)
    {
        var bot = brain.Bot;
        var maxFame = maxFameOverride > 0 ? maxFameOverride : BotCombatStyles.MaxPreyFame(brain);
        Mobile best = null;
        var bestDist = double.MaxValue;

        foreach (var c in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, SightRange))
        {
            if (!c.Alive || c.Fame > maxFame || preyType != null && c.GetType().Name != preyType || !HuntingAtlas.IsFairPrey(c) || !bot.CanBeHarmful(c, false) || !bot.InLOS(c))
            {
                continue;
            }

            var dist = bot.GetDistanceToSqrt(c);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = c;
            }
        }

        return best;
    }

    public override void Stop(BotBrain brain) => _step?.Stop(brain);

    public override string Describe(BotBrain brain) => _step?.Describe(brain) ?? $"Охотится: {_kills}/{_quota}";
}
