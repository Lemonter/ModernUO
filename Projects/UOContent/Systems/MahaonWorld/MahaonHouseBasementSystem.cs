using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Systems.MahaonWorld;

/// <summary>
///     Подвал под ставящимся домом (BaseHouse) — тот же, что у городских квартир
///     (MahaonCityHouse.BuildBasement): пол на пятьдесят ниже первого этажа, северная и
///     западная стены с факелами через клетку, люк в полу дома и ступени в углу подвала.
///
///     Размер — по площадке дома: пол ложится под каждую клетку, где у дома что-то стоит на
///     первом этаже, но не меньше <see cref="MahaonBasementLayout.MinSide"/> и не больше
///     <see cref="MaxSide"/> клеток по стороне — под замком пол из тысячи плит никому не нужен.
///
///     Подвал — часть дома: BaseHouse.IsInside спрашивает <see cref="InCellar"/>, поэтому
///     закреплять вещи и ставить мебель внизу можно так же, как наверху, а регион дома и так
///     идёт на всю высоту. Снос дома сносит и подвал; вещи и люди из подвала поднимаются к
///     порогу дома.
/// </summary>
public static class MahaonHouseBasementSystem
{
    public const int Depth = 50;
    public const int MaxSide = 20;

    private const int LowestZ = -120;
    private const int FloorId = 0x0515; // булыжник, как у городских подвалов
    private const int HatchGraphic = 0x3EAE;
    private const int ExitStepsGraphic = 0x079E;

    // Выше этого над основанием дома — уже второй этаж, а не первый.
    private const int GroundFloorReach = 10;

    private static readonly Dictionary<BaseHouse, MahaonHouseBasement> ByHouse = new();

    internal static void Register(MahaonHouseBasement basement)
    {
        if (basement.House != null)
        {
            ByHouse[basement.House] = basement;
        }
    }

    internal static void Unregister(MahaonHouseBasement basement)
    {
        if (basement.House != null && ByHouse.TryGetValue(basement.House, out var known) && known == basement)
        {
            ByHouse.Remove(basement.House);
        }
    }

    public static MahaonHouseBasement Get(BaseHouse house) =>
        house != null && ByHouse.TryGetValue(house, out var b) && !b.Deleted ? b : null;

    public static bool HasBasement(BaseHouse house) => Get(house) != null;

    /// <summary>Стоит ли точка в подвале этого дома. Зовётся из BaseHouse.IsInside на каждый
    /// вопрос «в доме ли» — поэтому сперва пустой словарь.</summary>
    public static bool InCellar(BaseHouse house, Point3D p) =>
        ByHouse.Count > 0 && ByHouse.TryGetValue(house, out var b) && !b.Deleted && b.Contains(p);

    /// <summary>Клетки будущего подвала: площадка дома на первом этаже, в пределах
    /// <see cref="MahaonBasementLayout.MinSide"/>..<see cref="MaxSide"/> по стороне.</summary>
    public static HashSet<Point2D> PlanColumns(BaseHouse house)
    {
        var mcl = house.Components;
        var columns = new HashSet<Point2D>();
        var foundation = house is HouseFoundation;

        for (var x = 0; x < mcl.Width; x++)
        {
            for (var y = 0; y < mcl.Height; y++)
            {
                var wx = house.X + mcl.Min.X + x;
                var wy = house.Y + mcl.Min.Y + y;

                // У настраиваемого дома последний ряд — крыльцо со ступенями, как в IsInside.
                if (foundation)
                {
                    if (y < mcl.Height - 1)
                    {
                        columns.Add(new Point2D(wx, wy));
                    }

                    continue;
                }

                foreach (var tile in mcl.Tiles[x][y])
                {
                    var id = tile.ID & TileData.MaxItemValue;
                    var data = TileData.ItemTable[id];

                    if (data.Roof || id is >= 0xB95 and <= 0xC0E or >= 0xC43 and <= 0xC44 || tile.Z > GroundFloorReach)
                    {
                        continue;
                    }

                    columns.Add(new Point2D(wx, wy));
                    break;
                }
            }
        }

        Clamp(columns, house);
        MahaonBasementLayout.ExpandToMinimum(columns, _ => true);
        return columns;
    }

