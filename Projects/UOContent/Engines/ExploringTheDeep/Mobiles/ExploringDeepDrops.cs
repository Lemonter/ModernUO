using Server.Engines.ExploringTheDeep;
using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Shared "the boss drops the quest item for everyone who helped" logic used by the five Exploring the
///     Deep encounter bosses. ServUO repeated the same loot-rights loop in each one, filtering players with
///     a single enum comparison; the comparison is now "is this player doing that quest".
///
///     The helper, and its <c>Func&lt;Item&gt;</c> factory, only run on a kill (cold path) and only once per
///     eligible player.
/// </summary>
internal static class ExploringDeepDrops
{
    public static void Give(BaseCreature creature, Type questType, Func<Item> createItem)
    {
        var rights = BaseCreature.GetLootingRights(creature.DamageEntries, creature.HitsMax);

        for (var i = 0; i < rights.Count; i++)
        {
            var entry = rights[i];

            if (!entry.m_HasRight || entry.m_Mobile is not PlayerMobile pm ||
                !ExploringDeepChain.IsDoing(pm, questType))
            {
                continue;
            }

            var item = createItem();

            if (item == null)
            {
                continue;
            }

            if (pm.Backpack == null || !pm.Backpack.TryDropItem(pm, item, false))
            {
                pm.BankBox?.DropItem(item);
            }

            pm.SendLocalizedMessage(1154489); // You received a Quest Item!
        }
    }
}
