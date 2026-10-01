using System;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Third;
using Server.Spells.Fourth;
using Server.Spells.Fifth;
using Server.Spells.Sixth;
using Server.Spells.Seventh;

namespace Server.Systems.Bots;

/// <summary>A bot's fight in progress. Transient: a restart ends every fight anyway.</summary>
public sealed class BotCombatState
{
    public BotCombatState()
    {
        // Deadlines start from a real tick, never the 0 default (tick-counts.md).
        var now = Core.TickCount;
        NextPotionTick = now;
        NextSongTick = now;
        FleeUntil = now;
        LastBlameTick = now;
    }

    public Mobile Opponent;

    /// <summary>Who a spell in flight is for; handed to its target cursor when it appears.</summary>
    public Mobile SpellTarget;

    public long NextPotionTick;
    public long NextSongTick;
    public long FleeUntil;
    public bool Fleeing;

    internal GoToAction Approach;
    internal Mobile ApproachTarget;

    // Who the bot last held to account for hitting it, and when, so a long fight sours the
    // relationship blow by blow rather than once per think.
    internal Mobile LastBlamed;
    internal long LastBlameTick;
}

/// <summary>
/// The reflex layer: runs before the brain's plan on every think and takes over while the bot is
/// in danger — fights back, heals, or runs. The plan resumes when the danger is over.
/// </summary>
public static class BotCombat
{
    private const int ThreatRange = 14;
    private const int FightTickMs = 300;
    private const long FleeMs = 20_000;

    // Archers and casters keep this band to their target.
    private const int KeepMin = 4;
    private const int KeepMax = 8;

    /// <summary>Returns the delay to the next think when the reflexes handled this one, or -1 to
    /// let the plan run.</summary>
    public static int Think(BotBrain brain)
    {
        var bot = brain.Bot;
        var state = brain.Combat;

        // A spell asked for its target: give it the one it was cast for.
        if (bot.Target is { } cursor && state.SpellTarget != null)
        {
            var target = state.SpellTarget;
            state.SpellTarget = null;
            cursor.Invoke(bot, target.Deleted || !target.Alive ? bot : target);
            return 250;
        }

        var threat = FindThreat(brain);

        if (threat == null)
        {
            EndFight(brain);
            return BotHealing.TryHeal(brain) ? 1000 : -1;
        }

        if (ShouldFlee(brain, threat))
        {
            return Flee(brain, threat);
        }

        return Fight(brain, threat);
    }

    /// <summary>Starts a fight the bot chose (hunting): the reflexes carry it from here.</summary>
    public static void Engage(BotBrain brain, Mobile target)
    {
        var bot = brain.Bot;
        brain.Combat.Opponent = target;
        bot.Warmode = true;
        bot.Combatant = target;
    }

    private static Mobile FindThreat(BotBrain brain)
    {
        var bot = brain.Bot;
        var state = brain.Combat;

        if (IsValidFoe(bot, state.Opponent))
        {
            return state.Opponent;
        }

        if (IsValidFoe(bot, bot.Combatant))
        {
            return bot.Combatant;
        }

        // Whoever hit us most recently and is still around.
        Mobile best = null;
        var bestTime = DateTime.MinValue;

        foreach (var info in bot.Aggressors)
        {
            var attacker = info.Attacker;
            if (info.LastCombatTime > bestTime && IsValidFoe(bot, attacker))
            {
                bestTime = info.LastCombatTime;
                best = attacker;
            }
        }

        if (best != null)
        {
            if (best.Player)
            {
                brain.AddGrudge(best, 30 * 60_000);
                Blame(brain, best);
            }

            return best;
        }

        return FindPlayerThreat(brain);
    }

    /// <summary>
    /// Fights the bot picks among people, only while healthy and only where it is lawful: someone
    /// attacking a friend nearby, a member of a guild at war with its own, or an outlaw it holds a
    /// grudge against.
    /// </summary>
    private static Mobile FindPlayerThreat(BotBrain brain)
    {
        var bot = brain.Bot;
        if (bot.Hits < bot.HitsMax * 0.6)
        {
            return null;
        }

        foreach (var m in bot.Map.GetMobilesInRange<PlayerMobile>(bot.Location, ThreatRange - 2))
        {
            if (m == bot || !m.Alive || m.Hidden)
            {
                continue;
            }

            // A friend under attack: their attacker is fair game.
            if (BotSocialRules.IsFriend(bot, m) && m.Combatant is { Alive: true } attacker &&
                !BotSocialRules.IsFriend(bot, attacker) && IsValidFoe(bot, attacker))
            {
                return attacker;
            }

            // Nobody picks a new fight next to a beacon, where bots rise from the dead.
            if (!IsValidFoe(bot, m) || BotSocialRules.IsFriend(bot, m) || Items.MahaonBotBeacon.IsNearAnyBeacon(m))
            {
                continue;
            }

            if (BotSocialRules.IsAtWar(bot, m) && bot.Hits > bot.HitsMax * 0.7)
            {
                return m;
            }

            if (brain.HoldsGrudge(m) && (m.Murderer || m.Criminal) && bot.Hits > bot.HitsMax * 0.7)
            {
                BotSpeech.Say(bot, BotTopic.Threat, m, 0.7);
                return m;
            }
        }

        return null;
    }