    // Площадка шире MaxSide — подвал под её серединой.
    private static void Clamp(HashSet<Point2D> columns, BaseHouse house)
    {
        if (columns.Count == 0)
        {
            return;
        }

        MahaonBasementLayout.GetBounds(columns, out var minX, out var minY, out var maxX, out var maxY);

        var fromX = maxX - minX + 1 > MaxSide ? house.X - MaxSide / 2 : minX;
        var fromY = maxY - minY + 1 > MaxSide ? house.Y - MaxSide / 2 : minY;
        var toX = maxX - minX + 1 > MaxSide ? fromX + MaxSide - 1 : maxX;
        var toY = maxY - minY + 1 > MaxSide ? fromY + MaxSide - 1 : maxY;

        columns.RemoveWhere(c => c.X < fromX || c.X > toX || c.Y < fromY || c.Y > toY);
    }

    /// <summary>Подвал стоит, как у городского дома: половина цены клетки пола за клетку.</summary>
    public static int PriceOf(BaseHouse house) => PlanColumns(house).Count * MahaonCityHouseSystem.PricePerTile / 2;

    /// <summary>Заказ подвала из меню усадьбы: деньги из банка, при неудаче — обратно.</summary>
    public static string TryBuy(Mobile buyer, BaseHouse house)
    {
        if (house?.Deleted != false || house.Map == null || house.Map == Map.Internal)
        {
            return "У дома нет действительного места на карте.";
        }

        if (!house.IsOwner(buyer))
        {
            return "Подвал может заказать только хозяин дома.";
        }

        if (HasBasement(house))
        {
            return "Подвал у этого дома уже есть.";
        }

        var price = PriceOf(house);
        var paid = buyer.AccessLevel == AccessLevel.Player;

        if (paid && !Banker.Withdraw(buyer, price))
        {
            return $"Не хватает золота в банке — подвал стоит {price}.";
        }

        if (Build(house) == null)
        {
            if (paid)
            {
                Banker.Deposit(buyer, price);
            }

            return "Подвал здесь не выкопать — деньги вернулись в банк.";
        }

        return $"Подвал выкопан: люк в полу первого этажа. Списано {price} золота.";
    }

    /// <summary>Копает подвал без оплаты (оплаченный — и при перестройке дома).</summary>
    public static MahaonHouseBasement Build(BaseHouse house)
    {
        if (house?.Deleted != false || house.Map == null || house.Map == Map.Internal || HasBasement(house))
        {
            return null;
        }

        var columns = PlanColumns(house);
        if (columns.Count == 0)
        {
            return null;
        }

        var map = house.Map;
        var z = Math.Max(LowestZ, MahaonBasementLayout.Below(map, columns, house.Z) - Depth);
        var basement = new MahaonHouseBasement(house);

        foreach (var c in columns)
        {
            var floor = new Static(FloorId);
            floor.MoveToWorld(new Point3D(c.X, c.Y, z), map);
            basement.Parts.Add(floor);
            basement.Cells.Add(new Point3D(c.X, c.Y, z));
        }

        basement.ResetColumns();

        MahaonBasementLayout.BuildWalls(columns, z, map, basement.Parts);

        var spot = PickHatchSpot(house, columns);
        var corner = MahaonBasementLayout.Corner(columns);
        var arrival = MahaonBasementLayout.Arrival(columns, corner, z);

        var down = new MahaonHouseBasementHatch(house, arrival, "люк в подвал", HatchGraphic);
        down.MoveToWorld(spot, map);
        var up = new MahaonHouseBasementHatch(house, spot, "ступени наверх", ExitStepsGraphic);
        up.MoveToWorld(new Point3D(corner.X, corner.Y, z), map);
        basement.Parts.Add(down);
        basement.Parts.Add(up);

        MahaonBasementLayout.PlaceTorches(columns, z, map, corner, basement.Parts);

        basement.MoveToWorld(house.Location, Map.Internal);
        Register(basement);
        return basement;
    }

