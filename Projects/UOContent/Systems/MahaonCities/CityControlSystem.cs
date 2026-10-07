using System.Collections.Generic;
using Server.Commands;
using Server.Guilds;
using Server.Mobiles;

namespace Server.Systems.MahaonCities;

/// <summary>
///     Mahaon city control: a guild can capture a city, collect a tax rate from it, and
///     station guards there that patrol normally (no teleporting town-guard nonsense) and
///     fight members of guilds it's at war with — reusing the engine's own
///     Guild.Enemies/war-declaration system rather than inventing a parallel rivalry
///     concept.
/// </summary>
public class CityControlSystem : GenericPersistence
{
    private static CityControlSystem _instance;

    // Real canonical Felucca town-center coordinates — the classic "starting city" inn
    // locations used across RunUO/ServUO-family shards. Good stand-ins for "town center"
    // until/unless the real Mahaon layout differs.
    public static readonly Dictionary<string, (Point3D spawn, Map map)> Cities = new()
    {
        ["Yew"] = (new Point3D(633, 858, 0), Map.Felucca),
        ["Minoc"] = (new Point3D(2476, 413, 15), Map.Felucca),
        ["Britain"] = (new Point3D(1496, 1628, 10), Map.Felucca),
        ["Moonglow"] = (new Point3D(4408, 1168, 0), Map.Felucca),
        ["Trinsic"] = (new Point3D(1845, 2745, 0), Map.Felucca),
        ["Magincia"] = (new Point3D(3734, 2222, 20), Map.Felucca),
        ["Jhelom"] = (new Point3D(1374, 3826, 0), Map.Felucca),
        ["Skara Brae"] = (new Point3D(618, 2234, 0), Map.Felucca),
        ["Vesper"] = (new Point3D(2771, 976, 0), Map.Felucca),
        ["Ocllo"] = (new Point3D(3667, 2625, 0), Map.Felucca)
    };

    private const int GuardsPerCity = 6;

    // How far a guard is allowed to wander from its post while patrolling — the stock
    // WalkRandomWithHome AI (Mobiles/AI/BaseAI/WalkRandomLogic.cs) already walks a mobile
    // back toward Home once it exceeds RangeHome, no teleporting involved, so this alone is
    // what keeps guards inside the city instead of drifting into the wilderness.
    public const int GuardPatrolRadius = 20;

    private static readonly Dictionary<string, Guild> Control = new();
    private static readonly Dictionary<string, int> TaxRate = new();

    public CityControlSystem() : base("MahaonCityControl", 1)
    {
    }

    public static void Configure()
    {
        _instance = new CityControlSystem();

        CommandSystem.Register("ClaimCity", AccessLevel.Player, ClaimCity_OnCommand);
        CommandSystem.Register("SetCityTax", AccessLevel.Player, SetCityTax_OnCommand);
        CommandSystem.Register("SetCityCenter", AccessLevel.GameMaster, SetCityCenter_OnCommand);
        CommandSystem.Register("CityStatus", AccessLevel.Player, CityStatus_OnCommand);
    }

    public static void Initialize()
    {
        // Apply any hand-marked city centers over the guessed defaults — see
        // [MarkCitySpot <city> center. Also runs here so a restart doesn't lose it.
        foreach (var city in new List<string>(Cities.Keys))
        {
            if (CityMarkers.TryGetMarker(city, "center", out var loc, out var map))
            {
                Cities[city] = (loc, map);
            }
        }
    }