    private const long BlameIntervalMs = 10_000;

    /// <summary>Feeds the guild politics: pairwise relationships are what DynamicGuildRelations
    /// turns into wars and alliances.</summary>
    private static void Blame(BotBrain brain, Mobile attacker)
    {
        var state = brain.Combat;
        var now = Core.TickCount;

        if (state.LastBlamed == attacker && now - state.LastBlameTick < BlameIntervalMs)
        {
            return;
        }

        state.LastBlamed = attacker;
        state.LastBlameTick = now;
        MahaonBots.BotRelationships.OnAttacked(brain.Bot, attacker);
    }

    private static bool IsValidFoe(Mobile bot, Mobile m) =>
        m is { Deleted: false, Alive: true } && m != bot && m.Map == bot.Map && bot.InRange(m, ThreatRange) &&
        !m.IsDeadBondedPet && bot.CanBeHarmful(m, false);

    private static bool ShouldFlee(BotBrain brain, Mobile threat)
    {
        var bot = brain.Bot;
        var state = brain.Combat;
        var now = Core.TickCount;

        if (state.Fleeing && now - state.FleeUntil < 0)
        {
            return true;
        }

        var hp = (double)bot.Hits / bot.HitsMax;
        var threshold = 0.15 + BotBrain.Trait(brain.Caution) * 0.25;

        // Out of its league: a creature far beyond what this bot hunts, it didn't pick.
        var outmatched = threat is BaseCreature creature && threat != state.Opponent &&
                         creature.Fame > BotCombatStyles.MaxPreyFame(brain) * 2 ||
                         threat.Player && BotCombatStyles.FightingSkill(threat) > BotCombatStyles.FightingSkill(bot) + 25 &&
                         brain.Group == null;

        if (hp < threshold || outmatched)
        {
            state.Fleeing = true;
            state.FleeUntil = now + FleeMs;
            return true;
        }

        state.Fleeing = false;
        return false;
    }

    private static int Fight(BotBrain brain, Mobile foe)
    {
        var bot = brain.Bot;
        var state = brain.Combat;

        state.Opponent = foe;
        if (!bot.Warmode)
        {
            bot.Warmode = true;
        }

        if (bot.Combatant != foe)
        {
            bot.Combatant = foe;
        }

        BotPets.Attack(bot, foe);

        if (BotHealing.TryHeal(brain))
        {
            return FightTickMs;
        }

        if (BotBard.TryDiscord(brain, foe) || BotSchools.TryCast(brain, SchoolUse.Buff, foe))
        {
            return FightTickMs;
        }

        var style = BotCombatStyles.Of(bot);
        var distance = bot.GetDistanceToSqrt(foe);

        switch (style)
        {
            case BotCombatStyle.Mage:
                {
                    if (distance < KeepMin - 1 && StepAway(bot, foe))
                    {
                        return BotMovement.StepDelay(bot, true);
                    }

                    if (bot.Spell == null && bot.Target == null && distance <= 10 && bot.InLOS(foe) &&
                        (bot.Skills.Magery.Value >= 30 && TryAttackSpell(brain, foe) || BotSchools.TryCast(brain, SchoolUse.Attack, foe)))
                    {
                        return FightTickMs;
                    }

                    return distance > KeepMax ? Approach(brain, foe, KeepMax) : FightTickMs;
                }
            case BotCombatStyle.Archer:
                {
                    if (distance < KeepMin - 1 && StepAway(bot, foe))
                    {
                        return BotMovement.StepDelay(bot, true);
                    }

                    return distance > KeepMax || !bot.InLOS(foe) ? Approach(brain, foe, KeepMax - 2) : FightTickMs;
                }
            default:
                {
                    // The swing itself is the engine's combat timer; the bot only has to be there.
                    if (bot.InRange(foe, 1))
                    {
                        BotSchools.TryArmSwing(bot);
                        return FightTickMs;
                    }

                    return Approach(brain, foe, 1);
                }
        }
    }

