using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.Bots;

/// <summary>
/// Keeping a bot's bank box from filling up. Unsold auction lots come back into the box, and a box
/// holds 125 items: left alone it fills with returned goods, gold stops going in, and every lot
/// that expires after that is liquidated at a flat price. So on each visit the bot takes its goods
/// back out to sell again, and folds loose gold into cheques.
/// </summary>
public static class BotBank
{
    private const int ChequeWorth = 1_000_000;
    private const int ConsolidateAt = 100;
    private const int MinCheque = 5000;

    private static bool IsCurrency(Item item) => item is Gold or BankCheck;

    /// <summary>Non-currency items waiting in the bank box.</summary>
    public static int WaitingGoods(Mobile bot)
    {
        if (bot.FindBankNoCreate() is not { } box)
        {
            return 0;
        }

        var count = 0;
        foreach (var item in box.Items)
        {
            if (!IsCurrency(item))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Moves goods from the bank box to the pack, as much as the bot can carry. They are
    /// marked as loot again, so the next market round sells them.</summary>
    public static int TakeOut(PlayerMobile bot)
    {
        if (bot.FindBankNoCreate() is not { } box || bot.Backpack is not { } pack)
        {
            return 0;
        }

        var waiting = new List<Item>();
        foreach (var item in box.Items)
        {
            if (!IsCurrency(item))
            {
                waiting.Add(item);
            }
        }

        var moved = 0;
        foreach (var item in waiting)
        {
            if (Mobile.BodyWeight + bot.TotalWeight + item.PileWeight + item.TotalWeight > bot.MaxWeight - 10 ||
                pack.Items.Count >= pack.MaxItems)
            {
                continue;
            }

            pack.DropItem(item);
            bot.GetBrain()?.MarkLoot(item);
            moved++;
        }

        return moved;
    }

    /// <summary>Folds the loose gold in a crowded box into cheques of up to a million, keeping
    /// the total exact.</summary>
    public static void Consolidate(Mobile bot)
    {
        if (bot.FindBankNoCreate() is not { } box || box.Items.Count < ConsolidateAt)
        {
            return;
        }

        var stacks = new List<Gold>();
        long total = 0;
        foreach (var item in box.Items)
        {
            if (item is Gold gold)
            {
                stacks.Add(gold);
                total += gold.Amount;
            }
        }

        if (stacks.Count < 2 || total < MinCheque)
        {
            return;
        }

        foreach (var gold in stacks)
        {
            gold.Delete();
        }

        while (total >= MinCheque)
        {
            var worth = (int)System.Math.Min(total, ChequeWorth);
            box.DropItem(new BankCheck(worth));
            total -= worth;
        }

        if (total > 0)
        {
            box.DropItem(new Gold((int)total));
        }
    }
}
