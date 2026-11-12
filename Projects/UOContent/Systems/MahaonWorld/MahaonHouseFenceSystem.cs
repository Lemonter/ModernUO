using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Systems.MahaonWorld;

/// <summary>
///     Builds/removes a house's fence and tracks the "fenced territory" bonus area it
///     encloses. The fence follows the ground: it stops at cliffs, mountains, water and
///     neighbouring houses and runs along them, each piece at its own tile's height, and the
///     trees standing on its line are cleared (MahaonFenceClearing) until the fence comes down. The ring is a distance transform from the house's plot
///     (see ComputeContour) — a straight rectangle on flat ground.
///
///     Only one fence per house at a time — building a new radius removes the old one
///     first. The registry below (ByHouse) is memory-only, same pattern as
///     GuildBank's in-memory caches elsewhere in Mahaon: it doesn't survive a restart by
///     itself, but every placed MahaonHouseFence segment remembers its own OwnerHouse and
///     Radius as real serialized fields, so EnsureLoaded can always rebuild the registry
///     (and therefore the training/harvest bonus territory) by re-running the same contour
///     math against the house's still-real, still-placed fence.
/// </summary>
public static class MahaonHouseFenceSystem
{
    public static readonly int[] AllowedRadii = { 1, 3, 5 };

    // Radius doubles as "level" — 1/3/5 tiles map to levels 1/2/3, each level worth another
    // +50% on top of the last (so level 1 = x1.5, level 2 = x2.0, level 3 = x2.5). Same
    // multiplier drives both the pet-training speed bonus and the crop/log/fruit yield
    // bonus — bigger fence, bigger territory, bigger bonus, all the way up.
    private static readonly Dictionary<int, int> LevelByRadius = new()
    {
        [1] = 1,
        [3] = 2,
        [5] = 3
    };

    private const double BonusPerLevel = 0.5;

    public static int GetLevel(int radius) => LevelByRadius.TryGetValue(radius, out var level) ? level : 0;

    public static double GetBonusMultiplier(int radius) => 1.0 + GetLevel(radius) * BonusPerLevel;

    // Gold cost to build (or rebuild at a different radius) — bigger radius, more fence
    // pieces, more territory, so a steeper price. Paid once per build, pulled straight
    // from the requester's bank box.
    private static readonly Dictionary<int, int> CostByRadius = new()
    {
        [1] = 500,
        [3] = 1_200,
        [5] = 2_500
    };

    public static int GetCost(int radius) => CostByRadius.TryGetValue(radius, out var cost) ? cost : 0;

    /// <summary>
    ///     Fences built before the iron one — ThinStoneWall pieces with a wooden gate, gaps at
    ///     three corners out of four — are rebuilt in iron once the world is loaded: same house,
    ///     same radius, free of charge (it was paid for already).
    /// </summary>
    public static void Initialize()
    {
        var stale = new Dictionary<BaseHouse, int>();
        var staleGates = new HashSet<BaseHouse>();
        var radii = new Dictionary<BaseHouse, int>();

        foreach (var item in World.Items.Values)
        {
            if (item is MahaonHouseFence { Deleted: false, OwnerHouse.Deleted: false } fence)
            {
                radii[fence.OwnerHouse] = fence.Radius;

                if (fence.IsLegacy)
                {
                    stale[fence.OwnerHouse] = fence.Radius;
                }
            }
            else if (item is MahaonHouseFenceGate { Deleted: false, OwnerHouse.Deleted: false, IsLegacy: true } gate)
            {
                staleGates.Add(gate.OwnerHouse);
            }
        }

        foreach (var house in staleGates)
        {
            if (!stale.ContainsKey(house) && radii.TryGetValue(house, out var radius))
            {
                stale[house] = radius;
            }
        }

        foreach (var (house, radius) in stale)
        {
            Construct(house, radius);
        }
    }

    private sealed class Territory
    {
        public int Radius;
        public Map Map;
        public HashSet<Point2D> Tiles; // footprint + every ring up to and including Radius — the bonus area
    }

