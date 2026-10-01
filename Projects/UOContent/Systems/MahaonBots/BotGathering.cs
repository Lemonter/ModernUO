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

/// <summary>Resource gathering — finding/picking gather spots, harvest tool handling, node depletion tracking, travel to/from gathering.</summary>
public partial class BotController
{



    // -- Gathering trip: real travel out, real travel back -------------------------------
    //
    // Picks a spot some real distance away, walks there over several ticks, digs/chops for
    // a while, then walks all the way back to town before handing off to crafting (which
    // reads as "brings the ore back and smelts it"). No more teleport-style instant resource
    // generation in place — the whole point is that this takes visible time and movement.

    // The engine's only real pathfinding algorithm (BitmapAStarAlgorithm.AreaSize = 38)
    // refuses to even attempt a route once start and goal are more than 38 tiles apart —
    // beyond that, PathFollower.Follow silently falls back to a blind straight-line walk
    // with zero obstacle avoidance (see PathFollower.cs). GatherMaxDistance used to be 300,
    // so any gather spot beyond ~38 tiles (routine, since cities clear out nearby resource
    // terrain) sent the bot into that fallback — the very first wall/fence it hit, it just
    // kept re-walking into forever, reading as "walks ~10 steps then stops". Kept safely
    // under 38 with margin so a real A* route is always attempted.
    private const int GatherMinDistance = 15;
    private const int GatherMaxDistance = 30;

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

                // Место из общей памяти тоже может оказаться недостижимым — жилу мог
                // найти бот, стоявший по ту сторону реки.
                if (!IsDepleted(spot, bot.Map) && !BotWorldKnowledge.IsKnownUnreachable(bot.Map, spot))
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
                            // По статике карты, а не по предметам. Третье место с той же
                            // ошибкой: лес нарисован в карте, предметов класса Static там
                            // нет, и поиск места для рубки не находил ни одной рощи —
                            // бот уходил «рубить» туда, где деревьев отродясь не было.
                            matches = false;

