using System;
using Server.Engines.MLQuests.Definitions;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.MLQuests;

/// <summary>
///     Ставит в мир квестодателей, у которых в ServUO была точка спавна: без этого квесты есть, но взять
///     их не у кого. Точки взяты из спавн-файлов ServUO (trammel.xml / felucca.xml / ilshenar.xml), а для
///     трёх NPC без записи в спавне (Kane, Дерек Мерчант, Джек-ростовщик) выбраны места рядом с банком
///     Британии, как в их текстах. Проверка идемпотентная: если такой NPC уже стоит рядом, второй не
///     создаётся — на существующих сейвах мира ничего не задублируется.
/// </summary>
public static class QuestGiverPlacement
{
    private static readonly ILogger Logger = LogFactory.GetLogger(typeof(QuestGiverPlacement));

    private readonly record struct Spot(Type Quester, Map Map, Point3D Location);

    private static readonly Spot[] Spots =
    {
        new(typeof(Verity), Map.Trammel, new Point3D(1416, 1604, 30)),
        new(typeof(Verity), Map.Felucca, new Point3D(1416, 1604, 30)),
        new(typeof(Sarakki), Map.Trammel, new Point3D(1422, 1619, 20)),
        new(typeof(Sarakki), Map.Felucca, new Point3D(1422, 1619, 20)),
        new(typeof(Petrus), Map.Ilshenar, new Point3D(1420, 407, 9)),
        new(typeof(Danoel), Map.Trammel, new Point3D(6282, 118, 0)),
        new(typeof(Danoel), Map.Felucca, new Point3D(6282, 118, 0)),
        new(typeof(Kane), Map.Trammel, new Point3D(1489, 1633, 20)),
        new(typeof(DerekMerchant), Map.Trammel, new Point3D(1489, 1637, 20)),
        new(typeof(JackLoanShark), Map.Trammel, new Point3D(1489, 1641, 20))
    };

    public static void Initialize()
    {
        EventSink.ServerStarted += OnServerStarted;
    }

    private static void OnServerStarted() => Timer.DelayCall(TimeSpan.FromSeconds(10), EnsureSetup);

    private static void EnsureSetup()
    {
        var placed = 0;

        foreach (var spot in Spots)
        {
            try
            {
                if (spot.Map == null || HasQuester(spot))
                {
                    continue;
                }

                if (Activator.CreateInstance(spot.Quester) is not BaseCreature creature)
                {
                    continue;
                }

                creature.MoveToWorld(FindFree(spot.Map, spot.Location), spot.Map);
                placed++;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Quest giver placement failed for {Quester}", spot.Quester.Name);
            }
        }

        if (placed > 0)
        {
            Logger.Information("Quest giver placement: {Count} NPC placed", placed);
        }
    }

    private static bool HasQuester(Spot spot)
    {
        foreach (var m in spot.Map.GetMobilesInRange(spot.Location, 15))
        {
            if (m.GetType() == spot.Quester)
            {
                return true;
            }
        }

        return false;
    }

    private static Point3D FindFree(Map map, Point3D location)
    {
        if (map.CanSpawnMobile(location))
        {
            return location;
        }

        for (var radius = 1; radius <= 8; radius++)
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
