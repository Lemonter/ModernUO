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

/// <summary>GM commands — [BotStatus, [ClearBots, [PurgeDeathRobes, [PurgeBotItems.</summary>
public partial class BotController
{
    [Usage("BotPolitics")]
    [Description("Кто с кем воюет и дружит — и какие накопленные отношения за этим стоят.")]
    private static void BotPolitics_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var names = BotGuilds.NamePool;
        var shown = 0;

        for (var i = 0; i < names.Length; i++)
        {
            for (var j = i + 1; j < names.Length; j++)
            {
                var relation = BotGuilds.GetRelation(names[i], names[j]);
                var (sentiment, pairs) = BotRelationships.GetGuildSentiment(names[i], names[j]);

                if (relation == BotGuildRelation.Neutral && pairs == 0)
                {
                    continue; // нейтралитет без истории — рассказывать не о чем
                }

                var word = relation switch
                {
                    BotGuildRelation.War  => "война",
                    BotGuildRelation.Ally => "союз",
                    _                     => "нейтралитет"
                };

                var hue = relation switch
                {
                    BotGuildRelation.War  => 0x22,
                    BotGuildRelation.Ally => 0x59,
                    _                     => 0x3B2
                };

                from.SendMessage(
                    hue,
                    $"{names[i]} — {names[j]}: {word} (счёт {sentiment:F1} по {pairs} парам)"
                );

                shown++;
            }
        }

