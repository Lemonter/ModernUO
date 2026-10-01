using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonBots;

/// <summary>
///     Everything the scheduler needs to know about one bot between decision ticks.
///     Kept separate from the Mobile itself so we're not fighting PlayerMobile's own
///     serialization for bot-specific bookkeeping.
/// </summary>
public class BotProfile
{
    public BotActivity Activity = BotActivity.Idle;
    public DateTime NextDecisionTime;

    // Mahaon: only meaningful when the bot ISN'T a real BotMobile (e.g. a real player
    // under [BecomeBot control) — BotMobile already carries its own Archetype field, this
    // is the fallback source BotController.GetArchetype() reads for anything else. Left
    // null for actual BotMobile instances (they don't need it here, redundant with their
    // own field) — set explicitly by whatever registers a non-BotMobile bot if full
    // archetype-specific AI is wanted.
    public BotArchetype? Archetype;

    public BotPersonality Personality = new(PersonalityTrait.Balanced);

    public Point3D HomeLocation;
    public Map HomeMap;
    public string HomeCity;
    public string CurrentCity;

    // If true, this bot never rolls into city-to-city travel (used for bots meant to stay
    // put in one city, like a named local fixture).
    public bool Stationary;

    // Strider: always picks a fight with any PK bot he finds — and loses, that's his fate.
    public bool IsChallenger;

    // Alard: panics and flees from any PK or combat attempt instead of fighting.
    public bool IsCoward;
    public DateTime LastPanicMessage;

    public string TravelDestinationCity;

    // Mage bots: who to heal once the pending GreaterHeal cast resolves its target.
    public Mobile PendingHealTarget;

    // Set while gathering/crafting/traveling — how many more decision cycles this
    // activity has left before it naturally wraps up.
    public int CyclesRemaining;

    // Длина похода. Для добычи считается в ДОБЫТОМ — насколько вырос рюкзак с начала
    // похода (см. BotGathering.DoGathering); для охоты — в убитых. По весу мерить нельзя:
    // ресурсы Mahaon невесомы, и «рюкзак полон» не наступает никогда.
    //
    // Раньше для добычи это были ЗАМАХИ, что и ломало ремесленников: бот с бесплодными
    // замахами добирал цель, ничего не принеся, и уходил крафтить с пустым рюкзаком.
    public int HarvestTarget;
    public int HarvestCount;

    // Mahaon: wall-clock time this bot has been continuously active (non-Idle) —
    // set whenever Activity leaves Idle, checked against FatigueForceRestDuration (see
    // BotController's DispatchJudgment). A real time span rather than a decision-cycle
    // count on purpose: cycle counts are tied to decision cadence, which differs by
    // activity/archetype/whether PollMovement is driving it — a cycle-count budget
    // silently changes meaning every time that cadence does (this is exactly what broke
    // travel activities before — see the movementOnly split in Dispatch).
    public DateTime ActiveSince;

    /// <summary>
    ///     Занятие, выбранное человеком через [BecomeBot, а не рулеткой.
    ///
    ///     Пока оно стоит, рулетка занятий не крутится вовсе: сказали рубить — значит
    ///     рубить, и после неудачи бот ищет другое дерево, а не уходит копать руду. Раньше
    ///     принудительная цель ничем не отличалась от случайной, и первый же недостижимый
    ///     ствол сбрасывал занятие в Idle, откуда рулетка уводила бота заниматься чем
    ///     угодно.
    /// </summary>
    public SkillName? ForcedGatherSkill;

    /// <summary>То же для охоты: приказано охотиться — не переключаемся.</summary>
    public bool ForcedHunt;

    public SkillName GatherSkill = SkillName.Mining;
    public Point3D GatherDestination;

    // How many consecutive ticks TryFindHarvestTarget has failed at the current spot —
    // once this crosses GatherGiveUpAfter, the spot's a bust and it's worth a real
    // re-scout instead of jittering to a fresh random point every single failed tick.
    public int GatherSearchFailures;

    // Сколько предметов было в рюкзаке на прошлом заходе добычи и сколько заходов подряд
    // их число не росло.
    //
    // Раньше поход считался по числу НАЧАТЫХ замахов (HarvestCount++ сразу после запуска
    // цепочки), а не по добытому. Бот, у которого замахи ничего не дают — не тот
    // инструмент, слишком низкий навык, выработанная жила, — честно отстаивал все
    // пятьдесят-семьдесят заходов по три-восемь секунд каждый, то есть несколько минут на
    // одном месте, и возвращался крафтить с пустым рюкзаком. Со стороны это и выглядело
    // как «пошёл рубить дерево, постоял и решил крафтить непойми что».
    public int GatherStartItemCount = -1;
    public int GatherLastItemCount = -1;
    public int GatherNoYieldStreak;


    // Mage bots: don't re-buff every single tick — refresh on a cooldown instead.
    public DateTime NextBuffTime;

    // Which field item a pending DispelField cast should resolve against.
    public Item PendingFieldTarget;

