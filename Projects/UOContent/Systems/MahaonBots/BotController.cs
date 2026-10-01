using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Third;
using Server.Spells.Fourth;
using Server.Spells.Fifth;
using Server.Spells.Sixth;
using Server.Spells.Seventh;
using Server.Spells.Eighth;
using Server.Spells.Necromancy;
using Server.Spells.Chivalry;
using Server.Spells.Bushido;
using Server.Spells.Ninjitsu;
using Server.Spells.Spellweaving;
using Server.Systems.MahaonAuction;
using Server.Systems.MahaonCities;
using Server.Systems.MahaonCombat;
using Server.Systems.MahaonMining;
using Server.Systems.MahaonProfessions;
using Server.Systems.MahaonRecipes;
using Server.Systems.MahaonRaids;
using Server.Targeting;

namespace Server.Systems.MahaonBots;

/// <summary>
///     Drives every registered bot. Core idea: we never iterate "all bots, every tick".
///     Instead each bot carries its own NextDecisionTime, and a cheap poll timer pulls a
///     small batch of "due" bots off a round-robin queue each pass. With N bots and a
///     conveyor cycle of C seconds, average per-poll work is N / (C / PollInterval) —
///     flat regardless of how infrequently any single bot needs to think.
///
///     Pacing: a bot with a player nearby thinks at normal speed; one with nobody around
///     (including the whole server being empty) just slows way down instead of freezing
///     outright — still technically "alive", barely spending any budget.
///
///     Split across multiple files (all `partial class BotController`) by responsibility
///     — this one keeps only the core lifecycle/registration/dispatch-loop plumbing.
///     Everything else (combat, gathering, crafting, looting, social, pets, mage AI,
///     travel, GM commands) lives in its own BotXxx.cs file alongside this one. Splitting
///     doesn't change behavior at all — every file is still the same class, all the
///     shared static state below (Bots, Conveyor, timers) is visible from all of them.
/// </summary>
public partial class BotController : GenericPersistence
{
    private static BotController _instance;

    private static readonly Dictionary<PlayerMobile, BotProfile> Bots = new();

    public static bool TryGetProfile(PlayerMobile bot, out BotProfile profile) => Bots.TryGetValue(bot, out profile);
    private static readonly Queue<PlayerMobile> Conveyor = new();

    public static int CountActive() => Bots.Count;

    /// <summary>
    ///     «Он сейчас дерётся». Один и тот же вопрос задавался в двадцати местах четырьмя
    ///     разными записями одного условия — и в паре мест по-разному, потому что
    ///     отрицание длинной цепочки легко написать не так. Теперь вопрос один.
    /// </summary>
    public static bool IsFighting(Mobile bot) => bot?.Combatant is { Deleted: false, Alive: true };

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static Timer _pollTimer;

    // Travel-type activities get their own faster tick — real one-tile-at-a-time walking
    // instead of the slow decision cadence, which used to look like teleporting (dozens of
    // tiles resolved instantly in a single dispatch).
    //
    // 0.25s is real running speed (Direction.Running, which every StepToward call already
    // flags) — 0.4s was actually walking speed, so every bot was capped below what its own
    // "Running" flag claimed. This is the ONE shared rate every bot's movement goes through
    // (kiting, pursuing, wandering, traveling — see PollMovement below), so nobody gets a
    // built-in edge: a fleeing mage and its pursuer both move at this exact same tile rate.
    private static readonly TimeSpan MovementInterval = TimeSpan.FromSeconds(0.25);

    /// <summary>Как часто идущий бот осматривается по сторонам — см. PollMovement.</summary>
    private static readonly TimeSpan TravelScanInterval = TimeSpan.FromSeconds(1);
    private static Timer _movementTimer;

    // How many bots we're willing to evaluate per poll. Tune based on measured tick time;
    // this is a conservative starting point, not a hard science.
    // Was 10 — an artificial "just in case" number, not based on any real measurement. At
    // 500+ bots that meant a full pass through everyone took 50 real seconds, which is its
    // own flavor of "bots just standing there." The actual per-bot decision work here is
    // still just branches and the occasional item creation — cheap enough to raise this a
    // lot without needing to touch the engine's single-threaded core loop at all.
    private const int BatchSize = 2000;

    // Decision cadence — same whether a player's nearby or not now. The old "much slower
    // when nobody's watching" throttle was a premature optimization; bots are cheap enough
    // (see [BotPerf) that it wasn't saving anything worth the "frozen for two minutes" look.
    private static readonly TimeSpan MinThink = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MaxThink = TimeSpan.FromSeconds(8);

    private const int PlayerProximityRange = 24;

    /// <summary>Сколько бот неприкосновенен после возрождения — см. BotProfile.ProtectedUntil.</summary>
    private static readonly TimeSpan SpawnProtection = TimeSpan.FromMinutes(1);