    private static int Approach(BotBrain brain, Mobile foe, int range)
    {
        var state = brain.Combat;
        if (state.Approach == null || state.ApproachTarget != foe)
        {
            state.Approach?.Stop(brain);
            state.Approach = new GoToAction(foe, range, "к противнику");
            state.ApproachTarget = foe;
            state.Approach.Start(brain);
        }

        var result = state.Approach.Tick(brain);
        if (result.Status != BotActionStatus.Running)
        {
            state.Approach.Stop(brain);
            state.Approach = null;
            return FightTickMs;
        }

        return Math.Min(result.DelayMs, FightTickMs);
    }

    /// <summary>One step away from the foe — straight back or to either side — if a cell is open.</summary>
    private static bool StepAway(Mobile bot, Mobile foe)
    {
        var away = (int)foe.GetDirectionTo(bot) & 7;

        foreach (var turn in StepAwayTurns)
        {
            var dir = (Direction)((away + turn) & 7);
            bot.Direction = dir;
            var old = bot.Location;
            if (bot.Move(dir | Direction.Running) && bot.Location != old)
            {
                return true;
            }
        }

        return false;
    }

    private static readonly int[] StepAwayTurns = [0, 1, 7];

    private static int Flee(BotBrain brain, Mobile threat)
    {
        var bot = brain.Bot;
        var state = brain.Combat;

        bot.Combatant = null;
        bot.Warmode = false;
        state.Opponent = null;

        if (!BotHealing.TryHeal(brain))
        {
            BotBard.TryPeace(brain, threat);
        }

        // Run toward town, where guards and healers are; failing a route, just away.
        if (state.Approach == null || state.ApproachTarget != null)
        {
            var city = BotSocialRules.TownFor(bot);
            state.Approach?.Stop(brain);
            state.ApproachTarget = null;
            state.Approach = city == null ? null : new GoToAction(city.Map, city.Center, 6, "бежит в город");
            state.Approach?.Start(brain);
        }

        if (state.Approach != null)
        {
            var result = state.Approach.Tick(brain);
            if (result.Status == BotActionStatus.Running)
            {
                return result.DelayMs;
            }

            state.Approach.Stop(brain);
            state.Approach = null;
        }

        return StepAway(bot, threat) ? BotMovement.StepDelay(bot, true) : FightTickMs;
    }

    private static void EndFight(BotBrain brain)
    {
        var state = brain.Combat;
        var bot = brain.Bot;

        if (state.Opponent != null || state.Approach != null)
        {
            state.Opponent = null;
            state.Approach?.Stop(brain);
            state.Approach = null;
            state.ApproachTarget = null;
        }

        state.Fleeing = false;
        BotPets.Follow(bot);

        if (bot.Warmode && bot.Combatant == null)
        {
            bot.Warmode = false;
        }
    }

    /// <summary>The strongest attack spell the bot can cast and afford, keeping mana for a heal.</summary>
    private static bool TryAttackSpell(BotBrain brain, Mobile foe)
    {
        var bot = brain.Bot;
        var magery = bot.Skills.Magery.Value;
        var reserve = magery >= 50 ? 11 : 4;

        if (magery >= 95 && Cast(brain, new FlameStrikeSpell(bot), foe, reserve))
        {
            return true;
        }

        if (magery >= 80 && Cast(brain, new EnergyBoltSpell(bot), foe, reserve))
        {
            return true;
        }

        if (magery >= 65 && !foe.Paralyzed && Utility.RandomDouble() < 0.15 && Cast(brain, new ParalyzeSpell(bot), foe, reserve))
        {
            return true;
        }

        if (magery >= 50 && Cast(brain, new FireballSpell(bot), foe, reserve))
        {
            return true;
        }

        if (magery >= 40 && !foe.Poisoned && Utility.RandomDouble() < 0.25 && Cast(brain, new PoisonSpell(bot), foe, reserve))
        {
            return true;
        }

        if (magery >= 30 && Cast(brain, new LightningSpell(bot), foe, reserve))
        {
            return true;
        }

        return Cast(brain, new MagicArrowSpell(bot), foe, 0);
    }

    private static bool Cast(BotBrain brain, Spell spell, Mobile target, int manaReserve) =>
        brain.Bot.Mana >= spell.GetMana() + manaReserve && BotHealing.TryCast(brain, spell, target);
}
