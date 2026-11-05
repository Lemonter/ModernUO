using System;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.CleanUpBritannia;

/// <summary>
///     Ставит в мир офицера чистоты и камень обмена очками. В ServUO их размещали вручную или спавнером
///     городов; здесь они появляются сами при старте мира, как и остальной контент, — один офицер и один
///     камень на Трамеле и Фелукке, рядом с банком Британии. Проверка идемпотентная: если офицер уже
///     стоит, второй не создаётся.
/// </summary>
public static class CleanUpBritanniaSetup
{
    private static readonly ILogger Logger = LogFactory.GetLogger(typeof(CleanUpBritanniaSetup));

    private static readonly Point3D OfficerLocation = new(1495, 1629, 20);
    private static readonly Point3D StoneLocation = new(1499, 1629, 20);

    public static void Initialize()
    {
        EventSink.ServerStarted += OnServerStarted;
    }

    private static void OnServerStarted() => Timer.DelayCall(TimeSpan.FromSeconds(9), EnsureSetup);

    private static void EnsureSetup()
    {
        try
        {
            Ensure(Map.Trammel);
            Ensure(Map.Felucca);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "CleanUp Britannia placement failed");
        }
    }

    private static void Ensure(Map map)
    {
        if (map == null || HasOfficer(map))
        {
            return;
        }

        var officer = new TheCleanupOfficer();
        officer.MoveToWorld(FindFree(map, OfficerLocation), map);

        var stone = new PointExchanceStone();
        stone.MoveToWorld(FindFree(map, StoneLocation), map);

        Logger.Information("CleanUp Britannia: officer and exchange stone placed on {Map}", map);
    }

    private static bool HasOfficer(Map map)
    {
        foreach (var m in map.GetMobilesInRange(OfficerLocation, 12))
        {
            if (m is TheCleanupOfficer)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Ищет свободную клетку рядом с точкой: банк Британии может быть заставлен.</summary>
    private static Point3D FindFree(Map map, Point3D location)
    {
        if (map.CanSpawnMobile(location))
        {
            return location;
        }

        for (var radius = 1; radius <= 6; radius++)
        {
            for (var x = -radius; x <= radius; x++)
            {
                for (var y = -radius; y <= radius; y++)
                {
                    var point = new Point3D(location.X + x, location.Y + y, map.GetAverageZ(location.X + x, location.Y + y));

                    if (map.CanSpawnMobile(point))
                    {
                        return point;
                    }
                }
            }
        }

        return location;
    }
}