    // Which creature a pending AnimalTaming attempt should resolve against.
    public Mobile PendingTameTarget;

    // The pet an archer "unpacked" for this fight — put away again once combat's over.
    public BaseCreature SummonedPet;

    // Which school a mage bot leans on for combat — Magery or Necromancy. Chosen once at
    // creation, not something they switch between mid-fight.
    public bool PrefersNecromancy;

    // If this bot was spawned by a beacon statue, it respawns there instead of wherever it
    // died — null for bots seeded the normal way (cities/travelers/named).
    public Item OwnerBeacon;

    // ---- Состояние движения (см. BotMovement.cs) --------------------------------------
    //
    // Три разные вещи, которые раньше путались между собой:
    //
    //   Destination   — куда бот идёт на самом деле (жила, город, дверь подземелья).
    //   ActivePathGoal — текущий отрезок: точка не дальше MaxLegLength, до которой A*
    //                    ТОЧНО построил маршрут. Дойти до неё — не значит дойти до цели.
    //   ActivePath     — сам PathFollower на этот отрезок.
    //
    // Ключевое правило: в ActivePathGoal попадает только та точка, для которой
    // MovementPath уже вернул Success. Иначе PathFollower внутри себя срывается на
    // слепой шаг по прямой (см. PathFollower.Follow: при !m_Path.Success он идёт
    // GetDirectionTo(goal)) — и бот утыкается в стену и молотит в неё.
    public PathFollower ActivePath;
    public Point3D ActivePathGoal;

    /// <summary>Конечная цель текущего маршрута — по её смене понимаем, что всё состояние
    /// пора обнулять.</summary>
    public Point3D PathDestination;

    /// <summary>Ставится движением, когда до цели дороги нет вовсе. Вызывающий сам решает,
    /// что с этим делать: путешествие бросает маршрут, погоня — саму жертву.</summary>
    public bool GoalUnreachable;

    /// <summary>Когда быстрому тику движения снова можно осмотреться по сторонам. Сам
    /// осмотр (труп рядом? враг рядом?) — два обхода области, и на 4 Гц он обходился
    /// дороже, чем всё остальное движение вместе взятое.</summary>
    public DateTime NextTravelScanTime;

    /// <summary>
    ///     До этого времени бота не трогают: он только что возродился.
    ///
    ///     Возрождается бот прямо у своего маяка, а разбойник ищет жертв в двадцати тайлах
    ///     вокруг себя — то есть у того же маяка. Получалась ловушка: убитый вставал и тут
    ///     же получал снова, и так по кругу, а каждая смерть уходила на форум. Минуты
    ///     неприкосновенности хватает, чтобы отойти.
    /// </summary>
    public DateTime ProtectedUntil;

    /// <summary>
    ///     Где бот стоял на прошлом тике движения и сколько тиков подряд не сдвинулся.
    ///
    ///     Главный признак застревания — именно неподвижность, а не «расстояние до цели не
    ///     уменьшается». Второе обманчиво: идя вдоль длинной стены в обход, бот совершенно
    ///     законно не приближается к цели по прямой, и считать это застреванием нельзя.
    /// </summary>
    public Point3D LastMoveCheckPosition;

    public int MotionlessTicks;

    /// <summary>
    ///     Куда бот бредёт, когда делать особо нечего: осматривает подземелье, дрейфует на
    ///     охотничьем месте, патрулирует в поисках жертвы.
    ///
    ///     Держится между тиками намеренно: выбери мы новую точку каждый тик, бот дёргался
    ///     бы на месте вместо того, чтобы куда-то дойти.
    /// </summary>
    public Point3D PatrolTarget;

    /// <summary>Сколько раз подряд для этой цели не нашлось ни одной достижимой
    /// промежуточной точки. Несколько раз подряд — значит, дороги действительно нет.</summary>
    public int WaypointSearchFailures;

    /// <summary>Сколько отрезков подряд прошло без приближения к цели. Ловит хождение
    /// кругами — то, что по неподвижности не ловится вовсе.</summary>
    public int LegsWithoutProgress;

    /// <summary>Худшее расстояние-ориентир: держится как ВТОРИЧНАЯ метрика, чтобы бот не
    /// бродил вокруг цели вечно, даже если формально каждый тик куда-то шагает.</summary>
    public double BestGoalDistance = double.MaxValue;

    public DateTime PathDeadline;

    /// <summary>
    ///     Промежуточные точки, которые не сработали, и когда это случилось.
    ///
    ///     Со временем забываются: дверь могут открыть, мост построить, а чужой мобиль,
    ///     перегородивший проход, просто уйдёт. Вечный запрет постепенно выел бы все
    ///     обходные пути.
    /// </summary>
    public readonly List<(Point3D loc, DateTime at)> FailedWaypoints = new();

    // ---- Счётчики одной дороги. Только для диагностики, в сохранение не идут ------------
    //
    // Нужны, чтобы по журналу было видно не отдельный шаг, а всю поездку целиком: сколько
    // отрезков пришлось строить, во сколько полноценных A* это обошлось и сколько времени
    // заняло. Без этого «бот не дошёл» неотличимо от «бот дошёл, но втрое длиннее».

