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

/// <summary>Core combat behavior — kiting, engagement decisions, combat tactics, fleeing, PK handling, weapon poison/special-move usage.</summary>
public partial class BotController
{

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
    /// <summary>
    ///     Mahaon: раньше погоня шла через StepToward — слепой шаг по прямой без обхода
    ///     препятствий. Из-за этого бот, у которого жертва за стеной, на другом берегу или
    ///     на недоступном островке, упирался в препятствие и молотил в него бесконечно:
    ///     аварийный телепорт домой его не спасал, потому что тот работает только для
    ///     путешествий и намеренно пропускает бой (см. PollMovement).
    ///
    ///     Теперь погоня идёт настоящим маршрутом, тем же, что и путешествия, и если
    ///     дороги к жертве нет — бот её просто бросает и ищет другую, вместо того чтобы
    ///     зависнуть.
    /// </summary>
    private static void TryPursueCombatant(PlayerMobile bot, BotProfile profile, int desiredRange)
    {
        var target = bot.Combatant;

        if (target == null || bot.Map != target.Map)
        {
            return;
        }

        if (bot.InRange(target.Location, desiredRange))
        {
            profile.ResetPathing(); // добежали, маршрут больше не нужен
            return;
        }

        StepTowardPath(bot, profile, target.Location);

        if (!profile.GoalUnreachable)
        {
            return;
        }

        profile.ResetPathing();
        bot.Combatant = null;
        bot.Warmode = false;
        DebugSayUnreachable(bot, target);
    }

    /// <summary>Видно только ГМу с включённой отладкой — обычным игрокам бот молча
    /// разворачивается и уходит.</summary>
    private static void DebugSayUnreachable(Mobile bot, Mobile target)
    {
        if (bot is BaseCreature { Debug: true } or PlayerMobile { AccessLevel: >= AccessLevel.GameMaster })
        {
            bot.PublicOverheadMessage(
                MessageType.Regular, 0x3B2, false, $"До {target.Name} не добраться, ищу другую цель."
            );
        }
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

            if (!BotGuilds.IsAtWar(bot, m) || IsSpawnProtected(m) || Items.MahaonBotBeacon.IsNearAnyBeacon(m))
            {
                continue;
            }

            // Никакого CriminalAction: война — это война, по её врагам бьют безнаказанно.
            // Раньше бот сам себя тут серил, и городская стража (которая теперь берёт и
            // серых) шла разнимать обе стороны законной войны. Движок и так не считает
            // этот удар преступлением — Notoriety.MobileNotoriety отдаёт по врагу войны
            // Notoriety.Enemy, а криминал вешается только за удар по Innocent.
            ApplyCombatTactics(bot, m);
            bot.Combatant = m;
            bot.Warmode = true;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Годится ли существо в добычу.
    ///
    ///     Раньше такой проверки не было вовсе: цель искалась перебором всех
    ///     BaseCreature вокруг, и «не питомец, не рейдовый моб, живой» было единственным
    ///     условием. Под него подходит и городская стража, и лавочник, и караванный
    ///     мул — отсюда и боты, бьющие защитников города.
    ///
    ///     Основной признак — нотоориентация: синий (Innocent), союзник и неуязвимый в
    ///     добычу не годятся, а это ровно вся мирная городская публика. Стража и торговцы
    ///     проверяются отдельно и сверх того: стражу, на которую кто-то уже напал, синей
    ///     не назовёшь, но нашим ботам она всё равно не противник.
    /// </summary>
    public static bool IsHuntable(Mobile bot, BaseCreature creature)
    {
        if (creature == null || creature.Deleted || !creature.Alive || creature.Controlled ||
            creature is IRaidSpawn || creature.Blessed || creature.IsInvulnerable)
        {
            return false;
        }

        if (creature is IMahaonTownDefender or BaseVendor or BaseEscortable)
        {
            return false;
        }

        return Notoriety.Compute(bot, creature) is not (Notoriety.Innocent or Notoriety.Ally
            or Notoriety.Invulnerable);
    }

    private static bool TryEngageWhileTraveling(PlayerMobile bot)
    {
        if (IsFighting(bot))
        {
            return true; // already fighting — hold position and let it resolve
        }

        if (bot.Map == null)
        {
            return false;
        }

        var lootRange = GetArchetype(bot) == BotArchetype.Archer ? ArcherIdealRange + 2 : CorpseSearchRangeWhileTraveling;

        if (TryLootCorpse(bot, lootRange))
        {
            return true; // grabbing what they just killed before wandering off
        }

        foreach (var creature in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, TravelEngageRange))
        {
            if (IsHuntable(bot, creature) && ShouldEngage(bot, creature))
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
                allyCount += BotRelationships.IsFriendlyTo(bot, otherBot) ? 2 : 1;
            }
        }

        // Crafters and Bards aren't fighters — they need overwhelming odds before they'll
        // even consider it, not just "roughly even". Everyone else keeps the normal rule.
        var isNonCombat = GetArchetype(bot) == BotArchetype.Crafter ||
                          IsProfessionCategory(bot, ProfessionCategory.Bard);

        if (isNonCombat)
        {
            return allyCount >= enemyCount * 3;
        }

        // Fine solo or roughly even; skip if seriously outnumbered. Personality shifts
        // how much slack "roughly even" gets — an Aggressive bot will pick a fight it's
        // slightly losing, a Cautious one wants a real edge first.
        var slack = 1;

        if (bot is PlayerMobile engageBotPm && Bots.TryGetValue(engageBotPm, out var engageProfile))
        {
            slack = (int)System.Math.Round(1 * engageProfile.Personality.EngageChanceMultiplier);
        }

        return enemyCount <= allyCount + System.Math.Max(0, slack);
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
            bot.Mana >= new CloseWoundsSpell(bot).GetMana() && Utility.RandomDouble() < BotTuning.ChivalryHealChance)
        {
            new CloseWoundsSpell(bot).Cast();
            return;
        }

        if (bot.Skills[SkillName.Bushido].Value >= 50 && Utility.RandomDouble() < BotTuning.BushidoMoveChance &&
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
        if (bot.Mana >= spell.GetMana())
        {
            spell.Cast();
        }
    }

    // How often ongoing combat re-applies stance/aimed-hit/poison/special moves (see the
    // call site in DispatchJudgment) — same cadence as bard skills, comfortably below the
    // fast in-combat decision tick so it always gets a chance to fire.
    private static readonly TimeSpan TacticsRefreshCooldown = TimeSpan.FromSeconds(5);

    private static void ApplyCombatTactics(PlayerMobile bot, Mobile target)
    {
        var archetype = GetArchetype(bot);
        var preferDefensive = archetype is BotArchetype.Warrior or BotArchetype.Mage;

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
            HitLocationSystem.SetLocation(bot, location);
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
            IsFighting(bot))
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
        if (IsFighting(bot) &&
            bot.Skills[SkillName.Discordance].Value >= 20 && Utility.RandomDouble() < BotTuning.DiscordanceChance)
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

                if (IsFighting(bot))
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

        var threatened = IsFighting(bot);

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
        if (IsFighting(bot))
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
}