    /// <summary>
    ///     Люк — на свободной клетке пола первого этажа, поближе к середине дома и подальше
    ///     от края подвала. Пол дома — поверхность самого мульти, а не земля под ним.
    /// </summary>
    private static Point3D PickHatchSpot(BaseHouse house, HashSet<Point2D> columns)
    {
        var map = house.Map;
        var candidates = new List<Point2D>(columns);
        candidates.Sort((a, b) =>
            ((a.X - house.X) * (a.X - house.X) + (a.Y - house.Y) * (a.Y - house.Y))
            .CompareTo((b.X - house.X) * (b.X - house.X) + (b.Y - house.Y) * (b.Y - house.Y)));

        Point3D? fallback = null;

        foreach (var c in candidates)
        {
            if (FloorZ(house, c.X, c.Y) is not { } floorZ)
            {
                continue;
            }

            var p = new Point3D(c.X, c.Y, floorZ);
            if (!house.IsInside(p, 16) || HasDoorAt(map, c) || !map.CanFit(p, 16, false, false))
            {
                continue;
            }

            if (MahaonBasementLayout.CountColumns(columns, c.X, c.Y) == 9)
            {
                return p;
            }

            fallback ??= p;
        }

        return fallback ?? new Point3D(house.X, house.Y, FloorZ(house, house.X, house.Y) ?? house.Z + 7);
    }

    // Верх самой высокой проходимой поверхности первого этажа в этом столбце дома.
    private static int? FloorZ(BaseHouse house, int wx, int wy)
    {
        var mcl = house.Components;
        var x = wx - house.X - mcl.Min.X;
        var y = wy - house.Y - mcl.Min.Y;

        if (x < 0 || y < 0 || x >= mcl.Width || y >= mcl.Height)
        {
            return null;
        }

        int? best = null;
        foreach (var tile in mcl.Tiles[x][y])
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
            if (!data.Surface || data.Impassable || tile.Z > GroundFloorReach)
            {
                continue;
            }

            var top = house.Z + tile.Z + data.CalcHeight;
            if (best == null || top > best)
            {
                best = top;
            }
        }

        return best;
    }

    private static bool HasDoorAt(Map map, Point2D p)
    {
        foreach (var _ in map.GetItemsAt<BaseDoor>(p))
        {
            return true;
        }

        return false;
    }

    /// <summary>Дом сносят — подвал уходит с ним. Чужое (вещи, люди, питомцы) поднимается к
    /// порогу дома, а не остаётся под землёй.</summary>
    public static void OnHouseDeleted(BaseHouse house)
    {
        if (Get(house) is { } basement)
        {
            Destroy(basement, house);
        }
    }

    internal static void Destroy(MahaonHouseBasement basement) => Destroy(basement, basement.House);

    private static void Destroy(MahaonHouseBasement basement, BaseHouse house)
    {
        var map = basement.Parts.Find(p => p?.Deleted == false)?.Map;

        if (map != null && map != Map.Internal && basement.Cells.Count > 0)
        {
            var exit = house?.Deleted == false ? house.BanLocation : basement.Cells[0];
            if (house?.Deleted != false)
            {
                // Дом уже исчез — к земле над подвалом.
                exit = new Point3D(exit.X, exit.Y, map.GetAverageZ(exit.X, exit.Y));
            }

            var parts = new HashSet<Item>(basement.Parts);
            var items = new List<Item>();
            var mobiles = new List<Mobile>();

            foreach (var c in basement.Columns)
            {
                foreach (var item in map.GetItemsAt(c.X, c.Y))
                {
                    if (!parts.Contains(item) && basement.Contains(item.Location))
                    {
                        items.Add(item);
                    }
                }

                foreach (var m in map.GetMobilesAt(new Point2D(c.X, c.Y)))
                {
                    if (basement.Contains(m.Location))
                    {
                        mobiles.Add(m);
                    }
                }
            }

            foreach (var item in items)
            {
                if (!item.Deleted)
                {
                    item.MoveToWorld(exit, map);
                }
            }

            foreach (var m in mobiles)
            {
                if (!m.Deleted)
                {
                    BaseCreature.TeleportPets(m, exit, map);
                    m.MoveToWorld(exit, map);
                }
            }
        }

        foreach (var part in basement.Parts)
        {
            part?.Delete();
        }

        basement.Parts.Clear();
        basement.Delete();
    }
}
