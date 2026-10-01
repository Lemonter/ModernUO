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
///     Сбор отряда для похода в подземелье: поиск свободного подземелья, набор ролей,
///     ожидание на месте встречи и выход, когда роли заполнены или вышло время.
///
///     Сам отряд как объект — в BotParty.cs; здесь только поведение вокруг него.
/// </summary>
public partial class BotController
{
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
                profile.FormingPartySince = Core.Now;
                bot.PublicOverheadMessage(
                    MessageType.Regular, 0x3B2, false,
                    $"Присоединяюсь к группе — беру роль «{RoleNameRu(claimedRole)}»."
                );
                TickFormingParty(bot, profile);
                return;
            }
        }

        // Mahaon: capped per the shard owner's ask — at most MaxActiveParties groups
        // running at once server-wide, and never two groups picking the same dungeon.
        // No free slot/dungeon right now just means wander instead of queuing forever.
        var dungeon = PickAvailableDungeon();
        if (dungeon == null)
        {
            profile.Activity = BotActivity.Wandering;
            return;
        }

        // Strong solo bots skip the whole group dance sometimes, same as before.
        var isStrong = bot.Skills[SkillName.Tactics].Value >= 70;
        if (isStrong && Utility.RandomDouble() < BotTuning.SoloDungeonChance)
        {
            var soloParty = new BotParty();
            soloParty.TryClaimSlot(bot, out _);
            soloParty.Destination = dungeon.Value;
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
        party.Destination = dungeon.Value;

        profile.Party = party;
        profile.FormingPartySince = Core.Now;
        ActiveParties.Add(party);

        bot.PublicOverheadMessage(
            MessageType.Regular, 0x3B2, false,
            $"Собираю группу в {party.Destination.Name} — я {RoleNameRu(myRole)}, нужны ещё."
        );
    }

    private const int MaxActiveParties = 3;

    // Mahaon: ActiveParties (BotController.cs) only ever grew — nothing removed a party
    // once its members died, scattered, or came home, so its Count could never actually
    // answer "how many groups are out there right now". Purges anything that's no longer
    // a real going concern before every cap/availability check.
    private static void CleanupActiveParties()
    {
        for (var i = ActiveParties.Count - 1; i >= 0; i--)
        {
            var party = ActiveParties[i];
            if (party.Members.Count == 0 || !party.IsAlive)
            {
                ActiveParties.RemoveAt(i);
            }
        }
    }

    /// <summary>Null means "don't form a party right now" — either MaxActiveParties is
    /// already reached, or (with 12 known dungeons and a 3-party cap, only really possible
    /// if it's ever lowered below 12) every dungeon already has a group in it.</summary>
    private static DungeonTarget? PickAvailableDungeon()
    {
        CleanupActiveParties();

        if (ActiveParties.Count >= MaxActiveParties)
        {
            return null;
        }

        var claimed = new HashSet<string>();
        foreach (var active in ActiveParties)
        {
            claimed.Add(active.Destination.Name);
        }

        var available = new List<DungeonTarget>();
        foreach (var known in DungeonTarget.Known)
        {
            if (!claimed.Contains(known.Name))
            {
                available.Add(known);
            }
        }

        return available.Count > 0 ? available.RandomElement() : null;
    }

    // Mahaon: no compatible role ever showing up nearby used to mean waiting at the
    // rendezvous point forever — TickFormingParty had no give-up condition at all, and
    // the role slots (1 Tank/1 Healer/2 Dps, one archetype each) are narrow enough that
    // this was a real, common dead end, not just a theoretical one. This is a large part
    // of why bots were observed standing around chatting near a beacon indefinitely.
    private static readonly TimeSpan FormingPartyTimeout = TimeSpan.FromSeconds(90);

    private static void TickFormingParty(PlayerMobile bot, BotProfile profile)
    {
        var party = profile.Party;
        if (party == null || !party.IsAlive)
        {
            profile.Activity = BotActivity.Idle;
            profile.Party = null;
            return;
        }

        if (Core.Now - profile.FormingPartySince > FormingPartyTimeout)
        {
            party.Remove(bot);
            profile.Party = null;
            profile.Activity = BotActivity.Wandering;
            bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Никого не дождался — пойду сам.");
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

        // Через поиск пути: место встречи — настоящая цель в городе, где хватает стен,
        // заборов и лавок. Слепой шаг сюда и был одной из причин, по которой отряды
        // подолгу не собирались — бот стоял в трёх тайлах за прилавком и считал, что идёт.
        if (StepTowardPath(bot, profile, party.RendezvousPoint) && bot.Map == party.RendezvousMap)
        {
            // Arrived and waiting — idle chat while the rest of the roster fills in.
            if (Utility.RandomDouble() < BotTuning.IdleGroupChatChance)
            {
                bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, IdleGroupChat.RandomElement());
            }
        }
    }
}
