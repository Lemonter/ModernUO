/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Setup.cs).
///
///     ServUO only ever placed this content through the <c>[SetupCityLoyaltySystem</c> admin command. Because the
///     cities, their stones and their officials are ordinary world content the shard owner wants live without
///     running a command, an idempotent auto-setup runs shortly after <c>ServerStarted</c> — the same pattern as
///     <c>Engines/ExploringTheDeep/Generate.cs</c> (<c>EnsureSetup</c>/<c>HasWinch</c>).
///
///     Presence check: for each city, an existing <see cref="CityStone" /> within two tiles of the city's
///     <c>StoneLocation</c> on <see cref="CityLoyaltySystem.SystemMap" /> means that city is already built, so only
///     missing cities are created. The same check guards the three Felucca <see cref="SlimTheFence" /> placements.
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             <c>Region.GetEnumeratedMobiles()</c>/<c>GetEnumeratedItems()</c> do not exist — the presence check uses
///             spatial queries (<c>Map.GetItemsInRange</c>/<c>Map.GetMobilesInRange</c>) as this codebase requires.
///         </item>
///         <item><c>Console.WriteLine</c> for a missing region became a logged warning.</item>
///         <item><c>m.SendMessage</c> is only used when the command was run by a player.</item>
///     </list>
/// </summary>
using System;
using Server.Commands;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.CityLoyalty;

public static class CityLoyaltySetup
{
    public static readonly string EntityName = "cityloyalty";

    private static readonly ILogger Logger = LogFactory.GetLogger(typeof(CityLoyaltySetup));

    public static void Initialize()
    {
        if (!CityLoyaltySystem.Enabled)
        {
            return;
        }

        CommandSystem.Register("SetupCityLoyaltySystem", AccessLevel.GameMaster, SetupCommand);
        CommandSystem.Register("DeleteCityLoyaltySystem", AccessLevel.GameMaster, DeleteCommand);

        EventSink.ServerStarted += OnServerStarted;
    }

    private static void OnServerStarted() => Timer.StartTimer(TimeSpan.FromSeconds(15), EnsureSetup);

    private static void EnsureSetup()
    {
        try
        {
            var created = Setup(null);

            if (created > 0)
            {
                Logger.Information("City Loyalty content is set up ({Count} cities placed)", created);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "City Loyalty setup failed");
        }
    }

    public static void SetupCommand(CommandEventArgs e) => Setup(e.Mobile);

    public static void DeleteCommand(CommandEventArgs e)
    {
        foreach (var city in CityLoyaltySystem.Cities)
        {
            DeleteNear(city.Definition.StoneLocation, 2);
            DeleteNear(city.Definition.TradeMinisterLocation, 4);
            DeleteNear(city.Definition.HeraldLocation, 2);
            DeleteNear(city.Definition.GuardsmanLocation, 2);
            DeleteNear(city.Definition.BoardLocation, 2);

            city.Stone = null;
            city.Herald = null;
            city.Captain = null;
            city.Minister = null;
            city.Board = null;
            city.CanUtilize = false;
        }

        e.Mobile?.SendMessage("Содержимое системы лояльности городов удалено.");
    }

    private static void DeleteNear(Point3D location, int range)
    {
        var items = new System.Collections.Generic.List<Item>();
        var mobiles = new System.Collections.Generic.List<Mobile>();

        foreach (var item in CityLoyaltySystem.SystemMap.GetItemsInRange(location, range))
        {
            if (item is CityStone or BoxOfRopes or CityMessageBoard or CityDonationItem or CityBanner)
            {
                items.Add(item);
            }
        }

        foreach (var mobile in CityLoyaltySystem.SystemMap.GetMobilesInRange(location, range))
        {
            if (mobile is TradeMinister or CityHerald or GuardCaptain)
            {
                mobiles.Add(mobile);
            }
        }

        foreach (var item in items)
        {
            item.Delete();
        }

        foreach (var mobile in mobiles)
        {
            mobile.Delete();
        }
    }

