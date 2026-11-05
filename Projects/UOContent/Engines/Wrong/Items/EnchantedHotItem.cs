using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (EnchantedHotItem.cs) — the seven "hot" chests in the
///     jail that reroll their loot and relock themselves after being picked.
///
///     Two ServUO systems this relied on are absent and were dropped rather than faked: the
///     <c>EnchantedHotItemSocket</c> spell-socket framework (no socket system here — the loot simply
///     spawns in the chest) and <c>RunicReforging.GenerateRandomItem</c> (no reforge engine), so loot is a
///     random scroll plus random armour/weapon/jewellery at hue 1258. The persistence hooks that only
///     stored socket ownership are gone with them.
/// </summary>
public static class EnchantedHotItem
{
    public static readonly Point3D[] ChestLocs =
    {
        new(5703, 664, 0), new(5703, 663, 0), new(5703, 662, 0), new(5703, 661, 0),
        new(5707, 659, 0), new(5708, 659, 0), new(5709, 659, 0)
    };

    public static void SpawnChests(Map map)
    {
        var stale = new System.Collections.Generic.List<Item>();

        foreach (var item in map.GetItemsInRange(new Point3D(5707, 662, 0), 15))
        {
            if (item is MetalGoldenChest)
            {
                stale.Add(item);
            }
        }

        // Удаление мутирует коллекцию, по которой идёт обход, поэтому сначала собираем список.
        foreach (var item in stale)
        {
            item.Delete();
        }

        for (var i = 0; i < ChestLocs.Length; i++)
        {
            var chest = new HotItemChest(i <= 3 ? 3648 : 3649);

            chest.MoveToWorld(ChestLocs[i], map);
            chest.SpawnLoot();
        }
    }
}

[SerializationGenerator(0, false)]
public partial class HotItemChest : LockableContainer
{
    public HotItemChest(int itemID) : base(itemID)
    {
        Movable = false;
        RequiredSkill = 92;
        LockLevel = RequiredSkill - Utility.Random(1, 10);
        MaxLockLevel = RequiredSkill;

        LiftOverride = true;
        Locked = true;
    }

    public void SpawnLoot()
    {
        var existing = 0;

        foreach (var item in Items)
        {
            existing++;
            item.Delete();
        }

        _ = existing;

        var toSpawn = Utility.RandomMinMax(2, 4);

        for (var i = 0; i < toSpawn; i++)
        {
            Item item;

            if (i == 0)
            {
                item = Loot.RandomScroll(0, Loot.RegularScrollTypes.Length, SpellbookType.Regular);
            }
            else
            {
                item = Loot.RandomArmorOrShieldOrWeaponOrJewelry();
                item.Hue = 1258;
            }

            if (item != null)
            {
                DropItem(item);
            }
        }
    }

    public override void LockPick(Mobile from)
    {
        base.LockPick(from);

        Timer.DelayCall(
            TimeSpan.FromMinutes(10),
            () =>
            {
                if (Deleted)
                {
                    return;
                }

                Locked = true;
                SpawnLoot();
            }
        );
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (!Locked)
        {
            Timer.DelayCall(TimeSpan.FromMinutes(5), () =>
            {
                if (!Deleted)
                {
                    Locked = true;
                }
            });
        }
    }
}