    /// <summary>Нельзя ли на него сейчас нападать: только что поднялся.</summary>
    public static bool IsSpawnProtected(Mobile m) =>
        m is PlayerMobile pm && Bots.TryGetValue(pm, out var profile) && Core.Now < profile.ProtectedUntil;

    private static readonly List<BotParty> ActiveParties = new();


    public BotController() : base("MahaonBots", 1)
    {
    }

    public static void Configure()
    {
        _instance = new BotController();

        // Gray duration, shard-wide (applies to everyone, not just PK bots — that's the
        // native engine mechanic and there's no per-mobile override for it).
        Mobile.ExpireCriminalDelay = TimeSpan.FromMinutes(5);

        CommandSystem.Register("SeedBots", AccessLevel.GameMaster, SeedBots_OnCommand);
        CommandSystem.Register("ClearBots", AccessLevel.GameMaster, ClearBots_OnCommand);
        CommandSystem.Register("BotStatus", AccessLevel.GameMaster, BotStatus_OnCommand);
        CommandSystem.Register("BotPolitics", AccessLevel.GameMaster, BotPolitics_OnCommand);
        CommandSystem.Register("BotPathDebug", AccessLevel.GameMaster, BotPathDebug_OnCommand);

        // Разбор маршрутов в консоль. По умолчанию выключен — на живом шарде это поток
        // строк; включается на время разбирательств настройкой или командой.
        PathDebug = ServerConfiguration.GetOrUpdateSetting("bots.pathDebug", false);
        CommandSystem.Register("PurgeDeathRobes", AccessLevel.GameMaster, PurgeDeathRobes_OnCommand);
        CommandSystem.Register("PurgeBotItems", AccessLevel.GameMaster, PurgeBotItems_OnCommand);
    }

    public static void Initialize()
    {
        _pollTimer = Timer.DelayCall(PollInterval, PollInterval, PollConveyor);

        _movementTimer = Timer.DelayCall(MovementInterval, MovementInterval, PollMovement);

        // Питомцы ботов не переживают перезапуск осмысленно: профиль с ссылкой на зверя
        // не сохраняется, а сам зверь — да. После рестарта такой питомец оставался в мире
        // ничьим, а бот при первой же стычке выпускал ещё одного. Подчистить их надо до
        // регистрации ботов, чтобы новые профили начинали с чистого листа.
        CleanupOrphanedBotPets();

        // Re-attach any bot Mobiles that survived a restart but aren't tracked yet
        // (BotController's own bookkeeping doesn't persist — the Mobiles do). Under the v2 engine
        // Systems.Bots registers them instead.
        foreach (var m in World.Mobiles.Values)
        {
            if (Systems.Bots.BotSystem.Engine == Systems.Bots.BotEngine.V1 && m is BotMobile bot && !Bots.ContainsKey(bot))
            {
                _restored.TryGetValue(bot, out var saved);

                RegisterBot(bot, bot.Location, bot.Map, saved.homeCity);
                RestoreProfile(bot, saved);

                // Боты, созданные до шаблона имени, живут в мире с прежней подписью —
                // проставляем «Имя Профессия [Гильдия]» и им тоже.
                BotMobile.ApplyNameTemplate(bot);
            }
        }

        // Боты появляются только от статуи-маяка (MahaonBotBeacon). Прежние автоматические
        // заселения городов, бродячих торговцев и именных ботов удалены: они не вызывались
        // ниоткуда, но продолжали тянуть за собой CityControlSystem и полторы сотни строк,
        // которые при чтении выглядели работающими.
    }

    /// <summary>
    ///     Возвращает боту то, что о нём помнил сейв, и — главное — возвращает его маяку.
    ///     Без этого маяк после каждой загрузки мира считал, что у него нет ни одного
    ///     бота, и доспавнивал полное поголовье поверх уже стоящего.
    /// </summary>
    private static void RestoreProfile(
        PlayerMobile bot,
        (Item beacon, string homeCity, string currentCity, PersonalityTrait trait, bool stationary, bool isPk) saved
    )
    {
        if (!Bots.TryGetValue(bot, out var profile))
        {
            return;
        }

        if (saved.currentCity != null)
        {
            profile.CurrentCity = saved.currentCity;
        }

        profile.Personality = new BotPersonality(saved.trait);
        profile.Stationary = saved.stationary;
        profile.IsPK = saved.isPk || profile.IsPK;

        if (saved.beacon is Items.MahaonBotBeacon { Deleted: false } beacon)
        {
            profile.OwnerBeacon = beacon;
            beacon.Claim(bot);

            // Починка для тех, кто уже стоит в мире: одно время гильдия выбиралась
            // случайно на каждого бота, и соседи по маяку оказывались друг другу
            // чужаками — законной добычей разбойников и поводом для войн на пустом месте.
            // Возвращаем всех своих в одну гильдию маяка.
            // Чиним только тех, кто оказался в гильдии, к городу вообще не относящейся
            // (наследие случайной раздачи). Кто уже в одной из городских — того не
            // трогаем: перекладывать его из гильдии в гильдию при каждом запуске значило
            // бы обнулять ему друзей и врагов.
            if (bot.Guild == null || !beacon.AllowedGuildNames().Contains(bot.Guild.Name))
            {
                BotGuilds.Join(beacon.GuildNameFor(bot), bot);
            }
        }

        _restored.Remove(bot);
    }