    private static readonly Dictionary<BaseHouse, Territory> ByHouse = new();
    private static bool _scanned;

    private static void EnsureScanned()
    {
        if (_scanned)
        {
            return;
        }

        _scanned = true;

        // One MahaonHouseFence segment per house is enough to know its OwnerHouse + Radius —
        // skip any house we've already got (either scanned already or built this session).
        foreach (var item in World.Items.Values)
        {
            if (item is MahaonHouseFence { Deleted: false } fence
                && fence.OwnerHouse?.Deleted == false
                && !ByHouse.ContainsKey(fence.OwnerHouse))
            {
                RegisterTerritory(fence.OwnerHouse, fence.Radius);
            }
        }
    }

    public static bool HasFence(BaseHouse house)
    {
        EnsureScanned();
        return house != null && ByHouse.ContainsKey(house);
    }

    public static int GetFenceRadius(BaseHouse house)
    {
        EnsureScanned();
        return house != null && ByHouse.TryGetValue(house, out var t) ? t.Radius : 0;
    }

    /// <summary>Radius of whichever fenced territory contains this point, or 0 if none —
    /// used by the pet-training and crop/tree/fruit yield bonuses to know which level
    /// applies. Cheap: just a HashSet lookup per fenced house (there's normally only a
    /// handful on a shard, not one per player).</summary>
    public static int GetTerritoryRadius(Point3D loc, Map map)
    {
        EnsureScanned();

        if (map == null)
        {
            return 0;
        }

        var p = new Point2D(loc.X, loc.Y);

        foreach (var territory in ByHouse.Values)
        {
            if (territory.Map == map && territory.Tiles.Contains(p))
            {
                return territory.Radius;
            }
        }

        return 0;
    }

    public static bool IsInsideFencedTerritory(Point3D loc, Map map) => GetTerritoryRadius(loc, map) > 0;

    public static double GetTrainingBonusMultiplier(Point3D loc, Map map) => GetBonusMultiplier(GetTerritoryRadius(loc, map));

    public static int ApplyYieldBonus(int baseAmount, Point3D loc, Map map) =>
        (int)System.Math.Round(baseAmount * GetBonusMultiplier(GetTerritoryRadius(loc, map)));

    public static long ApplyYieldBonus(long baseAmount, Point3D loc, Map map) =>
        (long)System.Math.Round(baseAmount * GetBonusMultiplier(GetTerritoryRadius(loc, map)));

    /// <summary>Builds (or replaces) the fence at the given radius, charging <paramref
    /// name="from"/>'s bank box the gold cost first. Returns a Russian status message to
    /// show the player either way — never throws for "normal" failure cases like an
    /// invalid radius or insufficient funds, and never charges without actually building.</summary>
    public static string Build(Mobile from, BaseHouse house, int radius)
    {
        if (house?.Deleted != false || house.Map == null)
        {
            return "У дома нет действительного места на карте.";
        }

        if (System.Array.IndexOf(AllowedRadii, radius) < 0)
        {
            return "Допустимые расстояния для ограды: 1, 3 или 5 тайлов.";
        }

        var cost = GetCost(radius);

        if (from.AccessLevel < AccessLevel.GameMaster && !Banker.Withdraw(from, cost))
        {
            return $"Не хватает золота в банке — ограда на {radius} тайлов стоит {cost}.";
        }

        if (!Construct(house, radius))
        {
            return "Не удалось построить ограду — вокруг дома не нашлось места.";
        }

        return $"Ограда построена на расстоянии {radius} тайлов от дома, с калиткой у входа. Списано {cost} золота.";
    }

