using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Network;

namespace Server.Systems.MahaonBots;

/// <summary>
///     A bot who witnesses something notable (rare drop, another bot's death) can share
///     it — the next OTHER bot that comes within earshot on their own decision cycle has
///     a chance to "hear" it and repeat it themselves, spreading organically instead of a
///     broadcast every bot instantly knows. Rumors expire — old news stops circulating.
/// </summary>
public static class BotRumors
{
    private const int HearRange = 6;
    private static readonly TimeSpan RumorLifetime = TimeSpan.FromMinutes(10);
    private const double HearChance = 0.15; // per nearby bot, per decision cycle

    private class Rumor
    {
        public string Text;
        public DateTime ExpiresAt;
    }

    private static readonly List<Rumor> Active = new();
    private const int MaxActive = 40; // don't let this grow unbounded during a busy session

    public static void Spread(string text)
    {
        if (Active.Count >= MaxActive)
        {
            Active.RemoveAt(0);
        }

        Active.Add(new Rumor { Text = text, ExpiresAt = Core.Now + RumorLifetime });
    }

    public static void NotableDrop(Mobile bot, string itemName)
    {
        var text = $"Говорят, {bot.Name} нашёл {itemName}!";
        Spread(text);
        Server.Systems.MahaonAi.MahaonForumBridge.OnRumor(bot, text);
    }

    /// <summary>Доля смертей, о которых вообще пишут на форум. Между ботами смерть —
    /// событие рядовое, и каждая в хронике не нужна; по шарду она расходится молвой
    /// (Spread ниже) в любом случае.</summary>
    private const double DeathWorthPostingChance = 0.05;

    public static void NotableDeath(Mobile bot, string killerDescription)
    {
        var text = $"Слыхали, {bot.Name} погиб от {killerDescription}...";
        Spread(text);

        if (Utility.RandomDouble() < DeathWorthPostingChance)
        {
            Server.Systems.MahaonAi.MahaonForumBridge.OnRumor(bot, text);
        }
    }

    /// <summary>A rumour still going round, for a bot to pass on in conversation
    /// (BotSpeech.Chat).</summary>
    public static bool TryPick(out string text)
    {
        Active.RemoveAll(r => r.ExpiresAt <= Core.Now);

        if (Active.Count == 0)
        {
            text = null;
            return false;
        }

        text = Active[Utility.Random(Active.Count)].Text;
        return true;
    }

    /// <summary>Called for town vendors (VendorRumorTeller): with someone standing close by,
    /// the vendor now and then repeats a rumour out loud.</summary>
    public static void TryHear(Mobile bot)
    {
        if (Active.Count == 0 || bot.Map == null || Utility.RandomDouble() > HearChance)
        {
            return;
        }

        Active.RemoveAll(r => r.ExpiresAt <= Core.Now);

        if (Active.Count == 0)
        {
            return;
        }

        var hasNeighbor = false;

        foreach (var nearby in bot.Map.GetMobilesInRange<Mobile>(bot.Location, HearRange))
        {
            if (nearby != bot && nearby is PlayerMobile)
            {
                hasNeighbor = true;
                break;
            }
        }

        if (!hasNeighbor)
        {
            return;
        }

        var rumor = Active[Utility.Random(Active.Count)];
        bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, rumor.Text);
    }
}
