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

/// <summary>Pet management — taming, summoning/dismissing a summoned pet in and out of combat, guild death-gem drops.</summary>
public partial class BotController
{

    // -- Ranger taming: spots wildlife using Tracking, tames it using real AnimalTaming ---

    private static bool TryTameNearby(PlayerMobile bot)
    {
        if (bot.Map == null)
        {
            return false;
        }

        var trackingSkill = bot.Skills[SkillName.Tracking].Value;
        var range = 4 + (int)(trackingSkill / 10.0); // better Tracking spots wildlife farther off

        BaseCreature target = null;

        foreach (var creature in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, range))
        {
            if (creature.Tamable && !creature.Controlled && creature.Alive && !creature.Deleted)
            {
                target = creature;
                break;
            }
        }

        if (target == null)
        {
            return false;
        }

        bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Попробую приручить {target.Name}.");

        Server.SkillHandlers.AnimalTaming.OnUse(bot);

        if (Bots.TryGetValue(bot, out var profile))
        {
            profile.PendingTameTarget = target;
        }

        return true;
    }

    private static void ResolvePendingTame(PlayerMobile bot, BotProfile profile)
    {
        var target = profile.PendingTameTarget;
        profile.PendingTameTarget = null;

        if (bot.Target == null || target?.Deleted != false || !target.Alive)
        {
            return;
        }

        bot.Target.Invoke(bot, target);
    }

    /// <summary>Archers "unpack" a pet for the fight — a fake unpacking (no real pouch
    /// item tracking, just a message) since there's no packed-pet mechanic to hook into
    /// here. Tier of animal scales with Animal Taming, same idea as everything else that
    /// scales off a real skill in this file.</summary>
    private static void TrySummonPet(Mobile bot, BotProfile profile)
    {
        if (!IsProfessionCategory(bot, ProfessionCategory.Ranger) || profile.SummonedPet?.Deleted == false)
        {
            return;
        }

        var tamingSkill = bot.Skills[SkillName.AnimalTaming].Value;
        if (tamingSkill < 20 || bot.Map == null)
        {
            return;
        }

        BaseCreature pet = tamingSkill switch
        {
            >= 70 => new GrizzlyBear(),
            >= 45 => new DireWolf(),
            _     => new GreyWolf()
        };

        pet.Controlled = true;
        pet.ControlMaster = bot;
        pet.ControlOrder = OrderType.Follow;
        pet.MoveToWorld(bot.Location, bot.Map);

        if (IsFighting(bot))
        {
            pet.ControlTarget = bot.Combatant;
            pet.ControlOrder = OrderType.Attack;
            pet.Combatant = bot.Combatant;
            pet.Warmode = true;
        }

        profile.SummonedPet = pet;
        bot.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Выпускаю {pet.Name}!");
    }

    /// <summary>Keeps the pet actually doing something instead of drifting once it's out —
    /// re-aims it at whatever the archer's currently fighting, or has it just follow when
    /// there's nothing to fight. Call every Dispatch tick while the pet is out.</summary>
    private static void TrySyncPet(Mobile bot, BotProfile profile)
    {
        var pet = profile.SummonedPet;
        if (pet?.Deleted != false)
        {
            return;
        }

        if (IsFighting(bot))
        {
            if (pet.ControlTarget != bot.Combatant)
            {
                pet.ControlTarget = bot.Combatant;
                pet.ControlOrder = OrderType.Attack;
                pet.Combatant = bot.Combatant;
                pet.Warmode = true;
            }
        }
        else if (pet.ControlOrder != OrderType.Follow)
        {
            pet.ControlTarget = bot;
            pet.ControlOrder = OrderType.Follow;
            pet.Combatant = null;
            pet.Warmode = false;
        }
    }

    /// <summary>Puts the pet away again once there's no fight left for it to be in —
    /// checked from Dispatch's not-fighting path.</summary>
    private static void TryDismissPet(Mobile bot, BotProfile profile)
    {
        if (profile.SummonedPet?.Deleted == false)
        {
            profile.SummonedPet.Delete();
        }

        profile.SummonedPet = null;
    }

    /// <summary>
    ///     Убирает питомцев, оставшихся без живого хозяина-бота: после перезапуска, после
    ///     удаления бота командой, после любой ситуации, где профиль исчез, а зверь нет.
    ///     Ссылка на питомца живёт только в BotProfile, а он не сохраняется, поэтому
    ///     единственный способ узнать сироту — посмотреть, кто им командует.
    /// </summary>
    public static void CleanupOrphanedBotPets()
    {
        var orphans = new List<Mobile>();

        foreach (var m in World.Mobiles.Values)
        {
            if (m is not BaseCreature { Controlled: true, Deleted: false } pet)
            {
                continue;
            }

            if (pet.ControlMaster is not BotMobile master)
            {
                continue; // чужой питомец, не наше дело
            }

            // Сирота — это зверь, которого не держит ни один живой профиль. Так метод
            // безопасно звать когда угодно, а не только до регистрации ботов на старте.
            var claimed = !master.Deleted &&
                          Bots.TryGetValue(master, out var profile) &&
                          profile.SummonedPet == pet;

            if (!claimed)
            {
                orphans.Add(pet);
            }
        }

        foreach (var pet in orphans)
        {
            pet.Delete();
        }

        if (orphans.Count > 0)
        {
            Console.WriteLine($"Боты: убрано питомцев без хозяина — {orphans.Count}");
        }
    }

    /// <summary>Called from PlayerMobile.OnDeath — a summoned pet disappears the instant its
    /// archer dies, not just once combat naturally ends.</summary>
    public static void DismissPetOnDeath(PlayerMobile bot)
    {
        if (Bots.TryGetValue(bot, out var profile) && profile.SummonedPet?.Deleted == false)
        {
            profile.SummonedPet.Delete();
            profile.SummonedPet = null;
        }

        DropGuildDeathGem(bot);
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

    private static void DropGuildDeathGem(PlayerMobile bot)
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
