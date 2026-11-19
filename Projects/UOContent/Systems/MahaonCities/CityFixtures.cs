using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Multis;
using Server.Systems.MahaonWorld;

namespace Server.Systems.MahaonCities;

/// <summary>
/// Городское хозяйство, которое ставит ГМ: камень (скрижаль) захвата и управления городом и маяк
/// ботов-горожан. [CitySetup показывает, что где стоит; [CitySetup here ставит недостающее в
/// город, где стоит ГМ, на открытое место рядом с ним; [CitySetup all — во все города у их
/// центров; [CitySetup renew — то же, что делает [Decorate: убирает маяки сверх положенного
/// городу, обновляет население и ставит недостающее.
/// </summary>
public class CityFixtures : GenericPersistence
{
    private static CityFixtures _instance;

    // Поднять, если в набор добавится что-то новое и его надо расставить ещё раз.
    // Раньше хозяйство ставилось само при запуске, и сохранение хранило, какая его версия уже
    // стоит. Теперь его ставит [Decorate; поле осталось, чтобы читались старые сохранения.
    private static int _placedVersion;

    // Сколько горожан-ботов держит маяк. Город должен казаться людным: встретить на улице
    // кого-то — обычное дело, а не событие. В столице больше всех, в островных городках меньше.
    private static readonly Dictionary<string, int> Population = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Britain"] = 150,
        ["Magincia"] = 40,
        ["Ocllo"] = 40
    };

    private const int DefaultPopulation = 80;

    // Маяк не встаёт на самое крыльцо трактира, с которого начинают поиск.
    private const int MinFromCenter = 3;

    public CityFixtures() : base("MahaonCityFixtures", 2)
    {
    }

    public static void Configure()
    {
        _instance = new CityFixtures();
        CommandSystem.Register("CitySetup", AccessLevel.GameMaster, CitySetup_OnCommand);
    }

    public static int PopulationOf(string city) => Population.GetValueOrDefault(city, DefaultPopulation);

    /// <summary>
    /// Приводит хозяйство городов к нынешнему: маяк, что держит больше положенного городу (старый
    /// британский на 600), уходит вместе со своими ботами; маяк с меньшим населением получает новое;
    /// недостающее ставится.
    /// </summary>
    public static void Renew(Mobile report)
    {
        foreach (var city in CityControlSystem.Cities.Keys)
        {
            var population = PopulationOf(city);
            foreach (var beacon in BeaconsOf(city))
            {
                if (beacon.TotalLimit > population)
                {
                    report?.SendMessage(0x22, $"{city}: маяк «{beacon.Name}» на {beacon.Location} (лимит {beacon.TotalLimit}) убран вместе с ботами.");
                    beacon.Delete();
                }
                else if (beacon.TotalLimit < population)
                {
                    SetUpBeacon(beacon, city);
                    report?.SendMessage($"{city}: маяк «{beacon.Name}» теперь на {population} ботов.");
                }
            }

            var (center, map) = CityControlSystem.Cities[city];
            Place(city, map, center, report);
        }
    }

    [Usage("CitySetup [here|all|renew]")]
    [Description("Камни захвата городов и маяки ботов: без аргументов — что где стоит; here — поставить недостающее в город, где ты стоишь; all — во все города; renew — заменить чужие маяки городов и поставить недостающее.")]
    private static void CitySetup_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var arg = e.Length > 0 ? e.GetString(0).ToLowerInvariant() : "";

        if (arg == "here")
        {
            if (NearestCity(from) is not { } city)
            {
                from.SendMessage("Ты не в городе.");
                return;
            }

            Place(city, from.Map, from.Location, from);
            return;
        }

        if (arg == "renew")
        {
            Renew(from);
            return;
        }

        if (arg == "all")
        {
            foreach (var city in CityControlSystem.Cities.Keys)
            {
                var (center, map) = CityControlSystem.Cities[city];
                Place(city, map, center, from);
            }

            return;
        }

        foreach (var city in CityControlSystem.Cities.Keys)
        {
            var stone = MahaonCityClaimPoint.Of(city);
            var beacons = BeaconsOf(city);
            var bots = 0;
            var limit = 0;
            foreach (var b in beacons)
            {
                bots += b.OwnedCount();
                limit += b.TotalLimit;
            }

            from.SendMessage(
                $"{city}: камень {(stone == null ? "нет" : stone.Location.ToString())}, маяков {beacons.Count}, ботов {bots}/{limit}"
            );
        }
    }

    // Город, в чьих пределах (или ближе всех к чьему центру) стоит ГМ.
    private static string NearestCity(Mobile m)
    {
        if (CityControlSystem.CityAt(m) is { } inside)
        {
            return inside;
        }

        string best = null;
        var bestDistance = 120.0;
        foreach (var (city, (center, map)) in CityControlSystem.Cities)
        {
            if (map == m.Map && m.GetDistanceToSqrt(center) is var d && d < bestDistance)
            {
                bestDistance = d;
                best = city;
            }
        }

        return best;
    }

    public static List<MahaonBotBeacon> BeaconsOf(string city)
    {
        var list = new List<MahaonBotBeacon>();
        foreach (var beacon in MahaonBotBeacon.All)
        {
            if (!beacon.Deleted && string.Equals(beacon.CityName, city, StringComparison.OrdinalIgnoreCase))
            {
                list.Add(beacon);
            }
        }

        return list;
    }

    /// <summary>
    /// Ставит городу недостающие камень и маяк. Камень — на самую просторную площадку у города, куда
    /// от точки можно дойти: у него сходятся гильдии и стража, бой там идёт большой. Маяк — подальше
    /// от камня, ближе к самому городу: новые горожане не должны появляться посреди битвы.
    /// </summary>
    public static void Place(string city, Map map, Point3D near, Mobile report)
    {
        if (map == null || map == Map.Internal)
        {
            return;
        }

        var survey = new Survey(map, near);

        var stone = MahaonCityClaimPoint.Of(city);
        if (stone == null)
        {
            if (survey.Battlefield(out var open) is { } p)
            {
                stone = new MahaonCityClaimPoint(city);
                stone.MoveToWorld(p, map);
                report?.SendMessage(0x59, $"{city}: камень поставлен на {p}, площадка открыта на {open * 100 / Survey.FieldCells}%, от точки {(int)p.GetDistanceToSqrt(near)} шагов.");
            }
            else
            {
                report?.SendMessage(0x22, $"{city}: не нашлось площадки для камня у {near}.");
            }
        }
        else
        {
            report?.SendMessage($"{city}: камень уже стоит на {stone.Location}.");
        }

        if (BeaconsOf(city).Count > 0)
        {
            report?.SendMessage($"{city}: маяк уже есть.");
            return;
        }

        if (survey.Spot(stone?.Map == map ? stone.Location : null) is not { } spot)
        {
            report?.SendMessage(0x22, $"{city}: не нашлось места для маяка у {near}.");
            return;
        }

        var beacon = new MahaonBotBeacon();
        SetUpBeacon(beacon, city);
        beacon.MoveToWorld(spot, map);
        report?.SendMessage(
            0x59,
            $"{city}: маяк «{beacon.Name}» на {spot}, ботов до {beacon.TotalLimit}" +
            (stone != null ? $", до камня {(int)spot.GetDistanceToSqrt(stone.Location)} шагов." : ".")
        );
    }

    // Состав горожан: четверть воинов, по шестой магов и лучников, четверть ремесленников,
    // остальное торговцы, и в городе побольше — свой вор-другой.
    internal static void SetUpBeacon(MahaonBotBeacon beacon, string city)
    {
        var total = PopulationOf(city);
        beacon.CityName = city;
        beacon.TotalLimit = total;

        var warriors = (int)Math.Round(total * 0.25);
        var mages = Math.Max(1, (int)Math.Round(total / 6.0));
        var archers = Math.Max(1, (int)Math.Round(total / 6.0));
        var crafters = (int)Math.Round(total * 0.25);

        beacon.SetTarget(Mobiles.BotArchetype.Warrior, warriors);
        beacon.SetTarget(Mobiles.BotArchetype.Mage, mages);
        beacon.SetTarget(Mobiles.BotArchetype.Archer, archers);
        beacon.SetTarget(Mobiles.BotArchetype.Crafter, crafters);
        beacon.SetTarget(Mobiles.BotArchetype.Trader, Math.Max(1, total - warriors - mages - archers - crafters));
        beacon.ThiefTarget = total / 10;
    }

    /// <summary>
    /// Обзор окрестностей точки: какие клетки проходимы (не в домах) и до каких из них от точки
    /// можно дойти. По нему ищут поле боя для камня и тихое место для маяка.
    /// </summary>
    internal sealed class Survey
    {
        public const int FieldRadius = 10; // площадка 21×21
        public const int FieldCells = (2 * FieldRadius + 1) * (2 * FieldRadius + 1);
        private const int SearchRadius = 50;
        private const int MinFieldDistance = 15;
        private const int BeaconFromStone = 30;
        private const int StepZ = 8;

        private readonly Map _map;
        private readonly int _ox, _oy, _size;
        private readonly bool[,] _open;
        private readonly int[,] _z;
        private readonly bool[,] _reached;
        private readonly int[,] _sum; // префиксные суммы достижимых клеток

        public Survey(Map map, Point3D near)
        {
            _map = map;
            var r = SearchRadius + FieldRadius;
            _size = 2 * r + 1;
            _ox = near.X - r;
            _oy = near.Y - r;
            _open = new bool[_size, _size];
            _z = new int[_size, _size];
            _reached = new bool[_size, _size];

            for (var y = 0; y < _size; y++)
            {
                for (var x = 0; x < _size; x++)
                {
                    var wx = _ox + x;
                    var wy = _oy + y;
                    if (map.CanSpawnMobile(wx, wy, near.Z - 30, near.Z + 30, false, false, out var z))
                    {
                        var p = new Point3D(wx, wy, z);
                        if (BaseHouse.FindHouseAt(p, map, 16) == null && MahaonCityHouseSystem.Find(p, map) == null)
                        {
                            _open[x, y] = true;
                            _z[x, y] = z;
                        }
                    }
                }
            }

            Flood();

            _sum = new int[_size + 1, _size + 1];
            for (var y = 0; y < _size; y++)
            {
                for (var x = 0; x < _size; x++)
                {
                    _sum[x + 1, y + 1] = _sum[x, y + 1] + _sum[x + 1, y] - _sum[x, y] + (_reached[x, y] ? 1 : 0);
                }
            }
        }

        // Волна от ближайшей к точке открытой клетки: шаг на соседнюю, если перепад высот невелик.
        private void Flood()
        {
            var c = _size / 2;
            int sx = -1, sy = -1;
            for (var d = 0; d <= 3 && sx < 0; d++)
            {
                for (var dy = -d; dy <= d && sx < 0; dy++)
                {
                    for (var dx = -d; dx <= d; dx++)
                    {
                        if (_open[c + dx, c + dy])
                        {
                            sx = c + dx;
                            sy = c + dy;
                            break;
                        }
                    }
                }
            }

            if (sx < 0)
            {
                return;
            }

            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((sx, sy));
            _reached[sx, sy] = true;
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var nx = x + dx;
                        var ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= _size || ny >= _size || _reached[nx, ny] || !_open[nx, ny] ||
                            Math.Abs(_z[nx, ny] - _z[x, y]) > StepZ)
                        {
                            continue;
                        }

                        _reached[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                }
            }
        }

        private int Reachable(int x0, int y0, int x1, int y1) =>
            _sum[x1 + 1, y1 + 1] - _sum[x0, y1 + 1] - _sum[x1 + 1, y0] + _sum[x0, y0];

        /// <summary>
        /// Самая просторная достижимая площадка 21×21 вокруг точки, но не вплотную к ней; при
        /// равной просторности — ближе. Камень — в её середине.
        /// </summary>
        public Point3D? Battlefield(out int open)
        {
            open = 0;
            Point3D? best = null;
            var bestScore = double.MinValue;
            var c = _size / 2;

            for (var y = FieldRadius; y < _size - FieldRadius; y++)
            {
                for (var x = FieldRadius; x < _size - FieldRadius; x++)
                {
                    var distance = Math.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    if (distance < MinFieldDistance || distance > SearchRadius || !_reached[x, y])
                    {
                        continue;
                    }

                    var count = Reachable(x - FieldRadius, y - FieldRadius, x + FieldRadius, y + FieldRadius);

                    // Десяток шагов ближе стоит одной клетки простора: поле у самого города лучше.
                    var score = count - distance * 0.1;
                    if (score > bestScore && IsStoneCell(x, y))
                    {
                        bestScore = score;
                        open = count;
                        best = new Point3D(_ox + x, _oy + y, _z[x, y]);
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Тихое место у точки для маяка: ближайшая к ней свободная достижимая клетка не ближе
        /// тридцати шагов от камня.
        /// </summary>
        public Point3D? Spot(Point3D? stone)
        {
            var c = _size / 2;
            for (var r = MinFromCenter; r <= SearchRadius; r++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    for (var dx = -r; dx <= r; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                        {
                            continue;
                        }

                        var p = new Point3D(_ox + c + dx, _oy + c + dy, _z[c + dx, c + dy]);
                        if ((stone is not { } s || !Utility.InRange(p, s, BeaconFromStone)) && IsStoneCell(c + dx, c + dy))
                        {
                            return p;
                        }
                    }
                }
            }

            return null;
        }

        // Клетка под камень или маяк: достижима, вокруг всё свободно, ни предметов, ни дверей рядом.
        private bool IsStoneCell(int x, int y)
        {
            if (x < 1 || y < 1 || x >= _size - 1 || y >= _size - 1)
            {
                return false;
            }

            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (!_reached[x + dx, y + dy])
                    {
                        return false;
                    }
                }
            }

            var p = new Point3D(_ox + x, _oy + y, _z[x, y]);
            foreach (var item in _map.GetItemsInRange(p, 2))
            {
                if (item is BaseDoor || item.X == p.X && item.Y == p.Y || item is MahaonBotBeacon or MahaonCityClaimPoint)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(_placedVersion);
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version
        _placedVersion = reader.ReadEncodedInt();
    }
}