    [Usage("SetCityCenter <city>")]
    [Description("Overrides a city's center point with your current location — use this if the built-in coordinates land somewhere wrong/empty on your map.")]
    private static void SetCityCenter_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Length < 1 || !Cities.ContainsKey(e.GetString(0)))
        {
            from.SendMessage($"Использование: [SetCityCenter <{string.Join("|", Cities.Keys)}>");
            return;
        }

        var city = e.GetString(0);
        Cities[city] = (from.Location, from.Map);

        // Persist it the same way [MarkCitySpot does, so it survives a restart too.
        CityMarkers.SetMarker(city, "center", from.Location, from.Map);

        from.SendMessage(0x59, $"Центр города {city} обновлён на твою текущую позицию. Запусти [SeedCities заново, чтобы расставить торговцев по новому месту.");
    }

    public static Guild GetController(string city) => Control.GetValueOrDefault(city);

    public static int GetTaxRate(string city) => TaxRate.GetValueOrDefault(city, 0);

    /// <summary>
    ///     Whether a city's guards should treat this mobile as hostile: a member of a guild
    ///     at war with the controlling guild, by a war declared through the guild gump or set
    ///     between bot guilds.
    /// </summary>
    public static bool IsHostileToCity(string city, Mobile m)
    {
        var controller = GetController(city);
        if (controller == null || m is not PlayerMobile pm || pm.Guild is not Guild g || g == controller)
        {
            return false;
        }

        // Whoever is claiming the city at its banner is fought by its guards.
        return Items.MahaonCityClaimPoint.Of(city) is { Contested: true } point && point.Contender == g ||
               controller.Enemies.Contains(g) || g.Enemies.Contains(controller) ||
               MahaonBots.BotGuilds.GetRelation(controller.Name, g.Name) == MahaonBots.BotGuildRelation.War;
    }

    private static bool IsCityVulnerable(string city)
    {
        foreach (var guard in CityGuard.Of(city))
        {
            if (!guard.Deleted && guard.Alive)
            {
                return false;
            }
        }

        return true;
    }

    public static void Capture(string city, Guild guild)
    {
        DespawnGuards(city);
        Control[city] = guild;
        TaxRate[city] = 0;
        SpawnGuards(city, guild);
        Server.Systems.MahaonAi.MahaonForumBridge.OnCityCaptured(city, guild);

        // The site's city map would otherwise show the old owner until the next scheduled
        // snapshot — up to 20 minutes of being plainly wrong about the one thing that page
        // exists to show.
        Server.Systems.MahaonAi.MahaonWorldSnapshotBridge.PushNow();
    }

    private static void SpawnGuards(string city, Guild guild)
    {
        if (!Cities.TryGetValue(city, out var info))
        {
            return;
        }

        var markedSpots = CityMarkers.GetMarkersByPrefix(city, "guard");

        if (markedSpots.Count > 0)
        {
            foreach (var (loc, map) in markedSpots)
            {
                var guard = new CityGuard(city, guild);
                guard.MoveToWorld(loc, map);
                guard.StationAt(loc, GuardPatrolRadius);
            }

            return;
        }

        // No hand-marked posts for this city yet — fall back to a guessed ring around the
        // center point. Mark real spots with [MarkCitySpot <city> guard1 (guard2, ...) and
        // this branch stops being used for that city.
        var angleStep = 360.0 / GuardsPerCity;

        for (var i = 0; i < GuardsPerCity; i++)
        {
            var angle = i * angleStep + Utility.RandomMinMax(-10, 10);
            var radius = Utility.RandomMinMax(12, 22);
            var radians = angle * System.Math.PI / 180.0;

            var loc = FindGuardSpot(
                info.spawn.X + (int)(System.Math.Cos(radians) * radius),
                info.spawn.Y + (int)(System.Math.Sin(radians) * radius),
                info.map
            );

            var guard = new CityGuard(city, guild);
            guard.MoveToWorld(loc, info.map);
            guard.StationAt(loc, GuardPatrolRadius);
        }
    }

    private static Point3D FindGuardSpot(int x, int y, Map map)
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            var z = map.GetAverageZ(x, y);
            var candidate = new Point3D(x, y, z);

            if (map.CanSpawnMobile(candidate))
            {
                return candidate;
            }

            // Nudge slightly and try again — a crude way to avoid spawning inside a wall.
            x += Utility.RandomMinMax(-3, 3);
            y += Utility.RandomMinMax(-3, 3);
        }

        return new Point3D(x, y, map.GetAverageZ(x, y));
    }

    private static void DespawnGuards(string city)
    {
        foreach (var guard in new List<CityGuard>(CityGuard.Of(city)))
        {
            guard.Delete();
        }
    }

    [Usage("ClaimCity <city>")]
    [Description("Lays your guild's claim to a city at its banner; held for ten minutes, the city is yours.")]
    private static void ClaimCity_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (from is not PlayerMobile pm || pm.Guild is not Guild guild)
        {
            from.SendMessage("Чтобы захватить город, нужно состоять в гильдии.");
            return;
        }

        if (guild.Leader != from)
        {
            from.SendMessage("Захватить город может только лидер гильдии.");
            return;
        }

        if (e.Length < 1 || !Cities.ContainsKey(e.GetString(0)))
        {
            from.SendMessage($"Использование: [ClaimCity <{string.Join("|", Cities.Keys)}>");
            return;
        }

        var city = e.GetString(0);

        if (GetController(city) == guild)
        {
            from.SendMessage("Твоя гильдия уже контролирует этот город.");
            return;
        }

        if (Items.MahaonCityClaimPoint.Of(city) is not { } point)
        {
            from.SendMessage($"В городе {city} нет знамени — захватить его нельзя.");
            return;
        }

        if (point.Map != from.Map || !from.InRange(point.GetWorldLocation(), Items.MahaonCityClaimPoint.ClaimRange))
        {
            from.SendMessage("Город берут у его знамени — подойди к нему.");
            return;
        }

        if (point.Contested)
        {
            from.SendMessage($"{city} уже оспаривает гильдия {point.Contender.Name}.");
            return;
        }

        point.StartContest(guild);
        from.SendMessage(0x59, "Права заявлены. Продержитесь у знамени десять минут.");
    }

    [Usage("SetCityTax <city> <percent>")]
    [Description("Sets the tax rate for a city your guild controls (0-30).")]
    private static void SetCityTax_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (from is not PlayerMobile pm || pm.Guild is not Guild guild)
        {
            from.SendMessage("Нужно состоять в гильдии.");
            return;
        }

        if (e.Length < 2 || !Cities.ContainsKey(e.GetString(0)) || !int.TryParse(e.GetString(1), out var percent))
        {
            from.SendMessage("Использование: [SetCityTax <город> <процент>");
            return;
        }

        var city = e.GetString(0);

        if (GetController(city) != guild)
        {
            from.SendMessage("Твоя гильдия не контролирует этот город.");
            return;
        }

        if (guild.Leader != from)
        {
            from.SendMessage("Установить налог может только лидер гильдии.");
            return;
        }

        TaxRate[city] = System.Math.Clamp(percent, 0, 30);
        from.SendMessage(0x59, $"Налог в {city} теперь {TaxRate[city]}%.");
    }

    [Usage("CityStatus")]
    [Description("Shows who controls each city and its tax rate.")]
    private static void CityStatus_OnCommand(CommandEventArgs e)
    {
        foreach (var city in Cities.Keys)
        {
            var controller = GetController(city);
            var status = controller == null
                ? "не контролируется"
                : $"{controller.Name} (налог {GetTaxRate(city)}%){(IsCityVulnerable(city) ? " [уязвим]" : "")}";

            e.Mobile.SendMessage($"{city}: {status}");
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(Control.Count);

        foreach (var (city, guild) in Control)
        {
            writer.Write(city);
            writer.Write(guild);
            writer.WriteEncodedInt(TaxRate.GetValueOrDefault(city, 0));
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var city = reader.ReadString();
            var guild = reader.ReadEntity<Guild>();
            var tax = reader.ReadEncodedInt();

            if (guild != null)
            {
                Control[city] = guild;
                TaxRate[city] = tax;
            }
        }
    }
}