        if (shown == 0)
        {
            from.SendMessage(0x3B2, "Гильдиям пока нечего делить: между их бойцами ничего не было.");
        }
    }

    [Usage("BotPathDebug")]
    [Description("Включает или выключает разбор маршрутов ботов в консоль.")]
    private static void BotPathDebug_OnCommand(CommandEventArgs e)
    {
        PathDebug = !PathDebug;
        ServerConfiguration.SetSetting("bots.pathDebug", PathDebug);

        e.Mobile.SendMessage(
            PathDebug ? 0x59 : 0x3B2,
            PathDebug
                ? "Разбор маршрутов включён — смотри консоль, строки помечены [BotPath]."
                : "Разбор маршрутов выключен."
        );
    }

    [Usage("BotStatus")]
    [Description("Перепись ботов: сколько их, чем заняты, сколько дерётся и сколько застряло без цели.")]
    private static void BotStatus_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (Bots.Count == 0)
        {
            from.SendMessage(0x22, "Ботов нет ни одного. Появляются они только от статуи-маяка ([AddLem).");
            return;
        }

        var byActivity = new Dictionary<BotActivity, int>();
        var byArchetype = new Dictionary<BotArchetype, int>();
        var fighting = 0;
        var dead = 0;
        var noGoal = 0;
        var noProfession = 0;

        foreach (var (bot, profile) in Bots)
        {
            byActivity[profile.Activity] = byActivity.GetValueOrDefault(profile.Activity) + 1;

            var archetype = GetArchetype(bot, profile);

            if (archetype != null)
            {
                byArchetype[archetype.Value] = byArchetype.GetValueOrDefault(archetype.Value) + 1;
            }

            if (IsFighting(bot))
            {
                fighting++;
            }

            if (!bot.Alive)
            {
                dead++;
            }

            if (profile.GoalUnreachable)
            {
                noGoal++;
            }

            if (ProfessionSystem.GetProfession(bot) == null)
            {
                noProfession++;
            }
        }

        from.SendMessage(0x59, $"Ботов на учёте: {Bots.Count}. Дерутся: {fighting}. Мертвы: {dead}.");

        foreach (var (activity, count) in byActivity)
        {
            from.SendMessage(0x3B2, $"  {RuActivity(activity)}: {count}");
        }

        foreach (var (archetype, count) in byArchetype)
        {
            from.SendMessage(0x480, $"  {archetype}: {count}");
        }

        if (noGoal > 0)
        {
            from.SendMessage(0x22, $"Бросили недостижимую цель и ждут новую: {noGoal}.");
        }

        if (noProfession > 0)
        {
            from.SendMessage(0x22, $"Без профессии (значит, без имени-звания и без перков): {noProfession}.");
        }
    }

    private static string RuActivity(BotActivity activity) => activity switch
    {
        BotActivity.Idle                => "без дела",
        BotActivity.Wandering           => "слоняются",
        BotActivity.TravelingToGather   => "идут добывать",
        BotActivity.Gathering           => "добывают",
        BotActivity.ReturningFromGather => "возвращаются с добычей",
        BotActivity.Crafting            => "мастерят",
        BotActivity.TravelingToMarket   => "идут на рынок",
        BotActivity.Selling             => "торгуют",
        BotActivity.FormingParty        => "собирают отряд",
        BotActivity.TravelingToDungeon  => "идут в подземелье",
        BotActivity.DungeonCombat       => "дерутся в подземелье",
        BotActivity.ReturningHome       => "возвращаются домой",
        BotActivity.Hunting             => "охотятся на игроков",
        BotActivity.CityTraveling       => "переезжают в другой город",
        BotActivity.BankingTrip         => "идут в банк",
        BotActivity.TravelingToHunt     => "идут на охоту",
        BotActivity.SoloHunting         => "охотятся",
        BotActivity.ReturningFromHunt   => "возвращаются с охоты",
        _                               => activity.ToString()
    };


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
        var failures = 0;

        foreach (var bot in bots)
        {
            try
            {
                // Delete() уходит в BotMobile.OnDelete, а тот снимает бота с учёта и
                // убирает его питомца — раньше эта команда удаляла ботов напрямую, мимо
                // UnregisterBot, и звери оставались в мире без хозяев.
                bot.Delete();
            }
            catch (Exception ex)
            {
                failures++;
                e.Mobile.SendMessage(
                    0x22,
                    $"Не удалось удалить {bot.Serial} ({bot.Name ?? "без имени"}): {ex.GetType().Name}: {ex.Message}"
                );
                Console.WriteLine($"[MahaonClearBots] {bot.Serial} ({bot.Name}) delete failed: {ex}");
            }
        }

        // Mahaon: unconditionally clear our own bookkeeping regardless of individual
        // delete failures above — a bot Mobile that resisted Delete() (still exists in
        // the world) shouldn't also keep blocking [SeedBots from starting fresh just
        // because it's still in this dictionary. Any survivors get picked back up by
        // Initialize()'s "re-attach any BotMobile that's not tracked" scan on next
        // restart if they're still around.
        Bots.Clear();
        Conveyor.Clear();

        // Страховка на случай бота, который Delete() пережил: его питомец всё равно
        // окажется ничьим и должен уйти.
        CleanupOrphanedBotPets();

        e.Mobile.SendMessage(
            0x59,
            failures == 0
                ? $"Удалено ботов: {count}."
                : $"Удалено ботов: {count - failures} из {count}, ошибок: {failures} (см. выше)."
        );
    }

    [Usage("PurgeDeathRobes")]
    private static void PurgeDeathRobes_OnCommand(CommandEventArgs e)
    {
        // Mahaon: cleans up the ~2 million DeathRobe items that piled up in bot
        // backpacks before BotMobile.Resurrect() started deleting them immediately (see
        // BotMobile.cs) — only ever created by the OLD behavior from now on, this is
        // purely for the backlog that already exists in the save. Only deletes robes NOT
        // currently worn (Parent is a Container, i.e. sitting loose in someone's pack) —
        // a robe someone is actively wearing right now (Layer == OuterTorso, Parent is a
        // Mobile) is left alone, since that could be a real player who just died.
        var toDelete = new List<DeathRobe>();

        foreach (var item in World.Items.Values)
        {
            if (item is DeathRobe robe && robe.Parent is Item)
            {
                toDelete.Add(robe);
            }
        }

        foreach (var robe in toDelete)
        {
            robe.Delete();
        }

        e.Mobile.SendMessage(0x59, $"Удалено роб смерти (в рюкзаках, не надетых): {toDelete.Count}.");
    }

    [Usage("PurgeBotItems")]
    private static void PurgeBotItems_OnCommand(CommandEventArgs e)
    {
        // Mahaon: broad, not surgical — DeathRobe wasn't the only thing that piled up
        // (armor, weapons, spellbooks, potions... anything a bot ever equipped or picked
        // up, across every death/respawn/gear-swap cycle over a long test session). Given
        // the whole bot system is getting rebuilt on new logic anyway, this just deletes
        // EVERYTHING currently owned by any BotMobile — worn items, backpack contents,
        // bank box contents, recursively — plus any truly orphaned item floating with no
        // parent and no map at all (Map == null && Parent == null), which shouldn't
        // legitimately exist for anything still relevant. Real players' own belongings
        // are untouched — this only ever walks up to a BotMobile root or finds a true
        // orphan, never touches items rooted on a PlayerMobile that isn't a bot.
        var toDelete = new List<Item>();
        var botRoots = new HashSet<Serial>();

        foreach (var mobile in World.Mobiles.Values)
        {
            if (mobile is BotMobile bot)
            {
                botRoots.Add(bot.Serial);
            }
        }

        foreach (var item in World.Items.Values)
        {
            if (item.Deleted)
            {
                continue;
            }

            if (item.Map == null && item.Parent == null)
            {
                toDelete.Add(item); // true orphan — floating, nothing references it
                continue;
            }

            if (item.RootParent is Mobile rootMobile && botRoots.Contains(rootMobile.Serial))
            {
                toDelete.Add(item);
            }
        }

        foreach (var item in toDelete)
        {
            if (!item.Deleted)
            {
                item.Delete();
            }
        }

        e.Mobile.SendMessage(0x59, $"Удалено предметов (принадлежащих ботам + настоящих сирот): {toDelete.Count}.");
    }
}
