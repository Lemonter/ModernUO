using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Systems.MahaonWorld;

/// <summary>
///     Builds/removes a house's fence and tracks the "fenced territory" bonus area it
///     encloses. The fence line follows the house's real footprint shape (via a Chebyshev
///     distance transform against BaseHouse.Components — see ComputeContour), not just its
///     rectangular bounding box, so an L-shaped or irregular house gets a fence that hugs
///     its actual walls instead of cutting through them.
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
    /// rebuilt house getting back the fence its owner already paid for.</summary>
    internal static bool Construct(BaseHouse house, int radius)
    {
        Remove(house); // one fence at a time — clear whatever was there first (already paid for is already paid for)

        var (territoryTiles, ringPlacements) = ComputeContour(house, radius);

        if (ringPlacements.Count == 0)
        {
            return false;
        }

        var gateSpot = PickGateSpot(house, ringPlacements);

        foreach (var (pos, kind) in ringPlacements)
        {
            // Don't fence into a neighboring house's plot — skip any ring tile that
            // already belongs to a different house.
            var loc = new Point3D(pos.X, pos.Y, house.Location.Z);
            var thereAlready = BaseHouse.FindHouseAt(loc, house.Map, 16);
            if (thereAlready != null && thereAlready != house)
            {
                continue;
            }

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

        ByHouse[house] = new Territory { Radius = radius, Map = house.Map, Tiles = territoryTiles };
        return true;
    }

    // Widest fence ring plus a tile: a fence piece is never further than this from the house.
    private static readonly int FenceReach = AllowedRadii[^1] + 2;

    /// <summary>Removes every fence piece (and the gate) belonging to this house — found around
    /// the house itself, so this also cleans up a fence built in a previous server session.
    /// Called when the house is deleted (demolished, decayed, rebuilt), so no fence outlives it.</summary>
    public static void Remove(BaseHouse house)
    {
        if (house?.Map == null || house.Map == Map.Internal)
        {
            if (house != null)
            {
                ByHouse.Remove(house);
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
    }

    private static void RegisterTerritory(BaseHouse house, int radius)
    {
        var (tiles, _) = ComputeContour(house, radius, ringOnly: false);
        ByHouse[house] = new Territory { Radius = radius, Map = house.Map, Tiles = tiles };
    }

    /// <summary>The heart of the system — an 8-directional (Chebyshev) multi-source
    /// distance transform from the house's real footprint (BaseHouse.Components, not its
    /// bounding box), computed on a grid padded by radius+1 in every direction. Cells at
    /// distance == radius become the fence ring (classified into wall/post pieces by which
    /// neighboring ring cells they touch); cells at distance &lt;= radius (footprint
    /// included) become the enclosed "territory" used by the bonus systems.</summary>
    private static (HashSet<Point2D> territory, List<(Point2D pos, MahaonFenceKind kind)> ring) ComputeContour(
        BaseHouse house, int radius, bool ringOnly = false
    )
    {
        var mcl = house.Components;
        var pad = radius + 1;
        var gridW = mcl.Width + pad * 2;
        var gridH = mcl.Height + pad * 2;

        // dist[x,y] in this padded grid — -1 means "not yet visited".
        var dist = new int[gridW, gridH];
        for (var x = 0; x < gridW; x++)
        {
            for (var y = 0; y < gridH; y++)
            {
                dist[x, y] = -1;
            }
        }

        var frontier = new List<(int x, int y)>();

        // Отсчёт идёт от ВСЕЙ площадки дома, а не от занятых тайлов постройки.
        //
        // Раньше засев шёл по mcl.Tiles[x][y].Length > 0, то есть по силуэту здания. Силуэт
        // у мультей рваный — выступы крыльца, свесы крыши, пустоты внутри плана, — и забор
        // послушно повторял каждую выемку. Отсюда «неровные заборы»: они были не кривые, а
        // слишком точные.
        //
        // Площадка же прямоугольная, поэтому кольцо на равном расстоянии от неё — тоже
        // ровный прямоугольник, как и ждёшь от ограды усадьбы.
        for (var x = 0; x < mcl.Width; x++)
        {
            for (var y = 0; y < mcl.Height; y++)
            {
                var gx = x + pad;
                var gy = y + pad;
                dist[gx, gy] = 0;
                frontier.Add((gx, gy));
            }
        }

        // Standard multi-source BFS ring-by-ring — cheap enough here since even a big
        // castle plot padded by 6 is well under a few thousand cells.
        var current = frontier;
        var d = 0;

        while (current.Count > 0 && d < radius)
        {
            var next = new List<(int x, int y)>();
            d++;

            foreach (var (x, y) in current)
            {
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

                        dist[nx, ny] = d;
                        next.Add((nx, ny));
                    }
                }
            }

            current = next;
        }

        // "current" now holds every cell at exactly Radius (the last ring BFS reached) —
        // that's the fence line itself.
        var ringSet = new HashSet<Point2D>();
        foreach (var (x, y) in current)
        {
            ringSet.Add(ToWorld(house, mcl, pad, x, y));
        }

        var ringPlacements = new List<(Point2D pos, MahaonFenceKind kind)>();
        foreach (var (x, y) in current)
        {
            var pos = ToWorld(house, mcl, pad, x, y);

            var north = ringSet.Contains(new Point2D(pos.X, pos.Y - 1));
            var south = ringSet.Contains(new Point2D(pos.X, pos.Y + 1));
            var east = ringSet.Contains(new Point2D(pos.X + 1, pos.Y));
            var west = ringSet.Contains(new Point2D(pos.X - 1, pos.Y));

            var vertical = north || south;
            var horizontal = east || west;

            var kind = vertical && !horizontal ? MahaonFenceKind.WallY
                : horizontal && !vertical ? MahaonFenceKind.WallX
                : MahaonFenceKind.Post; // corner, junction, or isolated cell — post either way

            ringPlacements.Add((pos, kind));
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

    private static Point2D ToWorld(BaseHouse house, Server.MultiComponentList mcl, int pad, int gx, int gy) =>
        new(
            house.Location.X + (gx - pad) - mcl.Center.X,
            house.Location.Y + (gy - pad) - mcl.Center.Y
        );

    /// <summary>Picks whichever ring tile sits closest to the house's own front door, so
    /// the gate always opens onto the same side the owner actually walks out of.</summary>
    private static (Point2D pos, DoorFacing facing) PickGateSpot(
        BaseHouse house, List<(Point2D pos, MahaonFenceKind kind)> ring
    )
    {
        Point3D doorLoc = house.Doors?.Count > 0 ? house.Doors[0].GetWorldLocation() : house.Location;

        var best = ring[0];
        var bestDist = long.MaxValue;

        foreach (var entry in ring)
        {
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
        // направление прогона стены.
        //
        // Раньше выбор был из двух: вертикальный прогон — WestCW, горизонтальный — SouthCW.
        // Направление створки при этом угадывалось верно ровно в двух случаях из четырёх:
        // калитка на северной стороне получала южную створку, на восточной — западную, и
        // смотрела внутрь двора вместо улицы. Отсюда и «калитка смотрит в другую сторону».
        //
        // Сторону определяем по положению относительно центра дома.
        var vertical = best.kind == MahaonFenceKind.WallY;

        var facing = vertical
            ? best.pos.X < house.X ? DoorFacing.WestCW : DoorFacing.EastCCW
            : best.pos.Y < house.Y ? DoorFacing.NorthCCW : DoorFacing.SouthCW;

        return (best.pos, facing);
    }
}
