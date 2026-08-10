using System.Collections.Generic;
using Server.Commands;

namespace Server.Systems.MahaonCities;

/// <summary>
///     Hand-picked location markers. Since nobody here can actually see the map, the only
///     reliable way to know "this is a nice plaza, not someone's living room" is for a
///     person standing in the client to mark it themselves. Seeders should prefer these
///     over guessed/randomized coordinates whenever a marker exists for the given city+label.
/// </summary>
public class CityMarkers : GenericPersistence
{
    private static CityMarkers _instance;

    // Key: (city, label) -> location. Label is freeform, e.g. "vendor1", "guard3",
    // "travelstone", "auctionstone" — whatever the seeder wants to look up.
    private static readonly Dictionary<(string city, string label), (Point3D loc, Map map)> Markers = new();

    public CityMarkers() : base("MahaonCityMarkers", 1)
    {
    }

    public static void Configure()
    {
        _instance = new CityMarkers();
        CommandSystem.Register("MarkCitySpot", AccessLevel.GameMaster, MarkCitySpot_OnCommand);
        CommandSystem.Register("ListCityMarkers", AccessLevel.GameMaster, ListCityMarkers_OnCommand);
        CommandSystem.Register("ClearCityMarker", AccessLevel.GameMaster, ClearCityMarker_OnCommand);
    }

    public static void SetMarker(string city, string label, Point3D loc, Map map) =>
        Markers[(city, label)] = (loc, map);

    public static bool TryGetMarker(string city, string label, out Point3D loc, out Map map)
    {
        if (Markers.TryGetValue((city, label), out var entry))
        {
            loc = entry.loc;
            map = entry.map;
            return true;
        }

        loc = Point3D.Zero;
        map = null;
        return false;
    }

    /// <summary>
    ///     Every marker whose label starts with the given prefix, for a city — e.g. all
    ///     "guard1", "guard2", "guard3" markers for spreading out guard posts.
    /// </summary>
    public static List<(Point3D loc, Map map)> GetMarkersByPrefix(string city, string labelPrefix)
    {
        var result = new List<(Point3D, Map)>();

        foreach (var ((markerCity, label), entry) in Markers)
        {
            if (markerCity == city && label.StartsWith(labelPrefix))
            {
                result.Add(entry);
            }
        }

        return result;
    }

    [Usage("MarkCitySpot <city> <label>")]
    [Description("Marks your current location as a named spot for a city (e.g. [MarkCitySpot Britain vendor1). Seeders will use this instead of guessing.")]
    private static void MarkCitySpot_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Length < 2)
        {
            from.SendMessage("Использование: [MarkCitySpot <город> <метка>");
            return;
        }

        var city = e.GetString(0);
        var label = e.GetString(1);

        if (!CityControlSystem.Cities.ContainsKey(city))
        {
            from.SendMessage($"Неизвестный город. Варианты: {string.Join(", ", CityControlSystem.Cities.Keys)}");
            return;
        }

        Markers[(city, label)] = (from.Location, from.Map);
        from.SendMessage(0x59, $"Точка «{label}» для города {city} сохранена: {from.Location} на {from.Map}.");
    }

    [Usage("ListCityMarkers <city>")]
    [Description("Lists every marked spot for a city.")]
    private static void ListCityMarkers_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Length < 1)
        {
            from.SendMessage("Использование: [ListCityMarkers <город>");
            return;
        }

        var city = e.GetString(0);
        var found = false;

        foreach (var ((markerCity, label), (loc, map)) in Markers)
        {
            if (markerCity != city)
            {
                continue;
            }

            found = true;
            from.SendMessage($"{label}: {loc} на {map}");
        }

        if (!found)
        {
            from.SendMessage($"Для {city} меток пока нет.");
        }
    }

    [Usage("ClearCityMarker <city> <label>")]
    [Description("Removes a marked spot.")]
    private static void ClearCityMarker_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Length < 2)
        {
            from.SendMessage("Использование: [ClearCityMarker <город> <метка>");
            return;
        }

        var city = e.GetString(0);
        var label = e.GetString(1);

        if (Markers.Remove((city, label)))
        {
            from.SendMessage(0x59, $"Метка «{label}» для {city} удалена.");
        }
        else
        {
            from.SendMessage("Такой метки не найдено.");
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(Markers.Count);

        foreach (var ((city, label), (loc, map)) in Markers)
        {
            writer.Write(city);
            writer.Write(label);
            writer.Write(loc);
            writer.Write(map);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var city = reader.ReadString();
            var label = reader.ReadString();
            var loc = reader.ReadPoint3D();
            var map = reader.ReadMap();

            if (map != null)
            {
                Markers[(city, label)] = (loc, map);
            }
        }
    }
}
