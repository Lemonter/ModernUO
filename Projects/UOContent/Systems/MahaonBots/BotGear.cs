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

/// <summary>Bot equipment/bank management — snapshotting current gear to the bank, pruning the bank down to the best kept items.</summary>
public partial class BotController
{

    /// <summary>
    ///     Раньше называлось SnapshotGearToBank — клонировало весь текущий комплект в
    ///     банк через безусловный bank.DropItem (обходит MaxItems=125 намеренно) при
    ///     каждой смерти. PruneBankToBest после этого чистила только броню/оружие — всё
    ///     остальное копилось без предела. Один бот на реальном сервере дорос до
    ///     1 027 461 предметов в банке за счёт этого механизма.
    ///
    ///     Теперь — ни одного предмета не создаётся и не хранится вообще. Просто
    ///     сравниваем текущий надетый рейтинг брони/урон оружия на каждом слое с уже
    ///     запомненным в BotProfile числом и обновляем, если текущее лучше. При
    ///     возрождении (см. BotMobile.RegearAfterDeath) эти числа используются, чтобы
    ///     подтянуть свежую базовую броню/оружие до когда-то достигнутого уровня —
    ///     не восстанавливая старые предметы, а просто усиливая новые.
    /// </summary>
    public static void RecordBestGear(BotMobile bot)
    {
        if (!Systems.MahaonBots.BotController.TryGetProfile(bot, out var profile))
        {
            return;
        }

        foreach (var item in bot.Items)
        {
            switch (item)
            {
                case BaseArmor armor:
                {
                    var rating = armor.ArmorRatingScaled;

                    if (!profile.BestArmorRating.TryGetValue(armor.Layer, out var best) || rating > best)
                    {
                        profile.BestArmorRating[armor.Layer] = rating;
                    }

                    break;
                }

                case BaseWeapon weapon:
                {
                    var damage = weapon.MinDamage + weapon.MaxDamage;

                    if (!profile.BestWeaponDamage.TryGetValue(weapon.Layer, out var best) || damage > best)
                    {
                        profile.BestWeaponDamage[weapon.Layer] = damage;
                    }

                    break;
                }
            }
        }
    }

    /// <summary>Подтягивает свежую базовую броню/оружие (уже надетые ApplyArchetype) до
    /// когда-то достигнутого уровня, если запомненное число выше. Через
    /// ArmorAttributes/PhysicalBonus (броня) и Attributes.WeaponDamage (оружие) — реальные
    /// движковые поля, не выдуманная параллельная система.</summary>
    public static void ApplyRememberedGearBonus(BotMobile bot)
    {
        if (!Systems.MahaonBots.BotController.TryGetProfile(bot, out var profile))
        {
            return;
        }

        foreach (var item in bot.Items)
        {
            switch (item)
            {
                case BaseArmor armor when profile.BestArmorRating.TryGetValue(armor.Layer, out var bestRating):
                {
                    var current = armor.ArmorRatingScaled;

                    if (bestRating > current)
                    {
                        // PhysicalBonus only feeds PhysicalResistance, never ArmorRating —
                        // boosting it here did nothing for AR at all. BaseArmorRating is
                        // the field that actually drives ArmorRating/ArmorRatingScaled;
                        // dividing by ArmorScalar converts the scaled-AR gap back into a
                        // base-AR delta.
                        var boost = (int)Math.Round((bestRating - current) / armor.ArmorScalar);
                        armor.BaseArmorRating += boost;
                    }

                    break;
                }

                case BaseWeapon weapon when profile.BestWeaponDamage.TryGetValue(weapon.Layer, out var bestDamage):
                {
                    var current = weapon.MinDamage + weapon.MaxDamage;

                    if (bestDamage > current)
                    {
                        // AosAttribute.WeaponDamage is a PERCENTAGE bonus (capped ~100%) —
                        // adding a raw flat damage-point delta into it mixed units. Spread
                        // the flat delta across Min/MaxDamage directly instead, keeping the
                        // weapon's damage range shape intact.
                        var delta = bestDamage - current;
                        weapon.MinDamage += delta / 2;
                        weapon.MaxDamage += delta - delta / 2;
                    }

                    break;
                }
            }
        }
    }
}
