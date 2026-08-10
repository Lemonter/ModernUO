using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     After a BaseCreature kills a player, it has a chance to loot and wear one piece of
///     the corpse's equipment, hitting noticeably harder against other players wearing it.
/// </summary>
public static class MonsterLootingSystem
{
    private const double EquipChance = 0.25;

    public static void TryEquipFromCorpse(PlayerMobile victim, Container corpse)
    {
        if (victim.LastKiller is not BaseCreature creature || creature.Deleted || !creature.Alive)
        {
            return;
        }

        if (!creature.Body.IsHuman)
        {
            return; // a snake isn't putting on your armor
        }

        if (Utility.RandomDouble() >= EquipChance)
        {
            return;
        }

        List<Item> candidates = null;

        foreach (var item in corpse.Items)
        {
            if (item is BaseWeapon or BaseArmor or BaseJewel)
            {
                (candidates ??= new List<Item>()).Add(item);
            }
        }

        if (candidates == null)
        {
            return;
        }

        var chosen = candidates.RandomElement();

        // Swap out whatever the creature already has in that slot first.
        var existing = creature.FindItemOnLayer(chosen.Layer);
        existing?.Delete();

        corpse.RemoveItem(chosen);
        creature.EquipItem(chosen);
    }
}
