using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Third;
using Server.Spells.Fourth;
using Server.Spells.Fifth;
using Server.Spells.Sixth;
using Server.Spells.Seventh;
using Server.Spells.Eighth;
using Server.Spells.Necromancy;
using Server.Spells.Chivalry;
using Server.Spells.Bushido;
using Server.Spells.Ninjitsu;
using Server.Spells.Spellweaving;
using Server.Systems.MahaonAuction;
using Server.Systems.MahaonCities;
using Server.Systems.MahaonCombat;
using Server.Systems.MahaonMining;
using Server.Systems.MahaonProfessions;
using Server.Systems.MahaonRecipes;
using Server.Systems.MahaonRaids;
using Server.Targeting;

namespace Server.Systems.MahaonBots;

/// <summary>Looting — corpses, chests, picking up loose gear, deciding whether a found item is an upgrade.</summary>
public partial class BotController
{

    // -- Dungeon chest looting -------------------------------------------------------------
    //
    // Nothing left to fight nearby — see if there's a chest worth cracking. Locked == true
    // doubles as our "hasn't been looted yet" marker; once a bot loots one it unlocks it, so
    // future bots (or the player) don't waste time on an already-emptied chest.

    private const int ChestSearchRange = 15;
    private const int ChestLootCount = 3;

    private const int CorpseSearchRange = 15;
    private const int CorpseSearchRangeWhileTraveling = 8; // worth a small detour, not the whole visible area
    private const int CorpseLootCount = 3;

    /// <summary>
    ///     Loots whatever's on the ground nearby — not just what this bot personally killed.
    ///     Matches "loot everything they can, wherever they can" rather than tracking kill
    ///     ownership, which real UO doesn't enforce for looting rights this loosely either.
    /// </summary>
    private static bool TryLootCorpse(Mobile bot) => TryLootCorpse(bot, CorpseSearchRange);

    private static bool TryLootCorpse(Mobile bot, int range)
    {
        if (bot.Map == null)
        {
            return false;
        }

        Corpse corpse = null;

        foreach (var candidate in bot.Map.GetItemsInRange<Corpse>(bot.Location, range))
        {
            if (!candidate.Deleted && candidate.Items.Count > 0)
            {
                corpse = candidate;
                break;
            }
        }

        if (corpse == null)
        {
            return TryPickUpLooseGear(bot, range);
        }

        if (!bot.InRange(corpse.GetWorldLocation(), 1))
        {
            StepToward(bot, corpse.Location, 3);
            return true;
        }

        var backpack = bot.Backpack;
        if (backpack == null)
        {
            return true;
        }

        var looted = 0;
        var items = new List<Item>(corpse.Items);

        foreach (var item in items)
        {
            if (looted >= CorpseLootCount)
            {
                break;
            }

            if (item is Container)
            {
                continue; // skip nested bags/etc, keep it to loose loot
            }

            if (TryEquipUpgrade(bot, item))
            {
                looted++;
                continue;
            }

            if (backpack.TryDropItem(bot, item, false))
            {
                looted++;
            }
        }

        if (looted > 0 && bot is BotMobile botMobile)
        {
            botMobile.RememberHuntingSpot(bot.Location);
        }

        return true;
    }

    /// <summary>
    ///     Real gear just lying loose on the ground (not in a corpse/container) gets
    ///     noticed too — otherwise a bot can walk right past a dropped sword and never
    ///     react to it, since the corpse-loot pass only ever looks inside corpses.
    /// </summary>
    private static bool TryPickUpLooseGear(Mobile bot, int range)
    {
        if (bot.Map == null)
        {
            return false;
        }

        foreach (var item in bot.Map.GetItemsInRange<Item>(bot.Location, range))
        {
            if (item.Deleted || !item.Movable || item.Parent != null)
            {
                continue;
            }

            var isUseful = item is BaseArmor or BaseWeapon or Bandage or BasePotion or SpellScroll;
            if (!isUseful)
            {
                continue;
            }

            if (!bot.InRange(item.GetWorldLocation(), 1))
            {
                StepToward(bot, item.Location, 3);
                return true;
            }

            if (item is BaseArmor or BaseWeapon)
            {
                if (TryEquipUpgrade(bot, item))
                {
                    return true;
                }

                continue;
            }

            // Bandages/potions/scrolls just go straight in the pack for later use.
            if (bot.Backpack?.TryDropItem(bot, item, false) == true)
            {
                bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Пригодится: {item.Name ?? item.GetType().Name}.");
                return true;
            }
        }

        return false;
    }