    public static void ScheduleResurrection(BotMobile bot)
    {
        Timer.DelayCall(TimeSpan.FromSeconds(15), () =>
        {
            if (bot.Deleted || bot.Alive)
            {
                return;
            }

            if (Bots.TryGetValue(bot, out var beaconProfile) && beaconProfile.OwnerBeacon?.Deleted == false)
            {
                return; // beacon-owned — its own faster resurrection timer handles this instead
            }

            PerformResurrection(bot);
        });
    }

    /// <summary>The actual "bring this bot back" logic — shared by the generic 15-second
    /// timer above and the beacon's own faster resurrection tick.</summary>
    public static void PerformResurrection(BotMobile bot)
    {
        if (bot.Deleted || bot.Alive)
        {
            return;
        }

        bot.Resurrect();

        if (Bots.TryGetValue(bot, out var resurrectedProfile))
        {
            resurrectedProfile.ProtectedUntil = Core.Now + SpawnProtection;
        }

        string city = null;
        Bots.TryGetValue(bot, out var profile);
        if (profile != null)
        {
            city = profile.CurrentCity ?? profile.HomeCity;
        }

        Point3D loc;
        Map map;

        if (profile?.OwnerBeacon?.Deleted == false)
        {
            loc = profile.OwnerBeacon.Location;
            map = profile.OwnerBeacon.Map;
        }
        else if (city != null && CityMarkers.TryGetMarker(city, "travelstone", out var markedLoc, out var markedMap))
        {
            loc = markedLoc;
            map = markedMap;
        }
        else if (city != null && CityControlSystem.Cities.TryGetValue(city, out var info))
        {
            loc = info.spawn;
            map = info.map;
        }
        else
        {
            loc = bot.Location;
            map = bot.Map;
        }

        bot.MoveToWorld(loc, map);
        BotMobile.RegearAfterDeath(bot);

        if (profile != null)
        {
            // Mahaon: a death mid-dungeon-trip used to reset straight to Idle here just
            // like any other resurrection — which handed the very next decision cycle to
            // DoIdle's normal task roulette (gathering/hunting/whatever), silently
            // abandoning a still-active dungeon party instead of walking back to rejoin
            // it. This is the actual cause of bots that were "clearing a dungeon" one
            // moment and off fishing the next — they died, respawned in town, and just
            // picked something unrelated. If the party still has other live members, send
            // it back toward the dungeon instead; DoTravelToDungeon already handles a
            // stale/dead party gracefully if that's changed by the time it gets there.
            if (profile.Party != null && profile.Party.IsAlive)
            {
                profile.Activity = BotActivity.TravelingToDungeon;
            }
            else
            {
                profile.Party = null;
                profile.Activity = BotActivity.Idle;
            }
        }
    }

    public static void RegisterBot(PlayerMobile bot, Point3D home, Map map, string homeCity = null)
    {
        if (Bots.ContainsKey(bot))
        {
            return;
        }

        var profile = new BotProfile(home, map)
        {
            NextDecisionTime = Core.Now + RandomThink(IsPlayerNearby(bot)),
            IsPK = bot is BotMobile botMobile && botMobile.IsPk,
            HomeCity = homeCity,
            CurrentCity = homeCity,
            PrefersNecromancy = bot is BotMobile { Archetype: BotArchetype.Mage } &&
                                Utility.RandomDouble() < BotTuning.MagePrefersNecromancyChance,
            Personality = BotPersonality.Roll()
        };

        Bots[bot] = profile;
        Conveyor.Enqueue(bot);
    }

    /// <summary>The BotMobiles this controller currently drives (possessed players excluded).</summary>
    public static List<BotMobile> RegisteredBotMobiles()
    {
        var list = new List<BotMobile>();
        foreach (var bot in Bots.Keys)
        {
            if (bot is BotMobile botMobile)
            {
                list.Add(botMobile);
            }
        }

        return list;
    }

    public static void UnregisterBot(PlayerMobile bot)
    {
        // Питомец живёт в профиле, а профиль сейчас исчезнет — без этого зверь оставался
        // в мире навсегда, с хозяином, которого больше нет.
        if (Bots.TryGetValue(bot, out var profile) && profile.SummonedPet?.Deleted == false)
        {
            profile.SummonedPet.Delete();
            profile.SummonedPet = null;
        }

        Bots.Remove(bot);
        // Left in the Conveyor queue harmlessly — PollConveyor skips unknown bots.
    }

