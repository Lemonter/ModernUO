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

/// <summary>Hunting and dungeon-specific activity — travel to hunt/dungeon, solo hunting, dungeon combat and looting.</summary>
public partial class BotController
{
    /// <summary>Насколько далеко бот отходит за один переход, когда просто бродит.</summary>
    private const int DungeonPatrolRadius = 12;

    private const int SoloHuntPatrolRadius = 8;

    private const int HuntPatrolRadius = 10;


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
        if (IsFighting(bot))
        {
            return; // mid-fight, let it play out
        }

        // Same principle as gathering — a real target of loot passes (50-70), not a tiny
        // decision-cycle cap. Checked only here, between fights, so a bot never turns tail
        // mid-swing.
        if (profile.HarvestCount >= profile.HarvestTarget)
        {
            profile.Activity = BotActivity.ReturningFromHunt;
            return;
        }

        if (TryLootCorpse(bot))
        {
            profile.HarvestCount++;
            return;
        }

        if (bot.Map == null)
        {
            return;
        }

        foreach (var creature in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, 10))
        {
            if (IsHuntable(bot, creature))
            {
                ApplyCombatTactics(bot, creature);
                bot.Combatant = creature;
                bot.Warmode = true;
                return;
            }
        }

        // Здесь сейчас пусто — подрейфуем, вместо того чтобы стоять и ждать. С поиском
        // пути: охотничьи места нередко в лесу и у скал, и слепой шаг утыкался в них так
        // же, как в подземелье, только незаметнее.
        Patrol(bot, profile, SoloHuntPatrolRadius);
    }

    private static void DoReturnFromHunt(PlayerMobile bot, BotProfile profile)
    {
        if (StepTowardPath(bot, profile, profile.HomeLocation))
        {
            profile.Activity = BotActivity.Idle;
        }
    }

    private const int HuntRange = 20;
    private const double FleeHealthThreshold = 0.25;

    // Mahaon: how long a bot can stay continuously active (any non-Idle activity, combat
    // exempt) before fatigue forces a rest. Long enough that a normal gathering run,
    // dungeon crawl, or hunting trip never gets interrupted mid-task — this exists to
    // catch a bot that's somehow been stuck cycling through activities for a genuinely
    // unreasonable stretch, not to pace normal play.
    private static readonly TimeSpan FatigueForceRestDuration = TimeSpan.FromHours(6);

    private static void DoHunting(PlayerMobile bot, BotProfile profile)
    {
        if (IsFighting(bot))
        {
            return; // already fighting, let combat play out
        }

        if (bot.Map == null)
        {
            return;
        }

        // Под присмотром стражи бот не завязывает драк, за которые его самого потом
        // будут резать. Раньше здесь стоял безусловный return — то есть у поста нельзя
        // было тронуть вообще никого, включая красного убийцу и врага по гильдейской
        // войне, хотя за них не наказывают. Теперь запрет поштучный: отсеиваются ровно
        // те цели, удар по которым сделает бота преступником (Notoriety.Innocent —
        // см. Mobile.IsHarmfulCriminal), остальные честная добыча даже на глазах стражи.
        var watchedByGuards = IsWatchedByGuards(bot);

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

            if (IsSpawnProtected(m) || Items.MahaonBotBeacon.IsNearAnyBeacon(m))
            {
                continue; // только что поднялся или стоит у маяка — не место для охоты
            }

            if (watchedByGuards && bot.IsHarmfulCriminal(m))
            {
                continue; // стража рядом, а за эту цель прилетит — не при ней
            }

            target = m;
            break;
        }

        if (target != null)
        {
            // Сереем сразу, не дожидаясь первого замаха (ExpireCriminalDelay в Configure),
            // но только если удар и правда преступление. Безусловный вызов вешал криминал
            // и за нападение на красного, и за войну — то есть бот сам звал на себя стражу
            // там, где по правилам ему ничего не грозило.
            if (bot.IsHarmfulCriminal(target))
            {
                bot.CriminalAction(false);
            }
            ApplyCombatTactics(bot, target);
            bot.Combatant = target;
            bot.Warmode = true;
            return;
        }

        if (TryLootCorpse(bot))
        {
            return;
        }

        // Никого рядом и нечего обирать — патрулируем, а не стоим.
        Patrol(bot, profile, HuntPatrolRadius);
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

    /// <summary>
    ///     Walks the bot to this dungeon's floor-transition marker (if a GM placed one for
    ///     the party's current floor) and, once there, treats itself as having moved to the
    ///     next floor — the actual teleport is the engine's own Teleporter.OnMoveOver
    ///     (nudged directly by StepTowardPath itself now if it doesn't fire on its own — see
    ///     that method). Returns false (does nothing) for any dungeon that has no such
    ///     marker, which is every dungeon until a GM explicitly places one — existing
    ///     single-floor dungeons are unaffected.
    /// </summary>
    private static bool TryAdvanceDungeonFloor(PlayerMobile bot, BotProfile profile, BotParty party)
    {
        var transitionMarker = Items.MahaonDungeonMarker.FindByFloor(party.Destination.Name, party.CurrentFloor);
        if (transitionMarker == null)
        {
            return false;
        }

        if (!StepTowardPath(bot, profile, transitionMarker.Location))
        {
            return true; // still walking there this cycle
        }

        party.CurrentFloor++;
        return true;
    }

    private static void DoDungeonCombat(PlayerMobile bot, BotProfile profile)
    {
        // Общий поход сближает. Это второй (после лечения) источник положительных
        // отношений, и без него союзы между гильдиями складывались бы куда медленнее
        // войн: подраться боты успевают гораздо чаще, чем полечить друг друга.
        //
        // По одному спутнику за тик, а не по всем сразу: отряд из четверых иначе набирал
        // бы союзнический порог за считанные минуты просто от факта совместного стояния.
        if (profile.Party is { Members.Count: > 1 } party)
        {
            var companion = party.Members.RandomElement();

            if (companion != bot && companion?.Deleted == false)
            {
                BotRelationships.OnHelped(bot, companion);
            }
        }

        if (GetArchetype(bot, profile) == BotArchetype.Warrior)
        {
            EnforceWarriorAggro(bot, profile);
        }

        if (GetArchetype(bot, profile) == BotArchetype.Mage)
        {
            DoMageAction(bot, profile);
            return;
        }

        if (!IsFighting(bot))
        {
            BaseCreature target = null;

            if (bot.Map != null)
            {
                foreach (var creature in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, 10))
                {
                    if (IsHuntable(bot, creature))
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

            if (profile.Party != null && TryAdvanceDungeonFloor(bot, profile, profile.Party))
            {
                return;
            }

            if (DoDungeonLooting(bot, profile))
            {
                return;
            }

            // Mahaon: genuinely nothing within range — most Felucca dungeons here (Despise,
            // Destard, Covetous, Shame...) turn out to be one big connected cave rather than
            // teleporter-separated floors (checked Distribution/Data/teleporters.json — the
            // only teleporter tied to most of these dungeons' entrances is a one-way
            // "shortcut back to the surface" deep inside, not a "go to the next floor"
            // link), so without this a party would just camp whatever pocket it first
            // stopped in and never see the rest of the dungeon at all. Wandering further in
            // is what actually spreads a party out to clear more of the interior, the same
            // idle-patrol pattern DoHunting/DoSoloHunting already use outside dungeons.
            // Ходим настоящим поиском пути, а не слепым шагом.
            //
            // Раньше здесь стояло StepToward к точке из RandomNearbyPoint — то есть шаг по
            // прямой в направлении цели, без всякого обхода, к точке, проверенной только
            // на «сюда можно встать». Обе половины неверны именно в подземелье: оно
            // сплошь из стен, точка за стеной проходит CanSpawnMobile прекрасно, а слепой
            // шаг в неё упирается в камень и молотит в него до конца похода. Снаружи такое
            // сходит с рук, внутри — нет.
            //
            // Цель держим между тиками: выбери мы новую каждый тик, бот дёргался бы на
            // месте вместо того, чтобы куда-то дойти.
            Patrol(bot, profile, DungeonPatrolRadius);

            // The leave-the-dungeon budget only burns down on a cycle where NOTHING
            // happened at all — no live target nearby, no floor to advance to, no corpse/
            // chest to loot. Used to decrement unconditionally on every not-fighting cycle
            // (including while walking between fights or looting), which — combined with
            // combat polling much faster than idle search — meant a party could burn its
            // whole budget and head home after clearing just one corner of a dungeon, long
            // before it was actually "cleared". Now a party only leaves once the area
            // around it is genuinely exhausted, matching "чистят его, а уже потом обратно".
            if (profile.CyclesRemaining-- <= 0)
            {
                profile.Activity = BotActivity.ReturningHome;
                profile.Party?.Remove(bot);
                profile.Party = null;
            }
        }
    }

    /// <returns>true if there was a corpse or chest worth dealing with this cycle.</returns>
    private static bool DoDungeonLooting(PlayerMobile bot, BotProfile profile)
    {
        if (TryLootCorpse(bot))
        {
            return true;
        }

        return TryLootChest(bot, profile);
    }
}
