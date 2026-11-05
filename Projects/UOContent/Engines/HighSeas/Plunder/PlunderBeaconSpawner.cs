/// <summary>
///     Портировано из ServUO (Scripts/Services/Seasonal Events/RisingTide/PlunderBeaconSpawner.cs).
///
///     Расписание пиратских маяков: раз в минуту проверяет, что в каждой морской зоне стоит не меньше
///     положенного числа плотов, и достраивает недостающие.
///
///     Отличия от исходника:
///     <list type="bullet">
///         <item>
///             В ServUO спавнер был живым объектом с <c>Serialize</c>/<c>Deserialize</c> и запускался
///             из сезонного контроллера. Здесь это статический класс: список плотов восстанавливается
///             сам — каждый <see cref="PlunderBeaconAddon" /> регистрируется при постановке в мир и при
///             загрузке, поэтому отдельный сейв не нужен.
///         </item>
///         <item>
///             <c>Map.GetRandomSpawnPoint</c> и <c>SpecialFishingNet.ValidateDeepWater</c> в этом
///             движке недоступны (первого нет, второй <c>private</c>), поэтому точка ищется вручную:
///             случайная точка прямоугольника, глубокая вода по тем же ID земли, что у
///             <c>SpecialFishingNet</c>, и свободные 15×15 клеток от лодок и игроков.
///         </item>
///         <item>
///             Спавнер включается только вместе с сезоном Rising Tide
///             (<see cref="RisingTideData.Enabled" />) и запускается командой
///             <c>[GenerateRisingTide</c> — сам по себе он мир не заполняет.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Engines.SeasonalEvents;
using Server.Mobiles;
using Server.Multis;

namespace Server.Engines.HighSeas;

public static class PlunderBeaconSpawner
{
    /// <summary>Морские зоны, в которых стоят маяки (в ServUO — <c>PlunderZone</c>).</summary>
    public enum PlunderZone
    {
        None = -1,
        Tram,
        Fel,
        Tokuno1,
        Tokuno2,
        Tokuno3,
        Tokuno4
    }

    /// <summary>Сколько маяков держится в каждой зоне (в ServUO — <c>_SpawnCount</c>).</summary>
    private static readonly int[] _spawnCount = [5, 5, 3, 3, 3, 3];

    /// <summary>Прямоугольники зон (в ServUO — <c>_Zones</c>).</summary>
    private static readonly Rectangle2D[] _zones =
    [
        new(1574, 3620, 766, 465), // Tram
        new(1574, 3620, 766, 465), // Fel
        new(403, 843, 80, 335),    // Tokuno1
        new(631, 20, 189, 110),    // Tokuno2
        new(1037, 20, 190, 150),   // Tokuno3
        new(1274, 977, 141, 221)   // Tokuno4
    ];

    /// <summary>ID глубокой воды — те же, что использует <c>SpecialFishingNet</c>.</summary>
    private static readonly int[] _waterTiles = [0x00A8, 0x00AB, 0x0136, 0x0137];

    /// <summary>Высота, на которой ставится плот (в ServUO — <c>p.Z = -5</c>).</summary>
    private const int SeaLevel = -5;

    /// <summary>Радиус свободной воды вокруг плота (в ServUO — 7 клеток).</summary>
    private const int FreeWaterRange = 7;

    private static readonly List<PlunderBeaconAddon>[] _beacons =
    [
        [], [], [], [], [], []
    ];

    private static TimerExecutionToken _timerToken;

    private static bool _active;

    /// <summary>Работает ли расписание (в ServUO — наличие <c>Spawner</c>).</summary>
    public static bool Active => _active;

    /// <summary>Сколько маяков сейчас стоит в зоне.</summary>
    public static int CountInZone(PlunderZone zone)
    {
        var index = (int)zone;

        return index >= 0 && index < _beacons.Length ? _beacons[index].Count : 0;
    }

