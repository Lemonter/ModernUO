using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Systems.Bots;

/// <summary>
/// Bots' houses: whose house a bot lives in, where a new one may go, and which kind it can
/// afford. Two kinds are built — one house per guild, owned by the member who paid for it with
/// every member a friend of the house, and a private house for a bot that has grown rich, within
/// a shard-wide cap. Both are ordinary houses, placed through the same placement check and paid
/// for from the bank like a player's, and they decay like any other when nobody comes home.
/// </summary>
public static class BotHousing
{
    private const string GuildHousePrefix = "Дом гильдии ";

    // Far enough out to clear the town's no-build region, near enough to walk home.
    private const int MinDistance = 40;
    private const int MaxDistance = 160;
    private const int SpotAttempts = 25;

    public static int MaxPrivateHouses { get; private set; } = 100;

    public static long RichThreshold { get; private set; } = 300_000;

    public static void Configure()
    {
        MaxPrivateHouses = ServerConfiguration.GetOrUpdateSetting("bots.houses.maxPrivate", 100);
        RichThreshold = ServerConfiguration.GetOrUpdateSetting("bots.houses.richThreshold", 300_000L);
    }

    private static HousePlacementEntry[] Entries => Core.EJ ? HousePlacementEntry.HousesEJ : HousePlacementEntry.ClassicHouses;

    public static bool IsGuildHouse(BaseHouse house) => house.Sign?.Name?.StartsWith(GuildHousePrefix, StringComparison.Ordinal) == true;

    /// <summary>A house the bot owns itself, guild house or private.</summary>
    public static BaseHouse OwnHouse(Mobile bot)
    {
        foreach (var house in BaseHouse.GetHouses(bot))
        {
            if (!house.Deleted)
            {
                return house;
            }
        }

        return null;
    }

    // Guild houses and the private-house count, re-read from the house list now and then: every
    // bot asks on every goal review, and the list holds every house on the shard.
    private const long CacheMs = 30_000;
    private static readonly Dictionary<Guild, BaseHouse> _guildHouses = new();
    private static int _privateCount;
    private static long _cachedAt;
    private static bool _cached;

    private static void Refresh()
    {
        var now = Core.TickCount;
        if (_cached && now - _cachedAt < CacheMs)
        {
            return;
        }

        _cached = true;
        _cachedAt = now;
        _guildHouses.Clear();
        _privateCount = 0;

        foreach (var house in BaseHouse.AllHouses)
        {
            if (house.Deleted || house.Owner is not BotMobile owner)
            {
                continue;
            }

            if (!IsGuildHouse(house))
            {
                _privateCount++;
            }
            else if (owner.Guild is Guild guild)
            {
                _guildHouses[guild] = house;
            }
        }
    }

    /// <summary>Forgets the cached figures after a bot builds or loses a house.</summary>
    public static void Invalidate() => _cached = false;

    /// <summary>Its guild's house, whoever owns it.</summary>
    public static BaseHouse GuildHouse(Guild guild)
    {
        if (guild == null)
        {
            return null;
        }

        Refresh();
        return _guildHouses.TryGetValue(guild, out var house) && !house.Deleted ? house : null;
    }

    /// <summary>The house a bot calls home: its own, else its guild's.</summary>
    public static BaseHouse HomeOf(Mobile bot) => OwnHouse(bot) ?? GuildHouse(bot.Guild as Guild);

    public static int PrivateHouseCount()
    {
        Refresh();
        return _privateCount;
    }

    /// <summary>
    /// Where a smith stands at home: a floor cell within reach of both the house's forge and its
    /// anvil (the craft checks both within two tiles).
    /// </summary>
    public static bool TryGetSmithy(BaseHouse house, out Point3D stand)
    {
        stand = default;
        Item forge = null, anvil = null;

        foreach (var addon in house.Addons)
        {
            if (addon is SmallForgeAddon { Deleted: false })
            {
                forge = addon;
            }
            else if (addon is AnvilEastAddon { Deleted: false })
            {
                anvil = addon;
            }
        }

        if (forge == null || anvil == null)
        {
            return false;
        }

        foreach (var p in FloorPoints(house))
        {
            if (Utility.InRange(p, forge.Location, 2) && Utility.InRange(p, anvil.Location, 2) && house.Map.CanFit(p, 16, false, false))
            {
                stand = p;
                return true;
            }
        }

        return false;
    }

    /// <summary>The house's own spinning wheel and loom, if it has both.</summary>
    public static bool TryGetTextiles(BaseHouse house, out Item wheel, out Item loom)
    {
        wheel = null;
        loom = null;

        foreach (var addon in house.Addons)
        {
            if (addon is ISpinningWheel && !addon.Deleted)
            {
                wheel = addon;
            }
            else if (addon is ILoom && !addon.Deleted)
            {
                loom = addon;
            }
        }

        return wheel != null && loom != null;
    }

    /// <summary>The house a budget buys: a guild wants room for everyone, a rich bot something it
    /// likes among what it can afford without emptying the bank.</summary>
    public static HousePlacementEntry ChooseEntry(long budget, bool forGuild)
    {
        HousePlacementEntry best = null;
        var affordable = new List<HousePlacementEntry>();

        foreach (var entry in Entries)
        {
            if (entry.Cost > budget)
            {
                continue;
            }

            affordable.Add(entry);
            if (best == null || entry.Cost > best.Cost)
            {
                best = entry;
            }
        }

        if (affordable.Count == 0)
        {
            return null;
        }

        return forGuild ? best : affordable[Utility.Random(affordable.Count)];
    }

