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

    // Rarer gems have proportionally lower weight — Diamond (the same one used for the
    // random-skill socket bonus, the strongest of the set) is by far the least likely.
    //
    // Таблица хранит фабрики, а не типы. Раньше здесь лежали typeof(...) и камень
    // создавался через Activator.CreateInstance(type) — а у всех наших самоцветов
    // конструктор вида «Emerald(int amount = 1)». Параметр со значением по умолчанию
    // конструктором без параметров для рефлексии не считается, поэтому каждая смерть
    // бота в гильдии роняла сервер MissingMethodException прямо посреди OnDeath.
    // Делегат проверяется компилятором и упасть так не может в принципе.
    private static readonly (System.Func<Item> make, int weight)[] GuildDeathGemTable =
    {
        (() => new Amber(), 30),
        (() => new Tourmaline(), 30),
        (() => new Amethyst(), 20),
        (() => new Sapphire(), 15),
        (() => new Emerald(), 15),
        (() => new Ruby(), 10),
        (() => new Citrine(), 8),
        (() => new StarSapphire(), 5),
        (() => new Diamond(), 2)
    };

    // A gold pile holds at most this much; a larger sum is split into several.
    private const int MaxGoldPile = 60000;

    /// <summary>Puts gold into the guild bank, topping up the piles already there.</summary>
    public static void DepositGold(string guildName, long amount)
    {
        if (amount <= 0 || GetOrCreate(guildName) is not { } bank)
        {
            return;
        }

        foreach (var pile in bank.FindItemsByType<Gold>(false))
        {
            var room = MaxGoldPile - pile.Amount;
            if (room <= 0)
            {
                continue;
            }

            var add = (int)System.Math.Min(room, amount);
            pile.Amount += add;
            amount -= add;
            if (amount <= 0)
            {
                return;
            }
        }

        while (amount > 0)
        {
            var add = (int)System.Math.Min(MaxGoldPile, amount);
            bank.DropItem(new Gold(add));
            amount -= add;
        }
    }

    /// <summary>Ingots of one metal in the guild bank.</summary>
    public static int GetIngots(string guildName, MahaonMetals.MahaonMetal metal)
    {
        if (string.IsNullOrEmpty(guildName))
        {
            return 0;
        }

        var total = 0;
        foreach (var ingot in GetOrCreate(guildName).FindItemsByType<MahaonIngot>(false))
        {
            if (ingot.Metal == metal)
            {
                total += ingot.Amount;
            }
        }

        return total;
    }

    /// <summary>What the guild bank takes: gold, ingots, and the bandages, potions and scrolls its city guard uses.</summary>
    public static bool Accepts(Item item) => item is Gold or MahaonIngot or Bandage or BasePotion or SpellScroll;

    /// <summary>Puts a pile into the guild bank, merging it with a like pile already there.</summary>
    public static bool Deposit(string guildName, Item item)
    {
        if (!Accepts(item) || GetOrCreate(guildName) is not { } bank)
        {
            return false;
        }

        if (item is Gold gold)
        {
            var amount = gold.Amount;
            gold.Delete();
            DepositGold(guildName, amount);
            return true;
        }

        foreach (var pile in bank.Items)
        {
            if (pile != item && pile.StackWith(null, item, false))
            {
                return true;
            }
        }

        bank.DropItem(item);
        return true;
    }

    /// <summary>How many of this kind of item the guild bank holds.</summary>
    public static int Count(string guildName, System.Type type) =>
        string.IsNullOrEmpty(guildName) ? 0 : GetOrCreate(guildName).GetAmount(type, false);

    /// <summary>Takes up to this many of a kind of item out of the guild bank, as one pile.</summary>
    public static Item Take(string guildName, System.Type type, int amount)
    {
        if (string.IsNullOrEmpty(guildName) || amount <= 0)
        {
            return null;
        }

        foreach (var pile in GetOrCreate(guildName).Items)
        {
            if (pile.GetType() != type)
            {
                continue;
            }

            if (pile.Amount <= amount)
            {
                pile.Internalize();
                return pile;
            }

            pile.Amount -= amount;
            var part = pile.GetType().CreateEntityInstance<Item>();
            part.Amount = amount;
            return part;
        }

        return null;
    }

    /// <summary>
    /// Takes gold and ingots of one metal from the guild bank together, or nothing at all when
    /// either falls short.
    /// </summary>
    public static bool TrySpend(string guildName, long gold, MahaonMetals.MahaonMetal metal, int ingots)
    {
        if (string.IsNullOrEmpty(guildName) || GetGoldValue(guildName) < gold || GetIngots(guildName, metal) < ingots)
        {
            return false;
        }

        var bank = GetOrCreate(guildName);
        var piles = new List<Item>();
        foreach (var item in bank.Items)
        {
            if (item is Gold || item is MahaonIngot ore && ore.Metal == metal)
            {
                piles.Add(item);
            }
        }

        foreach (var pile in piles)
        {
            var need = pile is Gold ? gold : ingots;
            if (need <= 0)
            {
                continue;
            }

            var take = (int)System.Math.Min(need, pile.Amount);
            pile.Consume(take);

            if (pile is Gold)
            {
                gold -= take;
            }
            else
            {
                ingots -= take;
            }
        }

        return true;
    }

    /// <summary>A guild member's death feeds the guild bank a gem.</summary>
    public static void OnMemberDeath(Mobile bot)
    {
        var guildName = bot.Guild?.Name;
        if (string.IsNullOrEmpty(guildName))
        {
            return;
        }

        var totalWeight = 0;
        foreach (var (_, weight) in GuildDeathGemTable)
        {
            totalWeight += weight;
        }

        var roll = Utility.Random(totalWeight);
        System.Func<Item> chosen = null;

        foreach (var (make, weight) in GuildDeathGemTable)
        {
            if (roll < weight)
            {
                chosen = make;
                break;
            }

            roll -= weight;
        }

        var gem = chosen?.Invoke();

        if (gem == null)
        {
            return;
        }

        var bank = GuildBank.GetOrCreate(guildName);
        bank?.DropItem(gem);
    }
}
