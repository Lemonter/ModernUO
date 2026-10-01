using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonBots;

/// <summary>
///     A guild "bank" for named bot guilds — same "keyed by name" pattern as
///     BotGuilds/GuildSpecialization. Backed by MahaonGuildBankContainer, a real persisted
///     Item that remembers its own guild name — so unlike the first version of this class
///     (a bare, unnamed Bag that got recreated from scratch, contents and all, on every
///     server restart), a restart just means the in-memory Banks cache below is empty and
///     needs rebuilding by scanning World.Items for containers that already know which
///     guild they belong to, not that the money/gems themselves are gone.
/// </summary>
public static class GuildBank
{
    private static readonly Dictionary<string, MahaonGuildBankContainer> Banks = new();
    private static bool _scanned;

    private static void EnsureScanned()
    {
        if (_scanned)
        {
            return;
        }

        _scanned = true;

        foreach (var item in World.Items.Values)
        {
            if (item is MahaonGuildBankContainer bank
                && !bank.Deleted
                && !string.IsNullOrEmpty(bank.OwnerGuildName)
                && !Banks.ContainsKey(bank.OwnerGuildName))
            {
                Banks[bank.OwnerGuildName] = bank;
            }
        }
    }

    public static MahaonGuildBankContainer GetOrCreate(string guildName)
    {
        if (string.IsNullOrEmpty(guildName))
        {
            return null;
        }

        EnsureScanned();

        if (Banks.TryGetValue(guildName, out var bank) && !bank.Deleted)
        {
            return bank;
        }

        bank = new MahaonGuildBankContainer { OwnerGuildName = guildName };
        // Deliberately never placed on a map (no MoveToWorld) — it just needs to be a
        // real, persisted Item for DropItem/FindItemByType to work against and for the
        // world save to actually keep it (still gets a Serial and gets saved normally;
        // items don't need to be on a map to persist, same as anything sitting in a bank
        // box that's currently closed).
        Banks[guildName] = bank;
        return bank;
    }

    /// <summary>
    ///     Всё золото в банке гильдии, а не первая попавшаяся стопка.
    ///
    ///     Раньше здесь стоял FindItemByType&lt;Gold&gt;(true)?.Amount — он возвращает ОДИН
    ///     предмет. Стопки золота обычно сливаются при вкладе, но не обязаны: как только в
    ///     банке оказывалось две стопки, гильдия видела баланс меньше настоящего и не могла
    ///     потратить остальное.
    /// </summary>
    public static long GetGoldValue(string guildName)
    {
        if (string.IsNullOrEmpty(guildName))
        {
            return 0;
        }

        var total = 0L;

        foreach (var item in GetOrCreate(guildName).FindItemsByType<Gold>(true))
        {
            total += item.Amount;
        }

        return total;
    }

    public static int GetGemCount(string guildName)
    {
        if (string.IsNullOrEmpty(guildName))
        {
            return 0;
        }

        var bank = GetOrCreate(guildName);
        var count = 0;

        foreach (var item in bank.Items)
        {
            if (Systems.MahaonGems.GemSocketingSystem.BonusTypeFor(item.GetType()) != null)
            {
                count += item.Amount;
            }
        }

        return count;
    }

    /// <summary>Removes exactly <paramref name="amount"/> gems total from the guild's bank,
    /// any mix of gem types, cheapest/most-common stacks first (so a guild doesn't lose its
    /// one precious Diamond to pay for something a pile of Amber could have covered)./// Returns false and spends nothing if the bank doesn't hold enough gems overall.</summary>
    public static bool TrySpendGems(string guildName, int amount)
    {
        if (string.IsNullOrEmpty(guildName) || amount <= 0)
        {
            return false;
        }

        var bank = GetOrCreate(guildName);

        if (GetGemCount(guildName) < amount)
        {
            return false;
        }

        // Snapshot first — deleting/reducing items while iterating bank.Items directly
        // would mutate the collection mid-foreach.
        var gemStacks = new List<Item>();

        foreach (var item in bank.Items)
        {
            if (Systems.MahaonGems.GemSocketingSystem.BonusTypeFor(item.GetType()) != null)
            {
                gemStacks.Add(item);
            }
        }

        // Cheapest-first: rarer gems (Diamond, StarSapphire, ...) have lower drop weight in
        // BotPets.GuildDeathGemTable, so smaller on-hand stacks are treated as rarer/more
        // valuable and spent last.
        gemStacks.Sort((a, b) => b.Amount.CompareTo(a.Amount));

        var remaining = amount;

        foreach (var stack in gemStacks)
        {
            if (remaining <= 0)
            {
                break;
            }

            var take = System.Math.Min(remaining, stack.Amount);
            remaining -= take;

            if (take >= stack.Amount)
            {
                stack.Delete();
            }
            else
            {
                stack.Amount -= take;
            }
        }

        return true;
    }
}