    /// <summary>
    ///     Включает расписание. Вызывается вместе с сезоном Rising Tide
    ///     (<c>RisingTideGeneration.Generate</c>).
    /// </summary>
    public static void Activate()
    {
        if (_active)
        {
            return;
        }

        _active = true;

        Timer.StartTimer(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), CheckSpawn, out _timerToken);
    }

    /// <summary>Выключает расписание и убирает все маяки (в ServUO — <c>SystemDeactivate</c>).</summary>
    public static void Deactivate()
    {
        _active = false;
        _timerToken.Cancel();

        for (var zone = 0; zone < _beacons.Length; zone++)
        {
            var list = _beacons[zone];

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var beacon = list[i];

                if (beacon?.Deleted == false)
                {
                    beacon.Delete();
                }
            }

            list.Clear();
        }
    }

    public static void Register(PlunderBeaconAddon beacon)
    {
        if (beacon?.Deleted != false)
        {
            return;
        }

        var zone = (int)FindZone(beacon.Location);

        if (zone < 0)
        {
            return;
        }

        var list = _beacons[zone];

        if (!list.Contains(beacon))
        {
            list.Add(beacon);
        }
    }

    public static void Unregister(PlunderBeaconAddon beacon)
    {
        if (beacon == null)
        {
            return;
        }

        for (var zone = 0; zone < _beacons.Length; zone++)
        {
            _beacons[zone].Remove(beacon);
        }
    }

    /// <summary>Достраивает недостающие маяки во всех зонах (в ServUO — <c>CheckSpawn</c>).</summary>
    public static void CheckSpawn()
    {
        if (!_active || !RisingTideData.Enabled)
        {
            return;
        }

        for (var zone = 0; zone < _beacons.Length; zone++)
        {
            var missing = _spawnCount[zone] - _beacons[zone].Count;

            if (missing > 0)
            {
                Spawn((PlunderZone)zone, missing);
            }
        }
    }

    /// <summary>Ставит <paramref name="amount" /> маяков в зоне (в ServUO — <c>Spawn</c>).</summary>
    public static void Spawn(PlunderZone zone, int amount)
    {
        var index = (int)zone;

        if (index < 0 || index >= _zones.Length)
        {
            return;
        }

        var map = zone switch
        {
            PlunderZone.Fel          => Map.Felucca,
            var z when z > PlunderZone.Fel => Map.Tokuno,
            _                        => Map.Trammel
        };

        if (map == null)
        {
            return;
        }

        for (var i = 0; i < amount; i++)
        {
            var spawn = FindSpawnPoint(map, _zones[index]);

            if (spawn == Point3D.Zero)
            {
                return;
            }

            var beacon = new PlunderBeaconAddon();
            beacon.MoveToWorld(spawn, map);

            _beacons[index].Add(beacon);
        }
    }

    private static PlunderZone FindZone(Point3D location)
    {
        for (var i = 0; i < _zones.Length; i++)
        {
            if (_zones[i].Contains(new Point2D(location.X, location.Y)))
            {
                return (PlunderZone)i;
            }
        }

        return PlunderZone.None;
    }

    /// <summary>Свободная глубокая вода в зоне; <c>Point3D.Zero</c> — места не нашлось.</summary>
    private static Point3D FindSpawnPoint(Map map, Rectangle2D zone)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var x = Utility.Random(zone.X, zone.Width);
            var y = Utility.Random(zone.Y, zone.Height);

            if (!IsDeepWater(map, x, y))
            {
                continue;
            }

            var point = new Point3D(x, y, SeaLevel);

            if (!HasFreeWater(map, point))
            {
                continue;
            }

            return point;
        }

        return Point3D.Zero;
    }

    private static bool HasFreeWater(Map map, Point3D point)
    {
        if (BaseBoat.FindBoatAt(point, map) != null)
        {
            return false;
        }

        foreach (var _ in map.GetMultisInRange<BaseBoat>(point, FreeWaterRange))
        {
            return false;
        }

        var bounds = new Rectangle2D(point.X - FreeWaterRange, point.Y - FreeWaterRange, 15, 15);

        foreach (var mobile in map.GetMobilesInBounds<PlayerMobile>(bounds, true))
        {
            if (!mobile.Deleted)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDeepWater(Map map, int x, int y)
    {
        var tileID = map.Tiles.GetLandTile(x, y).ID;

        for (var i = 0; i < _waterTiles.Length; i += 2)
        {
            if (tileID >= _waterTiles[i] && tileID <= _waterTiles[i + 1])
            {
                return true;
            }
        }

        return false;
    }
}
