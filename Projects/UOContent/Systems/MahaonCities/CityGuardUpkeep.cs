using System;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonAuction;
using Server.Systems.MahaonBots;
using Server.Systems.MahaonMetals;

namespace Server.Systems.MahaonCities;

/// <summary>
/// What a held city's guard costs its guild, settled from the guild bank every few minutes: wages,
/// the replacement of the fallen (gold, and ingots of the guard's metal for the new gear), and the
/// potions and scrolls the guards use up. What the bank lacks it buys at the auction; an unpaid
/// guard walks off.
/// </summary>
public static class CityGuardUpkeep
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    // A purchase run stops after this many lots, so one tick can't drain the auction.
    private const int MaxLotsPerNeed = 10;

    /// <summary>What every guard carries.</summary>
    public static readonly (Type type, int count)[] GuardKit =
    [
        (typeof(GreaterHealPotion), 3),
        (typeof(GreaterCurePotion), 1)
    ];

    /// <summary>What a battle mage carries on top: a scroll is burnt for every spell it casts.</summary>
    public static readonly (Type type, int count)[] MageKit =
    [
        (typeof(MagicArrowScroll), 5),
        (typeof(FireballScroll), 5),
        (typeof(LightningScroll), 5),
        (typeof(EnergyBoltScroll), 5),
        (typeof(HarmScroll), 3),
        (typeof(MindBlastScroll), 3),
        (typeof(ExplosionScroll), 3),
        (typeof(FlamestrikeScroll), 3),
        (typeof(ParalyzeScroll), 2),
        (typeof(PoisonScroll), 2),
        (typeof(GreaterHealScroll), 5),
        (typeof(HealScroll), 3),
        (typeof(CureScroll), 3)
    ];

    public static void Initialize() => Timer.DelayCall(Interval, Interval, Tick);

    /// <summary>A guard's wage per hour at its city's level.</summary>
    public static int WagePerHour(int level) => 50 + 25 * level;

    /// <summary>What replacing a fallen guard costs: gold, and ingots of the level's metal for its gear.</summary>
    public static (long gold, int ingots) ReplaceCost(int level) =>
        (1000 + 500L * level, level == 0 ? 0 : Math.Max(10, CityControlSystem.GuardSteps[level - 1].Ingots / 20));

    private static void Tick()
    {
        foreach (var city in CityControlSystem.Cities.Keys)
        {
            if (CityControlSystem.GetController(city) is { } guild)
            {
                Run(city, guild);
            }
        }
    }

    /// <summary>One settling of a city's guard: wages, replacements, supplies.</summary>
    public static void Run(string city, Guild guild)
    {
        PayWages(city, guild);
        ReplaceFallen(city, guild);
        Restock(city, guild);
    }

    private static void PayWages(string city, Guild guild)
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

        var wages = (long)(guards * WagePerHour(level) * Interval.TotalHours);
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
        var level = CityControlSystem.GetGuardLevel(city);
        var (gold, ingots) = ReplaceCost(level);
        var metal = CityControlSystem.MetalFor(level) ?? MahaonMetal.Iron;

        var missingSwords = CityControlSystem.GuardRoster(city) - CityControlSystem.SwordGuardsIn(city);
        var missingMages = CityControlSystem.GetMageSlots(city) - CityControlSystem.MagesIn(city);

        for (var i = 0; i < missingSwords + missingMages; i++)
        {
            if (ingots > 0)
            {
                BuyIngots(guild, metal, ingots - GuildBank.GetIngots(guild.Name, metal));
            }

            if (!GuildBank.TrySpend(guild.Name, gold, metal, ingots))
            {
                return;
            }

            CityControlSystem.SpawnGuard(city, guild, i >= missingSwords);
        }
    }

    private static void BuyIngots(Guild guild, MahaonMetal metal, int missing)
    {
        for (var lots = 0; missing > 0 && lots < MaxLotsPerNeed; lots++)
        {
            var listing = AuctionHouseSystem.Cheapest(item => item is MahaonIngot ingot && ingot.Metal == metal);
            var amount = listing?.Item.Amount ?? 0;
            if (!AuctionHouseSystem.TryBuyForGuild(guild.Name, listing))
            {
                return;
            }

            missing -= amount;
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

            if (guard is CityMageGuard)
            {
                Supply(guard, guild, MageKit);
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
