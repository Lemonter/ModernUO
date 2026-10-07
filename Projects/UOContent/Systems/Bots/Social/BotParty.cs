using System;
using System.Collections.Generic;
using Server.Engines.PartySystem;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// Bots and a real player's party. Invited, a bot weighs who asks — how it feels about them, how
/// sociable it is — and answers as a player does, through /accept or /decline. Hired for gold, it
/// joins for the hours paid. In the party it walks with the player, fights what the player fights
/// and heals whoever is hurt, through the same group behaviour bots use among themselves; it leaves
/// when dismissed, when its pay runs out, or when it can't go on.
/// </summary>
public static class BotParty
{
    private static readonly Dictionary<Mobile, BotGroup> _groups = new();

    // Paid service: until when, by whom.
    private static readonly Dictionary<Mobile, (Mobile employer, long until)> _hired = new();

    private static readonly TimeSpan AnswerDelay = TimeSpan.FromSeconds(2);

    public static bool IsBot(Mobile m) => m is BotMobile && BotSystem.IsActive(m);

    /// <summary>The player-led group a bot is in, if any.</summary>
    public static BotGroup GroupOf(Mobile player) => _groups.GetValueOrDefault(player);

    /// <summary>Whether this bot would follow this player of its own will.</summary>
    public static bool Willing(BotMobile bot, Mobile player)
    {
        if (bot.GetBrain() is not { } brain || !bot.Alive || brain.Group != null || bot.IsPk || BotSocialRules.IsOutlaw(bot) ||
            brain.HoldsGrudge(player) || MahaonBots.BotRelationships.IsHostileTo(bot, player))
        {
            return false;
        }

        var odds = 0.2 + BotBrain.Trait(brain.Sociability) * 0.5 + MahaonBots.BotRelationships.GetScore(bot, player) * 0.1;
        return Utility.RandomDouble() < odds;
    }

    /// <summary>Called when a player invites a bot into its party; the bot answers after a moment.</summary>
    public static void OnInvited(Mobile target, Mobile inviter)
    {
        if (target is not BotMobile bot || !IsBot(bot))
        {
            return;
        }

        var accept = IsHired(bot, inviter) || Willing(bot, inviter);
        Timer.StartTimer(AnswerDelay, () => Answer(bot, inviter, accept));
    }

    private static void Answer(BotMobile bot, Mobile inviter, bool accept)
    {
        if (bot.Deleted || bot.Party != inviter)
        {
            return;
        }

        if (accept)
        {
            PartyCommands.Handler?.OnAccept(bot, inviter);
            if (Party.Get(bot) is { } party && party.Contains(inviter))
            {
                Join(bot, inviter);
                BotSpeech.Reply(bot, "С радостью, пойдём вместе!");
            }
        }
        else
        {
            PartyCommands.Handler?.OnDecline(bot, inviter);
            BotSpeech.Reply(bot, "Не сейчас, у меня свои дела.");
        }
    }

    // The bot follows the player as a member of the player's group.
    private static void Join(BotMobile bot, Mobile player)
    {
        if (player is not PlayerMobile pm)
        {
            return;
        }

        if (!_groups.TryGetValue(player, out var group) || group.Disbanded)
        {
            _groups[player] = group = new BotGroup(pm);
        }

        group.TryAdd(bot);
    }

    /// <summary>Called when someone leaves a party or the party breaks up.</summary>
    public static void OnLeft(Mobile m)
    {
        if (_groups.Remove(m, out var led))
        {
            led.Disband();
        }

        if (m is BotMobile bot && bot.GetBrain()?.Group is { LedByPlayer: true } group)
        {
            group.Remove(bot);
            _hired.Remove(bot);
        }
    }

    /// <summary>Takes the bot out of the player's party.</summary>
    public static void Leave(BotMobile bot, string farewell)
    {
        BotSpeech.Reply(bot, farewell);
        if (Party.Get(bot) is { } party)
        {
            party.Remove(bot);
        }

        OnLeft(bot);
    }

    // ---- Hire --------------------------------------------------------------------------------

    /// <summary>What an hour of this bot's service costs: the better it fights, the dearer.</summary>
    public static int HourlyPrice(Mobile bot) => 50 + (int)(BotCombatStyles.FightingSkill(bot) * 5);