                            if (x >= 0 && y >= 0 && x < map.Width && y < map.Height)
                            {
                                foreach (var tile in map.Tiles.GetStaticTiles(x, y))
                                {
                                    if (Array.IndexOf(def.StaticTiles, tile.ID) >= 0)
                                    {
                                        matches = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else if (skill == SkillName.Mining)
                        {
                            var tileId = map.Tiles.GetLandTile(x, y).ID & TileData.MaxLandValue;
                            matches = (TileData.LandTable[tileId].Flags & TileFlag.Impassable) != 0;
                        }
                        else
                        {
                            // Рыбалка. Через Validate — списки тайлов у неё заданы
                            // диапазонами, и прямое сравнение находило бы только четыре
                            // граничных значения из всей воды мира. Плюс статика: пруды и
                            // фонтаны нарисованы ею, а не землёй.
                            var tileId = map.Tiles.GetLandTile(x, y).ID & TileData.MaxLandValue;
                            matches = def.Validate(tileId, true);

                            if (!matches && x >= 0 && y >= 0 && x < map.Width && y < map.Height)
                            {
                                foreach (var tile in map.Tiles.GetStaticTiles(x, y))
                                {
                                    var sf = TileData.ItemTable[tile.ID & TileData.MaxItemValue].Flags;

                                    if ((sf & TileFlag.Wet) != 0)
                                    {
                                        matches = true;
                                        break;
                                    }
                                }
                            }
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

        // Через ту же статику карты, что и TryFindTree: предметами лес не нарисован, и
        // прежняя проверка по GetItemsInRange<Static> не признавала хорошим ни одно
        // настоящее лесное место.
        return TryFindTree(map, loc, def, out _);
    }

    /// <summary>
    ///     Есть ли рядом вода, в которой реально получится ловить.
    ///
    ///     Кроме земли смотрим и статику: пруд или фонтан водой из тайлов земли не
    ///     нарисован, и без этой проверки бот никогда не запомнил бы такое место как
    ///     рыбное — хотя ловится там прекрасно.
    /// </summary>
    private static bool IsNearWater(Map map, Point3D loc)
    {
        if (TryFindStaticWater(map, loc, out _))
        {
            return true;
        }

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

    /// <summary>Сколько заходов подряд добыча может ничего не приносить, прежде чем бот
    /// признает место бесплодным и уйдёт. Заход — это медленный тик решений, три-восемь
    /// секунд, так что восемь заходов это примерно полминуты впустую.</summary>
    private const int GatherNoYieldGiveUp = 8;

    private static void DoGathering(PlayerMobile bot, BotProfile profile)
    {
        TryGainStat(bot, StatType.Str);

        if (profile.HarvestCount >= profile.HarvestTarget)
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

        if (!bot.CanBeginAction(GetSwingLockType(profile.GatherSkill)))
        {
            return; // mid swing-chain from a previous real harvest attempt — let it finish
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

        // Замахи идут своим чередом (цепочка асинхронная), поэтому результат прошлого
        // захода виден только сейчас. Рюкзак не вырос несколько заходов подряд — на этом
        // месте боту ловить нечего, и стоять тут ещё несколько минут незачем.
        var carried = bot.Backpack?.TotalItems ?? 0;

        if (profile.GatherLastItemCount >= 0 && carried <= profile.GatherLastItemCount)
        {
            profile.GatherNoYieldStreak++;

            if (profile.GatherNoYieldStreak >= GatherNoYieldGiveUp)
            {
                profile.GatherNoYieldStreak = 0;
                profile.GatherLastItemCount = -1;
                profile.Activity = BotActivity.ReturningFromGather;
                return;
            }
        }
        else
        {
            profile.GatherNoYieldStreak = 0;
        }

        // Поход меряется добытым, а не числом замахов: HarvestCount++ стоял сразу после
        // запуска цепочки, то есть до всякого результата, и бот с бесплодными замахами
        // «выполнял» весь поход, ничего не принеся. Первый заход задаёт точку отсчёта.
        if (profile.GatherStartItemCount < 0)
        {
            profile.GatherStartItemCount = carried;
        }

        profile.HarvestCount = System.Math.Max(0, carried - profile.GatherStartItemCount);
        profile.GatherLastItemCount = carried;

        // Mahaon: this used to call system.StartHarvesting(bot, tool, toHarvest) — the
        // VANILLA HarvestSystem — which gave bots vanilla ore/logs/fish and skipped every
        // Mahaon system entirely (MahaonOre/metal tiers, GatheringSpecializationSystem,
        // node reserve/respawn, mine-entrance offers). A real player never goes through
        // HarvestSystem.StartHarvesting at all — HarvestTarget.OnTarget (see
        // Engines/Harvest/Core/HarvestTarget.cs) routes every real click into
        // MahaonMiningSwings/MahaonLumberjackingSwings/MahaonFishingSwings instead. Bots
        // now go through the exact same real swing systems, same as a player clicking the
        // tile/tree/water themselves.
        StartRealSwingChain(bot, profile.GatherSkill, toHarvest, tool);
        RegisterNodeUse(profile.GatherDestination, bot.Map);
    }

    private static System.Type GetSwingLockType(SkillName skill) => skill switch
    {
        SkillName.Mining        => typeof(Systems.MahaonMining.MahaonMiningSwings),
        SkillName.Lumberjacking => typeof(Systems.MahaonMining.MahaonLumberjackingSwings),
        SkillName.Fishing       => typeof(Systems.MahaonMining.MahaonFishingSwings),
        _                       => typeof(BotController)
    };

    /// <summary>Mirrors HarvestTarget.OnTarget's own routing for the target shapes
    /// TryFindHarvestTarget actually produces — a LandTarget for Mining/Fishing, a real
    /// tree Static for Lumberjacking. One call kicks off a whole self-repeating swing
    /// chain (see the Mahaon*Swings classes) that keeps going on its own until the node's
    /// real reserve runs dry or something interrupts the bot — this dispatch doesn't need
    /// to be called again until then.</summary>
    private static void StartRealSwingChain(PlayerMobile bot, SkillName skill, object toHarvest, Item tool)
    {
        switch (skill)
        {
            case SkillName.Mining when toHarvest is LandTarget landTarget:
                Systems.MahaonMining.MahaonMiningSwings.StartSurfaceSwings(
                    bot, new Point3D(landTarget.X, landTarget.Y, landTarget.Z), bot.Map, tool
                );
                break;

            // Targeting.StaticTarget, а НЕ Items.Static. Здесь и стоял бот между сосен:
            // TryFindTree давно переписан на статику карты и возвращает StaticTarget, а
            // ветка осталась сверять с Items.Static — предметом, поставленным в мир. Типы
            // не родственные, сопоставление молча не срабатывало ни разу, и вся эта
            // функция для рубки была пустой: цель найдена, замахи не начаты, ошибки нет.
            // Со стороны — подошёл и стоит.
            case SkillName.Lumberjacking when toHarvest is Targeting.StaticTarget treeStatic:
                Systems.MahaonMining.MahaonLumberjackingSwings.StartTreeSwings(
                    bot,
                    new Point3D(treeStatic.X, treeStatic.Y, treeStatic.Z),
                    treeStatic.ItemID,
                    bot.Map,
                    tool
                );
                break;

            case SkillName.Fishing when toHarvest is LandTarget fishTarget:
                Systems.MahaonMining.MahaonFishingSwings.StartFishing(
                    bot, new Point3D(fishTarget.X, fishTarget.Y, fishTarget.Z), bot.Map, tool
                );
                break;

            // Та же поломка, второе место: TryFindStaticWater тоже отдаёт StaticTarget, а
            // ветка рыбалки принимала только LandTarget. Пруды, фонтаны и вырытые лопатой
            // водоёмы бот находил и так же молча не облавливал.
            case SkillName.Fishing when toHarvest is Targeting.StaticTarget waterStatic:
                Systems.MahaonMining.MahaonFishingSwings.StartFishing(
                    bot, new Point3D(waterStatic.X, waterStatic.Y, waterStatic.Z), bot.Map, tool
                );
                break;

            // Чтобы следующая такая рассинхронизация не пряталась годами: цель нашли, а
            // замахнуться нечем — это всегда ошибка кода, а не игровая ситуация.
            default:
                Debug(
                    bot,
                    $"замахи не начаты: {skill} не знает, что делать с " +
                    $"{toHarvest?.GetType().Name ?? "null"}"
                );
                break;
        }
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
    /// <summary>
    ///     Ищет дерево в статике КАРТЫ, а не среди предметов.
    ///
    ///     Здесь была причина, по которой боты подходили к дереву и не рубили. Поиск шёл
    ///     через GetItemsInRange&lt;Static&gt;, то есть по предметам класса Static,
    ///     поставленным в мир. Лес же нарисован в самой карте, предметов там нет вовсе — и
    ///     бот, стоя вплотную к сосне, честно не находил ни одного дерева. Срубить он мог
    ///     только то, что кто-то поставил декоратором.
    ///
    ///     Возвращаем StaticTarget: система добычи умеет его разбирать (см.
    ///     HarvestSystem.GetHarvestDetails), и это ровно то, что прислал бы клиент, если бы
    ///     по дереву кликнул живой игрок.
    /// </summary>
    private static bool TryFindTree(Map map, Point3D from, Engines.Harvest.HarvestDefinition def, out object toHarvest)
    {
        toHarvest = null;

        Targeting.StaticTarget best = null;
        var bestDistance = double.MaxValue;

        for (var dx = -HarvestSearchRadius; dx <= HarvestSearchRadius; dx++)
        {
            for (var dy = -HarvestSearchRadius; dy <= HarvestSearchRadius; dy++)
            {
                var x = from.X + dx;
                var y = from.Y + dy;

                if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                {
                    continue;
                }

                foreach (var tile in map.Tiles.GetStaticTiles(x, y))
                {
                    if (Array.IndexOf(def.StaticTiles, tile.ID) < 0)
                    {
                        continue;
                    }

                    // Ближайшее, а не первое попавшееся: дальний ствол в углу области
                    // поиска заставлял бы бота уходить от того, к которому он только что
                    // пришёл.
                    var distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance >= bestDistance)
                    {
                        continue;
                    }

                    // Цель собираем сразу такой, какой она уйдёт в замахи: StaticTarget
                    // правит Z на высоту тайла, и ключ учёта дерева считается уже по
                    // исправленной точке. Сверять пень по сырому Z — значит не сверять
                    // вовсе.
                    var candidate = new Targeting.StaticTarget(new Point3D(x, y, tile.Z), tile.ID);

                    // Пень стоит в карте как ни в чём не бывало — свою срубленность он
                    // помнит только в MahaonLumberjackingSwings. Без этой проверки бот,
                    // выбирающий ближайшее дерево, всякий раз возвращался бы к тому,
                    // которое сам же и извёл минуту назад.
                    if (Systems.MahaonMining.MahaonLumberjackingSwings.IsDepleted(map, candidate.Location))
                    {
                        continue;
                    }

                    bestDistance = distance;
                    best = candidate;
                }
            }
        }

        if (best == null)
        {
            return false;
        }

        toHarvest = best;

        return true;
    }

    /// <summary>
    ///     Ищет воду, нарисованную статикой — пруды, фонтаны, вырытые игроками водоёмы.
    ///
    ///     Тот же приём, что и у деревьев, и по той же причине: искать надо там, где
    ///     нарисовано, а не среди предметов.
    ///
    ///     Признак — флаг Wet, а не список тайлов рыбалки. Так решает сам сервер: в
    ///     HarvestTarget по статику проверяется именно Wet, и рыбачить можно в любой воде,
    ///     включая ту, что появилась после нас — вырытые лопатой пруды, например. Сверяйся
    ///     бот со списком из определения, он был бы слепее собственного сервера и обходил
    ///     стороной воду, в которой человек рядом спокойно ловит.
    /// </summary>
    private static bool TryFindStaticWater(Map map, Point3D from, out object toHarvest)
    {
        toHarvest = null;

        Point3D bestLocation = default;
        var bestId = 0;
        var bestDistance = double.MaxValue;

        for (var dx = -HarvestSearchRadius; dx <= HarvestSearchRadius; dx++)
        {
            for (var dy = -HarvestSearchRadius; dy <= HarvestSearchRadius; dy++)
            {
                var x = from.X + dx;
                var y = from.Y + dy;

                if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                {
                    continue;
                }

                foreach (var tile in map.Tiles.GetStaticTiles(x, y))
                {
                    var flags = TileData.ItemTable[tile.ID & TileData.MaxItemValue].Flags;

                    if ((flags & TileFlag.Wet) == 0)
                    {
                        continue;
                    }

                    var distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestLocation = new Point3D(x, y, tile.Z);
                        bestId = tile.ID;
                    }
                }
            }
        }

        if (bestDistance == double.MaxValue)
        {
            return false;
        }

        toHarvest = new Targeting.StaticTarget(bestLocation, bestId);

        return true;
    }

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
            return TryFindTree(map, bot.Location, def, out toHarvest);
        }

        // Рыбалка: сперва статика. Декоративные пруды и фонтаны нарисованы статикой
        // (waterStaticTiles у Fishing — целых четыре диапазона), и без этой проверки бот
        // видел бы только открытую воду из тайлов земли. Ровно та же слепота, что была у
        // рубки: искали не там, где нарисовано.
        if (skill == SkillName.Fishing && TryFindStaticWater(map, bot.Location, out toHarvest))
        {
            return true;
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
                    // Через Validate самого определения, а не IndexOf.
                    //
                    // У рыбалки стоит RangedTiles = true: списки тайлов там — это ПАРЫ
                    // границ, а не перечисление. LandTiles = { 0x00A8, 0x00AB, 0x0136,
                    // 0x0137 } означает два диапазона, 0x00A8..0x00AB и 0x0136..0x0137.
                    // IndexOf же сверял на точное совпадение и признавал водой только
                    // четыре граничных значения, пропуская 0x00A9 и 0x00AA — то есть
                    // половину воды в мире. Validate знает про диапазоны и решает это сам.
                    matches = def.Validate(tileId, true);
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

        if (bot.BankBox == null || !Banker.Withdraw(bot, (int)toolCost))
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
}
