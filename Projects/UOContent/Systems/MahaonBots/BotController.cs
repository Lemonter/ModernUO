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
/// </summary>
public class BotController : GenericPersistence
{
    private static BotController _instance;

    private static readonly Dictionary<PlayerMobile, BotProfile> Bots = new();

    public static bool TryGetProfile(PlayerMobile bot, out BotProfile profile) => Bots.TryGetValue(bot, out profile);
    private static readonly Queue<PlayerMobile> Conveyor = new();

    public static int CountActive() => Bots.Count;

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

    private static readonly List<BotParty> ActiveParties = new();

    // Only seed the default city population once, ever — not on every restart.
    private static bool _seeded;

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
    }

    [Usage("SeedBots")]
    [Description("Disabled — bots now only spawn from a MahaonBotBeacon statue. Left registered so the command doesn't just error out if someone still types it.")]
    private static void SeedBots_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage(0x22, "Отключено — теперь боты появляются только от статуи-маяка ([AddLem).");
    }

    [Usage("ClearBots")]
    [Description("Deletes every currently-tracked bot in one go and resets seeding, so [SeedBots starts fresh.")]
    private static void ClearBots_OnCommand(CommandEventArgs e)
    {
        var count = Bots.Count;
        var bots = new List<PlayerMobile>(Bots.Keys);

        foreach (var bot in bots)
        {
            bot.Delete();
        }

        Bots.Clear();
        Conveyor.Clear();
        _seeded = false;

        e.Mobile.SendMessage(0x59, $"Удалено ботов: {count}.");
    }

    public static void Initialize()
    {
        _pollTimer = Timer.DelayCall(PollInterval, PollInterval, PollConveyor);
        _movementTimer = Timer.DelayCall(MovementInterval, MovementInterval, PollMovement);

        // Re-attach any bot Mobiles that survived a restart but aren't tracked yet
        // (BotController's own bookkeeping doesn't persist — the Mobiles do).
        foreach (var m in World.Mobiles.Values)
        {
            if (m is BotMobile bot && !Bots.ContainsKey(bot))
            {
                RegisterBot(bot, bot.Location, bot.Map);
            }
        }

        // Bots now only ever spawn from a MahaonBotBeacon statue — the old automatic
        // city/traveler/named-bot seeding is disabled. The methods are still here if this
        // ever needs to come back, just not called anymore.
    }

    private static void SeedCityPopulations()
    {
        const int perCity = 50;

        foreach (var (cityName, info) in CityControlSystem.Cities)
        {
            for (var i = 0; i < perCity; i++)
            {
                var loc = FindSpawnableSpot(info.spawn, info.map, 20);

                var name = BotNameGenerator.Generate();
                var bot = BotMobile.Create(name, loc, info.map);
                RegisterBot(bot, loc, info.map, cityName);
            }
        }
    }

    /// <summary>
    ///     Jitters around a center point and recomputes Z at the actual candidate tile each
    ///     time — using the center's fixed Z with an X/Y offset was the bug behind bots
    ///     spawning sunk into the ground on any terrain with elevation changes nearby.
    /// </summary>
    /// <summary>
    ///     Expanding-ring search instead of a handful of random tries — random sampling
    ///     kept failing near imprecise city-center coordinates (leftover RunUO-era guesses)
    ///     and dumping bots exactly on top of each other at the center, which looked like
    ///     "the city is empty" even though the bots technically existed.
    /// </summary>
    private static Point3D FindSpawnableSpot(Point3D center, Map map, int radius)
    {
        if (map.CanSpawnMobile(center))
        {
            // still worth checking a couple of random offsets first so not literally
            // everyone lands on the exact same tile when the center itself is fine
            for (var attempt = 0; attempt < 6; attempt++)
            {
                var rx = center.X + Utility.RandomMinMax(-radius, radius);
                var ry = center.Y + Utility.RandomMinMax(-radius, radius);
                var rz = map.GetAverageZ(rx, ry);
                var randomCandidate = new Point3D(rx, ry, rz);

                if (map.CanSpawnMobile(randomCandidate))
                {
                    return randomCandidate;
                }
            }

            return center;
        }

        for (var ring = 1; ring <= radius; ring++)
        {
            for (var dx = -ring; dx <= ring; dx++)
            {
                for (var dy = -ring; dy <= ring; dy++)
                {
                    if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != ring)
                    {
                        continue; // only the current ring, inner rings already covered
                    }

                    var x = center.X + dx;
                    var y = center.Y + dy;
                    var z = map.GetAverageZ(x, y);
                    var candidate = new Point3D(x, y, z);

                    if (map.CanSpawnMobile(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        // Genuinely nothing walkable found within range at all — the city-center
        // coordinate itself is probably bad (still using an old RunUO-era guess instead of
        // a real [MarkCitySpot). Falling back to center is the least-bad option, but this
        // city needs its center re-marked.
        return center;
    }

    private static void SeedTravelers()
    {
        const int travelerCount = 20;
        var cities = new List<string>(CityControlSystem.Cities.Keys);

        for (var i = 0; i < travelerCount; i++)
        {
            var cityName = cities.RandomElement();
            var info = CityControlSystem.Cities[cityName];

            var loc = FindSpawnableSpot(info.spawn, info.map, 10);

            var name = BotNameGenerator.Generate();
            var bot = BotMobile.Create(name, loc, info.map, BotArchetype.Trader);
            RegisterBot(bot, loc, info.map, cityName);
        }
    }

    private static void SeedNamedBots()
    {
        // Urgot: a PK who sticks to Britain specifically.
        if (CityControlSystem.Cities.TryGetValue("Britain", out var britain))
        {
            var urgot = BotMobile.Create("Urgot", britain.spawn, britain.map, BotArchetype.Warrior);
            urgot.IsPk = true;
            RegisterBot(urgot, britain.spawn, britain.map, "Britain");
            if (Bots.TryGetValue(urgot, out var urgotProfile))
            {
                urgotProfile.IsPK = true;
                urgotProfile.Stationary = true;
            }
        }

        // Разарио Агро: a dedicated traveling trader who's also a PK — travels the roads
        // and preys on whoever he finds along the way.
        var traderCities = new List<string>(CityControlSystem.Cities.Keys);
        var startCity = traderCities.RandomElement();
        var startInfo = CityControlSystem.Cities[startCity];

        var rozario = BotMobile.Create("Разарио Агро", startInfo.spawn, startInfo.map, BotArchetype.Trader);
        rozario.IsPk = true;
        RegisterBot(rozario, startInfo.spawn, startInfo.map, startCity);
        if (Bots.TryGetValue(rozario, out var rozarioProfile))
        {
            rozarioProfile.IsPK = true;
        }

        // Strider: forever picks fights with PK bots and forever loses — deliberately
        // gutted stats/skills so a real fight is a foregone conclusion.
        if (CityControlSystem.Cities.TryGetValue("Yew", out var yew))
        {
            var strider = BotMobile.Create("Strider", yew.spawn, yew.map, BotArchetype.Warrior);
            strider.RawStr = 15;
            strider.RawDex = 15;
            strider.RawInt = 15;
            strider.Skills[SkillName.Swords].Base = 5;
            strider.Skills[SkillName.Tactics].Base = 5;
            strider.Skills[SkillName.Parry].Base = 0;
            strider.Hits = strider.HitsMax;
            RegisterBot(strider, yew.spawn, yew.map, "Yew");
            if (Bots.TryGetValue(strider, out var striderProfile))
            {
                striderProfile.IsChallenger = true;
            }
        }

        // Alard: wants absolutely no part of any of this.
        if (CityControlSystem.Cities.TryGetValue("Moonglow", out var moonglow))
        {
            var alard = BotMobile.Create("Alard", moonglow.spawn, moonglow.map, BotArchetype.Crafter);
            RegisterBot(alard, moonglow.spawn, moonglow.map, "Moonglow");
            if (Bots.TryGetValue(alard, out var alardProfile))
            {
                alardProfile.IsCoward = true;
            }
        }
    }

    /// <summary>
    ///     Called right before a bot's normal death processing strips its gear into the
    ///     corpse — clones everything it's wearing/carrying into its own bank first, so
    ///     death doesn't actually cost it anything long-term. The corpse still drops
    ///     normally for whoever's fighting it.
    /// </summary>
    public static void SnapshotGearToBank(BotMobile bot)
    {
        var bank = bot.BankBox;
        if (bank == null)
        {
            return;
        }

        var toClone = new List<Item>();

        foreach (var item in bot.Items)
        {
            if (item.Layer is Layer.Backpack or Layer.Bank || item is Backpack or BankBox)
            {
                continue;
            }

            toClone.Add(item);
        }

        if (bot.Backpack != null)
        {
            toClone.AddRange(bot.Backpack.Items);
        }

        foreach (var item in toClone)
        {
            if (item is Container)
            {
                continue; // keep this simple — no nested bags of clones
            }

            try
            {
                var clone = (Item)Activator.CreateInstance(item.GetType());
                item.Dupe(clone);
                bank.DropItem(clone);
            }
            catch
            {
                // Some item types don't have a parameterless constructor — skip those
                // rather than fail the whole snapshot over one odd item.
            }
        }

        PruneBankToBest(bank);
    }

    /// <summary>Keeps the bank from growing forever across repeated deaths — for armor,
    /// only the single best-rated item per layer survives; for weapons, only the single
    /// best-damage item per weapon layer (one/two-handed) survives. Everything else that
    /// accumulated gets deleted, worst first.</summary>
    private static void PruneBankToBest(BankBox bank)
    {
        var bestArmor = new Dictionary<Layer, BaseArmor>();
        var bestWeapon = new Dictionary<Layer, BaseWeapon>();
        var toDelete = new List<Item>();

        foreach (var item in bank.Items)
        {
            switch (item)
            {
                case BaseArmor armor:
                {
                    if (!bestArmor.TryGetValue(armor.Layer, out var current) ||
                        armor.ArmorRatingScaled > current.ArmorRatingScaled)
                    {
                        if (current != null)
                        {
                            toDelete.Add(current);
                        }

                        bestArmor[armor.Layer] = armor;
                    }
                    else
                    {
                        toDelete.Add(armor);
                    }

                    break;
                }

                case BaseWeapon weapon:
                {
                    if (!bestWeapon.TryGetValue(weapon.Layer, out var current) ||
                        weapon.MinDamage + weapon.MaxDamage > current.MinDamage + current.MaxDamage)
                    {
                        if (current != null)
                        {
                            toDelete.Add(current);
                        }

                        bestWeapon[weapon.Layer] = weapon;
                    }
                    else
                    {
                        toDelete.Add(weapon);
                    }

                    break;
                }
            }
        }

        foreach (var item in toDelete)
        {
            item.Delete();
        }
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
            profile.Activity = BotActivity.Idle;
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
            PrefersNecromancy = bot is BotMobile { Archetype: BotArchetype.Mage } && Utility.RandomDouble() < 0.3
        };

        Bots[bot] = profile;
        Conveyor.Enqueue(bot);
    }

    public static void UnregisterBot(PlayerMobile bot)
    {
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

    private static readonly TimeSpan MinFightingMageThink = TimeSpan.FromSeconds(1.0);
    private static readonly TimeSpan MaxFightingMageThink = TimeSpan.FromSeconds(1.8);

    private static TimeSpan FightingMageThink() =>
        MinFightingMageThink + (MaxFightingMageThink - MinFightingMageThink) * Utility.RandomDouble();

    private static void PollConveyor()
    {
        if (Bots.Count == 0)
        {
            return;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var processed = 0;

        while (processed < BatchSize && Conveyor.Count > 0)
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

                var isFightingMage = bot is BotMobile { Archetype: BotArchetype.Mage } &&
                                      bot.Combatant?.Deleted == false && bot.Combatant.Alive;

                profile.NextDecisionTime = Core.Now + (isFightingMage ? FightingMageThink() : RandomThink(IsPlayerNearby(bot)));
            }

            Conveyor.Enqueue(bot);
        }

        sw.Stop();
        BotPerf.RecordConveyor(sw.Elapsed.Ticks, processed);
    }

    // -- Fast movement pass ---------------------------------------------------------------
    //
    // Travel-type activities used to resolve dozens of tiles in one shot on the slow
    // decision cadence, which looked like teleporting. This runs much more often and moves
    // real bots one tile at a time, and lets them peel off to fight anything hostile they
    // walk past instead of ignoring it.

    // No cap here anymore — this used to silently starve most bots (a fixed-size dictionary
    // walk always hits the same subset first, so anyone past the cap never got a turn).
    // Per-bot movement work is cheap (a location check and maybe one Move() call), so
    // processing everyone currently traveling every tick is fine.
    private const int TravelEngageRange = 15;
    private const int ThreatAssessRange = 12;
    private const int GuildWarEngageRange = 12;

    private const int StuckTicksThreshold = 20;

    private static bool IsStuck(Mobile bot, BotProfile profile)
    {
        if (bot.Location == profile.LastCheckedPosition)
        {
            profile.StuckTicks++;
        }
        else
        {
            profile.StuckTicks = 0;
            profile.LastCheckedPosition = bot.Location;
        }

        return profile.StuckTicks >= StuckTicksThreshold;
    }

    private static void TeleportHomeStuck(Mobile bot, BotProfile profile)
    {
        profile.StuckTicks = 0;

        Point3D loc;
        Map map;

        var city = profile.CurrentCity ?? profile.HomeCity;

        if (city != null && CityControlSystem.Cities.TryGetValue(city, out var info))
        {
            loc = info.spawn;
            map = info.map;
        }
        else
        {
            loc = profile.HomeLocation;
            map = profile.HomeMap;
        }

        bot.MoveToWorld(loc, map);
        profile.Activity = BotActivity.Idle;
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

            if (bot is BotMobile { Archetype: BotArchetype.Archer } &&
                bot.Combatant?.Deleted == false && bot.Combatant.Alive)
            {
                TryArcherKite(bot);
                TryPursueCombatant(bot, ArcherMaxEffectiveRange);
                continue;
            }

            if (bot is BotMobile { Archetype: BotArchetype.Mage } &&
                bot.Combatant?.Deleted == false && bot.Combatant.Alive)
            {
                TryMageKite(bot);
                TryPursueCombatant(bot, MageMaxEffectiveRange);
                continue;
            }

            if (bot.Combatant?.Deleted == false && bot.Combatant.Alive)
            {
                TryPursueCombatant(bot, MeleeRange);
                continue;
            }

            if (!IsTravelActivity(profile.Activity))
            {
                continue;
            }

            var isFighting = bot.Combatant?.Deleted == false && bot.Combatant.Alive;

            if (!isFighting && IsStuck(bot, profile))
            {
                TeleportHomeStuck(bot, profile);
                continue;
            }

            if (TryEngageWhileTraveling(bot))
            {
                continue; // fighting something right now — movement resumes once it's dead
            }

            Dispatch(bot, profile);
        }

        sw.Stop();
        BotPerf.RecordMovement(sw.Elapsed.Ticks, touched);
    }

    // -- Archer kiting: keep distance instead of walking into melee range ------------------

    private const int ArcherIdealRange = 6;
    private const int ArcherTooCloseRange = 3;

    private const int MeleeRange = 1;
    private const int ArcherMaxEffectiveRange = 10;
    private const int MageIdealRange = 5;
    private const int MageTooCloseRange = 2;
    private const int MageMaxEffectiveRange = 8;

    /// <summary>
    ///     Bots are PlayerMobile, not BaseCreature — they never had the automatic "walk
    ///     toward my target" behavior real monster AI gets for free. Without this, a bot
    ///     just stands there swinging at nothing the moment its target steps out of weapon
    ///     range (or straight-up flees). Uses real pathfinding, same as travel does.
    /// </summary>
    private static void TryPursueCombatant(Mobile bot, int desiredRange)
    {
        var target = bot.Combatant;
        if (target == null || bot.Map != target.Map)
        {
            return;
        }

        if (bot.InRange(target.Location, desiredRange))
        {
            return; // close enough to actually fight — let the swing/shot timer do its thing
        }

        StepToward(bot, target.Location, 1);
    }

    private static void TryArcherKite(Mobile bot)
    {
        var target = bot.Combatant;
        if (target == null || bot.Map == null)
        {
            return;
        }

        var distance = bot.GetDistanceToSqrt(target.Location);

        if (distance >= ArcherTooCloseRange)
        {
            return; // close enough to be a problem, but not point-blank — let the shot fly
        }

        // Too close for comfort — step directly away from the target instead of standing
        // there trading melee hits with a bow in hand.
        var dx = bot.X - target.X;
        var dy = bot.Y - target.Y;

        if (dx == 0 && dy == 0)
        {
            dx = Utility.RandomMinMax(-1, 1);
            dy = Utility.RandomMinMax(-1, 1);
        }

        var fleeTo = new Point3D(
            bot.X + System.Math.Sign(dx) * ArcherIdealRange,
            bot.Y + System.Math.Sign(dy) * ArcherIdealRange,
            bot.Z
        );

        StepToward(bot, fleeTo, 1);
    }

    private static void TryMageKite(Mobile bot)
    {
        var target = bot.Combatant;
        if (target == null || bot.Map == null)
        {
            return;
        }

        // Don't step away mid-cast — that would just disturb our own spell for nothing.
        if (bot.Spell?.IsCasting == true)
        {
            return;
        }

        var distance = bot.GetDistanceToSqrt(target.Location);

        if (distance >= MageTooCloseRange)
        {
            return; // far enough to cast safely
        }

        var dx = bot.X - target.X;
        var dy = bot.Y - target.Y;

        if (dx == 0 && dy == 0)
        {
            dx = Utility.RandomMinMax(-1, 1);
            dy = Utility.RandomMinMax(-1, 1);
        }

        var fleeTo = new Point3D(
            bot.X + System.Math.Sign(dx) * MageIdealRange,
            bot.Y + System.Math.Sign(dy) * MageIdealRange,
            bot.Z
        );

        StepToward(bot, fleeTo, 1);
    }

    private static bool IsTravelActivity(BotActivity activity) => activity switch
    {
        BotActivity.TravelingToGather => true,
        BotActivity.ReturningFromGather => true,
        BotActivity.TravelingToDungeon => true,
        BotActivity.ReturningHome => true,
        BotActivity.TravelingToMarket => true,
        BotActivity.BankingTrip => true,
        BotActivity.CityTraveling => true,
        BotActivity.TravelingToHunt => true,
        BotActivity.ReturningFromHunt => true,
        _ => false
    };

    // -- Ranger taming: spots wildlife using Tracking, tames it using real AnimalTaming ---

    private static bool TryTameNearby(BotMobile bot)
    {
        if (bot.Map == null)
        {
            return false;
        }

        var trackingSkill = bot.Skills[SkillName.Tracking].Value;
        var range = 4 + (int)(trackingSkill / 10.0); // better Tracking spots wildlife farther off

        BaseCreature target = null;

        foreach (var creature in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, range))
        {
            if (creature.Tamable && !creature.Controlled && creature.Alive && !creature.Deleted)
            {
                target = creature;
                break;
            }
        }

        if (target == null)
        {
            return false;
        }

        bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Попробую приручить {target.Name}.");

        Server.SkillHandlers.AnimalTaming.OnUse(bot);

        if (Bots.TryGetValue(bot, out var profile))
        {
            profile.PendingTameTarget = target;
        }

        return true;
    }

    private static void ResolvePendingTame(PlayerMobile bot, BotProfile profile)
    {
        var target = profile.PendingTameTarget;
        profile.PendingTameTarget = null;

        if (bot.Target == null || target?.Deleted != false || !target.Alive)
        {
            return;
        }

        bot.Target.Invoke(bot, target);
    }

    /// <summary>
    ///     Guild war has real teeth: spotting a member of a guild yours is at war with is
    ///     a fight trigger on its own, independent of whatever this bot was otherwise doing
    ///     (gathering, traveling, wandering) — same as PK bots already interrupt whatever
    ///     they're doing to hunt. Never triggers against an allied guild, obviously.
    /// </summary>
    private static bool TryEngageGuildEnemy(PlayerMobile bot)
    {
        if (bot.Guild is not Guilds.Guild)
        {
            return false; // not in a guild at all — nothing to be at war over
        }

        foreach (var m in bot.Map.GetMobilesInRange<Mobile>(bot.Location, GuildWarEngageRange))
        {
            if (m == bot || !m.Alive || m.Deleted || m.Hidden)
            {
                continue;
            }

            if (!BotGuilds.IsAtWar(bot, m))
            {
                continue;
            }

            bot.CriminalAction(false);
            ApplyCombatTactics(bot, m);
            bot.Combatant = m;
            bot.Warmode = true;
            return true;
        }

        return false;
    }

    private static bool TryEngageWhileTraveling(PlayerMobile bot)
    {
        if (bot.Combatant?.Deleted == false && bot.Combatant.Alive)
        {
            return true; // already fighting — hold position and let it resolve
        }

        if (bot.Map == null)
        {
            return false;
        }

        var lootRange = bot is BotMobile { Archetype: BotArchetype.Archer } ? ArcherIdealRange + 2 : CorpseSearchRangeWhileTraveling;

        if (TryLootCorpse(bot, lootRange))
        {
            return true; // grabbing what they just killed before wandering off
        }

        foreach (var creature in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, TravelEngageRange))
        {
            if (creature.Alive && !creature.Deleted && creature is not IRaidSpawn && !creature.Controlled &&
                ShouldEngage(bot, creature))
            {
                ApplyCombatTactics(bot, creature);
                bot.Combatant = creature;
                bot.Warmode = true;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Weighs it up before committing to a fight — counts real nearby allies (other
    ///     bots, non-hostile) against real nearby threats (wild creatures) instead of just
    ///     charging whatever's closest. Doesn't guarantee a fair fight, just a sane one —
    ///     heavily outnumbered gets skipped rather than picked anyway.
    /// </summary>
    private static bool ShouldEngage(Mobile bot, Mobile enemy)
    {
        var allyCount = 0;
        var enemyCount = 1; // the one already being considered

        foreach (var nearby in bot.Map.GetMobilesInRange<Mobile>(bot.Location, ThreatAssessRange))
        {
            if (nearby == bot || nearby == enemy || !nearby.Alive || nearby.Deleted)
            {
                continue;
            }

            if (nearby is BaseCreature bc && !bc.Controlled)
            {
                enemyCount++;
            }
            else if (nearby is BotMobile otherBot && Bots.TryGetValue(otherBot, out var otherProfile) && !otherProfile.IsPK)
            {
                allyCount++;
            }
        }

        // Crafters and Bards aren't fighters — they need overwhelming odds before they'll
        // even consider it, not just "roughly even". Everyone else keeps the normal rule.
        var isNonCombat = bot is BotMobile { Archetype: BotArchetype.Crafter } ||
                          IsProfessionCategory(bot, ProfessionCategory.Bard);

        if (isNonCombat)
        {
            return allyCount >= enemyCount * 3;
        }

        // Fine solo or roughly even; skip if seriously outnumbered.
        return enemyCount <= allyCount + 1;
    }

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

    private static void Dispatch(PlayerMobile bot, BotProfile profile)
    {
        if (bot.Warmode && (bot.Combatant?.Deleted != false || !bot.Combatant.Alive))
        {
            bot.Warmode = false;
        }

        var isFightingNow = bot.Combatant?.Deleted == false && bot.Combatant.Alive;

        if (profile.BardAction != BotProfile.PendingBardAction.None && bot.Target is Target pendingBardTarget)
        {
            ResolvePendingBardTarget(bot, profile, pendingBardTarget);
            return;
        }

        if (!isFightingNow && bot.Map != null && !IsWatchedByGuards(bot) &&
            TryEngageGuildEnemy(bot))
        {
            return;
        }

        if (Bots.TryGetValue(bot, out var petCheckProfile) && petCheckProfile.SummonedPet?.Deleted == false)
        {
            if (isFightingNow)
            {
                TrySyncPet(bot, petCheckProfile);
            }
            else
            {
                TryDismissPet(bot, petCheckProfile);
            }
        }

        if (bot is BotMobile)
        {
            TryReplenishSupplies(bot);
        }

        if (bot is BotMobile { Archetype: BotArchetype.Mage } meditatingMage && !isFightingNow &&
            meditatingMage.Mana < meditatingMage.ManaMax && meditatingMage.Skills[SkillName.Meditation].Value >= 20 &&
            !meditatingMage.Meditating)
        {
            SkillHandlers.Meditation.OnUse(meditatingMage);
        }

        if (isFightingNow)
        {
            TryGainStat(bot, StatType.Str);
            TryGainStat(bot, StatType.Dex);
        }

        if (isFightingNow && bot.HitsMax > 0 && (double)bot.Hits / bot.HitsMax < FleeHealthThreshold)
        {
            TryDrinkHealPotion(bot);

            if (IsProfessionCategory(bot, ProfessionCategory.Thief) &&
                bot.Skills[SkillName.Ninjitsu].Value >= 40 && bot.Mana >= new MirrorImage(bot, null).GetMana() / 10)
            {
                new MirrorImage(bot, null).Cast();
            }

            FleeFromCombat(bot);
            return;
        }

        if (isFightingNow && bot.Combatant is BaseCreature && !ShouldEngage(bot, bot.Combatant))
        {
            var isNonCombatBot = bot is BotMobile { Archetype: BotArchetype.Crafter } ||
                                 IsProfessionCategory(bot, ProfessionCategory.Bard);

            if (Utility.RandomDouble() < (isNonCombatBot ? 0.6 : 0.15))
            {
                // Re-weighed the odds mid-fight (more of them showed up, allies died off) —
                // bail while there's still a chance to. Rolled, not automatic, so it isn't
                // an instant flee the moment the count tips.
                FleeFromCombat(bot);
                return;
            }
        }

        if (bot is BotMobile { Archetype: BotArchetype.Mage } universalMageBot &&
            (isFightingNow || bot.Poisoned || FindMostWoundedPartyMember(bot, profile) != null))
        {
            // Covers every activity uniformly — dungeon, solo hunting, mid-travel combat,
            // whatever — instead of only working in the one or two spots that happened to
            // call DoMageAction by hand.
            DoMageAction(universalMageBot, profile);
            return;
        }

        if (isFightingNow && Core.Now >= profile.NextBardSkillTime &&
            ProfessionSystem.TouchesCategory(bot, ProfessionCategory.Bard) &&
            bot.Backpack?.FindItemByType<BaseInstrument>() != null &&
            TryUseBardSkill(bot, profile))
        {
            return;
        }

        if (profile.PendingTameTarget != null)
        {
            ResolvePendingTame(bot, profile);
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

    // -- Idle / wandering ------------------------------------------------------------

    private static void DoIdle(PlayerMobile bot, BotProfile profile)
    {
        if (profile.IsCoward && TryCowardFlee(bot, profile))
        {
            return;
        }

        if (profile.IsChallenger && TryChallengePK(bot))
        {
            return;
        }

        if (bot is BotMobile { } thiefBot && ProfessionSystem.GetProfession(thiefBot) is { } thiefProfession &&
            ProfessionData.All[thiefProfession].Category == ProfessionCategory.Thief &&
            !IsWatchedByGuards(bot) && Utility.RandomDouble() < 0.15 && TryStealFromNearby(bot))
        {
            return;
        }

        if (ProfessionSystem.TouchesCategory(bot, ProfessionCategory.Bard) && Utility.RandomDouble() < 0.15 && TryBegNearby(bot))
        {
            return;
        }

        if (!bot.Hidden && IsProfessionCategory(bot, ProfessionCategory.Thief) && !bot.Warmode &&
            bot.CheckSkill(SkillName.Hiding, 0.0, 100.0))
        {
            bot.Hidden = true;
            bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "*растворяется в тенях*");
        }

        if (bot.Hidden && IsProfessionCategory(bot, ProfessionCategory.Thief) && bot.AllowedStealthSteps <= 0)
        {
            SkillHandlers.Stealth.OnUse(bot); // real stealth steps — otherwise moving would instantly reveal them
        }

        if (bot is BotMobile { Archetype: BotArchetype.Archer } tamerBot && bot.Target == null &&
            Utility.RandomDouble() < 0.1 && TryTameNearby(tamerBot))
        {
            return;
        }

        if (profile.IsPK && Utility.RandomDouble() < 0.8)
        {
            profile.Activity = BotActivity.Hunting;
            return;
        }

        if (Utility.RandomDouble() < 0.2)
        {
            profile.Activity = BotActivity.BankingTrip;
            return;
        }

        if (Utility.RandomDouble() < 0.05)
        {
            TryAcquireMount(bot);
        }

        var isTrader = bot is BotMobile { Archetype: BotArchetype.Trader };

        if (!profile.Stationary)
        {
            var travelChance = isTrader ? 0.5 : 0.03;
            if (Utility.RandomDouble() < travelChance)
            {
                var destination = PickTravelDestination(profile.CurrentCity);
                if (destination != null)
                {
                    profile.TravelDestinationCity = destination;
                    profile.Activity = BotActivity.CityTraveling;
                    return;
                }
            }
        }

        // No AFK at all anymore — every time a bot is free to decide, it picks something
        // real to do. The "roulette" always lands on a mission, and the sectors are sized
        // differently per archetype — a warrior's disc is mostly dungeon, a crafter's is
        // mostly gathering, and so on.
        var canHunt = !profile.IsPK && bot is BotMobile { Archetype: BotArchetype.Warrior or BotArchetype.Archer or BotArchetype.Mage };
        var archetype = (bot as BotMobile)?.Archetype;

        // (wander, gather, hunt, dungeon) weights — must sum to 1.0 per archetype.
        var (wWander, wGather, wHunt, wDungeon) = archetype switch
        {
            BotArchetype.Warrior => (0.15, 0.15, 0.25, 0.45),
            BotArchetype.Mage    => (0.15, 0.30, 0.15, 0.40),
            BotArchetype.Archer  => (0.15, 0.20, 0.40, 0.25),
            BotArchetype.Crafter => (0.20, 0.70, 0.05, 0.05),
            _                    => (0.30, 0.35, 0.15, 0.20) // trader and anything unlisted
        };

        var roll = Utility.RandomDouble();

        if (roll < wWander)
        {
            profile.Activity = BotActivity.Wandering;
        }
        else if (roll < wWander + wGather)
        {
            profile.Activity = BotActivity.TravelingToGather;
        }
        else if (roll < wWander + wGather + wHunt && canHunt)
        {
            profile.Activity = BotActivity.TravelingToHunt;
        }
        else
        {
            profile.Activity = BotActivity.FormingParty; // the "dungeon" sector
        }

        if (profile.Activity == BotActivity.TravelingToGather)
        {
            profile.GatherSkill = Utility.RandomList(SkillName.Mining, SkillName.Lumberjacking, SkillName.Fishing);
            profile.CyclesRemaining = Utility.RandomMinMax(3, 8);
            profile.GatherDestination = PickGatherSpot(bot, profile);
        }
        else if (profile.Activity == BotActivity.TravelingToHunt)
        {
            profile.CyclesRemaining = Utility.RandomMinMax(4, 10);
            profile.GatherDestination = bot is BotMobile huntBot && huntBot.PickKnownHuntingSpot() is { } known
                ? known
                : PickGatherSpot(bot, profile); // no known fight spots yet — scout blind, same as gathering does
        }

        AnnounceNewActivity(bot, profile);
    }

    // -- Bot speech: a debug trail as much as flavor — lets you watch what each bot just
    // decided to do without opening a debugger. Kept short, one line, only on activity
    // change (not spammed every tick).

    private static void AnnounceNewActivity(Mobile bot, BotProfile profile)
    {
        string line = profile.Activity switch
        {
            BotActivity.Wandering         => "Пройдусь пока.",
            BotActivity.TravelingToGather => profile.GatherSkill switch
            {
                SkillName.Mining        => "Пойду покопаю руды.",
                SkillName.Lumberjacking => "Пойду нарублю дерева.",
                SkillName.Fishing       => "Пойду порыбачу.",
                _                       => "Пойду добуду ресурсов."
            },
            BotActivity.TravelingToHunt   => "Пойду поохочусь на монстров.",
            BotActivity.FormingParty      => "Ищу пати в подземелье.",
            BotActivity.BankingTrip       => "Загляну в банк.",
            BotActivity.CityTraveling     => $"Пора съездить в {profile.TravelDestinationCity}.",
            BotActivity.Hunting           => "Кого бы тут поймать...",
            _                             => null
        };

        if (line != null)
        {
            bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, line);
        }
    }



    // -- Gathering trip: real travel out, real travel back -------------------------------
    //
    // Picks a spot some real distance away, walks there over several ticks, digs/chops for
    // a while, then walks all the way back to town before handing off to crafting (which
    // reads as "brings the ore back and smelts it"). No more teleport-style instant resource
    // generation in place — the whole point is that this takes visible time and movement.

    private const int GatherMinDistance = 15;
    private const int GatherMaxDistance = 300;

    // -- Resource depletion: don't let bots camp the same spot forever --------------------

    private static readonly Dictionary<(Point3D, Map), int> NodeUses = new();
    private static readonly Dictionary<(Point3D, Map), DateTime> NodeDepletedUntil = new();

    private const int UsesBeforeDepletion = 5;
    private static readonly TimeSpan DepletionRecovery = TimeSpan.FromHours(2);

    private static bool IsDepleted(Point3D loc, Map map) =>
        NodeDepletedUntil.TryGetValue((loc, map), out var until) && Core.Now < until;

    private static void RegisterNodeUse(Point3D loc, Map map)
    {
        var key = (loc, map);
        var uses = NodeUses.GetValueOrDefault(key, 0) + 1;
        NodeUses[key] = uses;

        if (uses >= UsesBeforeDepletion)
        {
            NodeDepletedUntil[key] = Core.Now + DepletionRecovery;
            NodeUses[key] = 0;
        }
    }

    private static Point3D PickGatherSpot(PlayerMobile bot, BotProfile profile)
    {
        if (bot is BotMobile botMobile)
        {
            // Try a few times to land on a remembered spot that isn't currently tapped out
            // before giving up and scouting somewhere new.
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var known = profile.GatherSkill switch
                {
                    SkillName.Mining        => botMobile.PickKnownMineSpot(),
                    SkillName.Lumberjacking => botMobile.PickKnownTreeSpot(),
                    SkillName.Fishing       => botMobile.PickKnownFishSpot(),
                    _                       => null
                };

                if (known is not { } spot)
                {
                    break; // no known spots at all — go scout
                }

                if (!IsDepleted(spot, bot.Map))
                {
                    return spot;
                }
            }
        }

        return FindRealGatherSpot(bot, profile.GatherSkill) ?? BlindScoutPoint(bot);
    }

    /// <summary>
    ///     Searches outward in rings for real terrain matching the skill — an actual
    ///     mountainside for Mining, a real tree static for Lumberjacking, real water for
    ///     Fishing. Gathering runs on genuine terrain now, not a simulation, so scouting
    ///     blind to an arbitrary empty point (the old behavior) meant arriving somewhere
    ///     with nothing to find at all.
    /// </summary>
    private static Point3D? FindRealGatherSpot(Mobile bot, SkillName skill)
    {
        var map = bot.Map;
        if (map == null)
        {
            return null;
        }

        var system = GetHarvestSystem(skill);
        var def = system?.GetDefinition();
        if (def == null)
        {
            return null;
        }

        for (var radius = GatherMinDistance; radius <= GatherMaxDistance; radius += 4)
        {
            for (var attempt = 0; attempt < 6; attempt++)
            {
                var angle = Utility.RandomDouble() * System.Math.PI * 2;
                var cx = bot.X + (int)(System.Math.Cos(angle) * radius);
                var cy = bot.Y + (int)(System.Math.Sin(angle) * radius);

                // Look in a small patch around this ring sample point for real matching
                // terrain, instead of demanding the exact sampled tile be a hit.
                for (var dx = -5; dx <= 5; dx++)
                {
                    for (var dy = -5; dy <= 5; dy++)
                    {
                        var x = cx + dx;
                        var y = cy + dy;

                        bool matches;
                        if (skill == SkillName.Lumberjacking)
                        {
                            matches = false;
                            foreach (var item in map.GetItemsInRange<Static>(new Point3D(x, y, 0), 0))
                            {
                                if (!item.Movable && Array.IndexOf(def.StaticTiles, item.ItemID) >= 0)
                                {
                                    matches = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            var lt = map.Tiles.GetLandTile(x, y);
                            var tileId = lt.ID & TileData.MaxLandValue;
                            matches = skill == SkillName.Mining
                                ? (TileData.LandTable[tileId].Flags & TileFlag.Impassable) != 0
                                : Array.IndexOf(def.LandTiles, tileId) >= 0;
                        }

                        if (!matches)
                        {
                            continue;
                        }

                        if (skill == SkillName.Lumberjacking)
                        {
                            // Same deal as Mining — the tree tile itself is very likely
                            // impassable, so don't demand CanSpawnMobile on (x, y) directly.
                            // Try standing right there first (some tree graphics genuinely
                            // are walkable), then fall back to a neighboring tile, same as a
                            // real player would end up standing to chop it.
                            var treeZ = map.GetAverageZ(x, y);
                            var treeLoc = new Point3D(x, y, treeZ);

                            if (map.CanSpawnMobile(treeLoc))
                            {
                                return treeLoc;
                            }

                            foreach (var (nx, ny) in StandingSpotsNear(x, y))
                            {
                                var nz = map.GetAverageZ(nx, ny);
                                var nloc = new Point3D(nx, ny, nz);

                                if (map.CanSpawnMobile(nloc))
                                {
                                    return nloc;
                                }
                            }

                            continue; // no walkable spot at or next to this tree — keep looking
                        }

                        if (skill == SkillName.Mining)
                        {
                            // Don't send them to stand IN the mountain tile itself — find a
                            // real walkable spot next to it, same as a player would stand.
                            foreach (var (nx, ny) in StandingSpotsNear(x, y))
                            {
                                var nz = map.GetAverageZ(nx, ny);
                                var nloc = new Point3D(nx, ny, nz);

                                if (map.CanSpawnMobile(nloc))
                                {
                                    return nloc;
                                }
                            }

                            continue; // no walkable neighbor found here, keep looking
                        }

                        var z = map.GetAverageZ(x, y);
                        var loc = new Point3D(x, y, z);

                        if (map.CanSpawnMobile(loc))
                        {
                            return loc;
                        }
                    }
                }
            }
        }

        return null; // genuinely nothing found within range — fall back to blind scouting
    }

    private static IEnumerable<(int x, int y)> StandingSpotsNear(int x, int y)
    {
        yield return (x + 1, y);
        yield return (x - 1, y);
        yield return (x, y + 1);
        yield return (x, y - 1);
        yield return (x + 1, y + 1);
        yield return (x - 1, y - 1);
        yield return (x + 1, y - 1);
        yield return (x - 1, y + 1);
    }

    private static Point3D BlindScoutPoint(Mobile bot)
    {
        var angle = Utility.RandomDouble() * System.Math.PI * 2;
        var distance = Utility.RandomMinMax(GatherMinDistance, GatherMaxDistance);

        var x = bot.X + (int)(System.Math.Cos(angle) * distance);
        var y = bot.Y + (int)(System.Math.Sin(angle) * distance);
        var z = bot.Map?.GetAverageZ(x, y) ?? bot.Z;

        return new Point3D(x, y, z);
    }

    // -- Solo hunting trip: real travel out, fight whatever's around, real travel back ----

    private static void DoTravelToHunt(PlayerMobile bot, BotProfile profile)
    {
        if (!StepTowardPath(bot, profile, profile.GatherDestination))
        {
            return;
        }

        if (bot is BotMobile botMobile)
        {
            botMobile.RememberHuntingSpot(bot.Location);
        }

        profile.Activity = BotActivity.SoloHunting;
    }

    private static void DoSoloHunting(PlayerMobile bot, BotProfile profile)
    {
        if (profile.CyclesRemaining-- <= 0)
        {
            profile.Activity = BotActivity.ReturningFromHunt;
            return;
        }

        if (bot.Combatant?.Deleted == false && bot.Combatant.Alive)
        {
            return; // mid-fight, let it play out
        }

        if (TryLootCorpse(bot))
        {
            return;
        }

        if (bot.Map == null)
        {
            return;
        }

        foreach (var creature in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, 10))
        {
            if (creature.Alive && !creature.Deleted && creature is not IRaidSpawn && !creature.Controlled)
            {
                ApplyCombatTactics(bot, creature);
                bot.Combatant = creature;
                bot.Warmode = true;
                return;
            }
        }

        // Nothing here right now — drift a little instead of standing still waiting.
        StepToward(bot, RandomNearbyPoint(bot.Location, 8), 1);
    }

    private static void DoReturnFromHunt(PlayerMobile bot, BotProfile profile)
    {
        if (StepTowardPath(bot, profile, profile.HomeLocation))
        {
            profile.Activity = BotActivity.Idle;
        }
    }

    private static void DoTravelToGather(PlayerMobile bot, BotProfile profile)
    {
        if (!StepTowardPath(bot, profile, profile.GatherDestination))
        {
            return;
        }

        // Arrived — if the terrain actually matches what they were after, remember this
        // spot for next time instead of scouting blind again. Doesn't affect this trip's
        // yield either way — bots aren't that discerning on the first visit.
        if (bot is BotMobile botMobile && bot.Map != null)
        {
            switch (profile.GatherSkill)
            {
                case SkillName.Mining when Items.MiningDigTarget.IsNearMountain(bot.Map, bot.Location):
                    botMobile.RememberMineSpot(bot.Location);
                    break;
                case SkillName.Lumberjacking when IsRealTreeNear(bot.Map, bot.Location):
                    botMobile.RememberTreeSpot(bot.Location);
                    break;
                case SkillName.Fishing when IsNearWater(bot.Map, bot.Location):
                    botMobile.RememberFishSpot(bot.Location);
                    break;
            }
        }

        profile.Activity = BotActivity.Gathering;
    }

    /// <summary>
    ///     Whether there's a real, chop-able tree within harvest range of this spot — checked
    ///     against the same graphic list TryFindHarvestTarget uses, not the broader tiledata
    ///     Foliage flag (which also covers bushes/decorative plants that Lumberjacking's real
    ///     system doesn't recognize as trees at all). A spot only gets remembered as "good"
    ///     if a bot could actually chop something there.
    /// </summary>
    private static bool IsRealTreeNear(Map map, Point3D loc)
    {
        var def = GetHarvestSystem(SkillName.Lumberjacking)?.GetDefinition();
        if (def == null)
        {
            return false;
        }

        foreach (var item in map.GetItemsInRange<Static>(loc, HarvestSearchRadius))
        {
            if (!item.Movable && Array.IndexOf(def.StaticTiles, item.ItemID) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNearWater(Map map, Point3D loc)
    {
        for (var dx = -2; dx <= 2; dx++)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                var lt = map.Tiles.GetLandTile(loc.X + dx, loc.Y + dy);
                if (TileData.LandTable[lt.ID & TileData.MaxLandValue].Flags.HasFlag(TileFlag.Wet))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // -- Pack animal: Albion-style carry weight bonus, not a separate cargo container -----
    //
    // Owning a live transport animal raises the bot's own MaxWeight (see the hook in
    // PlayerMobile.MaxWeight) instead of giving it a separate backpack to stash goods in.
    // If the animal dies, the bonus disappears immediately — whatever the bot was carrying
    // that's now over the (lower) limit just sits there as dead weight until it drops
    // something, same as a real player going over-encumbered.

    private const long PackHorseCost = 300;

    public static BaseCreature GetOwnedTransportAnimal(PlayerMobile bot)
    {
        if (bot.AllFollowers == null)
        {
            return null;
        }

        foreach (var follower in bot.AllFollowers)
        {
            if (follower is PackHorse or PackLlama or Horse && follower is BaseCreature { Deleted: false, Alive: true } creature)
            {
                return creature;
            }
        }

        return null;
    }

    public static int GetCarryWeightBonus(PlayerMobile bot)
    {
        var animal = GetOwnedTransportAnimal(bot);
        if (animal == null)
        {
            return 0;
        }

        // Pack-type animals are our "ox" equivalent — bred for cargo, not speed — so they
        // carry a much bigger multiplier than a plain riding horse.
        var tierMultiplier = animal is PackHorse or PackLlama ? 6 : 3;
        return animal.RawStr * tierMultiplier / 10;
    }

    private static void TryAcquireMount(PlayerMobile bot)
    {
        if (GetOwnedTransportAnimal(bot) != null)
        {
            return;
        }

        var bank = bot.BankBox;
        if (bank == null || CurrencyHelper.GetCopperValue(bank) < CurrencyHelper.ToCopperValue((int)PackHorseCost, 0, 0))
        {
            return;
        }

        if (!CurrencyHelper.TryWithdrawCopperValue(bank, CurrencyHelper.ToCopperValue((int)PackHorseCost, 0, 0)))
        {
            return;
        }

        var horse = new PackHorse();
        horse.Controlled = true;
        horse.ControlMaster = bot;
        horse.MoveToWorld(bot.Location, bot.Map);
    }

    // -- Economy: bots spend what they earn instead of just hoarding gold ------------------
    //
    // Crafters put surplus gold toward blueprints they don't know yet (reusing the same
    // universal recipe system players use). Everyone else puts it toward training up one
    // of their archetype's core skills. Both are simplified — no gump/UI flow, bots pay and
    // apply the effect directly, since there's no player on the other end to click anything.
    //
    // Bots keep at most PocketCap gold on hand — anything over that gets banked
    // automatically (mirrors the "don't carry more than you can afford to lose to a PK"
    // instinct a real player would have). Spending pulls from the bank, not the pocket.

    private const long PocketCap = 20;

    private static void EnforcePocketCap(PlayerMobile bot)
    {
        var backpack = bot.Backpack;
        var bank = bot.BankBox;
        if (backpack == null || bank == null)
        {
            return;
        }

        var pocketValue = CurrencyHelper.GetCopperValue(backpack);
        var capValue = CurrencyHelper.ToCopperValue((int)PocketCap, 0, 0);

        if (pocketValue <= capValue)
        {
            return;
        }

        var excess = pocketValue - capValue;
        if (CurrencyHelper.TryWithdrawCopperValue(backpack, excess))
        {
            CurrencyHelper.DepositCopperValue(bank, excess);
        }
    }

    private static void DoBankingTrip(PlayerMobile bot, BotProfile profile)
    {
        var city = profile.CurrentCity ?? profile.HomeCity;
        Point3D bankLoc;
        Map bankMap;

        if (city != null && CityMarkers.TryGetMarker(city, "banker", out var markedLoc, out var markedMap))
        {
            bankLoc = markedLoc;
            bankMap = markedMap;
        }
        else if (city != null && CityControlSystem.Cities.TryGetValue(city, out var info))
        {
            bankLoc = info.spawn;
            bankMap = info.map;
        }
        else
        {
            // No known city at all — just handle it on the spot rather than getting stuck.
            TrySpendGold(bot);
            profile.Activity = BotActivity.Idle;
            return;
        }

        if (bot.Map != bankMap || !bot.InRange(bankLoc, 3))
        {
            StepTowardPath(bot, profile, bankLoc);
            return;
        }

        TrySpendGold(bot);
        profile.Activity = BotActivity.Idle;
    }

    private static void RestockGatherTools(PlayerMobile bot)
    {
        foreach (var skill in new[] { SkillName.Mining, SkillName.Lumberjacking, SkillName.Fishing })
        {
            if (!HasGatherTool(bot, skill))
            {
                TryEquipGatherTool(bot, skill);
            }
        }
    }

    private static void TrySpendGold(PlayerMobile bot)
    {
        EnforcePocketCap(bot);
        RestockGatherTools(bot);

        if (Utility.RandomDouble() > 0.3)
        {
            return; // don't roll this every single idle tick
        }

        var backpack = bot.Backpack;
        var bank = bot.BankBox;
        if (backpack == null || bank == null)
        {
            return;
        }

        var available = CurrencyHelper.GetCopperValue(bank);
        if (available <= 0)
        {
            return;
        }

        if (bot is BotMobile { Archetype: BotArchetype.Crafter })
        {
            TryBuyBlueprint(bot, bank, available);
        }
        else
        {
            TryTrainSkill(bot, bank, available);
        }
    }

    /// <summary>Restricts crafters to armor and weapons of any kind (plate, leather, bows,
    /// swords, axes, whatever their craft skill actually makes) — previously bots learned
    /// and made whatever recipe happened to be cheapest/first, furniture and deco included.</summary>
    private static bool IsWeaponOrArmorRecipe(CraftItem craftItem) =>
        craftItem?.ItemType != null &&
        (typeof(BaseWeapon).IsAssignableFrom(craftItem.ItemType) || typeof(BaseArmor).IsAssignableFrom(craftItem.ItemType));

    private static void TryBuyBlueprint(PlayerMobile bot, Container backpack, long availableCopper)
    {
        Recipe cheapest = null;
        var cheapestPrice = long.MaxValue;

        foreach (var recipe in Recipe.Recipes.Values)
        {
            if (bot.HasRecipe(recipe) || !IsWeaponOrArmorRecipe(recipe.CraftItem))
            {
                continue;
            }

            var price = MahaonBlueprint.GetPrice(recipe);
            if (price < cheapestPrice)
            {
                cheapestPrice = price;
                cheapest = recipe;
            }
        }

        if (cheapest == null)
        {
            return; // already knows every recipe there is
        }

        var cost = CurrencyHelper.ToCopperValue((int)cheapestPrice, 0, 0);
        if (cost > availableCopper)
        {
            bot.PublicOverheadMessage(
                MessageType.Regular, 0x3B2, false,
                $"Хочу купить чертёж «{RecipeNameHelper.GetName(cheapest)}» ({cheapestPrice}зм), но пока не хватает денег в банке."
            );
            return;
        }

        if (!CurrencyHelper.TryWithdrawCopperValue(backpack, cost))
        {
            bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Не смог снять деньги на чертёж — что-то не так с монетами в банке.");
            return;
        }

        bot.AcquireRecipe(cheapest);
        bot.PublicOverheadMessage(MessageType.Regular, 0x59, false, $"Купил чертёж: {RecipeNameHelper.GetName(cheapest)}.");
    }

    private static readonly SkillName[] WarriorTrainSkills = { SkillName.Swords, SkillName.Tactics, SkillName.Anatomy };
    private static readonly SkillName[] MageTrainSkills = { SkillName.Magery, SkillName.EvalInt, SkillName.Meditation };
    private static readonly SkillName[] ArcherTrainSkills = { SkillName.Archery, SkillName.Tactics };

    private const long TrainCostPerPoint = 50;
    private const double TrainPointsMin = 1.0;
    private const double TrainPointsMax = 3.0;

    private static void TryTrainSkill(PlayerMobile bot, Container backpack, long availableCopper)
    {
        var profession = ProfessionSystem.GetProfession(bot);

        SkillName skillName;
        var trainingSecondary = false;

        if (profession != null && Utility.RandomDouble() < 0.7)
        {
            var info = ProfessionData.All[profession.Value];
            var primarySkills = ProfessionData.CategorySkills[info.Category];
            skillName = primarySkills[Utility.Random(primarySkills.Length)];
        }
        else
        {
            var skills = bot is BotMobile botMobile
                ? botMobile.Archetype switch
                {
                    BotArchetype.Warrior => WarriorTrainSkills,
                    BotArchetype.Mage    => MageTrainSkills,
                    BotArchetype.Archer  => ArcherTrainSkills,
                    _                    => WarriorTrainSkills
                }
                : WarriorTrainSkills;

            skillName = skills.RandomElement();
            trainingSecondary = true;
        }

        var skill = bot.Skills[skillName];
        if (skill.Base >= skill.Cap)
        {
            return; // already at this skill's cap (100 normal, 120 for the primary class kit)
        }

        var points = TrainPointsMin + (TrainPointsMax - TrainPointsMin) * Utility.RandomDouble();
        var cost = (long)(points * TrainCostPerPoint);

        if (cost > availableCopper || !CurrencyHelper.TryWithdrawCopperValue(backpack, cost))
        {
            return;
        }

        skill.Base = System.Math.Min(skill.Cap, skill.Base + points);

        var verb = trainingSecondary ? "Позанимаюсь ещё" : "Потренирую";
        bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"{verb} немного скилл «{skill.Name}».");
    }

    // Generous on purpose — gold is scarce for training skills, but stats are meant to grow
    // from just doing things, same as real UO's classic stat-gain-on-use.
    private const double StatGainChance = 0.08;

    /// <summary>Rolls a chance to tick the given stat up by 1 (toward this bot's profession
    /// cap), same as real UO's stat-gain-on-use — call this from real action points
    /// (landing a hit, taking damage, successful cast, successful harvest), not on a timer.</summary>
    private static void TryGainStat(Mobile bot, StatType stat)
    {
        if (Utility.RandomDouble() >= StatGainChance)
        {
            return;
        }

        var cap = ProfessionSystem.GetStatCap(bot, stat);

        var current = stat switch
        {
            StatType.Str => bot.RawStr,
            StatType.Dex => bot.RawDex,
            _            => bot.RawInt
        };

        if (current >= cap)
        {
            return;
        }

        switch (stat)
        {
            case StatType.Str:
                bot.RawStr = current + 1;
                break;
            case StatType.Dex:
                bot.RawDex = current + 1;
                break;
            default:
                bot.RawInt = current + 1;
                break;
        }
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

    private const int HuntRange = 20;
    private const double FleeHealthThreshold = 0.25;

    private static void DoHunting(PlayerMobile bot, BotProfile profile)
    {
        if (bot.Combatant?.Deleted == false && bot.Combatant.Alive)
        {
            return; // already fighting, let combat play out
        }

        if (bot.Map == null)
        {
            return;
        }

        if (IsWatchedByGuards(bot))
        {
            return; // playing it safe in town — no fresh fights while a guard can see them
        }

        Mobile target = null;

        foreach (var m in bot.Map.GetMobilesInRange<Mobile>(bot.Location, HuntRange))
        {
            if (m == bot || !m.Alive || m.Deleted || m.Hidden)
            {
                continue;
            }

            if (m is not (PlayerMobile or BaseCreature { Controlled: true }))
            {
                continue; // don't pick fights with regular wildlife/monsters
            }

            if (m is PlayerMobile otherPm && Bots.TryGetValue(otherPm, out var otherProfile) &&
                otherProfile.Party != null && otherProfile.Party == profile.Party)
            {
                continue; // don't attack your own party
            }

            if (BotGuilds.IsAllied(bot, m))
            {
                continue; // don't attack an allied guild's members
            }

            target = m;
            break;
        }

        if (target != null)
        {
            bot.CriminalAction(false); // goes gray — see ExpireCriminalDelay in Configure()
            ApplyCombatTactics(bot, target);
            bot.Combatant = target;
            bot.Warmode = true;
            return;
        }

        if (TryLootCorpse(bot))
        {
            return;
        }

        // Nobody around and nothing to loot — patrol instead of standing still.
        StepToward(bot, RandomNearbyPoint(bot.Location, 10), 2);
    }

    private static void DoWandering(PlayerMobile bot, BotProfile profile)
    {
        StepToward(bot, RandomNearbyPoint(bot.Location, 6), 1);

        if (Utility.RandomDouble() < 0.4)
        {
            profile.Activity = BotActivity.Idle;
        }
    }

    // -- Gathering / crafting ---------------------------------------------------------
    //
    // NOTE: this is a simplified simulation, not a real skill-use integration — it grants
    // a resource after a delay rather than rolling the actual Mining/Lumberjacking skill
    // checks against a harvested tile. Good first real upgrade: call into the existing
    // harvest system (BaseHarvestTool / Mining.cs) instead of granting directly.

    private static bool IsCraftsmanProfession(Mobile m) => IsProfessionCategory(m, ProfessionCategory.Craft);

    public static bool IsProfessionCategory(Mobile m, ProfessionCategory category)
    {
        var profession = ProfessionSystem.GetProfession(m);
        return profession != null && ProfessionData.All[profession.Value].Category == category;
    }

    private static void DoGathering(PlayerMobile bot, BotProfile profile)
    {
        TryGainStat(bot, StatType.Str);

        if (profile.CyclesRemaining-- <= 0)
        {
            profile.Activity = BotActivity.ReturningFromGather;
            return;
        }

        if (!HasGatherTool(bot, profile.GatherSkill))
        {
            // Forgot the tool — that's just a wasted trip, same as it would be for a real
            // player. No conjuring one up out in the middle of nowhere.
            profile.Activity = BotActivity.ReturningFromGather;
            return;
        }

        var tool = GetGatherToolItem(bot, profile.GatherSkill);
        var system = GetHarvestSystem(profile.GatherSkill);

        if (tool == null || system == null || bot.Map == null)
        {
            return;
        }

        if (bot.FindItemOnLayer(Layer.OneHanded) != tool && bot.FindItemOnLayer(Layer.TwoHanded) != tool)
        {
            bot.EquipItem(tool);
        }

        if (!bot.CanBeginAction(tool))
        {
            return; // mid-swing from a previous real harvest attempt — let it finish
        }

        if (!TryFindHarvestTarget(bot, profile.GatherSkill, out var toHarvest))
        {
            profile.GatherSearchFailures++;

            if (profile.GatherSearchFailures < GatherGiveUpAfter)
            {
                // Might just be mid-respawn or a bad angle — hold position and try the
                // same spot again next tick instead of wandering off immediately.
                return;
            }

            // Genuinely nothing here after several real attempts — this spot's a bust.
            // Re-scout for a real spot instead of endlessly re-randomizing a local point,
            // which just looked like aimless jittering without ever finding anything.
            profile.GatherSearchFailures = 0;
            profile.GatherDestination = PickGatherSpot(bot, profile);
            profile.Activity = BotActivity.TravelingToGather;
            return;
        }

        profile.GatherSearchFailures = 0;

        // This is the exact same call the real Target class makes after a player clicks a
        // tree/vein/water tile — real range check, real resource check, real skill roll,
        // real swing timer, real resource depletion, real chance at whatever side effects
        // that system has (a pickaxe swing can still trigger a mine entrance, same as a
        // real player's would).
        system.StartHarvesting(bot, tool, toHarvest);
        RegisterNodeUse(profile.GatherDestination, bot.Map);
    }

    private static Server.Engines.Harvest.HarvestSystem GetHarvestSystem(SkillName skill) => skill switch
    {
        SkillName.Mining        => Server.Engines.Harvest.Mining.System,
        SkillName.Lumberjacking => Server.Engines.Harvest.Lumberjacking.System,
        SkillName.Fishing       => Server.Engines.Harvest.Fishing.System,
        _                       => null
    };

    private static Item GetGatherToolItem(PlayerMobile bot, SkillName skill)
    {
        var backpack = bot.Backpack;
        if (backpack == null)
        {
            return null;
        }

        return skill switch
        {
            SkillName.Mining        => backpack.FindItemByType<Pickaxe>(),
            SkillName.Lumberjacking => backpack.FindItemByType<Hatchet>(),
            SkillName.Fishing       => backpack.FindItemByType<FishingPole>(),
            _                       => null
        };
    }

    private const int HarvestSearchRadius = 3;
    private const int GatherGiveUpAfter = 3;


    /// <summary>
    ///     Finds a real, actually-there thing to harvest near the bot — a tree static for
    ///     Lumberjacking, an impassable mountainside tile for Mining, a water tile for
    ///     Fishing — instead of just conjuring a result. Returns the exact object type
    ///     HarvestSystem.StartHarvesting expects for that skill.
    /// </summary>
    private static bool TryFindHarvestTarget(Mobile bot, SkillName skill, out object toHarvest)
    {
        toHarvest = null;
        var map = bot.Map;
        if (map == null)
        {
            return false;
        }

        var system = GetHarvestSystem(skill);
        var def = system?.GetDefinition();
        if (def == null)
        {
            return false;
        }

        if (skill == SkillName.Lumberjacking)
        {
            foreach (var item in map.GetItemsInRange<Static>(bot.Location, HarvestSearchRadius))
            {
                if (!item.Movable && Array.IndexOf(def.StaticTiles, item.ItemID) >= 0)
                {
                    toHarvest = item;
                    return true;
                }
            }

            return false;
        }

        // Mining and Fishing both work off land tiles — mountainside/impassable for
        // Mining, water for Fishing (checked against that system's own LandTiles list).
        for (var dx = -HarvestSearchRadius; dx <= HarvestSearchRadius; dx++)
        {
            for (var dy = -HarvestSearchRadius; dy <= HarvestSearchRadius; dy++)
            {
                var x = bot.X + dx;
                var y = bot.Y + dy;
                var lt = map.Tiles.GetLandTile(x, y);
                var tileId = lt.ID & TileData.MaxLandValue;

                bool matches;
                if (skill == SkillName.Mining)
                {
                    matches = (TileData.LandTable[tileId].Flags & TileFlag.Impassable) != 0;
                }
                else
                {
                    matches = Array.IndexOf(def.LandTiles, tileId) >= 0;
                }

                if (!matches)
                {
                    continue;
                }

                var z = map.GetAverageZ(x, y);
                var loc = new Point3D(x, y, z);

                if (!bot.InRange(loc, 2))
                {
                    continue;
                }

                toHarvest = new LandTarget(loc, map);
                return true;
            }
        }

        return false;
    }

    private static bool HasGatherTool(PlayerMobile bot, SkillName skill)
    {
        var backpack = bot.Backpack;
        if (backpack == null)
        {
            return false;
        }

        return skill switch
        {
            SkillName.Mining        => backpack.FindItemByType<Pickaxe>() != null,
            SkillName.Lumberjacking => backpack.FindItemByType<Hatchet>() != null,
            SkillName.Fishing       => backpack.FindItemByType<FishingPole>() != null,
            _                       => true
        };
    }

    /// <summary>Bots skip the trip to a tool vendor — if they've got the coin, the tool
    /// just appears in the pack, same simplification used for every other bot purchase.</summary>
    private static bool TryEquipGatherTool(PlayerMobile bot, SkillName skill)
    {
        var backpack = bot.Backpack;
        if (backpack == null)
        {
            return false;
        }

        var toolCost = skill switch
        {
            SkillName.Mining        => 6L,
            SkillName.Lumberjacking => 9L,
            SkillName.Fishing       => 5L,
            _                       => 5L
        };

        var bank = bot.BankBox;

        if (bank == null || !CurrencyHelper.TryWithdrawCopperValue(bank, CurrencyHelper.ToCopperValue((int)toolCost, 0, 0)))
        {
            return false;
        }

        Item tool = skill switch
        {
            SkillName.Mining        => new Pickaxe(),
            SkillName.Lumberjacking => new Hatchet(),
            SkillName.Fishing       => new FishingPole(),
            _                       => null
        };

        if (tool == null)
        {
            return false;
        }

        if (backpack.TryDropItem(bot, tool, false))
        {
            return true;
        }

        tool.Delete();
        return false;
    }

    private static void DoReturnFromGather(PlayerMobile bot, BotProfile profile)
    {
        if (StepTowardPath(bot, profile, profile.HomeLocation))
        {
            profile.Activity = BotActivity.Crafting;
            profile.CyclesRemaining = Utility.RandomMinMax(1, 3);
        }
    }

    private static void DoCrafting(PlayerMobile bot, BotProfile profile)
    {
        if (profile.CyclesRemaining-- <= 0)
        {
            profile.Activity = BotActivity.TravelingToMarket;
            return;
        }

        var backpack = bot.Backpack;
        if (backpack == null)
        {
            return;
        }

        // Raw materials still need turning into ingots/boards first — that part stays a
        // simplified stand-in (no real forge-fire minigame), but still gated by a real
        // skill check.
        var ore = backpack.FindItemByType<IronOre>();
        var logs = backpack.FindItemByType<Log>();

        if (ore != null)
        {
            if (bot.CheckSkill(SkillName.Blacksmith, 0.0, 100.0))
            {
                ore.Consume(Math.Min(ore.Amount, 5));
                backpack.DropItem(new IronIngot(1));
            }

            return;
        }

        if (logs != null)
        {
            if (bot.CheckSkill(SkillName.Carpentry, 0.0, 100.0))
            {
                logs.Consume(Math.Min(logs.Amount, 5));
                backpack.DropItem(new Board(1));
            }

            return;
        }

        // No raw materials left to process — try an actual craft attempt through the real
        // DefCraftSystem, using whatever recipe this bot has actually learned (the same
        // MahaonBlueprint system players use). No shortcuts here: real tool, real resource
        // consumption, real success chance off the real skill.
        TryRealCraft(bot, backpack);
    }

    private static void TryRealCraft(PlayerMobile bot, Container backpack)
    {
        var tool = backpack.FindItemByType<BaseTool>();
        if (tool == null)
        {
            return;
        }

        var knownCount = 0;

        foreach (var recipe in Recipe.Recipes.Values)
        {
            if (recipe.CraftSystem != tool.CraftSystem || !IsWeaponOrArmorRecipe(recipe.CraftItem))
            {
                continue;
            }

            knownCount++;

            var craftItem = recipe.CraftItem;
            if (craftItem?.Resources == null || craftItem.Resources.Count == 0)
            {
                continue;
            }

            // Only the simple single-resource-type recipes are worth a bot's time here —
            // enough to cover most early armor/weapon/tool recipes without needing a full
            // multi-resource-slot resolver.
            var resType = craftItem.Resources[0].ItemType;
            var needed = craftItem.Resources[0].Amount;
            var have = backpack.GetAmount(resType);

            if (have < needed)
            {
                // Deliberate cheat, per the request — craft bots know every blueprint and
                // pull materials from thin air instead of actually gathering them.
                try
                {
                    var conjured = (Item)Activator.CreateInstance(resType, needed - have);
                    backpack.DropItem(conjured);
                }
                catch
                {
                    continue; // this particular resource type doesn't support this — skip
                }
            }

            bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Попробую сделать: {RecipeNameHelper.GetName(recipe)}.");
            craftItem.Craft(bot, recipe.CraftSystem, resType, tool);
            return;
        }

        // Diagnostic — nothing got crafted, so say exactly why so it's obvious from the
        // journal instead of a silent no-op.
        if (knownCount == 0)
        {
            bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Не знаю ни одного подходящего рецепта под этот инструмент.");
        }
        else
        {
            bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Знаю {knownCount} рецепт(ов), но не хватает материала на них.");
        }
    }

    // -- City travel --------------------------------------------------------------------
    //
    // Simplified: no real pathfinding to the stone across a whole city, and no walking
    // between cities tile-by-tile either — a bot "uses the stone" by stepping toward its
    // own city center for a couple of cycles, then relocating straight to the destination
    // city's stone, same as a player clicking the gump would end up doing.

    private static string PickTravelDestination(string currentCity)
    {
        var cities = new List<string>(CityControlSystem.Cities.Keys);
        if (currentCity != null)
        {
            cities.Remove(currentCity);
        }

        return cities.Count > 0 ? cities.RandomElement() : null;
    }

    private static void DoCityTravel(PlayerMobile bot, BotProfile profile)
    {
        var destinationCity = profile.TravelDestinationCity;
        if (destinationCity == null || !CityControlSystem.Cities.TryGetValue(destinationCity, out var destInfo))
        {
            profile.Activity = BotActivity.Idle;
            return;
        }

        if (profile.CurrentCity != null && CityControlSystem.Cities.TryGetValue(profile.CurrentCity, out var ownInfo))
        {
            var farFromOwnStone = bot.Map != ownInfo.map || bot.GetDistanceToSqrt(ownInfo.spawn) > 5;

            if (farFromOwnStone && !StepTowardPath(bot, profile, ownInfo.spawn))
            {
                return; // still walking over to the local stone
            }
        }

        // "Use" the stone: pay the same fee a player would, then relocate straight to the
        // destination city's stone. If a bot can't afford it, it just stays put and idles.
        const long travelCost = 50;

        if (bot.Backpack == null || !CurrencyHelper.TryWithdrawCopperValue(bot.Backpack, travelCost))
        {
            profile.TravelDestinationCity = null;
            profile.Activity = BotActivity.Idle;
            return;
        }

        bot.MoveToWorld(destInfo.spawn, destInfo.map);
        profile.CurrentCity = destinationCity;
        profile.TravelDestinationCity = null;

        // A trader immediately tries to offload whatever it's carrying, then picks up
        // something cheap to carry onward next time.
        if (bot is BotMobile { Archetype: BotArchetype.Trader })
        {
            DoTraderTrade(bot, destinationCity);
        }

        profile.Activity = BotActivity.Idle;
    }

    // -- Thief bots: opportunistic stealing --------------------------------------------
    //
    // Simplified — no real snooping/stealth minigame, just a flat skill-based success
    // roll against whoever's nearby. Flagged as a proper criminal act either way. The
    // player already knows this is rough around the edges and wants to nerf it later.

    // -- Bard begging ----------------------------------------------------------------------
    //
    // Same formula as the real Begging skill handler (Skills/Begging.cs), just without the
    // interactive target-and-wait flow — bots pick a nearby vendor and resolve it in one go.

    private const int BegRange = 3;

    private static bool TryBegNearby(Mobile beggar)
    {
        if (beggar.Map == null)
        {
            return false;
        }

        foreach (var target in beggar.Map.GetMobilesInRange<Mobile>(beggar.Location, BegRange))
        {
            if (target.Player || !target.Body.IsHuman || target == beggar)
            {
                continue;
            }

            var theirPack = target.Backpack;
            if (theirPack == null)
            {
                continue;
            }

            if (!beggar.CheckSkill(SkillName.Begging, 0.0, 100.0))
            {
                continue;
            }

            var toConsume = theirPack.GetAmount(typeof(Gold)) / 10;
            var max = System.Math.Clamp(10 + beggar.Fame / 2500, 10, 14);

            if (ProfessionSystem.TouchesCategory(beggar, ProfessionCategory.Bard))
            {
                max = (int)(max * 1.75);
            }

            if (toConsume > max)
            {
                toConsume = max;
            }

            if (toConsume <= 0)
            {
                continue;
            }

            var consumed = theirPack.ConsumeUpTo(typeof(Gold), toConsume);
            if (consumed <= 0)
            {
                continue;
            }

            beggar.Backpack?.DropItem(new Gold(consumed));
            return true;
        }

        return false;
    }

    private const int StealRange = 8;

    private static bool TryStealFromNearby(Mobile thief)
    {
        if (thief.Map == null)
        {
            return false;
        }

        if (!thief.Hidden)
        {
            // Try to slip into hiding first — no minigame, just a skill-based roll, same
            // simplification used everywhere else for bot actions.
            var hidingSkill = thief.Skills[SkillName.Hiding].Value;
            if (Utility.RandomDouble() < System.Math.Clamp(hidingSkill / 100.0, 0.1, 0.95))
            {
                thief.Hidden = true;
            }

            return true; // spend this tick getting into position either way
        }

        foreach (var victim in thief.Map.GetMobilesInRange<Mobile>(thief.Location, StealRange))
        {
            if (victim == thief || !victim.Alive || victim.Deleted || victim.Backpack == null)
            {
                continue;
            }

            if (victim is PlayerMobile victimPm && Bots.TryGetValue(victimPm, out var victimProfile) &&
                Bots.TryGetValue((PlayerMobile)thief, out var thiefProfile) &&
                victimProfile.Party != null && victimProfile.Party == thiefProfile.Party)
            {
                continue; // don't steal from your own party
            }

            if (TryStealFrom(thief, victim))
            {
                thief.Hidden = false; // the act itself breaks stealth, same as real stealing
                thief.CriminalAction(false);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Money is always on the table; Snooping unlocks going after worn jewelry too.
    ///     Higher skill means more "slots" (item types) worth trying in a single pass, not
    ///     just a better success roll — a master thief has more options, not just better odds.
    /// </summary>
    private static readonly Layer[] JewelryLayers = { Layer.Ring, Layer.Bracelet, Layer.Earrings, Layer.Neck };

    private static bool TryStealFrom(Mobile thief, Mobile victim)
    {
        var stealingSkill = thief.Skills[SkillName.Stealing].Value;
        var snoopingSkill = thief.Skills[SkillName.Snooping].Value;

        var slots = new List<System.Func<Mobile, Mobile, bool>> { TryStealMoney };

        var jewelrySlotCount = (int)(snoopingSkill / 25); // 0 at <25 snooping, up to 4 at 100+
        for (var i = 0; i < jewelrySlotCount && i < JewelryLayers.Length; i++)
        {
            var layer = JewelryLayers[i];
            slots.Add((t, v) => TryStealJewelry(t, v, layer));
        }

        var chosen = slots[Utility.Random(slots.Count)];
        return chosen(thief, victim);
    }

    private static bool TryStealMoney(Mobile thief, Mobile victim)
    {
        Item stack = victim.Backpack.FindItemByType<Gold>()
                     ?? victim.Backpack.FindItemByType<MahaonSilver>()
                     ?? (Item)victim.Backpack.FindItemByType<MahaonCopper>();

        if (stack == null)
        {
            return false;
        }

        var stealingSkill = thief.Skills[SkillName.Stealing].Value;
        var successChance = System.Math.Clamp(stealingSkill / 120.0, 0.05, 0.9);

        if (!thief.CheckSkill(SkillName.Stealing, 0.0, 100.0) || Utility.RandomDouble() >= successChance)
        {
            return false;
        }

        // 100 Stealing = takes the whole stack; scales down linearly below that.
        var takeFraction = System.Math.Clamp(stealingSkill / 100.0, 0.0, 1.0);
        var takeAmount = System.Math.Max(1, (int)(stack.Amount * takeFraction));

        if (takeAmount >= stack.Amount)
        {
            return thief.Backpack?.TryDropItem(thief, stack, false) == true;
        }

        stack.Amount -= takeAmount;

        Item stolen = stack switch
        {
            Gold         => new Gold(takeAmount),
            MahaonSilver => new MahaonSilver(takeAmount),
            _            => new MahaonCopper(takeAmount)
        };

        if (thief.Backpack?.TryDropItem(thief, stolen, false) != true)
        {
            stolen.Delete();
            return false;
        }

        return true;
    }

    private static bool TryStealJewelry(Mobile thief, Mobile victim, Layer layer)
    {
        if (victim.FindItemOnLayer(layer) is not Item piece)
        {
            return false;
        }

        var stealingSkill = thief.Skills[SkillName.Stealing].Value;
        var successChance = System.Math.Clamp(stealingSkill / 120.0, 0.05, 0.9);

        if (!thief.CheckSkill(SkillName.Stealing, 0.0, 100.0) || Utility.RandomDouble() >= successChance)
        {
            return false;
        }

        return thief.Backpack?.TryDropItem(thief, piece, false) == true;
    }

    private const long TraderBuyBudget = 500;
    private const double TraderMarkup = 1.4; // resell at +40%

    private static void DoTraderTrade(PlayerMobile bot, string city)
    {
        var backpack = bot.Backpack;
        if (backpack == null)
        {
            return;
        }

        if (CityMarkers.TryGetMarker(city, "auctionstone", out var stoneLoc, out var stoneMap) && bot.Map == stoneMap)
        {
            bot.Location = stoneLoc; // already just teleported into the city via CityTravel — this just fine-tunes to the exact stone
        }

        // Sell anything already being carried (bought in a previous city) at a markup.
        List<Item> carried = null;
        foreach (var item in backpack.Items)
        {
            if (item is Gold or MahaonCopper or MahaonSilver)
            {
                continue;
            }

            (carried ??= new List<Item>()).Add(item);
        }

        if (carried != null)
        {
            foreach (var item in carried)
            {
                var value = EstimateValue(item);
                if (value > 0)
                {
                    AuctionHouseSystem.CreateListing(bot, item, (long)(value * TraderMarkup), city);
                }
            }
        }

        // Pick up the cheapest listing here to carry to the next city.
        AuctionListing cheapest = null;

        foreach (var listing in AuctionHouseSystem.ActiveListings(city))
        {
            if (listing.Seller == bot)
            {
                continue;
            }

            if (cheapest == null || listing.Price < cheapest.Price)
            {
                cheapest = listing;
            }
        }

        if (cheapest == null || cheapest.Price > TraderBuyBudget)
        {
            return;
        }

        AuctionHouseSystem.TryBuy(bot, cheapest.Id);
    }

    private static long EstimateValue(Item item) => item switch
    {
        IronIngot ingot => ingot.Amount * 8L,
        Board board     => board.Amount * 8L,
        IronOre ore     => ore.Amount * 3L,
        Log log         => log.Amount * 3L,
        _               => 0L
    };

    private static void DoTravelToMarket(PlayerMobile bot, BotProfile profile)
    {
        var (marketLoc, marketMap) = NearestMarket(bot, profile);

        if (StepTowardPath(bot, profile, marketLoc) && bot.Map == marketMap)
        {
            profile.Activity = BotActivity.Selling;
        }
    }

    private static void DoSelling(PlayerMobile bot, BotProfile profile)
    {
        var backpack = bot.Backpack;
        if (backpack == null)
        {
            profile.Activity = BotActivity.Idle;
            return;
        }

        var city = profile.CurrentCity ?? profile.HomeCity ?? "Britain";

        ListForAuction<IronIngot>(bot, backpack, 8, city);
        ListForAuction<Board>(bot, backpack, 8, city);
        ListForAuction<IronOre>(bot, backpack, 3, city);
        ListForAuction<Log>(bot, backpack, 3, city);
        ListForAuction<Fish>(bot, backpack, 5, city);
        ListSpareLootForAuction(bot, backpack, city);
        TryBuyNeededMaterials(bot, city);

        profile.Activity = BotActivity.Idle;
    }

    /// <summary>
    ///     Simplified "is this cheaper than the vendor" check — reagents run roughly 4-6gp
    ///     at an NPC in practice, so anything listed under that on the auction is a real
    ///     bargain worth grabbing instead. Not a live price feed against the actual vendor
    ///     stock, just a reasonable stand-in threshold.
    /// </summary>
    private const long ReagentVendorPriceEstimate = 5;

    private static void TryBuyNeededMaterials(PlayerMobile bot, string city)
    {
        var profession = ProfessionSystem.GetProfession(bot);
        if (profession == null)
        {
            return;
        }

        var info = ProfessionData.All[profession.Value];
        var needsReagents = info.Category == ProfessionCategory.Magic || info.SecondaryCategory == ProfessionCategory.Magic;

        if (!needsReagents)
        {
            return;
        }

        foreach (var listing in AuctionHouseSystem.ActiveListings(city))
        {
            if (listing.Seller == bot)
            {
                continue;
            }

            var isReagent = listing.Item is Garlic or Ginseng or MandrakeRoot or SpidersSilk or BlackPearl or SulfurousAsh or Nightshade;
            if (!isReagent || listing.Price > ReagentVendorPriceEstimate * System.Math.Max(1, listing.Item.Amount))
            {
                continue;
            }

            AuctionHouseSystem.TryBuy(bot, listing.Id);
        }
    }

    /// <summary>
    ///     Corpse loot that isn't this bot's own gear (armor/weapons that weren't upgrades,
    ///     reagents a non-caster doesn't use) shouldn't just sit in the pack — a real player
    ///     would sell it. Reagents fetch a small flat price; armor/weapons get valued by a
    ///     rough tier estimate rather than a real appraisal.
    /// </summary>
    private static void ListSpareLootForAuction(PlayerMobile bot, Container backpack, string city)
    {
        List<(Item item, long price)> spare = null;

        foreach (var item in backpack.Items)
        {
            long price = item switch
            {
                Garlic or Ginseng or MandrakeRoot or SpidersSilk or BlackPearl or SulfurousAsh or Nightshade
                    => 4L * item.Amount,
                BaseArmor armor when bot.FindItemOnLayer(armor.Layer) != armor => 60L,
                BaseWeapon weapon when bot.FindItemOnLayer(weapon.Layer) != weapon => 80L,
                _ => 0L
            };

            if (price > 0)
            {
                (spare ??= new List<(Item, long)>()).Add((item, price));
            }
        }

        if (spare == null)
        {
            return;
        }

        foreach (var (item, price) in spare)
        {
            AuctionHouseSystem.CreateListing(bot, item, price, city);
        }
    }

    private static void ListForAuction<T>(PlayerMobile bot, Container backpack, int copperPerUnit, string city) where T : Item
    {
        var item = backpack.FindItemByType<T>();
        if (item == null)
        {
            return;
        }

        var price = (long)item.Amount * copperPerUnit;
        if (price <= 0)
        {
            return;
        }

        AuctionHouseSystem.CreateListing(bot, item, price, city);
    }

    private static (Point3D loc, Map map) NearestMarket(PlayerMobile bot, BotProfile profile)
    {
        var city = profile.CurrentCity ?? profile.HomeCity;

        if (city != null)
        {
            if (CityMarkers.TryGetMarker(city, "auctionstone", out var loc, out var map))
            {
                return (loc, map);
            }

            if (CityControlSystem.Cities.TryGetValue(city, out var info))
            {
                return (info.spawn, info.map);
            }
        }

        return (bot.Location, bot.Map); // no known city — just "sell" on the spot
    }

    // -- Party / dungeon -----------------------------------------------------------------

    private static readonly string[] IdleGroupChat =
    {
        "Погодка сегодня так себе.",
        "Долго ещё стоять?",
        "Готовь оружие, скоро выходим.",
        "Кто-нибудь видел торговца поблизости?",
        "Не терпится добраться до подземелья.",
        "Тихо тут.",
        "Проверяю снаряжение ещё раз."
    };

    private static string RoleNameRu(PartyRole role) => role switch
    {
        PartyRole.Tank   => "танк",
        PartyRole.Healer => "лекарь",
        _                => "дамагер"
    };

    private static void DoFormParty(PlayerMobile bot, BotProfile profile)
    {
        if (profile.Party != null)
        {
            TickFormingParty(bot, profile);
            return;
        }

        // Try to join a nearby party that's still forming and has a slot this bot can
        // actually fill, before starting a brand new one.
        foreach (var (otherBot, otherProfile) in Bots)
        {
            if (otherBot == bot || otherProfile.Party == null || otherProfile.Party.IsFull)
            {
                continue;
            }

            if (otherProfile.Activity != BotActivity.FormingParty)
            {
                continue;
            }

            if (otherBot.Map != bot.Map || otherBot.GetDistanceToSqrt(bot) > 20)
            {
                continue;
            }

            if (otherProfile.Party.TryClaimSlot(bot, out var claimedRole))
            {
                profile.Party = otherProfile.Party;
                bot.PublicOverheadMessage(
                    MessageType.Regular, 0x3B2, false,
                    $"Присоединяюсь к группе — беру роль «{RoleNameRu(claimedRole)}»."
                );
                TickFormingParty(bot, profile);
                return;
            }
        }

        // Strong solo bots skip the whole group dance sometimes, same as before.
        var isStrong = bot.Skills[SkillName.Tactics].Value >= 70;
        if (isStrong && Utility.RandomDouble() < 0.5)
        {
            var soloParty = new BotParty();
            soloParty.TryClaimSlot(bot, out _);
            soloParty.Destination = DungeonTarget.Known.RandomElement();
            profile.Party = soloParty;
            ActiveParties.Add(soloParty);
            profile.Activity = BotActivity.TravelingToDungeon;
            bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Справлюсь один — иду в {soloParty.Destination.Name}.");
            return;
        }

        // Nobody to join — this bot starts a new party and waits right where it's
        // standing for others to show up and fill the remaining slots.
        var eligibleRoles = new List<PartyRole>(BotParty.EligibleRoles(bot));
        if (eligibleRoles.Count == 0)
        {
            // Doesn't have any eligible combat role at all (e.g. a Crafter) — just wander
            // instead of queuing forever with no way to ever field a role.
            profile.Activity = BotActivity.Wandering;
            return;
        }

        var party = new BotParty();
        party.TryClaimSlot(bot, out var myRole);
        party.RendezvousPoint = bot.Location;
        party.RendezvousMap = bot.Map;
        party.Destination = DungeonTarget.Known.RandomElement();

        profile.Party = party;
        ActiveParties.Add(party);

        bot.PublicOverheadMessage(
            MessageType.Regular, 0x3B2, false,
            $"Собираю группу в {party.Destination.Name} — я {RoleNameRu(myRole)}, нужны ещё."
        );
    }

    private static void TickFormingParty(PlayerMobile bot, BotProfile profile)
    {
        var party = profile.Party;
        if (party == null || !party.IsAlive)
        {
            profile.Activity = BotActivity.Idle;
            profile.Party = null;
            return;
        }

        if (party.IsFull)
        {
            var marker = Items.MahaonDungeonMarker.Find(party.Destination.Name);

            if (marker != null)
            {
                foreach (var member in party.Members)
                {
                    member.MoveToWorld(marker.Location, marker.Map);
                }

                bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "В сборе! Телепортируемся к цели.");
                profile.Activity = BotActivity.DungeonCombat;
                profile.CyclesRemaining = Utility.RandomMinMax(6, 15);
                return;
            }

            profile.Activity = BotActivity.TravelingToDungeon;
            bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "В сборе! Выдвигаемся.");
            return;
        }

        if (party.RendezvousMap == null)
        {
            return; // solo/already-departed party, shouldn't normally hit this state
        }

        if (StepToward(bot, party.RendezvousPoint, 1) && bot.Map == party.RendezvousMap)
        {
            // Arrived and waiting — idle chat while the rest of the roster fills in.
            if (Utility.RandomDouble() < 0.08)
            {
                bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, IdleGroupChat.RandomElement());
            }
        }
    }

    private static void DoTravelToDungeon(PlayerMobile bot, BotProfile profile)
    {
        var party = profile.Party;
        if (party == null || !party.IsAlive)
        {
            profile.Activity = BotActivity.Idle;
            profile.Party = null;
            return;
        }

        if (StepTowardPath(bot, profile, party.Destination.Entrance) && bot.Map == party.Destination.Map)
        {
            profile.Activity = BotActivity.DungeonCombat;
            profile.CyclesRemaining = Utility.RandomMinMax(6, 15);
        }
    }

    private static void DoDungeonCombat(PlayerMobile bot, BotProfile profile)
    {
        if (profile.CyclesRemaining-- <= 0)
        {
            profile.Activity = BotActivity.ReturningHome;
            profile.Party?.Remove(bot);
            profile.Party = null;
            return;
        }

        if (bot is BotMobile { Archetype: BotArchetype.Warrior })
        {
            EnforceWarriorAggro(bot, profile);
        }

        if (bot is BotMobile { Archetype: BotArchetype.Mage } mageBot)
        {
            DoMageAction(mageBot, profile);
            return;
        }

        if (bot.Combatant?.Deleted != false || !bot.Combatant.Alive)
        {
            BaseCreature target = null;

            if (bot.Map != null)
            {
                foreach (var creature in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, 10))
                {
                    if (creature.Alive && !creature.Deleted && creature is not IRaidSpawn)
                    {
                        target = creature;
                        break;
                    }
                }
            }

            if (target != null)
            {
                ApplyCombatTactics(bot, target);
                bot.Combatant = target;
                bot.Warmode = true;
                return;
            }

            DoDungeonLooting(bot);
        }
    }

    // -- Dungeon chest looting -------------------------------------------------------------
    //
    // Nothing left to fight nearby — see if there's a chest worth cracking. Locked == true
    // doubles as our "hasn't been looted yet" marker; once a bot loots one it unlocks it, so
    // future bots (or the player) don't waste time on an already-emptied chest.

    private const int ChestSearchRange = 15;
    private const int ChestLootCount = 3;

    private static void DoDungeonLooting(PlayerMobile bot)
    {
        if (TryLootCorpse(bot))
        {
            return;
        }

        TryLootChest(bot);
    }

    private const int CorpseSearchRange = 15;
    private const int CorpseSearchRangeWhileTraveling = 8; // worth a small detour, not the whole visible area
    private const int CorpseLootCount = 3;

    /// <summary>
    ///     Loots whatever's on the ground nearby — not just what this bot personally killed.
    ///     Matches "loot everything they can, wherever they can" rather than tracking kill
    ///     ownership, which real UO doesn't enforce for looting rights this loosely either.
    /// </summary>
    private static bool TryLootCorpse(Mobile bot) => TryLootCorpse(bot, CorpseSearchRange);

    private static bool TryLootCorpse(Mobile bot, int range)
    {
        if (bot.Map == null)
        {
            return false;
        }

        Corpse corpse = null;

        foreach (var candidate in bot.Map.GetItemsInRange<Corpse>(bot.Location, range))
        {
            if (!candidate.Deleted && candidate.Items.Count > 0)
            {
                corpse = candidate;
                break;
            }
        }

        if (corpse == null)
        {
            return TryPickUpLooseGear(bot, range);
        }

        if (!bot.InRange(corpse.GetWorldLocation(), 1))
        {
            StepToward(bot, corpse.Location, 3);
            return true;
        }

        var backpack = bot.Backpack;
        if (backpack == null)
        {
            return true;
        }

        var looted = 0;
        var items = new List<Item>(corpse.Items);

        foreach (var item in items)
        {
            if (looted >= CorpseLootCount)
            {
                break;
            }

            if (item is Container)
            {
                continue; // skip nested bags/etc, keep it to loose loot
            }

            if (TryEquipUpgrade(bot, item))
            {
                looted++;
                continue;
            }

            if (backpack.TryDropItem(bot, item, false))
            {
                looted++;
            }
        }

        if (looted > 0 && bot is BotMobile botMobile)
        {
            botMobile.RememberHuntingSpot(bot.Location);
        }

        return true;
    }

    /// <summary>
    ///     Real gear just lying loose on the ground (not in a corpse/container) gets
    ///     noticed too — otherwise a bot can walk right past a dropped sword and never
    ///     react to it, since the corpse-loot pass only ever looks inside corpses.
    /// </summary>
    private static bool TryPickUpLooseGear(Mobile bot, int range)
    {
        if (bot.Map == null)
        {
            return false;
        }

        foreach (var item in bot.Map.GetItemsInRange<Item>(bot.Location, range))
        {
            if (item.Deleted || !item.Movable || item.Parent != null)
            {
                continue;
            }

            var isUseful = item is BaseArmor or BaseWeapon or Bandage or BasePotion or SpellScroll;
            if (!isUseful)
            {
                continue;
            }

            if (!bot.InRange(item.GetWorldLocation(), 1))
            {
                StepToward(bot, item.Location, 3);
                return true;
            }

            if (item is BaseArmor or BaseWeapon)
            {
                if (TryEquipUpgrade(bot, item))
                {
                    return true;
                }

                continue;
            }

            // Bandages/potions/scrolls just go straight in the pack for later use.
            if (bot.Backpack?.TryDropItem(bot, item, false) == true)
            {
                bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Пригодится: {item.Name ?? item.GetType().Name}.");
                return true;
            }
        }

        return false;
    }

    /// <summary>Equips armor/weapons straight from loot if they're an upgrade over what's
    /// currently worn/wielded — an empty slot always counts as an upgrade.</summary>
    public static bool TryEquipUpgrade(Mobile bot, Item item)
    {
        switch (item)
        {
            case BaseArmor armor:
            {
                var current = bot.FindItemOnLayer(armor.Layer) as BaseArmor;
                if (current != null && current.ArmorRatingScaled >= armor.ArmorRatingScaled)
                {
                    return false;
                }

                if (!bot.EquipItem(armor))
                {
                    return false;
                }

                bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Мне пригодится эта {armor.Name ?? armor.GetType().Name}.");
                return true;
            }

            case BaseWeapon weapon:
            {
                var current = bot.FindItemOnLayer(Layer.OneHanded) as BaseWeapon
                              ?? bot.FindItemOnLayer(Layer.TwoHanded) as BaseWeapon;

                if (current != null &&
                    current.MinDamage + current.MaxDamage >= weapon.MinDamage + weapon.MaxDamage)
                {
                    return false;
                }

                if (!bot.EquipItem(weapon))
                {
                    return false;
                }

                bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Возьму этот {weapon.Name ?? weapon.GetType().Name} себе.");
                return true;
            }

            default:
                return false;
        }
    }

    private static void TryLootChest(Mobile bot)
    {
        if (bot.Map == null)
        {
            return;
        }

        BaseTreasureChest chest = null;

        foreach (var candidate in bot.Map.GetItemsInRange<BaseTreasureChest>(bot.Location, ChestSearchRange))
        {
            if (candidate.Locked && !candidate.Deleted)
            {
                chest = candidate;
                break;
            }
        }

        if (chest == null)
        {
            return;
        }

        if (!bot.InRange(chest.GetWorldLocation(), 1))
        {
            StepToward(bot, chest.Location, 3);
            return;
        }

        // Real Lockpicking check now — same CheckSkill mechanic as everything else, so a
        // thief actually needs the skill to get in, and gets real skill gain for trying.
        if (!bot.CheckSkill(SkillName.Lockpicking, 0.0, 100.0))
        {
            return; // failed the attempt — chest stays locked, try again another cycle
        }

        chest.Locked = false;

        var backpack = bot.Backpack;
        if (backpack == null)
        {
            return;
        }

        var looted = 0;
        var items = new List<Item>(chest.Items);

        foreach (var item in items)
        {
            if (looted >= ChestLootCount)
            {
                break;
            }

            if (backpack.TryDropItem(bot, item, false))
            {
                looted++;
            }
        }
    }

    // -- Combat tactics: stance + aimed hits, actually used this time ---------------------
    //
    // Warriors and mages lean Defensive once they've got any Tactics at all (survivability
    // first). Everyone else who's built for damage (archer, thief, trader) goes as
    // aggressive as their Tactics allows — 3x if unlocked, else 2x, else Normal. Hit
    // location gets re-picked most fights instead of always aiming the same spot.

    /// <summary>
    ///     Thieves coat their weapon before a fight — real Poisoning skill check, real
    ///     poison level gated by that skill (same thresholds the actual Poisoning skill
    ///     uses for potions), real charges that run out and need reapplying.
    /// </summary>
    private static void TryCoatWeaponWithPoison(Mobile bot)
    {
        if (!IsProfessionCategory(bot, ProfessionCategory.Thief))
        {
            return;
        }

        if (bot.Weapon is not BaseWeapon weapon || weapon.PoisonCharges > 0)
        {
            return; // already coated
        }

        var poisoningSkill = bot.Skills[SkillName.Poisoning].Value;
        if (poisoningSkill < 20 || !bot.CheckSkill(SkillName.Poisoning, 0.0, 100.0))
        {
            return;
        }

        var level = poisoningSkill switch
        {
            >= 100 => Poison.Lethal,
            >= 75  => Poison.Deadly,
            >= 50  => Poison.Greater,
            >= 25  => Poison.Regular,
            _      => Poison.Lesser
        };

        weapon.Poison = level;
        weapon.PoisonCharges = Utility.RandomMinMax(3, 6);
    }

    /// <summary>Warriors lean on Chivalry to patch themselves up and Bushido for a bonus
    /// strike, on top of plain swinging — real skill checks, real mana cost, no reagents
    /// or tithing (that overhead was waived per profession touch already).</summary>
    private static void TryUseChivalryOrBushido(Mobile bot)
    {
        if (!IsProfessionCategory(bot, ProfessionCategory.Warrior))
        {
            return;
        }

        if (bot.HitsMax > 0 && (double)bot.Hits / bot.HitsMax < 0.6 &&
            bot.Skills[SkillName.Chivalry].Value >= 30 &&
            bot.Mana >= new CloseWoundsSpell(bot).GetMana() / 10 && Utility.RandomDouble() < 0.4)
        {
            new CloseWoundsSpell(bot).Cast();
            return;
        }

        if (bot.Skills[SkillName.Bushido].Value >= 50 && Utility.RandomDouble() < 0.25 &&
            SpecialMove.GetCurrentMove(bot) == null)
        {
            SpecialMove.SetCurrentMove(bot, new LightningStrike());
            bot.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Молниеносный удар*");
        }
    }

    /// <summary>Thief-leaning bots throw in a Ninjitsu strike from stealth on top of the
    /// usual backstab — same "no overhead" waiver as everything else here.</summary>
    private static void TryUseNinjitsu(Mobile bot)
    {
        if (!IsProfessionCategory(bot, ProfessionCategory.Thief))
        {
            return;
        }

        if (bot.Skills[SkillName.Ninjitsu].Value < 40 || Utility.RandomDouble() >= 0.3)
        {
            return;
        }

        if (SpecialMove.GetCurrentMove(bot) == null)
        {
            SpecialMove.SetCurrentMove(bot, new SurpriseAttack());
            bot.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Внезапная атака*");
        }
    }

    /// <summary>Archers throw in Spellweaving damage on top of shooting — the "druid" flavor
    /// stand-in requested, since this build doesn't have a real Druid school.</summary>
    private static void TryUseSpellweaving(Mobile bot)
    {
        if (!IsProfessionCategory(bot, ProfessionCategory.Ranger))
        {
            return;
        }

        if (bot.Skills[SkillName.Spellweaving].Value < 30 || Utility.RandomDouble() >= 0.2)
        {
            return;
        }

        var spell = new NatureFurySpell(bot);
        if (bot.Mana >= spell.GetMana() / 10)
        {
            spell.Cast();
        }
    }

    /// <summary>Archers "unpack" a pet for the fight — a fake unpacking (no real pouch
    /// item tracking, just a message) since there's no packed-pet mechanic to hook into
    /// here. Tier of animal scales with Animal Taming, same idea as everything else that
    /// scales off a real skill in this file.</summary>
    private static void TrySummonPet(Mobile bot, BotProfile profile)
    {
        if (!IsProfessionCategory(bot, ProfessionCategory.Ranger) || profile.SummonedPet?.Deleted == false)
        {
            return;
        }

        var tamingSkill = bot.Skills[SkillName.AnimalTaming].Value;
        if (tamingSkill < 20 || bot.Map == null)
        {
            return;
        }

        BaseCreature pet = tamingSkill switch
        {
            >= 70 => new GrizzlyBear(),
            >= 45 => new DireWolf(),
            _     => new GreyWolf()
        };

        pet.Controlled = true;
        pet.ControlMaster = bot;
        pet.ControlOrder = OrderType.Follow;
        pet.MoveToWorld(bot.Location, bot.Map);

        if (bot.Combatant?.Deleted == false && bot.Combatant.Alive)
        {
            pet.ControlTarget = bot.Combatant;
            pet.ControlOrder = OrderType.Attack;
            pet.Combatant = bot.Combatant;
            pet.Warmode = true;
        }

        profile.SummonedPet = pet;
        bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Выпускаю {pet.Name}!");
    }

    /// <summary>Keeps the pet actually doing something instead of drifting once it's out —
    /// re-aims it at whatever the archer's currently fighting, or has it just follow when
    /// there's nothing to fight. Call every Dispatch tick while the pet is out.</summary>
    private static void TrySyncPet(Mobile bot, BotProfile profile)
    {
        var pet = profile.SummonedPet;
        if (pet?.Deleted != false)
        {
            return;
        }

        if (bot.Combatant?.Deleted == false && bot.Combatant.Alive)
        {
            if (pet.ControlTarget != bot.Combatant)
            {
                pet.ControlTarget = bot.Combatant;
                pet.ControlOrder = OrderType.Attack;
                pet.Combatant = bot.Combatant;
                pet.Warmode = true;
            }
        }
        else if (pet.ControlOrder != OrderType.Follow)
        {
            pet.ControlTarget = bot;
            pet.ControlOrder = OrderType.Follow;
            pet.Combatant = null;
            pet.Warmode = false;
        }
    }

    /// <summary>Puts the pet away again once there's no fight left for it to be in —
    /// checked from Dispatch's not-fighting path.</summary>
    private static void TryDismissPet(Mobile bot, BotProfile profile)
    {
        if (profile.SummonedPet?.Deleted == false)
        {
            bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Убираю питомца обратно в сумку.");
            profile.SummonedPet.Delete();
        }

        profile.SummonedPet = null;
    }

    /// <summary>Called from PlayerMobile.OnDeath — a summoned pet disappears the instant its
    /// archer dies, not just once combat naturally ends.</summary>
    public static void DismissPetOnDeath(PlayerMobile bot)
    {
        if (Bots.TryGetValue(bot, out var profile) && profile.SummonedPet?.Deleted == false)
        {
            profile.SummonedPet.Delete();
            profile.SummonedPet = null;
        }

        DropGuildDeathGem(bot);
    }

    // Rarer gems have proportionally lower weight — Diamond (the same one used for the
    // random-skill socket bonus, the strongest of the set) is by far the least likely.
    private static readonly (System.Type type, int weight)[] GuildDeathGemTable =
    {
        (typeof(Amber), 30),
        (typeof(Tourmaline), 30),
        (typeof(Amethyst), 20),
        (typeof(Sapphire), 15),
        (typeof(Emerald), 15),
        (typeof(Ruby), 10),
        (typeof(Citrine), 8),
        (typeof(StarSapphire), 5),
        (typeof(Diamond), 2)
    };

    private static void DropGuildDeathGem(PlayerMobile bot)
    {
        var guildName = bot.Guild?.Name;
        if (string.IsNullOrEmpty(guildName))
        {
            return;
        }

        var totalWeight = 0;
        foreach (var (_, weight) in GuildDeathGemTable)
        {
            totalWeight += weight;
        }

        var roll = Utility.Random(totalWeight);
        System.Type chosen = null;

        foreach (var (type, weight) in GuildDeathGemTable)
        {
            if (roll < weight)
            {
                chosen = type;
                break;
            }

            roll -= weight;
        }

        if (chosen == null || System.Activator.CreateInstance(chosen) is not Item gem)
        {
            return;
        }

        var bank = GuildBank.GetOrCreate(guildName);
        bank?.DropItem(gem);
    }

    private static void ApplyCombatTactics(PlayerMobile bot, Mobile target)
    {
        var preferDefensive = bot is BotMobile { Archetype: BotArchetype.Warrior or BotArchetype.Mage };

        CombatStance desired;
        if (preferDefensive)
        {
            desired = CombatStance.Defensive;
        }
        else if (CombatStanceSystem.CanUseStance(bot, CombatStance.Aggressive3x))
        {
            desired = CombatStance.Aggressive3x;
        }
        else if (CombatStanceSystem.CanUseStance(bot, CombatStance.Aggressive2x))
        {
            desired = CombatStance.Aggressive2x;
        }
        else
        {
            desired = CombatStance.Normal;
        }

        if (CombatStanceSystem.GetStance(bot) != desired)
        {
            CombatStanceSystem.SetStance(bot, desired);
        }

        // Find the actual weak point in this specific target's gear for the weapon
        // currently in hand — not a random slot. That's the whole point of aiming.
        if (HitLocationSystem.GetBestLocation(bot, target) is { } location)
        {
            HitLocationSystem.SetPending(bot, location);
        }

        TryCoatWeaponWithPoison(bot);
        TryUseNinjitsu(bot);
        TryUseChivalryOrBushido(bot);
        TryUseSpellweaving(bot);

        if (Bots.TryGetValue(bot, out var profile))
        {
            TrySummonPet(bot, profile);
        }

        // Thief backstab: a sneak attack out of hiding against a monster (not another
        // player) does a chunk of its current HP directly, then breaks stealth same as a
        // real attack would.
        if (bot.Hidden && target is BaseCreature && IsProfessionCategory(bot, ProfessionCategory.Thief))
        {
            var backstabDamage = (int)(target.Hits * 0.7);
            if (backstabDamage > 0)
            {
                AOS.Damage(target, bot, backstabDamage, 100, 0, 0, 0, 0);
            }

            bot.RevealingAction();
        }
    }


    //
    // No real "taunt" mechanic exists to hook into cleanly, so this fakes the effect
    // directly: any nearby hostile creature currently focused on a non-warrior party
    // member gets its Combatant redirected to the warrior instead, as long as the warrior
    // is alive and in range. Simple, a little heavy-handed, but does the job.

    private const int TauntRange = 12;

    private static bool TryCastNecromancy(BotMobile mage)
    {
        if (!TransformationSpellHelper.UnderTransformation(mage) && Utility.RandomDouble() < 0.15 &&
            mage.Skills[SkillName.Necromancy].Value >= 60 &&
            mage.Mana >= new LichFormSpell(mage).GetMana() / 10)
        {
            new LichFormSpell(mage).Cast();
            return true;
        }

        if (mage.Skills[SkillName.SpiritSpeak].Value >= 30 && Utility.RandomDouble() < 0.1 &&
            mage.Mana >= new AnimateDeadSpell(mage).GetMana() / 10 && FindRaisableCorpse(mage) != null)
        {
            new AnimateDeadSpell(mage).Cast();
            return true;
        }

        Spell spell = null;

        if (mage.Mana >= new PainSpikeSpell(mage).GetMana() / 10)
        {
            spell = new PainSpikeSpell(mage);
        }
        else if (mage.Mana >= new StrangleSpell(mage).GetMana() / 10)
        {
            spell = new StrangleSpell(mage);
        }
        else if (mage.Mana >= new WitherSpell(mage).GetMana() / 10)
        {
            spell = new WitherSpell(mage);
        }

        if (spell == null)
        {
            return false;
        }

        spell.Cast();
        return true;
    }

    private static Corpse FindRaisableCorpse(Mobile bot)
    {
        if (bot.Map == null)
        {
            return null;
        }

        foreach (var corpse in bot.Map.GetItemsInRange<Corpse>(bot.Location, 8))
        {
            if (!corpse.Deleted && corpse.Owner is BaseCreature { Summoned: false })
            {
                return corpse;
            }
        }

        return null;
    }

    private static void EnforceWarriorAggro(PlayerMobile warrior, BotProfile profile)
    {
        var party = profile.Party;
        if (party == null || warrior.Map == null || !warrior.Alive)
        {
            return;
        }

        foreach (var creature in warrior.Map.GetMobilesInRange<BaseCreature>(warrior.Location, TauntRange))
        {
            if (!creature.Alive || creature.Deleted || creature is IRaidSpawn)
            {
                continue;
            }

            if (creature.Combatant is PlayerMobile currentTarget &&
                currentTarget != warrior &&
                party.Members.Contains(currentTarget))
            {
                creature.Combatant = warrior;
                creature.Warmode = true;
            }
        }
    }

    // -- Mage support: real spellcasting, real reagents, real mana ------------------------
    //
    // A cast just sets Caster.Target to a pending SpellTarget (see GreaterHeal/Lightning
    // OnCast) — for a real player that becomes an interactive cursor; for a bot we resolve
    // it ourselves via Target.Invoke(caster, target), the same mechanism BaseAI's HealerAI
    // uses. Reagents and mana are consumed by the spell itself, same as any player cast.

    private const double HealThreshold = 0.7; // heal below this fraction of max HP
    private const int ManaReserveForHeal = 20; // keep this much mana in reserve for emergencies

    /// <summary>
    ///     Caught inside kite range — reach for a real escape tool instead of just walking
    ///     away (which TryMageKite alone could never win with an equally-fast pursuer).
    ///     Priority: Teleport clear > drop a wall in their path > paralyze them outright.
    ///     Each option gates on real skill/mana/reagents, same as every other spell here —
    ///     no shortcuts, just an actual decision a player would make in this spot.
    /// </summary>
    /// <summary>
    ///     Combat-only summon, tiered by real Magery skill — Blade Spirits at 30+, Energy
    ///     Vortex at 50+, Summon Daemon at 80+ (each gate also needs the real mana and a
    ///     free follower slot, same as a player would). Deliberately NOT maintained outside
    ///     combat like the Ranger's tamed pet — reuses the same SummonedPet field, so the
    ///     existing "dismiss when not fighting" logic at the top of Dispatch already handles
    ///     that on its own; nothing extra needed here for that part.
    /// </summary>
    private static bool TryMageSummonPet(BotMobile mage, BotProfile profile)
    {
        if (profile.SummonedPet?.Deleted == false && profile.SummonedPet.Alive)
        {
            return false; // already got one out
        }

        if (mage.Combatant?.Deleted != false || !mage.Combatant.Alive || mage.Map == null)
        {
            return false; // combat-only — never summon just to have one standing around
        }

        // The cast from a previous tick might have just resolved — claim it before trying
        // to cast another one on top of it.
        foreach (var creature in mage.Map.GetMobilesInRange<BaseCreature>(mage.Location, 4))
        {
            if (creature.ControlMaster == mage && creature.Alive && !creature.Deleted &&
                creature is BladeSpirits or EnergyVortex or SummonedDaemon)
            {
                profile.SummonedPet = creature;
                creature.ControlTarget = mage.Combatant;
                creature.ControlOrder = OrderType.Attack;
                creature.Combatant = mage.Combatant;
                creature.Warmode = true;
                return true;
            }
        }

        if (Core.Now < profile.NextSummonAttempt)
        {
            return false; // still waiting on the last cast's target to resolve — don't spam it
        }

        var skill = mage.Skills[SkillName.Magery].Value;
        profile.NextSummonAttempt = Core.Now + TimeSpan.FromSeconds(4);

        if (skill >= 80 && mage.Followers + 5 <= mage.FollowersMax &&
            mage.Mana >= new SummonDaemonSpell(mage).GetMana() / 10)
        {
            new SummonDaemonSpell(mage).Cast(); // self-targeting — resolves immediately, no ground target needed
            return true;
        }

        if (skill >= 50 && mage.Followers + 1 <= mage.FollowersMax &&
            mage.Mana >= new EnergyVortexSpell(mage).GetMana() / 10)
        {
            profile.PendingGroundTarget = mage.Location;
            new EnergyVortexSpell(mage).Cast();
            return true;
        }

        if (skill >= 30 && mage.Followers + 1 <= mage.FollowersMax &&
            mage.Mana >= new BladeSpiritsSpell(mage).GetMana() / 10)
        {
            profile.PendingGroundTarget = mage.Location;
            new BladeSpiritsSpell(mage).Cast();
            return true;
        }

        return false;
    }

    private static bool TryMageEscapeSpell(BotMobile mage, BotProfile profile, Mobile target, double magerySkill)
    {
        // Teleport (3rd circle) — the cleanest option when it's available: instantly clear
        // the melee range instead of trying to outwalk something moving at the same speed.
        if (magerySkill >= 50 && mage.Mana >= new TeleportSpell(mage).GetMana() / 10 + ManaReserveForHeal)
        {
            var escapePoint = FindEscapePoint(mage, target);
            if (escapePoint != null)
            {
                profile.PendingGroundTarget = escapePoint;
                new TeleportSpell(mage).Cast();
                return true;
            }
        }

        // No clean teleport spot (boxed in, or just don't know/can't afford it) — drop a
        // field between us and them. Doesn't remove them from melee range this instant,
        // but blocks the tile they'd need to close through, buying room to actually run.
        if (magerySkill >= 45 && Utility.RandomDouble() < 0.6)
        {
            var wallPoint = FindWallPoint(mage, target);

            if (wallPoint != null)
            {
                if (mage.Mana >= new PoisonFieldSpell(mage).GetMana() / 10 + ManaReserveForHeal)
                {
                    profile.PendingGroundTarget = wallPoint;
                    new PoisonFieldSpell(mage).Cast();
                    return true;
                }

                if (mage.Mana >= new FireFieldSpell(mage).GetMana() / 10 + ManaReserveForHeal)
                {
                    profile.PendingGroundTarget = wallPoint;
                    new FireFieldSpell(mage).Cast();
                    return true;
                }
            }
        }

        // Last resort short of just walking — lock them down directly so distance can
        // actually open up on the next few movement ticks.
        if (!target.Paralyzed && magerySkill >= 65 &&
            mage.Mana >= new ParalyzeSpell(mage).GetMana() / 10 + ManaReserveForHeal)
        {
            new ParalyzeSpell(mage).Cast();
            return true;
        }

        return false; // nothing affordable/known — fall through to the normal spell ladder
    }

    /// <summary>Picks a walkable point a few tiles away, roughly opposite the threat, for
    /// an escape Teleport to land on.</summary>
    private static Point3D? FindEscapePoint(Mobile mage, Mobile target)
    {
        if (mage.Map == null)
        {
            return null;
        }

        var dx = mage.X - target.X;
        var dy = mage.Y - target.Y;

        if (dx == 0 && dy == 0)
        {
            dx = Utility.RandomMinMax(-1, 1);
            dy = Utility.RandomMinMax(-1, 1);
        }

        for (var distance = MageIdealRange; distance >= 3; distance--)
        {
            var x = mage.X + System.Math.Sign(dx) * distance;
            var y = mage.Y + System.Math.Sign(dy) * distance;
            var z = mage.Map.GetAverageZ(x, y);
            var loc = new Point3D(x, y, z);

            if (mage.Map.CanSpawnMobile(loc) && mage.InLOS(loc))
            {
                return loc;
            }
        }

        return null;
    }

    /// <summary>Picks the tile directly between the mage and the target — where a field
    /// spell actually blocks the chase instead of just sitting off to one side.</summary>
    private static Point3D? FindWallPoint(Mobile mage, Mobile target)
    {
        if (mage.Map == null)
        {
            return null;
        }

        var dx = System.Math.Sign(target.X - mage.X);
        var dy = System.Math.Sign(target.Y - mage.Y);

        if (dx == 0 && dy == 0)
        {
            return null; // standing on top of each other — no useful "between" tile
        }

        var x = mage.X + dx;
        var y = mage.Y + dy;
        var z = mage.Map.GetAverageZ(x, y);
        var loc = new Point3D(x, y, z);

        return mage.Map.CanFit(x, y, z, 16) ? loc : null;
    }

    // -- Bard skills: Provocation/Peacemaking/Discordance actually get used now, not just
    // trained numbers sitting unused. All three are hard-gated on having an instrument —
    // see BotMobile.GiveInstrumentIfBard — same as a real player would need one in hand.

    private static readonly TimeSpan BardSkillCooldown = TimeSpan.FromSeconds(8);

    private static bool TryUseBardSkill(PlayerMobile bot, BotProfile profile)
    {
        var hpRatio = bot.HitsMax > 0 ? (double)bot.Hits / bot.HitsMax : 1.0;

        // Losing badly — try to talk the fight down instead of just soaking more hits.
        // Peacemaking calms everyone near whoever we target it on, combatant included.
        if (hpRatio < 0.4 && bot.Skills[SkillName.Peacemaking].Value >= 30 &&
            bot.Combatant?.Deleted == false && bot.Combatant.Alive)
        {
            profile.BardAction = BotProfile.PendingBardAction.Peace;
            profile.NextBardSkillTime = Core.Now + BardSkillCooldown;
            SkillHandlers.Peacemaking.OnUse(bot);
            return true;
        }

        // Facing real monsters, two or more nearby — turn a couple of them on each other
        // instead of soaking all of it personally. Provocation only ever works on
        // BaseCreature (never on another player/bot), same restriction a real player runs
        // into — see Provocation.InternalFirstTarget.OnTarget upstream.
        if (bot.Combatant is BaseCreature && bot.Skills[SkillName.Provocation].Value >= 30 && bot.Map != null)
        {
            BaseCreature first = null;
            BaseCreature second = null;

            foreach (var creature in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, 8))
            {
                if (!creature.Alive || creature.Deleted || creature.Controlled || creature is IRaidSpawn)
                {
                    continue;
                }

                if (first == null)
                {
                    first = creature;
                }
                else if (second == null)
                {
                    second = creature;
                    break;
                }
            }

            if (first != null && second != null)
            {
                profile.PendingProvokeFirst = first;
                profile.BardAction = BotProfile.PendingBardAction.ProvokeFirst;
                profile.NextBardSkillTime = Core.Now + BardSkillCooldown;
                SkillHandlers.Provocation.OnUse(bot);
                return true;
            }
        }

        // Otherwise, soften whoever's actually fighting us before it comes down to weapon
        // swings — Discordance debuffs their combat/resist stats for the fight's duration.
        if (bot.Combatant?.Deleted == false && bot.Combatant.Alive &&
            bot.Skills[SkillName.Discordance].Value >= 20 && Utility.RandomDouble() < 0.5)
        {
            profile.BardAction = BotProfile.PendingBardAction.Discord;
            profile.NextBardSkillTime = Core.Now + BardSkillCooldown;
            SkillHandlers.Discordance.OnUse(bot);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Resolves whatever Target TryUseBardSkill's real SkillHandlers.*.OnUse call just
    ///     put on the bot — can't pattern-match on the Target's concrete type the way mage
    ///     spells do (Provocation/Peacemaking/Discordance all keep their Target classes
    ///     private), so profile.BardAction (set right before the OnUse call) carries that
    ///     information instead.
    /// </summary>
    private static void ResolvePendingBardTarget(PlayerMobile bot, BotProfile profile, Target pendingTarget)
    {
        switch (profile.BardAction)
        {
            case BotProfile.PendingBardAction.Discord:
            case BotProfile.PendingBardAction.Peace:
                profile.BardAction = BotProfile.PendingBardAction.None;

                if (bot.Combatant?.Deleted == false && bot.Combatant.Alive)
                {
                    pendingTarget.Invoke(bot, bot.Combatant);
                }
                else
                {
                    pendingTarget.Cancel(bot, TargetCancelType.Canceled);
                }

                break;

            case BotProfile.PendingBardAction.ProvokeFirst:
                var first = profile.PendingProvokeFirst;
                profile.BardAction = BotProfile.PendingBardAction.ProvokeSecond;

                if (first?.Deleted != false || !first.Alive)
                {
                    profile.BardAction = BotProfile.PendingBardAction.None;
                    profile.PendingProvokeFirst = null;
                    pendingTarget.Cancel(bot, TargetCancelType.Canceled);
                }
                else
                {
                    pendingTarget.Invoke(bot, first);
                }

                break;

            case BotProfile.PendingBardAction.ProvokeSecond:
                var against = profile.PendingProvokeFirst;
                profile.PendingProvokeFirst = null;
                profile.BardAction = BotProfile.PendingBardAction.None;

                BaseCreature second = null;
                if (bot.Map != null)
                {
                    foreach (var creature in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, 8))
                    {
                        if (creature != against && creature.Alive && !creature.Deleted &&
                            !creature.Controlled && creature is not IRaidSpawn)
                        {
                            second = creature;
                            break;
                        }
                    }
                }

                if (second != null)
                {
                    pendingTarget.Invoke(bot, second);
                }
                else
                {
                    pendingTarget.Cancel(bot, TargetCancelType.Canceled);
                }

                break;

            default:
                pendingTarget.Cancel(bot, TargetCancelType.Canceled);
                break;
        }
    }

    private static void DoMageAction(BotMobile mage, BotProfile profile)
    {
        TryGainStat(mage, StatType.Int);

        // Resolve a pending cast from last tick, if any.
        if (mage.Target is Target pendingTarget)
        {
            ResolvePendingSpellTarget(mage, profile, pendingTarget);
            return;
        }

        if (mage.Poisoned && mage.Mana >= new CureSpell(mage).GetMana() / 10)
        {
            new CureSpell(mage).Cast();
            return;
        }

        var wounded = FindMostWoundedPartyMember(mage, profile);

        if (wounded != null && mage.Mana >= new GreaterHealSpell(mage).GetMana() / 10)
        {
            profile.PendingHealTarget = wounded;
            new GreaterHealSpell(mage).Cast();
            return;
        }

        if (mage.Combatant?.Deleted == false && mage.Combatant.Alive && Utility.RandomDouble() < 0.95)
        {
            var magerySkill = mage.Skills[SkillName.Magery].Value;
            var target = mage.Combatant;

            // Caught at melee range — this is what TryMageKite's plain walk-away step was
            // handling alone before. A real mage has better tools than just legging it:
            // teleport clear, drop a wall in the pursuer's path, or paralyze them outright.
            // Falls through to the normal spell ladder below if none of these are
            // affordable/known — the plain kite step in TryMageKite is still the last resort.
            if (mage.GetDistanceToSqrt(target.Location) < MageTooCloseRange &&
                TryMageEscapeSpell(mage, profile, target, magerySkill))
            {
                return;
            }

            if (TryMageSummonPet(mage, profile))
            {
                return;
            }

            // Dispel any summoned creature on sight — a nuke that just gets replaced by
            // another summon isn't worth casting.
            if (target is BaseCreature { Summoned: true } && magerySkill >= 60 &&
                mage.Mana >= new DispelSpell(mage).GetMana() / 10)
            {
                new DispelSpell(mage).Cast();
                return;
            }

            if (profile.PrefersNecromancy && mage.Skills[SkillName.Necromancy].Value >= 30 &&
                TryCastNecromancy(mage))
            {
                return;
            }

            // Warriors run a big Str/Int gap by design (our own profession stat spread) —
            // Mind Blast scales off exactly that gap, so it's a real anti-warrior pick, not
            // just flavor text.
            var isHighGapTarget = System.Math.Abs(target.RawStr - target.RawInt) >= 30;

            if (magerySkill >= 45 && Utility.RandomDouble() < 0.15)
            {
                if (mage.Mana >= new PoisonFieldSpell(mage).GetMana() / 10 + ManaReserveForHeal)
                {
                    new PoisonFieldSpell(mage).Cast();
                    return;
                }

                if (mage.Mana >= new FireFieldSpell(mage).GetMana() / 10 + ManaReserveForHeal)
                {
                    new FireFieldSpell(mage).Cast();
                    return;
                }
            }

            if (isHighGapTarget && magerySkill >= 70 && mage.Mana >= new MindBlastSpell(mage).GetMana() / 10 + ManaReserveForHeal)
            {
                new MindBlastSpell(mage).Cast();
                return;
            }

            if (!target.Poisoned && magerySkill >= 40 && Utility.RandomDouble() < 0.35 &&
                mage.Mana >= new PoisonSpell(mage).GetMana() / 10 + ManaReserveForHeal)
            {
                new PoisonSpell(mage).Cast();
                return;
            }

            if (!target.Paralyzed && magerySkill >= 65 && Utility.RandomDouble() < 0.25 &&
                mage.Mana >= new ParalyzeSpell(mage).GetMana() / 10 + ManaReserveForHeal)
            {
                new ParalyzeSpell(mage).Cast();
                return;
            }

            if (magerySkill >= 30 && Utility.RandomDouble() < 0.2)
            {
                var debuff = Utility.RandomList(0, 1, 2);
                var debuffCost = debuff switch
                {
                    0 => new WeakenSpell(mage).GetMana() / 10,
                    1 => new ClumsySpell(mage).GetMana() / 10,
                    _ => new FeeblemindSpell(mage).GetMana() / 10
                };

                if (mage.Mana >= debuffCost + ManaReserveForHeal)
                {
                    switch (debuff)
                    {
                        case 0: new WeakenSpell(mage).Cast(); return;
                        case 1: new ClumsySpell(mage).Cast(); return;
                        default: new FeeblemindSpell(mage).Cast(); return;
                    }
                }
            }

            if (TryDispelNearbyField(mage, profile))
            {
                return;
            }

            // Reach for the strongest damage spell the mage can actually afford and has
            // the Magery for — falls back down the ladder instead of always trying the top.
            if (magerySkill >= 95 && mage.Mana >= new FlameStrikeSpell(mage).GetMana() / 10 + ManaReserveForHeal)
            {
                new FlameStrikeSpell(mage).Cast();
                return;
            }

            if (magerySkill >= 90 && mage.Mana >= new ExplosionSpell(mage).GetMana() / 10 + ManaReserveForHeal)
            {
                new ExplosionSpell(mage).Cast();
                return;
            }

            if (magerySkill >= 80 && mage.Mana >= new EnergyBoltSpell(mage).GetMana() / 10 + ManaReserveForHeal)
            {
                new EnergyBoltSpell(mage).Cast();
                return;
            }

            if (magerySkill >= 50 && mage.Mana >= new FireballSpell(mage).GetMana() / 10 + ManaReserveForHeal)
            {
                new FireballSpell(mage).Cast();
                return;
            }

            if (mage.Mana >= new LightningSpell(mage).GetMana() / 10 + ManaReserveForHeal)
            {
                new LightningSpell(mage).Cast();
                return;
            }

            if (mage.Mana >= new MagicArrowSpell(mage).GetMana() / 10)
            {
                new MagicArrowSpell(mage).Cast();
                return;
            }
        }

        // Nothing urgent — top off buffs on downtime before just standing around.
        if ((mage.Combatant?.Deleted != false || !mage.Combatant.Alive) && Core.Now >= profile.NextBuffTime)
        {
            if (TryCastBuff(mage, profile))
            {
                return;
            }
        }

        // Still nothing urgent — acquire a target like everyone else so there's something
        // to eventually nuke, but don't melee it.
        if (mage.Combatant?.Deleted != false || !mage.Combatant.Alive)
        {
            if (mage.Map != null)
            {
                foreach (var creature in mage.Map.GetMobilesInRange<BaseCreature>(mage.Location, 10))
                {
                    if (creature.Alive && !creature.Deleted && creature is not IRaidSpawn)
                    {
                        mage.Combatant = creature;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Buffs self first (Bless/Strength/Agility/Cunning/Protection/Magic Reflect, one
    ///     per call so it spreads over a few ticks like a real player buffing up would),
    ///     then a wounded-adjacent party member if self is already topped up. Cooldown gets
    ///     refreshed either way so this doesn't get rolled every single idle tick.
    /// </summary>
    private static bool TryCastBuff(BotMobile mage, BotProfile profile)
    {
        profile.NextBuffTime = Core.Now + TimeSpan.FromMinutes(2);

        Mobile buffTarget = mage;
        if (Utility.RandomBool() && profile.Party != null)
        {
            foreach (var member in profile.Party.Members)
            {
                if (member != mage && member.Alive && !member.Deleted)
                {
                    buffTarget = member;
                    break;
                }
            }
        }

        var castSelf = buffTarget == mage;

        if (castSelf && mage.Mana >= new MagicReflectSpell(mage).GetMana() / 10 && Utility.RandomBool())
        {
            new MagicReflectSpell(mage).Cast(); // no target phase — applies immediately
            return true;
        }

        if (mage.Mana >= new BlessSpell(mage).GetMana() / 10)
        {
            profile.PendingHealTarget = buffTarget; // reuse the same "who" slot buffs resolve against
            new BlessSpell(mage).Cast();
            return true;
        }

        if (mage.Mana >= new ProtectionSpell(mage).GetMana() / 10)
        {
            profile.PendingHealTarget = buffTarget;
            new ProtectionSpell(mage).Cast();
            return true;
        }

        if (mage.Mana >= new StrengthSpell(mage).GetMana() / 10)
        {
            profile.PendingHealTarget = buffTarget;
            new StrengthSpell(mage).Cast();
            return true;
        }

        return false;
    }

    private static bool TryDispelNearbyField(BotMobile mage, BotProfile profile)
    {
        if (mage.Map == null || mage.Mana < new DispelFieldSpell(mage).GetMana() / 10)
        {
            return false;
        }

        foreach (var item in mage.Map.GetItemsInRange<Item>(mage.Location, 8))
        {
            if (item is FireFieldItem or PoisonField && !item.Deleted)
            {
                profile.PendingFieldTarget = item;
                new DispelFieldSpell(mage).Cast();
                return true;
            }
        }

        return false;
    }

    private static void ResolvePendingSpellTarget(BotMobile mage, BotProfile profile, Target pendingTarget)
    {
        // Item-targeted spells (Dispel Field targets the field itself, not a Mobile).
        if (pendingTarget is ITargetingSpell<Item> necroItemTarget)
        {
            var corpse = FindRaisableCorpse(mage);
            if (corpse == null)
            {
                pendingTarget.Cancel(mage, TargetCancelType.Canceled);
                return;
            }

            pendingTarget.Invoke(mage, corpse);
            return;
        }

        if (pendingTarget is ISpellTarget<Item> itemSpellTarget)
        {
            var fieldItem = profile.PendingFieldTarget;
            profile.PendingFieldTarget = null;

            if (itemSpellTarget.Spell is DispelFieldSpell && fieldItem?.Deleted == false)
            {
                pendingTarget.Invoke(mage, fieldItem);
            }
            else
            {
                pendingTarget.Cancel(mage, TargetCancelType.Canceled);
            }

            return;
        }

        // Ground-targeted spells (fields, teleport, combat summons) all implement
        // ISpellTarget<IPoint3D> instead of ISpellTarget<Mobile> — the actual spell class
        // itself only implements ITargetingSpell<IPoint3D> (no .Spell member on that one;
        // it's the interface the spell exposes, not the interface the wrapping Target
        // exposes), so this has to check ISpellTarget<IPoint3D> to read .Spell at all.
        //
        // Fields default to landing on the combatant (offensive use) unless
        // TryMageEscapeSpell already set PendingGroundTarget for the defensive "wall
        // between us" cast. Teleport/Blade Spirits/Energy Vortex always resolve via
        // PendingGroundTarget (set by TryMageEscapeSpell or TryMageSummonPet respectively)
        // — if that's missing for some reason, better to eat the reagents on a canceled
        // cast than target blind.
        if (pendingTarget is ISpellTarget<IPoint3D> groundSpellTarget)
        {
            if (groundSpellTarget.Spell is FireFieldSpell or PoisonFieldSpell && mage.Map != null &&
                profile.PendingGroundTarget is { } wallPoint)
            {
                profile.PendingGroundTarget = null;
                pendingTarget.Invoke(mage, new LandTarget(wallPoint, mage.Map));
            }
            else if (groundSpellTarget.Spell is FireFieldSpell or PoisonFieldSpell &&
                mage.Combatant?.Deleted == false && mage.Combatant.Alive && mage.Map != null)
            {
                pendingTarget.Invoke(mage, new LandTarget(mage.Combatant.Location, mage.Map));
            }
            else if (groundSpellTarget.Spell is TeleportSpell or BladeSpiritsSpell or EnergyVortexSpell &&
                mage.Map != null && profile.PendingGroundTarget is { } summonPoint)
            {
                profile.PendingGroundTarget = null;
                pendingTarget.Invoke(mage, new LandTarget(summonPoint, mage.Map));
            }
            else
            {
                pendingTarget.Cancel(mage, TargetCancelType.Canceled);
            }

            return;
        }

        if (pendingTarget is ITargetingSpell<Mobile> necroTarget)
        {
            var necroResolved = mage.Combatant?.Deleted == false && mage.Combatant.Alive ? mage.Combatant : null;

            if (necroResolved == null)
            {
                pendingTarget.Cancel(mage, TargetCancelType.Canceled);
                return;
            }

            pendingTarget.Invoke(mage, necroResolved);
            return;
        }

        if (pendingTarget is not ISpellTarget<Mobile> spellTarget)
        {
            pendingTarget.Cancel(mage, TargetCancelType.Canceled);
            return;
        }

        Mobile resolved = spellTarget.Spell switch
        {
            GreaterHealSpell or HealSpell or BlessSpell or ProtectionSpell or StrengthSpell =>
                profile.PendingHealTarget?.Deleted == false && profile.PendingHealTarget.Alive
                    ? profile.PendingHealTarget
                    : mage,
            CureSpell => mage,
            LightningSpell or MagicArrowSpell or FireballSpell or EnergyBoltSpell or
                MindBlastSpell or FlameStrikeSpell or ExplosionSpell or PoisonSpell or
                ParalyzeSpell or DispelSpell or WeakenSpell or ClumsySpell or FeeblemindSpell =>
                mage.Combatant?.Deleted == false && mage.Combatant.Alive ? mage.Combatant : null,
            _ => null
        };

        profile.PendingHealTarget = null;

        if (resolved == null)
        {
            pendingTarget.Cancel(mage, TargetCancelType.Canceled);
            return;
        }

        pendingTarget.Invoke(mage, resolved);
    }

    private static Mobile FindMostWoundedPartyMember(Mobile mage, BotProfile profile)
    {
        Mobile worst = null;
        var worstRatio = HealThreshold;

        var selfRatio = mage.HitsMax > 0 ? (double)mage.Hits / mage.HitsMax : 1.0;
        if (mage.Alive && selfRatio < worstRatio)
        {
            worst = mage;
            worstRatio = selfRatio;
        }

        if (profile.Party != null)
        {
            foreach (var member in profile.Party.Members)
            {
                if (member == mage || !member.Alive || member.Deleted || member.HitsMax <= 0)
                {
                    continue;
                }

                var ratio = (double)member.Hits / member.HitsMax;
                if (ratio < worstRatio)
                {
                    worst = member;
                    worstRatio = ratio;
                }
            }
        }

        return worst;
    }

    private static void DoReturnHome(PlayerMobile bot, BotProfile profile)
    {
        if (StepTowardPath(bot, profile, profile.HomeLocation) && bot.Map == profile.HomeMap)
        {
            profile.Activity = BotActivity.Idle;
        }
    }

    // -- Movement helpers ----------------------------------------------------------------
    //
    // Simple direct-step movement, not real pathfinding (see dev-docs/pathfinding.md for
    // the real BaseAI/PathFollower stack). Fine on open ground; a bot can get stuck on
    // complex terrain. Good next upgrade once basic behaviors are validated.

    private const int PanicRange = 15;
    private static readonly TimeSpan PanicMessageCooldown = TimeSpan.FromSeconds(30);

    private static bool TryCowardFlee(PlayerMobile bot, BotProfile profile)
    {
        if (bot.Map == null)
        {
            return false;
        }

        var threatened = bot.Combatant?.Deleted == false && bot.Combatant.Alive;

        if (!threatened)
        {
            foreach (var m in bot.Map.GetMobilesInRange<Mobile>(bot.Location, PanicRange))
            {
                if (m == bot || !m.Alive || m.Deleted)
                {
                    continue;
                }

                if (m is BotMobile { IsPk: true } || m.Combatant == bot)
                {
                    threatened = true;
                    break;
                }
            }
        }

        if (!threatened)
        {
            return false;
        }

        if (Core.Now - profile.LastPanicMessage > PanicMessageCooldown)
        {
            bot.PublicOverheadMessage(MessageType.Regular, 0x22, false, "сантехники пришли, я не могу");
            profile.LastPanicMessage = Core.Now;
        }

        FleeFromCombat(bot);
        return true;
    }

    private static bool TryChallengePK(PlayerMobile bot)
    {
        if (bot.Combatant?.Deleted == false && bot.Combatant.Alive)
        {
            return true; // already at it — leave the fight (and the inevitable loss) alone
        }

        if (bot.Map == null)
        {
            return false;
        }

        foreach (var m in bot.Map.GetMobilesInRange<Mobile>(bot.Location, HuntRange))
        {
            if (m is BotMobile { IsPk: true } pk && pk.Alive && !pk.Deleted)
            {
                ApplyCombatTactics(bot, pk);
                bot.Combatant = pk;
                bot.Warmode = true;
                return true;
            }
        }

        return false;
    }

    private static void TryDrinkHealPotion(Mobile bot)
    {
        var potion = bot.Backpack?.FindItemByType<BaseHealPotion>();
        if (potion == null || !potion.CanDrink(bot))
        {
            return;
        }

        potion.Drink(bot);
        bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Пью зелье лечения!");
    }

    private static void FleeFromCombat(Mobile bot)
    {
        var threat = bot.Combatant;

        bot.Combatant = null;
        bot.Warmode = false;

        if (threat == null || bot.Map == null)
        {
            return;
        }

        var dx = bot.X - threat.X;
        var dy = bot.Y - threat.Y;

        if (dx == 0 && dy == 0)
        {
            dx = Utility.RandomMinMax(-1, 1);
            dy = Utility.RandomMinMax(-1, 1);
        }

        var fleeTarget = new Point3D(
            bot.X + Math.Sign(dx) * 6,
            bot.Y + Math.Sign(dy) * 6,
            bot.Z
        );

        StepToward(bot, fleeTarget, 3);
    }

    private static bool StepToward(Mobile m, Point3D destination, int steps)
    {
        if (m.Spell?.IsCasting == true && !m.Mounted)
        {
            return false; // moving would disturb our own cast — just hold position this tick
        }

        for (var i = 0; i < steps; i++)
        {
            if (m.Location == destination)
            {
                return true;
            }

            var direction = m.GetDirectionTo(destination) | Direction.Running;
            m.Direction = direction;
            m.Move(direction);
        }

        return m.Location == destination || m.GetDistanceToSqrt(destination) < 2;
    }

    /// <summary>
    ///     Real pathfinding — routes around walls/buildings/water instead of walking
    ///     straight at the destination and getting stuck on the first obstacle. Reuses the
    ///     engine's own PathFollower (same thing normal creature AI navigates with), cached
    ///     per bot so it isn't recomputing the route every tick. Steps once per call — the
    ///     fast movement pass already provides the tick rate.
    /// </summary>
    private static bool StepTowardPath(PlayerMobile bot, BotProfile profile, Point3D destination)
    {
        if (bot.Spell?.IsCasting == true && !bot.Mounted)
        {
            return false;
        }

        if (bot.Location == destination)
        {
            return true;
        }

        if (profile.ActivePath == null || profile.ActivePathGoal != destination)
        {
            profile.ActivePath = new PathFollower(bot, destination);
            profile.ActivePathGoal = destination;
        }

        var arrived = profile.ActivePath.Follow(true, 0);

        if (arrived || bot.GetDistanceToSqrt(destination) < 2)
        {
            profile.ActivePath = null;
            return true;
        }

        return false;
    }

    private static Point3D RandomNearbyPoint(Point3D center, int radius)
    {
        var x = center.X + Utility.RandomMinMax(-radius, radius);
        var y = center.Y + Utility.RandomMinMax(-radius, radius);
        return new Point3D(x, y, center.Z);
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.Write(_seeded);
        // Bot Mobiles persist on their own; profiles are cheap to rebuild fresh on world
        // load (see Initialize's re-attach loop) rather than round-tripping them here.
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version
        _seeded = reader.ReadBool();
    }
}