    /// <summary>Puts up the fence without charging: <see cref="Build"/> after payment, and a
    /// rebuilt house getting back the fence its owner already paid for. Деревья на линии ограды
    /// убираются (MahaonFenceClearing) и вернутся, когда ограду снимут или дом снесут.</summary>
    internal static bool Construct(BaseHouse house, int radius)
    {
        Remove(house); // one fence at a time — clear whatever was there first (already paid for is already paid for)

        var (territoryTiles, ringPlacements) = ComputeContour(house, radius);

        if (ringPlacements.Count == 0)
        {
            logger.Warning(
                "Fence around {House} at {Location}: no ring (cells refused — land {Land}, obstacle {Obstacle}, house {OtherHouse}, step {Step})",
                house.Serial, house.Location, _refusedLand, _refusedObstacle, _refusedHouse, _refusedStep
            );
            return false;
        }

        var gateSpot = PickGateSpot(house, ringPlacements);
        var clearing = new MahaonFenceClearing(house, house.Map);

        Systems.MahaonMapEdits.MahaonMapEdits.BeginBatch();
        try
        {
            foreach (var (pos, kind, z) in ringPlacements)
            {
                // Don't fence into a neighboring house's plot — skip any ring tile that
                // already belongs to a different house.
                var loc = new Point3D(pos.X, pos.Y, z);
                var thereAlready = BaseHouse.FindHouseAt(loc, house.Map, 16);
                if (thereAlready != null && thereAlready != house)
                {
                    continue;
                }

                clearing.Clear(pos.X, pos.Y, z);

                if (pos == gateSpot.pos)
                {
                    var gate = new MahaonHouseFenceGate(gateSpot.facing, house);
                    gate.MoveToWorld(loc, house.Map);
                }
                else
                {
                    var fence = new MahaonHouseFence(kind, house, radius);
                    fence.MoveToWorld(loc, house.Map);
                }
            }
        }
        finally
        {
            Systems.MahaonMapEdits.MahaonMapEdits.EndBatch();
        }

        if (clearing.IsEmpty)
        {
            clearing.Delete();
        }
        else
        {
            clearing.MoveToWorld(house.Location, Map.Internal);
            RegisterClearing(clearing);
        }

        ByHouse[house] = new Territory { Radius = radius, Map = house.Map, Tiles = territoryTiles };
        return true;
    }

    // ---- Убранные деревья ---------------------------------------------------------------------

    private static readonly Dictionary<BaseHouse, MahaonFenceClearing> Clearings = new();

    internal static void RegisterClearing(MahaonFenceClearing clearing)
    {
        if (clearing.House != null)
        {
            Clearings[clearing.House] = clearing;
        }
    }

    internal static void UnregisterClearing(MahaonFenceClearing clearing)
    {
        if (clearing.House != null && Clearings.TryGetValue(clearing.House, out var known) && known == clearing)
        {
            Clearings.Remove(clearing.House);
        }
    }

    private static void RestoreTrees(BaseHouse house)
    {
        if (house != null && Clearings.TryGetValue(house, out var clearing))
        {
            Clearings.Remove(house);
            if (!clearing.Deleted)
            {
                clearing.Restore();
            }
        }
    }

    // Widest fence ring plus a tile: a fence piece is never further than this from the house.
    private static readonly int FenceReach = AllowedRadii[^1] + 2;

    /// <summary>Removes every fence piece (and the gate) belonging to this house — found around
    /// the house itself, so this also cleans up a fence built in a previous server session —
    /// and puts back the trees the fence had cleared.
    /// Called when the house is deleted (demolished, decayed, rebuilt), so no fence outlives it.</summary>
    public static void Remove(BaseHouse house)
    {
        if (house?.Map == null || house.Map == Map.Internal)
        {
            if (house != null)
            {
                ByHouse.Remove(house);
                RestoreTrees(house);
            }

            return;
        }

        var toDelete = new List<Item>();
        var mcl = house.Components;
        var bounds = new Rectangle2D(
            house.X + mcl.Min.X - FenceReach,
            house.Y + mcl.Min.Y - FenceReach,
            mcl.Width + FenceReach * 2,
            mcl.Height + FenceReach * 2
        );

        foreach (var item in house.Map.GetItemsInBounds(bounds))
        {
            if (item is MahaonHouseFence fence && fence.OwnerHouse == house
                || item is MahaonHouseFenceGate gate && gate.OwnerHouse == house)
            {
                toDelete.Add(item);
            }
        }

        foreach (var item in toDelete)
        {
            if (!item.Deleted)
            {
                item.Delete();
            }
        }

        ByHouse.Remove(house);
        RestoreTrees(house);
    }

