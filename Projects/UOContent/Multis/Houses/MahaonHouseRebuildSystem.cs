using Server.Items;
using Server.Mobiles;
using Server.Systems.MahaonWorld;

namespace Server.Multis;

/// <summary>
///     "Перестройка" — replace a house with a different type at the exact same anchor
///     point. Deliberately skips HousePlacement.Check's terrain validation (the old house
///     already occupies this exact footprint, so re-validating a new footprint against
///     itself is more trouble than it's worth) — the tradeoff, confirmed with the project
///     owner, is that this always fully demolishes the old house first: any locked-down
///     items, secures, an anvil sitting on the floor, all of it is gone. The gump this is
///     called from shows an explicit warning before this ever runs.
/// </summary>
public static class MahaonHouseRebuildSystem
{
    /// <summary>Attempts the swap. Returns a Russian status message either way. Never
    /// charges or demolishes anything if it's going to fail afterward — cost is charged
    /// and the new house is actually constructed before the old one is touched.</summary>
    public static string Rebuild(Mobile from, BaseHouse oldHouse, HousePlacementEntry newEntry)
    {
        if (oldHouse?.Deleted != false || oldHouse.Map == null)
        {
            return "Дом недоступен для перестройки.";
        }

        if (!oldHouse.IsOwner(from))
        {
            return "Перестраивать дом может только его владелец.";
        }

        var cost = newEntry.Cost;

        if (from.AccessLevel < AccessLevel.GameMaster && !Banker.Withdraw(from, cost))
        {
            return $"Не хватает золота — новый дом стоит {cost}.";
        }

        var newHouse = newEntry.ConstructHouse(from);

        if (newHouse == null)
        {
            // Refund — nothing was actually built, don't keep the gold.
            Banker.Deposit(from, cost);
            return "Не удалось построить новый дом — ничего не списано.";
        }

        newHouse.Price = cost;

        var center = oldHouse.Location;
        var map = oldHouse.Map;

        var owner = oldHouse.Owner;
        var coOwners = oldHouse.CoOwners;
        var friends = oldHouse.Friends;
        var bans = oldHouse.Bans;
        var isPublic = oldHouse.Public;

        MahaonHouseFenceSystem.Remove(oldHouse); // old fence was shaped for the old footprint

        oldHouse.RemoveKeys(from);
        oldHouse.Delete(); // everything inside — locked down or not — goes with it

        newHouse.MoveToWorld(center, map);
        newHouse.Owner = owner;

        if (coOwners != null)
        {
            newHouse.CoOwners = new System.Collections.Generic.List<Mobile>(coOwners);
        }

        if (friends != null)
        {
            newHouse.Friends = new System.Collections.Generic.List<Mobile>(friends);
        }

        if (bans != null)
        {
            newHouse.Bans = new System.Collections.Generic.List<Mobile>(bans);
        }

        newHouse.Public = isPublic;

        return $"Дом перестроен. Списано {cost} золота.";
    }
}
