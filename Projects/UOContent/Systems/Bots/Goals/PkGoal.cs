using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// A player killer goes where people work and hunt — away from town guards — and preys on those it
/// can beat: bots and players alike, the same rules for both.
/// </summary>
public sealed class PkGoal : BotGoal
{
    private const int SearchRange = 300;

    public override string Name => "Разбой";

    public override double Score(BotBrain brain)
    {
        var bot = brain.Bot;
        if (bot is not BotMobile { IsPk: true } || bot.Hits < bot.HitsMax * 0.9 || BotCombatStyles.FightingSkill(bot) < 40)
        {
            return 0;
        }

        return 0.35 + BotBrain.Trait((byte)(100 - brain.Caution)) * 0.4 + BotBrain.Trait(brain.Greed) * 0.2 -
               brain.Fatigue * 0.5;
    }

    public override List<BotAction> Plan(BotBrain brain)
    {
        var bot = brain.Bot;

        // Hunting grounds draw the prey: pick one outside the guards' reach.
        HuntSpot best = null;
        var bestDist = double.MaxValue;

        foreach (var spot in HuntingAtlas.Spots)
        {
            if (spot.Map != bot.Map || BotSocialRules.IsGuarded(spot.Map, spot.Center))
            {
                continue;
            }

            var dist = spot.Center.GetDistanceToSqrt(bot.Location) * (0.6 + Utility.RandomDouble() * 0.8);
            if (dist < SearchRange && dist < bestDist)
            {
                bestDist = dist;
                best = spot;
            }
        }

        return best == null ? null : [new GoToAction(best.Map, best.Center, 6, "на разбой"), new StalkAction(best)];
    }
}

/// <summary>Lies in wait at a spot and jumps whoever looks beatable; loots the victims.</summary>
public sealed class StalkAction : BotAction
{
    private const long MaxDurationMs = 20 * 60_000;
    private const int SightRange = 14;

    private readonly HuntSpot _spot;
    private long _deadline;
    private Mobile _victim;
    private BotAction _step;
    private int _kills;

    public StalkAction(HuntSpot spot) => _spot = spot;

    public override void Start(BotBrain brain) => _deadline = Core.TickCount + MaxDurationMs;

    public override BotActionResult Tick(BotBrain brain)
    {
        var bot = brain.Bot;

        if (_victim != null && !_victim.Alive)
        {
            _kills++;
            BotSpeech.Say(bot, BotTopic.Victory, _victim, 0.6);

            if (_victim.Corpse is Corpse corpse && LootCorpseAction.MayLoot(bot, corpse))
            {
                _step = new Sequence(new GoToAction(corpse.Map, corpse.Location, 1, "к трупу жертвы"), new LootCorpseAction(corpse));
                _step.Start(brain);
            }

            _victim = null;
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

        if (Core.TickCount - _deadline >= 0)
        {
            return BotActionResult.Done();
        }

        if (FindVictim(brain) is { } victim)
        {
            _victim = victim;
            BotSpeech.Say(bot, BotTopic.Threat, victim, 0.8);
            BotCombat.Engage(brain, victim);
            return BotActionResult.Running(300);
        }

        if (BotMovement.TryRandomSpot(_spot.Map, _spot.Center, _spot.Range + 10, out var spot))
        {
            _step = new GoToAction(_spot.Map, spot, 1, "высматривает жертву");
            _step.Start(brain);
            return BotActionResult.Running(250);
        }

        return BotActionResult.Running(3000);
    }

    private static Mobile FindVictim(BotBrain brain)
    {
        var bot = brain.Bot;
        var strength = BotCombatStyles.FightingSkill(bot);

        foreach (var m in bot.Map.GetMobilesInRange<PlayerMobile>(bot.Location, SightRange))
        {
            if (m == bot || !m.Alive || m.Hidden || m.AccessLevel > AccessLevel.Player || m is BotMobile { IsPk: true } ||
                BotSocialRules.IsFriend(bot, m) || BotSocialRules.IsGuarded(m.Map, m.Location) ||
                Items.MahaonBotBeacon.IsNearAnyBeacon(m) ||
                !bot.CanBeHarmful(m, false) || !bot.InLOS(m))
            {
                continue;
            }

            // Prey it expects to beat: not much stronger, not at full strength if equal.
            var theirs = BotCombatStyles.FightingSkill(m);
            if (theirs <= strength - 10 || theirs <= strength + 10 && m.Hits < m.HitsMax * 0.8)
            {
                return m;
            }
        }

        return null;
    }

    public override void Stop(BotBrain brain) => _step?.Stop(brain);

    public override string Describe(BotBrain brain) => _step?.Describe(brain) ?? $"Подстерегает жертв ({_kills})";
}
