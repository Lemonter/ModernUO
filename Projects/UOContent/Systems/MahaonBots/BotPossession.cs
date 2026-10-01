using System;
using Server.Commands;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Systems.MahaonBots;

/// <summary>
///     [BecomeBot — toggles bot AI control on the CALLER'S OWN character, open to any
///     player (not GM-gated). Two uses in one: an AFK mode (character stays productive —
///     gathers, hunts, crafts, trades, travels, fights, casts as a mage/kites as an
///     archer — while you're away, even after the client disconnects, see
///     PlayerMobile.GetLogoutDelay) and a debug tool (watch the real decision loop drive a
///     character from the inside, in real time, instead of only reading logs).
///
///     No args opens BotArchetypeGump (archetype pick) then BotGoalGump (dig/chop/hunt/let
///     the archetype decide/turn off) — see BotPossessionGump.cs. A typed archetype arg
///     still works as a shortcut straight to the goal gump, for anyone who prefers it.
///
///     Registers through the exact same BotController.RegisterBot/Bots/Conveyor/Dispatch
///     path every spawned bot goes through — no separate/parallel AI, so this genuinely
///     exercises the real system, not a simplified stand-in. Full archetype-specific AI
///     (mage spell rotation, archer kiting, necromancy, etc.) now works too —
///     BotController.GetArchetype() reads BotProfile.Archetype (set here) for anything
///     that isn't a real BotMobile, so every archetype gate throughout the AI answers
///     correctly for a possessed real player, not just spawned bots.
///
///     Still safe for a real account: none of the BotMobile-specific death handling
///     (auto-regear, death-robe skip, fast auto-resurrect) applies to a real PlayerMobile
///     — those are gated on `bot is BotMobile` specifically, not on archetype, so a
///     possessed character dies and resurrects by completely normal player rules. Their
///     actual gear is never at risk from the more aggressive bot-only systems.
/// </summary>
public static class BotPossession
{
    public static void Initialize()
    {
        CommandSystem.Register("BecomeBot", AccessLevel.Player, BecomeBot_OnCommand);
    }

    [Usage("BecomeBot [warrior|mage|archer|crafter|trader]")]
    [Description("Открывает гамп ИИ-контроля на себе — AFK-режим и дебаг-инструмент одновременно.")]
    private static void BecomeBot_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile player)
        {
            return;
        }

        // Already possessed — reopen the goal picker (change task, or turn it off from
        // there) instead of instantly toggling off, so a mid-session goal change doesn't
        // need a full off/on round trip through archetype selection again.
        if (BotController.TryGetProfile(player, out _))
        {
            player.SendGump(new BotGoalGump(player));
            return;
        }

        // Typed-archetype shortcut still works for anyone who prefers it — skips straight
        // to the goal gump instead of the archetype one.
        if (e.Length >= 1 && Enum.TryParse(e.GetString(0), true, out BotArchetype archetype))
        {
            Activate(player, archetype);
            player.SendGump(new BotGoalGump(player));
            return;
        }

        player.SendGump(new BotArchetypeGump(player));
    }

    public static void Activate(PlayerMobile player, BotArchetype archetype)
    {
        BotController.RegisterBot(player, player.Location, player.Map);

        if (BotController.TryGetProfile(player, out var profile))
        {
            profile.Archetype = archetype;
        }

        player.SendMessage(
            0x59,
            $"ИИ-контроль включён (архетип: {archetype}) — персонаж будет действовать сам " +
            "(бродить, добывать, охотиться, торговать, путешествовать, драться и применять " +
            "архетипные боевые фишки). Смерть и воскрешение — как у обычного игрока, снаряжение " +
            "не в зоне риска ботовских систем. Продолжит работать, даже если ты отключишься от игры."
        );
    }
}