    public static bool IsHired(Mobile bot, Mobile employer) =>
        _hired.TryGetValue(bot, out var hire) && hire.employer == employer && Core.TickCount - hire.until < 0;

    /// <summary>
    /// Gold handed to a bot: at least an hour's price hires it for the hours it covers, and it joins
    /// the payer's party. Less than that it hands back.
    /// </summary>
    public static bool OnPaid(BotMobile bot, Mobile payer, Gold gold)
    {
        if (!IsBot(bot) || payer is not PlayerMobile player || bot.GetBrain() is not { } brain)
        {
            return false;
        }

        var price = HourlyPrice(bot);
        if (gold.Amount < price || bot.IsPk || brain.HoldsGrudge(payer) || MahaonBots.BotRelationships.IsHostileTo(bot, payer))
        {
            BotSpeech.Reply(bot, $"За такие деньги я не служу. Час — {price} золота.");
            return false;
        }

        var hours = gold.Amount / price;
        var start = IsHired(bot, payer) ? _hired[bot].until : Core.TickCount;
        _hired[bot] = (payer, start + hours * 60 * 60_000L);
        bot.Backpack?.DropItem(gold);

        if (Party.Get(bot) is not { } current || !current.Contains(payer))
        {
            brain.Group?.Remove(bot);
            var party = Party.Get(payer);
            if (party == null)
            {
                payer.Party = party = new Party(payer);
            }

            if (party.Members.Count + party.Candidates.Count < Party.Capacity)
            {
                party.OnAccept(bot, true);
                Join(bot, payer);
            }
        }

        BotSpeech.Reply(bot, $"Договорились: служу тебе {hours} ч.");
        return true;
    }

    /// <summary>
    /// A bot in a player's party checks it still wants to be there: a hired one until its time is
    /// up, a volunteer while it isn't worn out.
    /// </summary>
    public static bool StillServes(BotMobile bot, BotGroup group)
    {
        var brain = bot.GetBrain();
        if (_hired.TryGetValue(bot, out var hire))
        {
            if (Core.TickCount - hire.until < 0)
            {
                return true;
            }

            _hired.Remove(bot);
            if (!MahaonBots.BotRelationships.IsFriendlyTo(bot, group.Leader))
            {
                Leave(bot, "Время вышло — плати ещё, или я пошёл.");
                return false;
            }
        }

        if (brain.Fatigue > 0.95 || bot.Hits < bot.HitsMax / 5)
        {
            Leave(bot, "Всё, я выдохся. Пойду отдохну.");
            return false;
        }

        return true;
    }

    // ---- Guild -------------------------------------------------------------------------------

    /// <summary>
    /// A player's guild invites a bot. A bot that thinks well of the player, or is sociable and owes
    /// its own guild little, leaves it and joins; a guild leader with members doesn't abandon them.
    /// </summary>
    public static void OnGuildInvite(BotMobile bot, Guild guild, Mobile inviter)
    {
        if (bot.GetBrain() is not { } brain || bot.IsPk || brain.HoldsGrudge(inviter) ||
            MahaonBots.BotRelationships.IsHostileTo(bot, inviter) ||
            bot.Guild is Guild own && own.Leader == bot && own.Members.Count > 1)
        {
            inviter.SendMessage($"{bot.Name} отказывается вступать в {guild.Name}.");
            BotSpeech.Reply(bot, "Нет уж, у меня своя гильдия.");
            return;
        }

        var odds = 0.15 + BotBrain.Trait(brain.Sociability) * 0.3 + MahaonBots.BotRelationships.GetScore(bot, inviter) * 0.1;
        if (Utility.RandomDouble() >= odds)
        {
            inviter.SendMessage($"{bot.Name} отказывается вступать в {guild.Name}.");
            BotSpeech.Reply(bot, "Спасибо, но я пока останусь, где есть.");
            return;
        }

        (bot.Guild as Guild)?.RemoveMember(bot);
        guild.AddMember(bot);
        bot.DisplayGuildTitle = true;
        inviter.SendMessage(0x59, $"{bot.Name} вступает в {guild.Name}.");
        BotSpeech.Reply(bot, $"Почту за честь, {inviter.Name}!");
    }
}