    /// <summary>Сколько отрезков построено за эту дорогу.</summary>
    public int TripLegs;

    /// <summary>Сколько раз запускался настоящий поиск пути при подборе точек.</summary>
    public int TripProbes;

    /// <summary>Когда дорога началась и с какого места.</summary>
    public DateTime TripStarted;

    public Point3D TripOrigin;

    /// <summary>Чем закончился последний удачный подбор точки — для журнала.</summary>
    public double LastLegTurn;

    public int LastLegLength;

    public int LastLegProbes;

    /// <summary>Сбрасывает состояние маршрута — вызывается при смене цели.</summary>
    public void ResetPathing()
    {
        ActivePath = null;
        ActivePathGoal = default;
        PathDestination = default;
        GoalUnreachable = false;
        MotionlessTicks = 0;
        WaypointSearchFailures = 0;
        LegsWithoutProgress = 0;
        BestGoalDistance = double.MaxValue;
        PathDeadline = DateTime.MinValue;
        LastMoveCheckPosition = default;
        FailedWaypoints.Clear();

        TripLegs = 0;
        TripProbes = 0;
        TripStarted = DateTime.MinValue;
        TripOrigin = default;
    }

    // Stuck detection — if a bot doesn't actually move for too many consecutive movement
    // ticks while supposedly traveling, something's wrong (bad pathing, boxed in) and it
    // should just teleport home instead of standing there forever.

    public BotParty Party;

    // When this bot started waiting at a party rendezvous point — TickFormingParty gives
    // up and disbands past FormingPartyTimeout instead of waiting forever for a role that
    // may never show up nearby (see BotSocial.cs).
    public DateTime FormingPartySince;

    // Combat tactics (stance/aimed hit/poison/special moves) refresh on this cooldown
    // while a fight is ongoing instead of only being applied once at engagement.
    public DateTime NextTacticsRefreshTime;

    // -- Bard skills (Provocation/Peacemaking/Discordance) ------------------------------

    public enum PendingBardAction
    {
        None,
        ProvokeFirst,
        ProvokeSecond,
        Discord,
        Peace
    }

    // Don't roll bard skills every single tick — refresh on a cooldown instead, same
    // pattern as NextBuffTime for mages.
    public DateTime NextBardSkillTime;

    // Which bard skill's Target we're currently waiting on the resolution of. Set right
    // before calling the real SkillHandlers.*.OnUse, read back on the next tick once
    // bot.Target is populated — can't pattern-match on the Target's concrete type since
    // Provocation/Peacemaking/Discordance all keep their Target classes private.
    public PendingBardAction BardAction;

    // Provocation targets two creatures against each other — who we already picked as
    // target #1 while waiting for the second tick's Target to pick #2.
    public Mobile PendingProvokeFirst;

    // -- Mage escape kit (Paralyze/Wall/Teleport when caught at melee range) ------------

    // Where an escape-cast Fire/Poison Field should land — a blocking point between the
    // mage and whoever's chasing, as opposed to the normal offensive use (cast on top of
    // the target). Null means "use the normal offensive spot". Also reused as the summon
    // point for Blade Spirits/Energy Vortex (dropped right at the mage's own feet).
    public Point3D? PendingGroundTarget;

    // Combat-only summon (Blade Spirits/Energy Vortex/Summon Daemon) — don't try to recast
    // every single tick while the last cast is still resolving (multi-tick target flow).
    public DateTime NextSummonAttempt;

    // Simple per-bot "personality" knob: higher = more likely to go do something productive
    // instead of idling. Lets us have a mix of AFK-forever bots and industrious ones.
    public double Ambition = Utility.RandomDouble();

    // A minority of bots are player-killers: instead of the usual peaceful loop, they
    // hunt down anyone nearby (other bots or real players) that isn't in their own party.
    // Sourced from BotMobile.IsPk (persisted there, not rerolled here) — see BotController.RegisterBot.
    public bool IsPK;

    // Mahaon: заменяет SnapshotGearToBank/PruneBankToBest — раньше при каждой смерти
    // клонировался весь текущий комплект в банк (bank.DropItem, безусловно, в обход
    // MaxItems), с чисткой только брони/оружия — всё остальное копилось без предела
    // (один бот дорос до 1 027 461 предметов в банке). Теперь помним только ЧИСЛА —
    // лучший рейтинг брони на слот, лучший урон оружия на слот — ни одного лишнего
    // предмета не создаётся и не хранится ради этого механизма вообще.
    public readonly Dictionary<Layer, double> BestArmorRating = new();
    public readonly Dictionary<Layer, int> BestWeaponDamage = new();

    public BotProfile(Point3D homeLocation, Map homeMap)
    {
        HomeLocation = homeLocation;
        HomeMap = homeMap;
        ActiveSince = Core.Now;
    }
}
