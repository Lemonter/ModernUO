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
///     Что бот делает, когда свободен, — и как он выбирает, чем заняться дальше.
///
///     Это «мозг» распорядка дня: DoIdle прогоняет срочное (убежать от набега, спрятаться,
///     обчистить соседа), а если ничего срочного нет — крутит рулетку занятий, разную для
///     каждого склада (у воина диск в основном подземелье, у ремесленника — добыча).
///     Вероятности этой рулетки лежат в BotTuning.
///
///     Раньше всё это жило в BotSocial.cs вместе с воровством, попрошайничеством и сбором
///     отрядов — 720 строк трёх несвязанных тем в одном файле.
/// </summary>
public partial class BotController
{

    // -- Idle / wandering ------------------------------------------------------------

    private static void DoIdle(PlayerMobile bot, BotProfile profile)
    {
        BotRumors.TryHear(bot);

        // Приказ человека сильнее распорядка дня.
        //
        // Сюда бот попадает не только «закончив дело», но и после любой неудачи: цель
        // оказалась недостижимой, участок выработан, дерево не срубилось. Раньше в этот
        // момент крутилась рулетка занятий — и бот, которому сказали рубить, уходил копать
        // руду, потому что рулетка так легла. Со стороны это выглядело как своеволие.
        //
        // Теперь при поставленной задаче он просто берётся за неё снова, с новым местом:
        // ищет другое дерево вместо того, к которому не дошёл.
        if (TryResumeForcedGoal(bot, profile))
        {
            return;
        }

        // Mahaon: peaceful archetypes bail from an active raid instead of going about
        // their normal business next to it — a raid mob is anything named "Raid..."
        // (RaidBandit, RaidSkeleton, RaidLich, etc. — see Systems.MahaonRaids), detected
        // by type name rather than a shared base class so this doesn't need to know the
        // full raid mob roster.
        var archetype0 = GetArchetype(bot, profile);
        if ((archetype0 == BotArchetype.Crafter || archetype0 == BotArchetype.Trader) && bot.Map != null)
        {
            Mobile nearestRaider = null;
            var nearestDist = int.MaxValue;

            foreach (var nearby in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, 10))
            {
                if (!nearby.GetType().Name.StartsWith("Raid", StringComparison.Ordinal))
                {
                    continue;
                }

                var dist = (int)bot.GetDistanceToSqrt(nearby.Location);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestRaider = nearby;
                }
            }

