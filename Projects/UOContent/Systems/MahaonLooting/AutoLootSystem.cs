using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonLooting;

/// <summary>
///     Called right after a corpse is opened — automatically pulls gold/currency and
///     "obviously always useful, never worth manually dragging" consumables (reagents,
///     bandages, potions, arrows/bolts) straight into the looter's own backpack. Doesn't
///     touch anything else (armor, weapons, special loot) — those still need a deliberate
///     drag, this is purely for the tedious "click every coin stack" busywork.
///     Respects the exact same CanLoot permission check the corpse itself uses, so this
///     never bypasses ownership/blue-looting rules.
/// </summary>
public static class AutoLootSystem
{
    public static void TryAutoLoot(Mobile from, Corpse corpse)
    {
        if (from?.Backpack == null || corpse.Deleted)
        {
            return;
        }

        Run(from, corpse, item => corpse.CanLoot(from, item));
    }

    /// <summary>Same sweep, for a dungeon treasure chest instead of a corpse — chests have
    /// no owner/blue-looting concept the way a corpse does, so there's no per-item
    /// permission check beyond "the chest is open" (already guaranteed by the caller).
    /// Wired from BaseTreasureChest.Open.</summary>
    public static void TryAutoLootChest(Mobile from, Container chest)
    {
        if (from?.Backpack == null || chest.Deleted)
        {
            return;
        }

        Run(from, chest, static _ => true);
    }

    private static void Run(Mobile from, Container source, Func<Item, bool> canLoot)
    {
        var grabbed = new List<Item>();

        foreach (var item in new List<Item>(source.Items))
        {
            if (!IsAutoLootable(from, item) || !canLoot(item))
            {
                continue;
            }

            if (Systems.MahaonWorld.MahaonResourceBagSystem.TryGive(from, item) || from.Backpack.TryDropItem(from, item, false))
            {
                grabbed.Add(item);
            }
        }

        if (grabbed.Count == 0)
        {
            return;
        }

        var goldTotal = 0;
        var otherCount = 0;

        foreach (var item in grabbed)
        {
            if (item is Gold)
            {
                goldTotal += item.Amount;
            }
            else
            {
                otherCount++;
            }
        }

        if (goldTotal > 0 && otherCount > 0)
        {
            from.SendMessage(0x59, $"Автоматически подобрано: {goldTotal} монет и {otherCount} расходников.");
        }
        else if (goldTotal > 0)
        {
            from.SendMessage(0x59, $"Автоматически подобрано: {goldTotal} монет.");
        }
        else
        {
            from.SendMessage(0x59, $"Автоматически подобрано расходников: {otherCount}.");
        }
    }

    private static bool IsAutoLootable(Mobile from, Item item) => item switch
    {
        Gold => true,
        BaseReagent                          => true,
        BasePotion                           => true,
        Bandage                              => true,
        Arrow or Bolt                        => true,
        _ when Systems.MahaonGems.GemSocketingSystem.BonusTypeFor(item.GetType()) != null => true,
        // Player-extensible list — see MahaonAutoLootListSystem/MahaonAutoLootGump. Built-in
        // categories above are always on; this is whatever the player targeted themselves.
        _ => MahaonAutoLootListSystem.IsTracked(from, item.GetType())
    };
}