    /// <summary>Places any city that is not already built. Returns the number of cities placed.</summary>
    public static int Setup(Mobile caller)
    {
        var placed = 0;

        foreach (var city in CityLoyaltySystem.Cities)
        {
            if (HasStone(city.Definition.StoneLocation))
            {
                city.CanUtilize = true;
                continue;
            }

            var minister = new TradeMinister(city.City);
            var herald = new CityHerald(city.City);
            var captain = new GuardCaptain(city.City);
            var stone = new CityStone(city);
            var itemDonation = new CityItemDonation(city.City, minister);
            var petDonation = new CityPetDonation(city.City, minister);
            var box = new BoxOfRopes(city.City);
            var board = new CityMessageBoard(city.City, 0xA0C5);

            city.Minister = minister;
            minister.MoveToWorld(city.Definition.TradeMinisterLocation, CityLoyaltySystem.SystemMap);

            city.Herald = herald;
            herald.MoveToWorld(city.Definition.HeraldLocation, CityLoyaltySystem.SystemMap);

            city.Captain = captain;
            captain.MoveToWorld(city.Definition.GuardsmanLocation, CityLoyaltySystem.SystemMap);

            city.Stone = stone;
            stone.MoveToWorld(city.Definition.StoneLocation, CityLoyaltySystem.SystemMap);

            var ministerLocation = city.Definition.TradeMinisterLocation;
            itemDonation.MoveToWorld(
                new Point3D(ministerLocation.X, ministerLocation.Y - 1, ministerLocation.Z),
                CityLoyaltySystem.SystemMap
            );
            petDonation.MoveToWorld(
                new Point3D(ministerLocation.X, ministerLocation.Y - 2, ministerLocation.Z),
                CityLoyaltySystem.SystemMap
            );

            var guardsmanLocation = city.Definition.GuardsmanLocation;
            box.MoveToWorld(
                new Point3D(guardsmanLocation.X, guardsmanLocation.Y - 1, guardsmanLocation.Z),
                CityLoyaltySystem.SystemMap
            );

            city.Board = board;
            board.MoveToWorld(city.Definition.BoardLocation, CityLoyaltySystem.SystemMap);

            city.CanUtilize = true;
            placed++;

            caller?.SendMessage($"Город {city.Definition.Name} подготовлен!");
        }

        PlaceSlims(caller);

        return placed;
    }

    private static void PlaceSlims(Mobile caller)
    {
        var spots = new[]
        {
            (Name: "Ocllo", Location: new Point3D(3674, 2648, 0)),
            (Name: "Nujel'm", Location: new Point3D(3765, 1219, 0)),
            (Name: "Serpent's Hold", Location: new Point3D(3017, 3452, 15))
        };

        foreach (var (name, location) in spots)
        {
            if (HasNearbyMobile<SlimTheFence>(location, 2, Map.Felucca))
            {
                continue;
            }

            var region = Region.Regions.Find(reg => reg.Map == Map.Felucca && reg.Name == name);

            if (region == null)
            {
                Logger.Warning("City Loyalty: region {Region} not found on Felucca", name);
                continue;
            }

            new SlimTheFence().MoveToWorld(location, Map.Felucca);
            caller?.SendMessage($"Слим размещён: {name}.");
        }
    }

    private static bool HasStone(Point3D location) => HasNearby<CityStone>(location, 2, CityLoyaltySystem.SystemMap);

    private static bool HasNearby<T>(Point3D location, int range, Map map) where T : Item
    {
        foreach (var item in map.GetItemsInRange(location, range))
        {
            if (item is T)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasNearbyMobile<T>(Point3D location, int range, Map map) where T : Mobile
    {
        foreach (var mobile in map.GetMobilesInRange(location, range))
        {
            if (mobile is T)
            {
                return true;
            }
        }

        return false;
    }
}
