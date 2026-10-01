using System;
using System.Collections.Generic;
using System.Linq;
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

/// <summary>Travel and movement — mounts, banking trips, pocket cap enforcement, city travel, step-by-step pathing helpers.</summary>
public partial class BotController
{

    /// <summary>Сколько случайных точек перебрать, прежде чем признать, что идти
    /// некуда.</summary>
    private const int ScoutAttempts = 12;

    /// <summary>
    ///     Точка «пойду посмотрю, что там» — последний способ выбрать цель, когда ни одной
    ///     известной жилы, рощи или заводи в памяти нет.
    ///
    ///     Раньше она брала случайный угол и случайное расстояние и возвращала результат
    ///     как есть — не проверяя ничего. Половина таких целей оказывалась водой, скалой,
    ///     чужим материком или вовсе точкой за краем карты; GetAverageZ послушно выдаёт
    ///     высоту для любых координат, поэтому невозможная точка выглядела совершенно
    ///     нормальной. Бот шёл к ней четверть часа, упирался и вставал — и это принимали
    ///     за плохой поиск пути, хотя искать было нечего: дороги туда не существовало.
    ///
    ///     Теперь точка выбирается из нескольких проб и обязана быть хотя бы правдоподобной
    ///     (см. BotWorldKnowledge.IsPlausibleDestination): в границах карты, на неё можно
    ///     встать, и недавно об неё никто не убился. Не нашлось ни одной за дюжину
    ///     попыток — бот остаётся на месте, что честнее, чем идти в никуда.
    /// </summary>
    private static Point3D BlindScoutPoint(Mobile bot)
    {
        for (var attempt = 0; attempt < ScoutAttempts; attempt++)
        {
            var angle = Utility.RandomDouble() * System.Math.PI * 2;
            var distance = Utility.RandomMinMax(GatherMinDistance, GatherMaxDistance);

            var x = bot.X + (int)(System.Math.Cos(angle) * distance);
            var y = bot.Y + (int)(System.Math.Sin(angle) * distance);
            var z = bot.Map?.GetAverageZ(x, y) ?? bot.Z;
            var candidate = new Point3D(x, y, z);

            if (BotWorldKnowledge.IsPlausibleDestination(bot.Map, candidate))
            {
                return candidate;
            }
        }

        return bot.Location; // идти некуда — значит, никуда и не идём
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

        if (bot.BankBox == null || Banker.GetBalance(bot) < (int)PackHorseCost)
        {
            return;
        }

        if (!Banker.Withdraw(bot, (int)PackHorseCost))
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

        var pocketValue = backpack.FindItemByType<Gold>(false)?.Amount ?? 0;

        if (pocketValue <= PocketCap)
        {
            return;
        }

        var excess = (int)(pocketValue - PocketCap);
        if (backpack.ConsumeTotal(typeof(Gold), excess))
        {
            Banker.Deposit(bot, excess);
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

    // -- City travel --------------------------------------------------------------------
    //
    // Simplified: no real pathfinding to the stone across a whole city, and no walking
    // between cities tile-by-tile either — a bot "uses the stone" by stepping toward its
    // own city center for a couple of cycles, then relocating straight to the destination
    // city's stone, same as a player clicking the gump would end up doing.

    private static string PickTravelDestination(string currentCity, bool preferHotMarkets = false)
    {
        var cities = new List<string>(CityControlSystem.Cities.Keys);
        if (currentCity != null)
        {
            cities.Remove(currentCity);
        }

        if (cities.Count == 0)
        {
            return null;
        }

        // Traders lean toward a city with a recent notable sale — real pull, not a hard
        // requirement, so it still occasionally picks something else and doesn't turn
        // into every trader in the world dog-piling the same city.
        if (preferHotMarkets)
        {
            var hot = BotHotMarkets.ActiveCities().FirstOrDefault(c => c != currentCity && cities.Contains(c));

            if (hot != null && Utility.RandomDouble() < BotTuning.HotMarketDetourChance)
            {
                return hot;
            }
        }

        return cities.RandomElement();
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

        // "Use" the stone: pay the same fee a player would (see CityTravelGump.TravelCost),
        // then relocate straight to the destination city's stone. If a bot can't afford it,
        // it just stays put and idles.
        const int travelCost = 1;

        if (bot.Backpack == null || !bot.Backpack.ConsumeTotal(typeof(Gold), travelCost))
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
        if (GetArchetype(bot, profile) == BotArchetype.Trader)
        {
            DoTraderTrade(bot, destinationCity);
        }

        profile.Activity = BotActivity.Idle;
    }

    private static void DoReturnHome(PlayerMobile bot, BotProfile profile)
    {
        if (StepTowardPath(bot, profile, profile.HomeLocation) && bot.Map == profile.HomeMap)
        {
            profile.Activity = BotActivity.Idle;
        }
    }

    /// <summary>
    ///     Брожение с поиском пути: осмотреть подземелье, подрейфовать на охотничьем месте,
    ///     пройтись в поисках жертвы.
    ///
    ///     Заменяет связку StepToward + RandomNearbyPoint, которая стояла в этих местах и
    ///     была неверна дважды. Точка бралась по CanSpawnMobile — «сюда можно встать», а не
    ///     «сюда можно дойти»; идти к ней полагалось шагом по прямой, без обхода вообще.
    ///     Снаружи это сходило с рук — промахнулся и побрёл дальше, — а в подземелье бот
    ///     упирался в стену и молотил в неё до конца похода.
    ///
    ///     Цель держится в профиле между тиками: выбирай мы новую каждый раз, бот дёргался
    ///     бы на месте. Достигнутая или признанная недостижимой — сбрасывается, и в
    ///     следующий тик берётся новая.
    /// </summary>
    private static void Patrol(PlayerMobile bot, BotProfile profile, int radius)
    {
        if (profile.PatrolTarget == default)
        {
            profile.PatrolTarget = RoutableNearbyPoint(bot, radius);
        }

        if (StepTowardPath(bot, profile, profile.PatrolTarget) || profile.GoalUnreachable)
        {
            profile.GoalUnreachable = false;
            profile.PatrolTarget = default;
        }
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
    ///     Directly invokes whatever Teleporter sits at this exact tile, instead of relying
    ///     purely on Mobile.Move's own OnMoveOver side effect to have already fired. A
    ///     scripted bot Move() call goes through the identical engine pipeline a real
    ///     player's click-to-move does, but in practice bots reliably park forever right on
    ///     top of a mine/cave/dungeon entrance tile ("стоит на чёрном тайле, но стоит его
    ///     протолкнуть все ок" — a player shoving them forces a fresh move onto the tile
    ///     from a different starting point, which is the one thing that reliably re-fires
    ///     it). Called from every StepTowardPath arrival, so it covers all of them uniformly
    ///     — MineComplexSystem's player-dug mine entrances encountered during ordinary
    ///     gathering/hunting travel included, not just BotHuntingDungeon.cs's own party
    ///     dungeon system.
    /// </summary>
    private static void TryTriggerTeleporterAt(Mobile bot, Point3D loc, Map map)
    {
        if (map == null)
        {
            return;
        }

        foreach (var item in map.GetItemsInRange<Teleporter>(loc, 0))
        {
            if (item.Location == loc)
            {
                item.OnMoveOver(bot);
                return;
            }
        }
    }

    // Straight ahead first, then progressively wider detours to each side (radians).
}
