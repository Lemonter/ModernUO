using System;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonAuction;
using Server.Systems.MahaonBots;
using Server.Systems.MahaonMetals;

namespace Server.Systems.MahaonCities;

/// <summary>
/// What a held city's guard costs its guild, paid from the guild bank: wages every hour, and every
/// few minutes the replacement of the fallen and the bandages, potions and scrolls the guards use
/// up. What the bank lacks it buys at the auction; an unpaid guard walks off.
/// </summary>
public static class CityGuardUpkeep
{
    public static readonly TimeSpan WageInterval = TimeSpan.FromHours(1);
    public static readonly TimeSpan SupplyInterval = TimeSpan.FromMinutes(10);

    // A purchase run stops after this many lots, so one settling can't drain the auction.
    private const int MaxLotsPerNeed = 10;

    /// <summary>What every guard carries.</summary>
    public static readonly (Type type, int count)[] GuardKit =
    [
        (typeof(Bandage), 20),
        (typeof(GreaterHealPotion), 3),
        (typeof(GreaterCurePotion), 1)
    ];

    /// <summary>What a sword guard carries on top: scrolls it reads, a mage needing none.</summary>
    public static readonly (Type type, int count)[] ScrollKit =
    [
        (typeof(GreaterHealScroll), 3),
        (typeof(CureScroll), 2),
        (typeof(LightningScroll), 5)
    ];

    public static void Initialize()
    {
        Timer.DelayCall(WageInterval, WageInterval, () => ForEachHeld(PayWages));
        Timer.DelayCall(SupplyInterval, SupplyInterval, () => ForEachHeld(Maintain));
    }

    /// <summary>A guard's wage per hour at its city's level.</summary>
    public static int WagePerHour(int level) => 5 + 2 * level;

    /// <summary>What replacing a fallen guard costs: a little gold.</summary>
    public static int ReplaceCost(int level) => 100 + 50 * level;

    private static void ForEachHeld(Action<string, Guild> settle)
    {
        foreach (var city in CityControlSystem.Cities.Keys)
        {
            if (CityControlSystem.GetController(city) is { } guild)
            {
                settle(city, guild);
            }
        }
    }

    /// <summary>A full settling of a city's guard: wages, replacements, supplies.</summary>
    public static void Run(string city, Guild guild)
    {
        PayWages(city, guild);
        Maintain(city, guild);
    }

    /// <summary>Replacements and supplies.</summary>
    public static void Maintain(string city, Guild guild)
    {
        ReplaceFallen(city, guild);
        Restock(city, guild);
    }

    public static void PayWages(string city, Guild guild)
    {
        var level = CityControlSystem.GetGuardLevel(city);
        var guards = 0;
        CityGuard last = null;
        foreach (var guard in CityGuard.Of(city))
        {
            if (guard.Alive && !guard.Deleted)
            {
                guards++;
                last = guard;
            }
        }

        var wages = (long)guards * WagePerHour(level);
        if (wages <= 0 || GuildBank.TrySpend(guild.Name, wages, MahaonMetal.Iron, 0))
        {
            return;
        }

        // Nothing to pay with: one guard leaves the city's service each time.
        last.Say("Без жалованья служить не стану!");
        last.Delete();
    }

    private static void ReplaceFallen(string city, Guild guild)
    {
        var cost = ReplaceCost(CityControlSystem.GetGuardLevel(city));
        var missingSwords = CityControlSystem.GuardRoster(city) - CityControlSystem.SwordGuardsIn(city);
        var missingMages = CityControlSystem.GetMageSlots(city) - CityControlSystem.MagesIn(city);

        for (var i = 0; i < missingSwords + missingMages; i++)
        {
            if (!GuildBank.TrySpend(guild.Name, cost, MahaonMetal.Iron, 0))
            {
                return;
            }

            CityControlSystem.SpawnGuard(city, guild, i >= missingSwords);
        }
    }

    private static void Restock(string city, Guild guild)
    {
        foreach (var guard in CityGuard.Of(city))
        {
            if (!guard.Alive || guard.Deleted)
            {
                continue;
            }

            Supply(guard, guild, GuardKit);

            if (guard is not CityMageGuard)
            {
                Supply(guard, guild, ScrollKit);
            }
        }
    }

    private static void Supply(CityGuard guard, Guild guild, (Type type, int count)[] kit)
    {
        var pack = guard.Backpack;
        if (pack == null)
        {
            guard.AddItem(pack = new Backpack { Movable = false });
        }

        foreach (var (type, count) in kit)
        {
            var need = count - pack.GetAmount(type);
            if (need <= 0)
            {
                continue;
            }

            for (var lots = 0; GuildBank.Count(guild.Name, type) < need && lots < MaxLotsPerNeed; lots++)
            {
                if (!AuctionHouseSystem.TryBuyForGuild(guild.Name, AuctionHouseSystem.Cheapest(item => item.GetType() == type)))
                {
                    break;
                }
            }

            if (GuildBank.Take(guild.Name, type, need) is { } supplies)
            {
                pack.DropItem(supplies);
            }
        }
    }
}