    /// <summary>A spot near <paramref name="around"/> where the house passes the placement check,
    /// or null after a number of tries.</summary>
    public static Point3D? FindSpot(Mobile bot, HousePlacementEntry entry, Point3D around)
    {
        var map = bot.Map;

        for (var attempt = 0; attempt < SpotAttempts; attempt++)
        {
            var angle = Utility.RandomDouble() * Math.PI * 2;
            var dist = Utility.RandomMinMax(MinDistance, MaxDistance);
            var x = around.X + (int)(Math.Cos(angle) * dist);
            var y = around.Y + (int)(Math.Sin(angle) * dist);

            if (x < 16 || y < 16 || x >= map.Width - 16 || y >= map.Height - 16)
            {
                continue;
            }

            var center = new Point3D(x, y, map.GetAverageZ(x, y));
            if (HousePlacement.Check(bot, entry.MultiID, center, out _, entry.HouseDirection) == HousePlacementResult.Valid)
            {
                return center;
            }
        }

        return null;
    }

    /// <summary>Builds the house at <paramref name="center"/>, as the placement tool does: placement
    /// re-checked, cost taken from the bank, anything standing in the way moved out.</summary>
    public static BaseHouse Place(Mobile bot, HousePlacementEntry entry, Point3D center, bool forGuild)
    {
        if (HousePlacement.Check(bot, entry.MultiID, center, out var toMove, entry.HouseDirection) != HousePlacementResult.Valid)
        {
            return null;
        }

        var house = entry.ConstructHouse(bot);
        if (house == null)
        {
            return null;
        }

        house.Price = entry.Cost;

        if (!Banker.Withdraw(bot, entry.Cost))
        {
            house.RemoveKeys(bot);
            house.Delete();
            return null;
        }

        house.MoveToWorld(center, bot.Map);

        foreach (var entity in toMove)
        {
            if (entity is Mobile m)
            {
                m.Location = house.BanLocation;
            }
            else if (entity is Item item)
            {
                item.Location = house.BanLocation;
            }
        }

        if (forGuild && bot.Guild is Guild guild && house.Sign != null)
        {
            house.Sign.Name = $"{GuildHousePrefix}{guild.Name}";
            AdmitGuild(house, guild);
        }

        Invalidate();
        return house;
    }

    /// <summary>Makes every member of the guild a friend of its house, so doors and the shared
    /// chests open for them. Called on building and whenever a member comes home.</summary>
    public static void AdmitGuild(BaseHouse house, Guild guild)
    {
        house.Friends ??= [];

        foreach (var member in guild.Members)
        {
            if (member != house.Owner && !house.Friends.Contains(member))
            {
                house.Friends.Add(member);
            }
        }
    }

    /// <summary>A spot inside the house to stand: the floor beside the house's front door is
    /// always reachable.</summary>
    public static Point3D Inside(BaseHouse house)
    {
        foreach (var point in FloorPoints(house))
        {
            return point;
        }

        return house.BanLocation;
    }

    /// <summary>Ground-floor cells of the house with the Z a walker stands at there.</summary>
    public static IEnumerable<Point3D> FloorPoints(BaseHouse house)
    {
        var mcl = house.Components;
        var map = house.Map;
        if (map == null)
        {
            yield break;
        }

        for (var lx = 1; lx < mcl.Width - 1; lx++)
        {
            for (var ly = 1; ly < mcl.Height - 1; ly++)
            {
                var floorZ = int.MaxValue;
                foreach (var tile in mcl.Tiles[lx][ly])
                {
                    var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
                    if (data.Surface && tile.Z + data.CalcHeight < floorZ)
                    {
                        floorZ = tile.Z + data.CalcHeight;
                    }
                }

                if (floorZ == int.MaxValue)
                {
                    continue;
                }

                var p = new Point3D(house.X + mcl.Min.X + lx, house.Y + mcl.Min.Y + ly, house.Z + floorZ);
                if (house.IsInside(p, 16))
                {
                    yield return p;
                }
            }
        }
    }

    /// <summary>A deleted bot's houses: a guild house passes to another member of the guild,
    /// anything else is pulled down rather than left to stand empty for weeks.</summary>
    public static void OnOwnerDeleted(BotMobile bot)
    {
        // A city apartment simply goes back on sale.
        if (BotCityHomes.CityHome(bot) is { } apartment)
        {
            apartment.ReleaseAll();
            apartment.ReturnAddons(null);
            apartment.Owner = null;
            apartment.Friends.Clear();
            apartment.Bans.Clear();
            BotCityHomes.FindSign(apartment)?.RefreshName();
        }

        foreach (var house in BaseHouse.GetHouses(bot))
        {
            if (house.Deleted)
            {
                continue;
            }

            if (IsGuildHouse(house) && bot.Guild is Guild guild && NextOwner(guild, bot) is { } heir)
            {
                house.Owner = heir;
                house.Friends?.Remove(heir);
                continue;
            }

            house.Delete();
        }

        Invalidate();
    }

    private static Mobile NextOwner(Guild guild, Mobile leaving)
    {
        foreach (var member in guild.Members)
        {
            if (member != leaving && member is BotMobile { Deleted: false })
            {
                return member;
            }
        }

        return null;
    }
}