    /// <summary>Equips armor/weapons straight from loot if they're an upgrade over what's
    /// currently worn/wielded — an empty slot always counts as an upgrade.</summary>
    public static bool TryEquipUpgrade(Mobile bot, Item item)
    {
        switch (item)
        {
            case BaseArmor armor:
            {
                var current = bot.FindItemOnLayer(armor.Layer) as BaseArmor;
                if (current != null && current.ArmorRatingScaled >= armor.ArmorRatingScaled)
                {
                    return false;
                }

                if (!bot.EquipItem(armor))
                {
                    return false;
                }

                // Mahaon: EquipItem doesn't delete whatever was previously in this layer —
                // same underlying gap that caused the DeathRobe leak (~2 million orphaned
                // items). The displaced piece would otherwise sit in the bot's backpack
                // forever across every future gear upgrade, same failure mode, different
                // item type — the huge RingmailArms/StuddedChest/BoneLegs/etc counts from
                // [CountEntities were this, not death robes.
                current?.Delete();

                bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Мне пригодится эта {armor.Name ?? armor.GetType().Name}.");
                return true;
            }

            case BaseWeapon weapon:
            {
                var current = bot.FindItemOnLayer(Layer.OneHanded) as BaseWeapon
                              ?? bot.FindItemOnLayer(Layer.TwoHanded) as BaseWeapon;

                if (current != null &&
                    current.MinDamage + current.MaxDamage >= weapon.MinDamage + weapon.MaxDamage)
                {
                    return false;
                }

                if (!bot.EquipItem(weapon))
                {
                    return false;
                }

                current?.Delete(); // same reasoning as the armor case above

                bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Возьму этот {weapon.Name ?? weapon.GetType().Name} себе.");
                return true;
            }

            default:
                return false;
        }
    }

    /// <returns>true if there was a chest worth dealing with this cycle — found, walked
    /// toward, or looted — false only when there's genuinely nothing nearby. Used by
    /// DoDungeonCombat to avoid burning the leave-the-dungeon budget while a bot is still
    /// making real progress on the current floor.</returns>
    private static bool TryLootChest(Mobile bot, BotProfile profile)
    {
        if (bot.Map == null)
        {
            return false;
        }

        BaseTreasureChest chest = null;

        foreach (var candidate in bot.Map.GetItemsInRange<BaseTreasureChest>(bot.Location, ChestSearchRange))
        {
            if (candidate.Locked && !candidate.Deleted)
            {
                chest = candidate;
                break;
            }
        }

        if (chest == null)
        {
            return false;
        }

        if (!bot.InRange(chest.GetWorldLocation(), 1))
        {
            if (bot is PlayerMobile pm)
            {
                StepTowardPath(pm, profile, chest.Location);
            }
            else
            {
                StepToward(bot, chest.Location, 3);
            }

            return true;
        }

        // Mahaon: пока ВСЕ боты взламывают без проверки навыка вообще — в пати может не
        // быть вора, а сундук всё равно должен открываться (временное решение, по
        // просьбе шард-овнера). Настоящий LockableContainer.LockPick всё равно
        // используется вместо ручного chest.Locked = false, чтобы Picker/тренировка
        // специализации/ловушки отрабатывали как положено.
        chest.LockPick(bot);

        var backpack = bot.Backpack;
        if (backpack == null)
        {
            return true;
        }

        var looted = 0;
        var items = new List<Item>(chest.Items);

        foreach (var item in items)
        {
            if (looted >= ChestLootCount)
            {
                break;
            }

            if (backpack.TryDropItem(bot, item, false))
            {
                looted++;
            }
        }

        return true;
    }
}