    private static void RegisterTerritory(BaseHouse house, int radius)
    {
        var (tiles, _) = ComputeContour(house, radius, ringOnly: false);
        ByHouse[house] = new Territory { Radius = radius, Map = house.Map, Tiles = tiles };
    }

    // Почему волна не пошла дальше — для журнала, когда ограды не вышло вовсе.
    private static int _refusedLand, _refusedObstacle, _refusedHouse, _refusedStep;

    private static readonly Server.Logging.ILogger logger = Server.Logging.LogFactory.GetLogger(typeof(MahaonHouseFenceSystem));

    // Шаг между соседними клетками, который ещё можно пройти: склон холма — да, обрыв — нет.
    private const int MaxStep = 12;

    // С площадки дома на землю рядом — дом стоит на фундаменте, ступенька выше.
    private const int HouseStep = 16;

    /// <summary>The heart of the system — an 8-directional (Chebyshev) multi-source
    /// distance transform from the house's plot, computed on a grid padded by radius+1 in every
    /// direction.
    ///
    /// Рельеф учитывается: волна идёт только по клеткам, куда можно ступить, — не в скалу, не в
    /// воду, не с обрыва и не на обрыв (перепад больше <see cref="MaxStep"/>), не в чужой дом.
    /// Поэтому территория упирается в гору или край обрыва, а ограда идёт вдоль такой преграды,
    /// а не сквозь неё; на ровном месте всё как прежде — ровный прямоугольник.
    ///
    /// Ограда — это край территории: её клетки, у которых сосед по стороне уже вне её
    /// (дальше радиуса или за преградой). Каждая деталь стоит на высоте своей клетки.
    /// Cells at distance &lt;= radius (plot included) become the enclosed "territory" used by
    /// the bonus systems.</summary>
    private static (HashSet<Point2D> territory, List<(Point2D pos, MahaonFenceKind kind, int z)> ring) ComputeContour(
        BaseHouse house, int radius, bool ringOnly = false
    )
    {
        var map = house.Map;
        var mcl = house.Components;
        var pad = radius + 1;
        _refusedLand = _refusedObstacle = _refusedHouse = _refusedStep = 0;
        var gridW = mcl.Width + pad * 2;
        var gridH = mcl.Height + pad * 2;

        // dist[x,y] in this padded grid — -1 means "not reached"; zs — где на клетке стоят.
        var dist = new int[gridW, gridH];
        var zs = new int[gridW, gridH];
        for (var x = 0; x < gridW; x++)
        {
            for (var y = 0; y < gridH; y++)
            {
                dist[x, y] = -1;
            }
        }

        var frontier = new List<(int x, int y)>();

        // Отсчёт идёт от ВСЕЙ площадки дома, а не от занятых тайлов постройки: силуэт у мультей
        // рваный (крыльцо, свесы крыши), и забор повторял бы каждую выемку. Площадка же
        // прямоугольная, и кольцо на равном расстоянии от неё — ровный прямоугольник.
        for (var x = 0; x < mcl.Width; x++)
        {
            for (var y = 0; y < mcl.Height; y++)
            {
                var gx = x + pad;
                var gy = y + pad;
                dist[gx, gy] = 0;
                zs[gx, gy] = house.Z;
                frontier.Add((gx, gy));
            }
        }

        var current = frontier;
        var d = 0;

        while (current.Count > 0 && d < radius)
        {
            var next = new List<(int x, int y)>();
            d++;

            foreach (var (x, y) in current)
            {
                var step = dist[x, y] == 0 ? HouseStep : MaxStep;

                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        var nx = x + dx;
                        var ny = y + dy;

                        if (nx < 0 || ny < 0 || nx >= gridW || ny >= gridH || dist[nx, ny] != -1)
                        {
                            continue;
                        }

                        var w = ToWorld(house, mcl, pad, nx, ny);
                        if (map == null || !TryStand(map, house, w.X, w.Y, zs[x, y], out var nz))
                        {
                            continue;
                        }

                        if ((nz - zs[x, y]).Abs() > step)
                        {
                            _refusedStep++;
                            continue; // сюда не ступить отсюда — может, дойдём с другой стороны
                        }

                        dist[nx, ny] = d;
                        zs[nx, ny] = nz;
                        next.Add((nx, ny));
                    }
                }
            }