            if (nearestRaider != null)
            {
                var fleeX = bot.X + (bot.X - nearestRaider.X);
                var fleeY = bot.Y + (bot.Y - nearestRaider.Y);

                if (Utility.RandomDouble() < BotTuning.RaidFleeShoutChance)
                {
                    bot.PublicOverheadMessage(MessageType.Regular, 0x22, false, "Набег! Бежим!");
                }

                StepToward(bot, new Point3D(fleeX, fleeY, bot.Z), 3);
                return;
            }
        }

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
            !IsWatchedByGuards(bot) && Utility.RandomDouble() < BotTuning.StealAttemptChance &&
            TryStealFromNearby(bot))
        {
            return;
        }

        if (ProfessionSystem.TouchesCategory(bot, ProfessionCategory.Bard) &&
            Utility.RandomDouble() < BotTuning.BegAttemptChance && TryBegNearby(bot))
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

        if (GetArchetype(bot, profile) == BotArchetype.Archer && bot.Target == null &&
            Utility.RandomDouble() < BotTuning.TameAttemptChance && TryTameNearby(bot))
        {
            return;
        }

        if (profile.IsPK && Utility.RandomDouble() < BotTuning.PkGoesHuntingChance)
        {
            profile.Activity = BotActivity.Hunting;
            return;
        }

        if (Utility.RandomDouble() < BotTuning.BankingTripChance)
        {
            profile.Activity = BotActivity.BankingTrip;
            return;
        }

        if (Utility.RandomDouble() < BotTuning.AcquireMountChance)
        {
            TryAcquireMount(bot);
        }

        var archetype = GetArchetype(bot, profile);
        var isTrader = archetype == BotArchetype.Trader;

        // Mahaon: no in-game day/night clock exists to hook into, so this uses the real
        // server clock hour as a simple day/night proxy — traders lean into travel/trade
        // during "day" hours, and everyone has a small chance to just linger in a tavern
        // ("sleep") during "night" hours instead of picking a new active task. Cheap,
        // doesn't need a whole new time system, and still gives population rhythm.
        var serverHour = Core.Now.Hour;
        var isNight = serverHour is >= 22 or < 6;

        if (isNight && Utility.RandomDouble() < BotTuning.NightIdleChance)
        {
            profile.Activity = BotActivity.Idle;
            return;
        }

        if (!profile.Stationary)
        {
            var travelChance = isTrader ? (isNight ? 0.15 : 0.5) : 0.03;
            if (Utility.RandomDouble() < travelChance)
            {
                var destination = PickTravelDestination(profile.CurrentCity, isTrader);
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
        var canHunt = !profile.IsPK && archetype is BotArchetype.Warrior or BotArchetype.Archer or BotArchetype.Mage;

        // (wander, gather, hunt, dungeon) weights — must sum to 1.0 per archetype.
        var (wWander, wGather, wHunt, wDungeon) = archetype switch
        {
            BotArchetype.Warrior => (0.15, 0.15, 0.25, 0.45),
            BotArchetype.Mage    => (0.15, 0.30, 0.15, 0.40),
            BotArchetype.Archer  => (0.15, 0.20, 0.40, 0.25),
            BotArchetype.Crafter => (0.20, 0.70, 0.05, 0.05),
            _                    => (0.30, 0.35, 0.15, 0.20) // trader and anything unlisted
        };

        // Mahaon: Ambition was declared per-bot (personality variance) but never actually
        // read anywhere — a more ambitious bot now leans harder into real activity instead
        // of aimless wandering, shifting up to 60% of its wander share over to gathering/
        // hunting/dungeoning in proportion to how ambitious it rolled at creation. Weights
        // still sum to 1.0 either way.
        var wanderCut = wWander * profile.Ambition * 0.6;
        wWander -= wanderCut;
        var ambitionShare = wanderCut / 3.0;
        wGather += ambitionShare;
        wHunt += ambitionShare;
        wDungeon += ambitionShare;

        // Mahaon: idle is one activity choice among the others, not a break other flows
        // fall back into — a small, fixed slice of the roulette, carved out of the other
        // four proportionally so they still sum to (1 - IdleChance) and the whole roll
        // stays exactly 1.0. Real idling should now only ever come from here (or the
        // deliberate night-sleep flavor above) — fatigue/stuck/death recovery all still
        // land in BotActivity.Idle too, but the very next decision cycle rolls again here,
        // same as any other bot that just finished a task.
        const double IdleChance = 0.004;
        wWander *= 1.0 - IdleChance;
        wGather *= 1.0 - IdleChance;
        wHunt *= 1.0 - IdleChance;
        wDungeon *= 1.0 - IdleChance;

        var roll = Utility.RandomDouble();

        if (roll < IdleChance)
        {
            profile.Activity = BotActivity.Idle;
        }
        else if (roll < IdleChance + wWander)
        {
            profile.Activity = BotActivity.Wandering;
        }
        else if (roll < IdleChance + wWander + wGather)
        {
            profile.Activity = BotActivity.TravelingToGather;
        }
        else if (roll < IdleChance + wWander + wGather + wHunt && canHunt)
        {
            profile.Activity = BotActivity.TravelingToHunt;
        }
        else
        {
            profile.Activity = BotActivity.FormingParty; // the "dungeon" sector
        }

        if (profile.Activity == BotActivity.TravelingToGather)
        {
            // Real harvest-attempt target now (50-70), not a tiny 3-8 decision-cycle cap —
            // Mahaon resources are weightless, so a backpack-fullness check would never
            // trigger and a bot would gather forever; see BotProfile.HarvestTarget.
            profile.GatherSkill = Utility.RandomList(SkillName.Mining, SkillName.Lumberjacking, SkillName.Fishing);
            profile.HarvestTarget = Utility.RandomMinMax(50, 70);
            profile.HarvestCount = 0;
        profile.GatherStartItemCount = -1;
        profile.GatherLastItemCount = -1;
        profile.GatherNoYieldStreak = 0;
            profile.GatherDestination = PickGatherSpot(bot, profile);
        }
        else if (profile.Activity == BotActivity.TravelingToHunt)
        {
            // Same idea as gathering — a real kill-count target (50-70), not a tiny
            // 4-10 decision-cycle cap.
            profile.HarvestTarget = Utility.RandomMinMax(50, 70);
            profile.HarvestCount = 0;
        profile.GatherStartItemCount = -1;
        profile.GatherLastItemCount = -1;
        profile.GatherNoYieldStreak = 0;
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

    private static void DoWandering(PlayerMobile bot, BotProfile profile)
    {
        Patrol(bot, profile, 6);

        if (Utility.RandomDouble() < BotTuning.StopWanderingChance)
        {
            profile.Activity = BotActivity.Idle;
        }
    }

    // -- Thief bots: opportunistic stealing --------------------------------------------
    //
    // Simplified — no real snooping/stealth minigame, just a flat skill-based success
    // roll against whoever's nearby. Flagged as a proper criminal act either way. The
    // player already knows this is rough around the edges and wants to nerf it later.
}
