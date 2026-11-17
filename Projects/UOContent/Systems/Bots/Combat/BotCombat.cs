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

    /// <summary>The spell in hand was started to help <see cref="SpellTarget"/>, a friend.</summary>
    public bool SpellForAlly;

    /// <summary>Until then the bot fights hand to hand whatever its style: a mage with nothing it
    /// can cast. A tick, not a flag, so it tries its spells again once mana has come back.</summary>
    public long MeleeUntil = Core.TickCount;

    public long NextPotionTick;
    public long NextSongTick;
    public long FleeUntil;
    public bool Fleeing;

    internal GoToAction Approach;
    internal Mobile ApproachTarget;

    // Как живой игрок: огляделся, запомнил, кто вокруг, — дальше замечает только новых, тех, кто
    // полез в драку, и перемену «цвета» (войну гильдий). Раз в несколько секунд оглядывается снова.
    internal long NextLookAround = Core.TickCount;
    internal int RelationsSeen = -1;
    internal readonly System.Collections.Generic.List<Mobile> Noticed = [];

    internal void Notice(Mobile m)
    {
        if (Noticed.Count < 32 && !Noticed.Contains(m))
        {
            Noticed.Add(m);
        }
    }

    // Who the bot last held to account for hitting it, and when, so a long fight sours the
    // relationship blow by blow rather than once per think.
    internal Mobile LastBlamed;
    internal long LastBlameTick;

    /// <summary>Когда можно снова подумать о смене цели на общую. Без этого счётчика отряд
    /// переключался бы каждый тик и не добивал никого вовсе.</summary>
    internal long NextFocusTick;
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
    private const long MeleeWhileDryMs = 10_000;

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
            var forAlly = state.SpellForAlly;
            state.SpellTarget = null;
            state.SpellForAlly = false;

            // A helping spell's cursor wants a friend: the bot itself, not the foe it fights
            // (masteries pass the foe even for their helping abilities) — unless it was started
            // for a wounded ally, which then gets it.
            var helps = (cursor.Flags & Targeting.TargetFlags.Beneficial) != 0;
            var onTarget = !target.Deleted && target.Alive && (!helps || forAlly);
            cursor.Invoke(bot, onTarget ? target : bot);

            if (onTarget && forAlly)
            {
                // Help remembered both ways is what lets guilds drift into alliances.
                MahaonBots.BotRelationships.OnHelped(target, bot);
            }

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
            return PreferFocus(brain, state.Opponent);
        }

        if (IsValidFoe(bot, bot.Combatant))
        {
            return PreferFocus(brain, bot.Combatant);
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

        var t = BotProfile.Start();
        var threat = FindPlayerThreat(brain);
        BotProfile.Stop("бой: поиск угрозы среди людей", t);
        return threat;
    }

    /// <summary>
    /// Fights the bot picks among people, only while healthy and only where it is lawful: someone
    /// attacking a friend nearby, a member of a guild at war with its own, or an outlaw it holds a
    /// grudge against.
    /// </summary>
    private const int SightRange = ThreatRange - 2;
    private const int LookAroundMinMs = 8_000;
    private const int LookAroundMaxMs = 15_000;

    /// <summary>
    /// Кого из людей вокруг пора бить. Раньше бот на каждом обдумывании перебирал всех в округе и
    /// каждого проверял на дружбу, войну, злобу — в городе с сотнями ботов это и было главное, что
    /// дорожало с их числом. Теперь как у игрока: огляделся раз в 8–15 секунд (или когда гильдии
    /// объявили войну — сменился «цвет»), а между этим смотрит только на тех, кого заметил: вошедших
    /// в поле зрения и напавших на своих.
    /// </summary>
    private static Mobile FindPlayerThreat(BotBrain brain)
    {
        var bot = brain.Bot;
        var state = brain.Combat;
        if (bot.Hits < bot.HitsMax * 0.6)
        {
            state.Noticed.Clear();
            return null;
        }

        var now = Core.TickCount;
        if (now - state.NextLookAround >= 0 || state.RelationsSeen != MahaonBots.BotGuilds.RelationsEpoch)
        {
            state.NextLookAround = now + Utility.RandomMinMax(LookAroundMinMs, LookAroundMaxMs);
            state.RelationsSeen = MahaonBots.BotGuilds.RelationsEpoch;
            state.Noticed.Clear();

            var looks = BotProfile.Start();
            Mobile found = null;
            foreach (var m in bot.Map.GetMobilesInRange<PlayerMobile>(bot.Location, SightRange))
            {
                if ((found = Appraise(brain, m)) != null)
                {
                    break;
                }
            }

            BotProfile.Stop("бой: огляделся", looks);
            return found;
        }

        if (state.Noticed.Count == 0)
        {
            return null;
        }

        Mobile threat = null;
        foreach (var m in state.Noticed)
        {
            if (m.Map == bot.Map && bot.InRange(m, SightRange) && (threat = Appraise(brain, m)) != null)
            {
                break;
            }
        }

        state.Noticed.Clear();
        return threat;
    }

    /// <summary>Чем грозит этот человек: дешёвые признаки («цвет», дружба) — первыми.</summary>
    private static Mobile Appraise(BotBrain brain, Mobile m)
    {
        var bot = brain.Bot;
        if (m == bot || !m.Alive || m.Hidden)
        {
            return null;
        }

        // Свой: опасен не он, а тот, кто на него напал.
        if (BotSocialRules.IsFriend(bot, m))
        {
            return m.Combatant is { Alive: true } attacker && !BotSocialRules.IsFriend(bot, attacker) &&
                   IsValidFoe(bot, attacker)
                ? attacker
                : null;
        }

        // Напал на своего — враг.
        if (m.Combatant is { Alive: true } victim && victim != bot && BotSocialRules.IsFriend(bot, victim) &&
            IsValidFoe(bot, m))
        {
            return m;
        }

        if (bot.Hits <= bot.HitsMax * 0.7)
        {
            return null;
        }

        var atWar = BotSocialRules.IsAtWar(bot, m);
        var grudge = !atWar && (m.Murderer || m.Criminal) && brain.HoldsGrudge(m);

        // Nobody picks a new fight next to a beacon, where bots rise from the dead.
        if (!atWar && !grudge || !IsValidFoe(bot, m) || Items.MahaonBotBeacon.IsNearAnyBeacon(m))
        {
            return null;
        }

        if (grudge)
        {
            BotSpeech.Say(bot, BotTopic.Threat, m, 0.7);
        }

        return m;
    }

    /// <summary>
    /// Кто-то шагнул в поле зрения: заметили его боты рядом — и он их, если сам бот. Зовёт
    /// PlayerMobile.OnMovement на каждый шаг человека поблизости.
    /// </summary>
    public static void OnSeen(Mobile watcher, Mobile mover, Point3D oldLocation)
    {
        if (!Utility.InRange(watcher.Location, mover.Location, SightRange) || Utility.InRange(watcher.Location, oldLocation, SightRange))
        {
            return;
        }

        watcher.GetBrain()?.Combat.Notice(mover);
        mover.GetBrain()?.Combat.Notice(watcher);
    }

    private static readonly System.Collections.Generic.Dictionary<Mobile, long> _lastAlarm = new();
    private const long AlarmIntervalMs = 3_000;

    /// <summary>
    /// На бота напали: драку видят и слышат все вокруг, свои заметят нападающего сразу, не
    /// дожидаясь, пока оглядятся.
    /// </summary>
    public static void OnAttacked(Mobile victim, Mobile aggressor)
    {
        if (victim.Map == null || aggressor == null || aggressor == victim)
        {
            return;
        }

        var now = Core.TickCount;
        if (_lastAlarm.TryGetValue(victim, out var last) && now - last < AlarmIntervalMs)
        {
            return;
        }

        _lastAlarm[victim] = now;
        if (_lastAlarm.Count > 2048)
        {
            _lastAlarm.Clear();
        }

        foreach (var bot in victim.Map.GetMobilesInRange<BotMobile>(victim.Location, SightRange))
        {
            if (bot != victim && bot != aggressor)
            {
                bot.Brain?.Combat.Notice(aggressor);
            }
        }
    }

    /// <summary>Как часто бот пересматривает, не стоит ли бить общую цель.</summary>
    private const long FocusIntervalMs = 4_000;

    /// <summary>Докуда видно, по кому бьют свои.</summary>
    private const int FocusRange = 10;

    /// <summary>
    ///     Сосредоточенный огонь: добить того, кого уже бьют свои.
    ///
    ///     Чего не хватало. Цель выбиралась в одиночку — «кто меня ударил» или «кто ближе», —
    ///     поэтому впятером против двоих все пятеро честно били по разным целям, и ни одна не
    ///     падала. Урон размазывался, враги доживали до своих ударов, и численный перевес не
    ///     давал почти ничего. Живой отряд бьёт по одному и снимает противников по очереди.
    ///
    ///     Переключаемся только когда общая цель БЛИЖЕ К СМЕРТИ своей: иначе бот бросал бы
    ///     почти добитого ради свежего, и размазывание осталось бы, только с лишней беготнёй.
    ///     Плюс счётчик в несколько секунд — без него отряд дёргал бы цель каждый тик.
    /// </summary>
    private static Mobile PreferFocus(BotBrain brain, Mobile current)
    {
        var bot = brain.Bot;
        var state = brain.Combat;
        var now = Core.TickCount;

        if (now - state.NextFocusTick < 0)
        {
            return current;
        }

        state.NextFocusTick = now + FocusIntervalMs;

        var currentShare = Share(current);

        // Почти добитого не бросаем ни ради кого.
        if (currentShare <= 0.25)
        {
            return current;
        }

        Mobile best = null;
        var bestShare = currentShare;

        foreach (var m in bot.Map.GetMobilesInRange<PlayerMobile>(bot.Location, FocusRange))
        {
            if (m == bot || !m.Alive || m.Hidden || !BotSocialRules.IsFriend(bot, m))
            {
                continue;
            }

            var theirFoe = m.Combatant;

            if (theirFoe == null || theirFoe == current || !IsValidFoe(bot, theirFoe))
            {
                continue;
            }

            var share = Share(theirFoe);

            if (share < bestShare)
            {
                bestShare = share;
                best = theirFoe;
            }
        }

        if (best == null)
        {
            return current;
        }

        state.Opponent = best;
        return best;
    }

    /// <summary>Доля оставшегося здоровья, 0..1. Ноль у того, кого уже не спасти.</summary>
    private static double Share(Mobile m) => m.HitsMax <= 0 ? 1.0 : Math.Clamp((double)m.Hits / m.HitsMax, 0.0, 1.0);

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

        // Расстановка. Раньше порог бегства зависел только от своего здоровья, то есть один
        // против пяти бот держался до тех же пятнадцати процентов, что и один на один, —
        // и, разумеется, не доживал до них. Живой игрок при таком перевесе уходит заранее,
        // пока уйти ещё можно.
        var odds = BotTactics.Assess(brain, threat);

        if (odds.Outnumbered)
        {
            // Чем хуже перевес, тем раньше. При одном против пяти порог доходит примерно до
            // половины здоровья — то есть бот разворачивается, пока у него ещё есть ноги.
            threshold += Math.Min(0.35, (0.6 - odds.Odds) * 0.7);
        }
        else if (odds.Outnumbering)
        {
            // Со своими за спиной держатся дольше: отступать, когда вы впятером добиваете
            // двоих, глупо, и товарищи при этом остаются одни.
            threshold -= 0.07;
        }

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

        if (BotBard.TryDiscord(brain, foe) || BotMasteries.TryUse(brain, foe) || BotSchools.TryCast(brain, SchoolUse.Buff, foe))
        {
            return FightTickMs;
        }

        // Лучник без стрел снимает лук: с пустым колчаном замах не делает ничего, и бот стоял бы
        // на дистанции до самого бегства.
        if (bot.Weapon is BaseRanged { AmmoType: { } ammo } && !BotCombatStyles.HasAmmo(bot, ammo))
        {
            BotCombatStyles.PutAwayRanged(bot);
        }

        var style = Core.TickCount - state.MeleeUntil < 0 ? BotCombatStyle.Melee : BotCombatStyles.Of(bot);
        var distance = bot.GetDistanceToSqrt(foe);

        switch (style)
        {
            case BotCombatStyle.Mage:
                {
                    if (distance < KeepMin - 1 && StepAway(bot, foe))
                    {
                        return BotMovement.StepDelay(bot, true);
                    }

                    // Порога по навыку здесь больше нет. Он стоял на 30 и отнимал у слабого
                    // мага его единственное надёжное заклинание: первый круг при магии 20
                    // проходит с вероятностью 95% (диапазон −18..22), а бот с таким навыком
                    // вместо стрелы шёл в рукопашную. Годность решает BotSpellbook, по
                    // настоящему шансу, а не по круглому числу.
                    if (bot.Spell == null && bot.Target == null && distance <= 10 && bot.InLOS(foe) &&
                        (TryMassSpell(brain, foe) || TryAttackSpell(brain, foe) ||
                         BotSchools.TryCast(brain, SchoolUse.Attack, foe)))
                    {
                        return FightTickMs;
                    }

                    // Мог колдовать (не в касте, не в откате, цель видна), а начать было нечего —
                    // ни маны, ни реагентов. Раньше маг держал дистанцию и не делал ничего до
                    // порога бегства. Теперь — в рукопашную, пока мана не вернётся.
                    if (bot.Spell == null && bot.Target == null && distance <= 10 && bot.InLOS(foe) &&
                        Core.TickCount - bot.NextSpellTime >= 0)
                    {
                        state.MeleeUntil = Core.TickCount + MeleeWhileDryMs;
                        return Approach(brain, foe, 1);
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

    /// <summary>
    ///     Массовое заклинание — когда враги стоят кучей и это того стоит.
    ///
    ///     Чего не хватало. Бой был разговором один на один: бот брал противника и бил по нему,
    ///     сколько бы их ни стояло рядом. Площадные заклинания в книге есть, но выбор до них
    ///     просто не доходил, потому что никто не спрашивал, сколько врагов вокруг.
    ///
    ///     Три порога, и каждый по делу. Куча — минимум три врага в трёх тайлах друг от друга,
    ///     иначе площадь бьёт по одному за цену семерки. Мана с большим запасом: массовое
    ///     заклинание стоит сорок-пятьдесят, и вылив столько, маг остаётся пустым. Шанс — через
    ///     BotSpellbook, как и всё остальное: седьмой круг при магии 70 фитилит чаще, чем
    ///     попадает, и бросать на него последнюю ману незачем.
    ///
    ///     Землетрясение стоит первым и только когда бот окружён: оно бьёт вокруг САМОГО
    ///     заклинателя, то есть именно в той расстановке, где всё плохо, и бесполезно, когда
    ///     куча врагов стоит в стороне.
    /// </summary>
    private static bool TryMassSpell(BotBrain brain, Mobile foe)
    {
        var bot = brain.Bot;
        var situation = BotTactics.Assess(brain, foe);

        if (situation.Clustered < BotTactics.MassCastThreshold)
        {
            return false;
        }

        // Запас больше обычного: после массового каста маг должен остаться живым, а не пустым.
        const int massReserve = 20;

        var center = situation.ClusterCenter ?? foe;

        // Окружили — бьём вокруг себя. Три врага в двух шагах это уже окружение.
        if (situation.Outnumbered && CountNear(bot, 2) >= BotTactics.MassCastThreshold)
        {
            var quake = new Spells.Eighth.EarthquakeSpell(bot);

            if (BotSpellbook.Usable(bot, quake, massReserve) && BotHealing.TryCast(brain, quake, bot))
            {
                return true;
            }
        }

        var chain = new ChainLightningSpell(bot);

        if (BotSpellbook.Usable(bot, chain, massReserve) && BotHealing.TryCast(brain, chain, center))
        {
            return true;
        }

        var swarm = new MeteorSwarmSpell(bot);

        return BotSpellbook.Usable(bot, swarm, massReserve) && BotHealing.TryCast(brain, swarm, center);
    }

    /// <summary>Сколько врагов стоит вплотную — признак окружения.</summary>
    private static int CountNear(Mobile bot, int range)
    {
        var count = 0;

        foreach (var m in bot.Map.GetMobilesInRange<Mobile>(bot.Location, range))
        {
            if (m != bot && m.Alive && !m.Deleted && !m.Hidden && bot.CanBeHarmful(m, false) && m.Combatant == bot)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    ///     Относительная сила атакующих заклинаний — ради сравнения между собой, не абсолютный
    ///     урон. Числа подобраны по порядку величины среднего урона в каноне; важны только
    ///     пропорции, потому что делится всё на ману и множится на шанс (BotSpellbook.Value).
    /// </summary>
    private const double PowerFlameStrike = 32;
    private const double PowerEnergyBolt = 26;
    private const double PowerLightning = 19;
    private const double PowerFireball = 14;
    private const double PowerHarm = 9;
    private const double PowerMagicArrow = 6;

    /// <summary>
    ///     Чем бот бьёт: не «самым сильным, что позволяет навык», а тем, что даёт больше
    ///     урона на потраченную ману С УЧЁТОМ шанса сорваться.
    ///
    ///     Как было. Лестница из угаданных порогов: магия 30 — Lightning, 50 — Fireball,
    ///     80 — EnergyBolt, и так далее, сверху вниз, первое подошедшее. Беда в том, что
    ///     пороги не имели отношения к правилам игры. Lightning — четвёртый круг, при магии
    ///     30 его шанс по таблице движка ровно 15%; бот выливал ману в воздух пять раз из
    ///     шести и выглядел именно так, как ты описал.
    ///
    ///     Как стало. Кандидаты перебираются все, каждый проверяется на ману с запасом,
    ///     реагенты и шанс (BotSpellbook), а выбирается тот, у кого выше ожидаемая польза.
    ///     Поэтому маг с магией 30 сам приходит к третьему кругу и ниже, а не потому, что
    ///     кто-то вписал ему порог: четвёртый круг он просто не пройдёт по шансу.
    ///
    ///     Яд и паралич стоят отдельно и не участвуют в сравнении по урону: их ценность не в
    ///     уроне, а в том, чего противник из-за них НЕ делает, и сравнивать это с огнём
    ///     одной линейкой нельзя.
    /// </summary>
    private static bool TryAttackSpell(BotBrain brain, Mobile foe)
    {
        var bot = brain.Bot;

        // Запас под лечение: у слабого мага своё лечение дорогое относительно запаса маны,
        // поэтому резерв меньше — иначе он не атакует вовсе.
        var reserve = bot.Skills.Magery.Value >= 50 ? 11 : 4;

        // Управляющие заклинания — до урона, пока они ещё что-то меняют. Редко, чтобы бот не
        // занимался одним контролем вместо боя.
        if (!foe.Paralyzed && Utility.RandomDouble() < 0.15 &&
            TryBest(brain, foe, reserve, (new ParalyzeSpell(bot), 1.0)))
        {
            return true;
        }

        if (!foe.Poisoned && Utility.RandomDouble() < 0.25 &&
            TryBest(brain, foe, reserve, (new PoisonSpell(bot), 1.0)))
        {
            return true;
        }

        return TryBest(
            brain, foe, reserve,
            (new FlameStrikeSpell(bot), PowerFlameStrike),
            (new EnergyBoltSpell(bot), PowerEnergyBolt),
            (new LightningSpell(bot), PowerLightning),
            (new FireballSpell(bot), PowerFireball),
            (new HarmSpell(bot), PowerHarm),
            (new MagicArrowSpell(bot), PowerMagicArrow)
        );
    }

    /// <summary>
    ///     Накладывает лучшее из предложенных по ожидаемой пользе. Возвращает false, когда
    ///     ни одно не проходит — тогда маг не стоит столбом, а идёт в ближний бой (см.
    ///     вызывающий код).
    /// </summary>
    private static bool TryBest(BotBrain brain, Mobile foe, int reserve, params (Spell Spell, double Power)[] options)
    {
        var bot = brain.Bot;

        Spell best = null;
        var bestValue = 0.0;

        foreach (var (spell, power) in options)
        {
            if (!BotSpellbook.Usable(bot, spell, reserve))
            {
                continue;
            }

            var value = BotSpellbook.Value(bot, spell, power);

            if (value > bestValue)
            {
                bestValue = value;
                best = spell;
            }
        }

        // Последний довод: первый круг почти не фитилит и стоит четыре маны, так что когда не
        // проходит вообще ничего — чаще всего это нехватка маны, а не навыка. Запас здесь
        // снимаем: беречь ману под лечение, не ударив ни разу, смысла нет.
        if (best == null)
        {
            var arrow = new MagicArrowSpell(bot);

            if (BotSpellbook.Usable(bot, arrow, 0))
            {
                best = arrow;
            }
        }

        return best != null && BotHealing.TryCast(brain, best, foe);
    }
}