            current = next;
        }

        bool Inside(int x, int y) => x >= 0 && y >= 0 && x < gridW && y < gridH && dist[x, y] >= 0;

        // Край территории: клетка за площадкой дома, у которой сосед по стороне — снаружи.
        var ringSet = new HashSet<Point2D>();
        var ringCells = new List<(int x, int y)>();
        for (var x = 0; x < gridW; x++)
        {
            for (var y = 0; y < gridH; y++)
            {
                if (dist[x, y] < 1)
                {
                    continue;
                }

                if (!Inside(x + 1, y) || !Inside(x - 1, y) || !Inside(x, y + 1) || !Inside(x, y - 1))
                {
                    ringSet.Add(ToWorld(house, mcl, pad, x, y));
                    ringCells.Add((x, y));
                }
            }
        }

        // Fence art stands on a tile's south or east edge (see MahaonFenceKind), so a piece
        // joins its tile to the WEST or NORTH neighbour: the fence line runs through the
        // south-east corners of the ring tiles and closes at every corner, convex or concave.
        //
        // Клетка без соседей с запада и севера, но с соседями с востока и юга — северо-западный
        // угол прогона: детали ей не нужно, линии соседей сходятся в её углу, как на исходной
        // карте, а шаг наискосок сквозь неё закрыт — ему нужны свободными обе стороны.
        // У края вдоль скалы или обрыва бывают и клетки, касающиеся линии только с одной
        // стороны или углом: им деталь ставится, иначе в ограде осталась бы дыра.
        var ringPlacements = new List<(Point2D pos, MahaonFenceKind kind, int z)>();
        foreach (var (x, y) in ringCells)
        {
            var pos = ToWorld(house, mcl, pad, x, y);

            var north = ringSet.Contains(new Point2D(pos.X, pos.Y - 1));
            var west = ringSet.Contains(new Point2D(pos.X - 1, pos.Y));
            var east = ringSet.Contains(new Point2D(pos.X + 1, pos.Y));
            var south = ringSet.Contains(new Point2D(pos.X, pos.Y + 1));

            MahaonFenceKind kind;

            if (north && west)
            {
                kind = MahaonFenceKind.Corner;
            }
            else if (west)
            {
                kind = MahaonFenceKind.WallX;
            }
            else if (north)
            {
                kind = MahaonFenceKind.WallY;
            }
            else if (east && south)
            {
                continue;
            }
            else if (east)
            {
                kind = MahaonFenceKind.WallX;
            }
            else if (south)
            {
                kind = MahaonFenceKind.WallY;
            }
            else
            {
                kind = MahaonFenceKind.Corner;
            }

            ringPlacements.Add((pos, kind, zs[x, y]));
        }

        var territory = new HashSet<Point2D>();

        if (!ringOnly)
        {
            for (var x = 0; x < gridW; x++)
            {
                for (var y = 0; y < gridH; y++)
                {
                    if (dist[x, y] >= 0 && dist[x, y] <= radius)
                    {
                        territory.Add(ToWorld(house, mcl, pad, x, y));
                    }
                }
            }
        }

        return (territory, ringPlacements);
    }

    /// <summary>
    ///     Можно ли встать на клетку и на какой высоте — ближайшей к <paramref name="refZ"/>.
    ///     Нельзя: скала и вода (непроходимая земля без настила), что-то непроходимое на уровне
    ///     роста (камень, стена, чужая ограда), чужой дом. Деревья не преграда — с линии ограды
    ///     их убирают.
    /// </summary>
    private static bool TryStand(Map map, BaseHouse house, int x, int y, int refZ, out int z)
    {
        z = 0;
        int? best = null;

        var land = map.Tiles.GetLandTile(x, y);
        var landFlags = TileData.LandTable[land.ID & TileData.MaxLandValue].Flags;
        if ((landFlags & TileFlag.Impassable) == 0)
        {
            best = map.GetAverageZ(x, y);
        }

        var statics = Systems.MahaonMapEdits.MahaonMapEdits.StaticsAt(map, x, y);

        foreach (var s in statics)
        {
            var data = TileData.ItemTable[s.Graphic & TileData.MaxItemValue];
            if (!data.Surface || data.Impassable || data.Wet || MahaonFenceClearing.IsTreePart(s.Graphic))
            {
                continue;
            }

            var top = s.Z + data.CalcHeight;
            if ((top - refZ).Abs() <= MaxStep + 4 && (best == null || (top - refZ).Abs() < (best.Value - refZ).Abs()))
            {
                best = top;
            }
        }

        if (best == null)
        {
            _refusedLand++;
            return false;
        }

        foreach (var s in statics)
        {
            var data = TileData.ItemTable[s.Graphic & TileData.MaxItemValue];
            if (!data.Impassable || MahaonFenceClearing.IsTreePart(s.Graphic))
            {
                continue;
            }

            if (s.Z + data.CalcHeight > best.Value && s.Z < best.Value + 16)
            {
                _refusedObstacle++;
                return false;
            }
        }

        var other = BaseHouse.FindHouseAt(new Point3D(x, y, best.Value), map, 16);
        if (other != null && other != house)
        {
            _refusedHouse++;
            return false;
        }

        z = best.Value;
        return true;
    }

    private static Point2D ToWorld(BaseHouse house, Server.MultiComponentList mcl, int pad, int gx, int gy) =>
        new(
            house.Location.X + (gx - pad) - mcl.Center.X,
            house.Location.Y + (gy - pad) - mcl.Center.Y
        );

    /// <summary>Picks whichever ring tile sits closest to the house's own front door, so
    /// the gate always opens onto the same side the owner actually walks out of.</summary>
    private static (Point2D pos, DoorFacing facing) PickGateSpot(
        BaseHouse house, List<(Point2D pos, MahaonFenceKind kind, int z)> ring
    )
    {
        Point3D doorLoc = house.Doors?.Count > 0 ? house.Doors[0].GetWorldLocation() : house.Location;

        var best = ring[0];
        var bestDist = long.MaxValue;

        foreach (var entry in ring)
        {
            // A gate only fits a straight run — there's no corner gate graphic.
            if (entry.kind == MahaonFenceKind.Corner)
            {
                continue;
            }

            var dx = entry.pos.X - doorLoc.X;
            var dy = entry.pos.Y - doorLoc.Y;
            var d = (long)dx * dx + (long)dy * dy;

            if (d < bestDist)
            {
                bestDist = d;
                best = entry;
            }
        }

        // Куда калитка смотрит, решает СТОРОНА кольца, на которой она стоит, а не только
        // направление прогона стены: West/East — створки ГОРИЗОНТАЛЬНОГО прогона (вдоль X),
        // South/North — вертикального (так стоят железные калитки в декорациях: Мунглоу,
        // кладбище Веспера). Внутри пары — та, чья створка уходит НАРУЖУ (BaseDoor.GetOffset):
        // линия ограды идёт по южному и восточному краю клеток кольца, поэтому северный и
        // западный ряды стоят снаружи линии, а южный и восточный — внутри.
        var vertical = best.kind == MahaonFenceKind.WallY;

        var facing = vertical
            ? best.pos.X < house.X ? DoorFacing.NorthCW : DoorFacing.SouthCW
            : best.pos.Y < house.Y ? DoorFacing.EastCW : DoorFacing.WestCW;

        return (best.pos, facing);
    }
}