    private static bool IsPlayerNearby(Mobile bot)
    {
        if (NetState.Instances.Count == 0 || bot.Map == null)
        {
            return false;
        }

        foreach (var m in bot.Map.GetMobilesInRange<PlayerMobile>(bot.Location, PlayerProximityRange))
        {
            if (m.NetState != null)
            {
                return true;
            }
        }

        return false;
    }

    private static TimeSpan RandomThink(bool nearPlayer) => MinThink + (MaxThink - MinThink) * Utility.RandomDouble();

    private static readonly TimeSpan MinFightingThink = TimeSpan.FromSeconds(1.0);
    private static readonly TimeSpan MaxFightingThink = TimeSpan.FromSeconds(1.8);

    // Mahaon: was mage-only ("mages need it for spell timing") — but every archetype's
    // flee/heal-potion/tactics-refresh judgment (DispatchJudgment) only runs on this same
    // decision cadence too, so a warrior or archer on the normal 3-8s tick could take two
    // to four full weapon swings between chances to drink a potion or bail. Any actively
    // fighting bot now gets the fast tick, not just mages.
    private static TimeSpan FightingThink() =>
        MinFightingThink + (MaxFightingThink - MinFightingThink) * Utility.RandomDouble();

    private static void PollConveyor()
    {
        if (Bots.Count == 0)
        {
            return;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var processed = 0;

        // Every bot pulled off the conveyor is put straight back on, so Conveyor.Count
        // never falls and "processed < BatchSize" alone meant the loop always ran the full
        // 2000 iterations — with, say, 30 bots registered that is the same 30 bots visited
        // ~66 times each, every second, forever. Cap the pass at one visit per registered
        // bot, which is what BatchSize was meant to express in the first place.
        var limit = Math.Min(BatchSize, Bots.Count);

        while (processed < limit && Conveyor.Count > 0)
        {
            var bot = Conveyor.Dequeue();
            processed++;

            if (!Bots.TryGetValue(bot, out var profile) || bot.Deleted)
            {
                continue; // stale entry, drop it silently
            }

            if (!bot.Alive)
            {
                // Don't think while dead; just requeue for a later check.
                Conveyor.Enqueue(bot);
                continue;
            }

            if (Core.Now >= profile.NextDecisionTime)
            {
                Dispatch(bot, profile);

                var isFightingNow = IsFighting(bot);

                profile.NextDecisionTime = Core.Now + (isFightingNow ? FightingThink() : RandomThink(IsPlayerNearby(bot)));
            }

            Conveyor.Enqueue(bot);
        }

        sw.Stop();
        BotPerf.RecordConveyor(sw.Elapsed.Ticks, processed);
    }

    private static void PollMovement()
    {
        if (Bots.Count == 0)
        {
            return;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var touched = 0;

        foreach (var (bot, profile) in Bots)
        {
            if (bot.Deleted || !bot.Alive)
            {
                continue;
            }

            touched++;

            if (GetArchetype(bot, profile) == BotArchetype.Archer &&
                IsFighting(bot))
            {
                TryArcherKite(bot);
                TryPursueCombatant(bot, profile, ArcherMaxEffectiveRange);
                continue;
            }

            if (GetArchetype(bot, profile) == BotArchetype.Mage &&
                IsFighting(bot))
            {
                TryMageKite(bot);
                TryPursueCombatant(bot, profile, MageMaxEffectiveRange);
                continue;
            }

            if (IsFighting(bot))
            {
                TryPursueCombatant(bot, profile, MeleeRange);
                continue;
            }

            if (!IsTravelActivity(profile.Activity))
            {
                continue;
            }

            // Застревание не лечится телепортом домой: этим целиком заведует движение
            // (BotMovement.StepTowardPath) — оно обходит, а если дороги нет, бросает цель
            // и отдаёт решение занятию.

            // Осматриваться по сторонам на каждом тике движения незачем: TryEngageWhileTraveling
            // обходит область дважды (труп для сбора и цель для драки), и на четырёх тиках
            // в секунду на бота это была самая дорогая часть всего цикла — при том что за
            // четверть секунды вокруг ничего не меняется. Раз в секунду достаточно, а шаги
            // при этом остаются плавными, потому что сам шаг ниже никуда не делся.
            if (Core.Now >= profile.NextTravelScanTime)
            {
                profile.NextTravelScanTime = Core.Now + TravelScanInterval;

                if (TryEngageWhileTraveling(bot))
                {
                    continue; // ввязался в драку — идти будет, когда закончит
                }
            }

            Dispatch(bot, profile, movementOnly: true);
        }

        sw.Stop();
        BotPerf.RecordMovement(sw.Elapsed.Ticks, touched);
    }

    /// <summary>
    ///     Занятия, во время которых бот куда-то идёт, — то есть те, которым нужен быстрый
    ///     тик движения, а не только медленный тик решений.
    ///
    ///     Список лежит рядом с самим перечислением (BotActivity.IsTravel), а не отдельной
    ///     копией здесь: раньше добавить занятие можно было, забыв дописать его сюда, и
    ///     бот с таким занятием просто стоял на месте, потому что PollMovement его не
    ///     считал идущим.
    /// </summary>
    private static bool IsTravelActivity(BotActivity activity) => activity.IsTravel();

    private const int BandageStockTarget = 20;
    private const int HealPotionStockTarget = 5;
    private const int ScrollStockTarget = 5;

    /// <summary>Bandages and heal potions are effectively free for bots, same as arrows and
    /// reagents already are — instead of intercepting every place they get consumed, just
    /// keep the stock topped up.</summary>
    private static void TryReplenishSupplies(Mobile bot)
    {
        var backpack = bot.Backpack;
        if (backpack == null)
        {
            return;
        }

        var bandages = backpack.FindItemByType<Bandage>();
        var bandageCount = bandages?.Amount ?? 0;
        if (bandageCount < BandageStockTarget)
        {
            if (bandages != null)
            {
                bandages.Amount += BandageStockTarget - bandageCount;
            }
            else
            {
                backpack.DropItem(new Bandage(BandageStockTarget));
            }
        }

        var potionCount = 0;
        foreach (var _ in backpack.FindItemsByType<BaseHealPotion>())
        {
            potionCount++;
        }

        for (var i = potionCount; i < HealPotionStockTarget; i++)
        {
            backpack.DropItem(new HealPotion());
        }

        // Spell scrolls a bot happens to be carrying (picked up loose, or dropped in from
        // elsewhere) never run out either — top back up to what they started with whenever
        // one gets used up.
        foreach (var scroll in backpack.FindItemsByType<SpellScroll>())
        {
            if (scroll.Amount < ScrollStockTarget)
            {
                scroll.Amount = ScrollStockTarget;
            }
        }
    }

    /// <summary>
    ///     movementOnly=true is the fast 0.25s PollMovement tick (see PollMovement) — it
    ///     skips straight to the activity switch below to keep a traveling bot's steps
    ///     smooth, without redoing fatigue/supply/meditation/flee/mage/bard judgment
    ///     (DispatchJudgment), which already runs for the same bot on the normal slow
    ///     decision cadence via PollConveyor. Used to run that whole judgment block
    ///     4x/sec during every travel activity, which blew through
    ///     FatigueForceRestThreshold in ~10 seconds instead of the intended several
    ///     minutes — a bot aborted almost every trip before reaching its destination.
    /// </summary>
    private static void Dispatch(PlayerMobile bot, BotProfile profile, bool movementOnly = false)
    {
        if (!movementOnly && DispatchJudgment(bot, profile))
        {
            return;
        }

        // Занятия, при которых бот идёт, целиком за быстрым тиком движения: он вызывает
        // тот же обработчик четыре раза в секунду. Медленному тику решений тут делать
        // нечего — раньше он всё равно прогонял тот же обработчик ещё раз, и один и тот
        // же шаг делался из двух разных циклов. Судить о самочувствии (DispatchJudgment
        // выше) он при этом продолжает: усталость, припасы и бегство из боя от занятия не
        // зависят.
        if (!movementOnly && profile.Activity.IsTravel())
        {
            return;
        }

        switch (profile.Activity)
        {
            case BotActivity.Idle:
                DoIdle(bot, profile);
                break;
            case BotActivity.Wandering:
                DoWandering(bot, profile);
                break;
            case BotActivity.TravelingToGather:
                DoTravelToGather(bot, profile);
                break;
            case BotActivity.Gathering:
                DoGathering(bot, profile);
                break;
            case BotActivity.ReturningFromGather:
                DoReturnFromGather(bot, profile);
                break;
            case BotActivity.Crafting:
                DoCrafting(bot, profile);
                break;
            case BotActivity.TravelingToMarket:
                DoTravelToMarket(bot, profile);
                break;
            case BotActivity.Selling:
                DoSelling(bot, profile);
                break;
            case BotActivity.FormingParty:
                DoFormParty(bot, profile);
                break;
            case BotActivity.TravelingToDungeon:
                DoTravelToDungeon(bot, profile);
                break;
            case BotActivity.DungeonCombat:
                DoDungeonCombat(bot, profile);
                break;
            case BotActivity.ReturningHome:
                DoReturnHome(bot, profile);
                break;
            case BotActivity.Hunting:
                DoHunting(bot, profile);
                break;
            case BotActivity.CityTraveling:
                DoCityTravel(bot, profile);
                break;
            case BotActivity.BankingTrip:
                DoBankingTrip(bot, profile);
                break;
            case BotActivity.TravelingToHunt:
                DoTravelToHunt(bot, profile);
                break;
            case BotActivity.SoloHunting:
                DoSoloHunting(bot, profile);
                break;
            case BotActivity.ReturningFromHunt:
                DoReturnFromHunt(bot, profile);
                break;
        }
    }

    /// <returns>true if this decision cycle was fully handled and the activity switch
    /// above should be skipped (a flee, a spell, a bard skill, forced rest, ...); false
    /// to fall through to the normal per-activity Do* dispatch.</returns>
    private static bool DispatchJudgment(PlayerMobile bot, BotProfile profile)
    {
        if (bot.Warmode && (!IsFighting(bot)))
        {
            bot.Warmode = false;
        }

        var isFightingNow = IsFighting(bot);

        // Mahaon: fatigue — too long continuously active without a real Idle stretch
        // forces one now, rather than letting a bot run flat out forever. Combat is
        // exempt (never force a bot to stand still mid-fight) — the clock just keeps
        // running during a fight and gets caught right after it ends. Measured in real
        // time (ActiveSince), not decision cycles — see BotProfile.ActiveSince for why.
        if (!isFightingNow)
        {
            if (profile.Activity == BotActivity.Idle)
            {
                profile.ActiveSince = Core.Now;
            }
            else if (Core.Now - profile.ActiveSince > FatigueForceRestDuration)
            {
                profile.ActiveSince = Core.Now;
                profile.Activity = BotActivity.Idle;
                bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Устал... нужно передохнуть.");
                return true;
            }
        }

        if (profile.BardAction != BotProfile.PendingBardAction.None && bot.Target is Target pendingBardTarget)
        {
            ResolvePendingBardTarget(bot, profile, pendingBardTarget);
            return true;
        }

        // Проверки на стражу здесь намеренно нет. Гильдейская война — законный повод
        // для драки где угодно, хоть посреди Британии: стража на оранжевых не реагирует.
        // Пока гейт стоял, две воюющие гильдии переставали замечать друг друга, стоило им
        // сойтись в городе, — то есть ровно там, где они чаще всего и сходятся.
        if (!isFightingNow && bot.Map != null && TryEngageGuildEnemy(bot))
        {
            return true;
        }

        // Mahaon: раньше в мирной ветке стоял TryDismissPet, то есть питомец УДАЛЯЛСЯ на
        // каждом тике вне боя — а следующая стычка выпускала нового. Со стороны это
        // выглядело как «выпускают питомцев по причине и без»: два выкрика над головой на
        // каждую перепалку и постоянное создание-уничтожение существа. Следопыт держит
        // питомца при себе; убирается он только со смертью хозяина или снятием бота с
        // учёта (DismissPetOnDeath / UnregisterBot).
        if (Bots.TryGetValue(bot, out var petCheckProfile) && petCheckProfile.SummonedPet?.Deleted == false)
        {
            TrySyncPet(bot, petCheckProfile);
        }

        if (bot is BotMobile)
        {
            TryReplenishSupplies(bot);
        }

        if (GetArchetype(bot, profile) == BotArchetype.Mage && !isFightingNow &&
            bot.Mana < bot.ManaMax && bot.Skills[SkillName.Meditation].Value >= 20 &&
            !bot.Meditating)
        {
            SkillHandlers.Meditation.OnUse(bot);
        }

        if (isFightingNow)
        {
            TryGainStat(bot, StatType.Str);
            TryGainStat(bot, StatType.Dex);
        }

        // Mahaon: stance/aimed hit/poison/special moves used to be applied once, right at
        // engagement, and never again for the rest of the fight — the aimed-hit bonus
        // burns off on the first successful swing (see HitLocationSystem), poison charges
        // run dry, and Bushido/Ninjitsu/Chivalry never got a second roll. Re-running this
        // on the same cooldown as bard skills keeps a long fight tactically alive instead
        // of degrading into a flat swing-trade after the first second.
        if (isFightingNow && bot.Combatant != null && Core.Now >= profile.NextTacticsRefreshTime)
        {
            ApplyCombatTactics(bot, bot.Combatant);
            profile.NextTacticsRefreshTime = Core.Now + TacticsRefreshCooldown;
        }

        var effectiveFleeThreshold = FleeHealthThreshold * profile.Personality.FleeThresholdMultiplier;

        // A grudge against THIS specific attacker overrides some of the usual caution —
        // fights a hated enemy longer than it normally would, personality notwithstanding.
        if (bot.Combatant != null && BotRelationships.IsHostileTo(bot, bot.Combatant))
        {
            effectiveFleeThreshold *= 0.6;
        }

        if (isFightingNow && bot.Combatant != null)
        {
            BotRelationships.OnAttacked(bot, bot.Combatant);
        }

        if (isFightingNow && bot.HitsMax > 0 && (double)bot.Hits / bot.HitsMax < effectiveFleeThreshold)
        {
            if (Utility.RandomDouble() < BotTuning.PanicShoutChance)
            {
                bot.PublicOverheadMessage(MessageType.Regular, 0x22, false, "Дело плохо...");
            }

            TryDrinkHealPotion(bot);

            if (IsProfessionCategory(bot, ProfessionCategory.Thief) &&
                bot.Skills[SkillName.Ninjitsu].Value >= 40 && bot.Mana >= new MirrorImage(bot, null).GetMana())
            {
                new MirrorImage(bot, null).Cast();
            }

            FleeFromCombat(bot);
            return true;
        }

        if (isFightingNow && bot.Combatant is BaseCreature && !ShouldEngage(bot, bot.Combatant))
        {
            var isNonCombatBot = GetArchetype(bot, profile) == BotArchetype.Crafter ||
                                 IsProfessionCategory(bot, ProfessionCategory.Bard);

            if (Utility.RandomDouble() < (isNonCombatBot ? 0.6 : 0.15))
            {
                // Re-weighed the odds mid-fight (more of them showed up, allies died off) —
                // bail while there's still a chance to. Rolled, not automatic, so it isn't
                // an instant flee the moment the count tips.
                FleeFromCombat(bot);
                return true;
            }
        }

        if (GetArchetype(bot, profile) == BotArchetype.Mage &&
            (isFightingNow || bot.Poisoned || FindMostWoundedPartyMember(bot, profile) != null))
        {
            // Covers every activity uniformly — dungeon, solo hunting, mid-travel combat,
            // whatever — instead of only working in the one or two spots that happened to
            // call DoMageAction by hand.
            DoMageAction(bot, profile);
            return true;
        }

        if (isFightingNow && Core.Now >= profile.NextBardSkillTime &&
            ProfessionSystem.TouchesCategory(bot, ProfessionCategory.Bard) &&
            bot.Backpack?.FindItemByType<BaseInstrument>() != null &&
            TryUseBardSkill(bot, profile))
        {
            return true;
        }

        if (profile.PendingTameTarget != null)
        {
            ResolvePendingTame(bot, profile);
            return true;
        }

        return false;
    }

    // -- PK bots ---------------------------------------------------------------------------

    private const int CityLimitRadius = 30;
    private const int GuardSightRange = 15;

    private static bool IsWatchedByGuards(Mobile bot)
    {
        if (bot.Map == null)
        {
            return false;
        }

        var inCityLimits = false;
        foreach (var (_, info) in CityControlSystem.Cities)
        {
            if (bot.Map == info.map && bot.GetDistanceToSqrt(info.spawn) <= CityLimitRadius)
            {
                inCityLimits = true;
                break;
            }
        }

        if (!inCityLimits)
        {
            return false;
        }

        foreach (var guard in bot.Map.GetMobilesInRange<CityGuard>(bot.Location, GuardSightRange))
        {
            if (guard.Alive && !guard.Deleted && !guard.Hidden)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Первичная ли это категория профессии у бота. Вторичная сюда не годится:
    /// вопрос всюду задаётся в смысле «он по сути вор/бард/ремесленник», а не «он их
    /// краем задевает».</summary>
    public static bool IsProfessionCategory(Mobile m, ProfessionCategory category) =>
        ProfessionSystem.GetProfession(m) is { } profession &&
        ProfessionData.All[profession].Category == category;

    // -- Добыча и ремесло -------------------------------------------------------------
    //
    // Здесь долго висела заметка «это упрощённая имитация, ресурс просто выдаётся через
    // задержку». Она давно неверна: BotGathering.StartRealSwingChain гоняет бота через те
    // же MahaonMiningSwings / MahaonLumberjackingSwings / MahaonFishingSwings, что и
    // настоящий игрок по клику. Оставлять её было хуже, чем не иметь комментария вовсе —
    // читающий делал вывод, что добыча ботов ненастоящая, и шёл искать несуществующую
    // выдачу ресурсов.

    /// <summary>The effective archetype for AI-routing purposes — BotMobile's own field if
    /// it is one, otherwise BotProfile.Archetype (set for a real player under
    /// [BecomePossession control). Every "bot is BotMobile { Archetype: X }" check
    /// throughout this class exists to answer "should this bot behave like a mage/archer/
    /// warrior/crafter/trader right now" — this is that same question, just answerable
    /// for a real PlayerMobile too, not only an actual BotMobile.</summary>
    public static BotArchetype? GetArchetype(Mobile bot, BotProfile profile) =>
        bot is BotMobile botMobile ? botMobile.Archetype : profile?.Archetype;

    /// <summary>Overload for call sites that don't already have a BotProfile in hand —
    /// looks it up itself. Prefer the two-argument version when a profile is already
    /// available (most of Dispatch and friends), this one's for the many smaller helpers
    /// that only take a bare Mobile/PlayerMobile.</summary>
    public static BotArchetype? GetArchetype(Mobile bot)
    {
        if (bot is BotMobile botMobile)
        {
            return botMobile.Archetype;
        }

        return bot is PlayerMobile player && Bots.TryGetValue(player, out var profile) ? profile.Archetype : null;
    }

    /// <summary>
    ///     Точка, куда потоптаться поблизости.
    ///
    ///     Раньше брала случайное смещение и возвращала его как есть, вместе с высотой
    ///     исходной точки. На ровном месте это работает, а у воды, у скалы и на любом
    ///     перепаде высот выдавало точку, на которую не встать: бот утыкался в неё и
    ///     топтался, что и выглядело как «завис». Теперь несколько проб, и берётся первая,
    ///     на которую действительно можно ступить; не нашлось — бот просто стоит.
    /// </summary>
    /// <summary>
    ///     Случайная точка рядом, до которой ЕСТЬ дорога.
    ///
    ///     Отличается от RandomNearbyPoint одной проверкой, но проверка та самая: обычная
    ///     версия спрашивает CanSpawnMobile, то есть «сюда можно встать». Стоять там можно,
    ///     а вот дойти — вопрос, который никто не задавал, и в подземелье он решающий:
    ///     точка за стеной проходит CanSpawnMobile прекрасно.
    /// </summary>
    private static Point3D RoutableNearbyPoint(Mobile bot, int radius)
    {
        var map = bot.Map;

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var x = bot.X + Utility.RandomMinMax(-radius, radius);
            var y = bot.Y + Utility.RandomMinMax(-radius, radius);
            var z = map?.GetAverageZ(x, y) ?? bot.Z;
            var candidate = new Point3D(x, y, z);

            if (map != null && map.CanSpawnMobile(candidate) && new MovementPath(bot, candidate).Success)
            {
                return candidate;
            }
        }

        return bot.Location;
    }

    private static Point3D RandomNearbyPoint(Mobile bot, int radius)
    {
        var map = bot.Map;

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var x = bot.X + Utility.RandomMinMax(-radius, radius);
            var y = bot.Y + Utility.RandomMinMax(-radius, radius);
            var z = map?.GetAverageZ(x, y) ?? bot.Z;
            var candidate = new Point3D(x, y, z);

            if (map != null && map.CanSpawnMobile(candidate))
            {
                return candidate;
            }
        }

        return bot.Location;
    }

    /// <summary>
    ///     Что от бота остаётся после перезапуска.
    ///
    ///     Раньше здесь сохранялся один-единственный флаг «города уже заселены», а профиль
    ///     объявлялся «дешевле построить заново». На деле заново он строился ПУСТЫМ, и это
    ///     стоило дорого:
    ///
    ///     — Маяк держал список своих ботов только в памяти и после загрузки мира считал,
    ///       что у него нет ни одного. Он честно доспавнивал полное поголовье заново — при
    ///       живых прежних, которые никуда не делись. Каждый перезапуск умножал население
    ///       на маяк, и это и есть главная причина, по которой ботов становилось столько,
    ///       что [ClearBots начинал висеть.
    ///     — HomeCity и CurrentCity при повторной регистрации подставлялись null. Бот
    ///       переставал понимать, откуда он: поход в банк, переезд между городами и
    ///       возвращение домой теряли точку отсчёта.
    ///     — Характер (BotPersonality) бросался заново. Осторожный трус после рестарта мог
    ///       стать бесстрашным, и наоборот — «характеры» существовали ровно до первого
    ///       перезапуска.
    ///
    ///     Хранится намеренно немногое: привязка к маяку, город и характер. Занятие,
    ///     маршрут и цель добычи восстанавливать незачем — бот и должен начинать день
    ///     сначала.
    /// </summary>
    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(1); // version

        var count = 0;

        foreach (var (bot, profile) in Bots)
        {
            if (bot is BotMobile { Deleted: false })
            {
                count++;
            }
        }

        writer.WriteEncodedInt(count);

        foreach (var (bot, profile) in Bots)
        {
            if (bot is not BotMobile { Deleted: false })
            {
                continue;
            }

            writer.Write(bot);
            writer.Write(profile.OwnerBeacon);
            writer.Write(profile.HomeCity);
            writer.Write(profile.CurrentCity);
            writer.WriteEncodedInt((int)profile.Personality.Trait);
            writer.Write(profile.Stationary);
            writer.Write(profile.IsPK);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        if (version < 1)
        {
            reader.ReadBool(); // бывший флаг «города заселены» — заселение удалено
            return;
        }

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var bot = reader.ReadEntity<PlayerMobile>();
            var beacon = reader.ReadEntity<Item>();
            var homeCity = reader.ReadString();
            var currentCity = reader.ReadString();
            var trait = (PersonalityTrait)reader.ReadEncodedInt();
            var stationary = reader.ReadBool();
            var isPk = reader.ReadBool();

            if (bot?.Deleted != false)
            {
                continue;
            }

            // Профиля ещё нет: Initialize с его перерегистрацией идёт после загрузки мира.
            // Складываем прочитанное во временный список и раздаём там же.
            _restored[bot] = (beacon, homeCity, currentCity, trait, stationary, isPk);
        }
    }

    /// <summary>Прочитанное из сейва до того, как боты будут зарегистрированы.</summary>
    private static readonly Dictionary<PlayerMobile,
        (Item beacon, string homeCity, string currentCity, PersonalityTrait trait, bool stationary, bool isPk)>
        _restored = new();
}
