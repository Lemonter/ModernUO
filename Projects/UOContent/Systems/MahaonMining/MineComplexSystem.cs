using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;

namespace Server.Systems.MahaonMining;

/// <summary>
///     Carves mine interiors out of placed items (floor/wall/support tiles) directly
///     beneath the exact spot they were dug — same map, same X/Y as the entrance, just
///     buried inside the mountain itself, not teleported somewhere else.
/// </summary>
public static class MineComplexSystem
{
    private const int MaxOreDistance = 15;
    private const double SkillCap = 120.0;

    private static readonly Type[] OreTiers =
    {
        typeof(IronOre), typeof(DullCopperOre), typeof(ShadowIronOre), typeof(CopperOre),
        typeof(BronzeOre), typeof(GoldOre), typeof(AgapiteOre), typeof(VeriteOre), typeof(ValoriteOre)
    };

    private static readonly Dictionary<Type, string> OreNamesRu = new()
    {
        [typeof(IronOre)] = "железо", [typeof(DullCopperOre)] = "тусклая медь",
        [typeof(ShadowIronOre)] = "теневое железо", [typeof(CopperOre)] = "медь",
        [typeof(BronzeOre)] = "бронза", [typeof(GoldOre)] = "золото",
        [typeof(AgapiteOre)] = "агапит", [typeof(VeriteOre)] = "верит", [typeof(ValoriteOre)] = "валорит"
    };

    // Gems show up in veins right alongside ore — no depth gate needed, the tier picker
    // already biases toward the plain stuff at low depth/skill on its own.
    private static readonly (Type type, string nameRu, int hue)[] GemTiers =
    {
        (typeof(Amber), "янтарь", 0x3B3), (typeof(Citrine), "цитрин", 0x3B7),
        (typeof(Tourmaline), "турмалин", 0x48F), (typeof(Emerald), "изумруд", 0x59),
        (typeof(Ruby), "рубин", 0x26), (typeof(Sapphire), "сапфир", 0x1A),
        (typeof(StarSapphire), "звёздный сапфир", 0x1E), (typeof(Amethyst), "аметист", 0x30),
        (typeof(Diamond), "алмаз", 0x481)
    };

    private static readonly int[] OreHues =
    {
        0, 0x453, 0x455, 0x459, 0x45D, 0x461, 0x465, 0x469, 0x46D
    };

    // How likely a fresh frontier wall is to hide a vein worth flagging, and how the odds
    // and richness improve with depth and Mining skill.
    private const double BaseVeinChance = 0.04;

    // How long an unsupported floor tile has before it's checked for collapse.
    private static readonly TimeSpan CollapseCheckDelay = TimeSpan.FromHours(12);
    private const int SupportRadius = 3;
    private const double CollapseChanceIfUnsupported = 0.35;

    // Only these specific graphics count as "mountain you can dig a mine into" — shared
    // between the initial dig-target check (HarvestTarget) and the straightness check
    // below. Add more IDs here as they get confirmed, one at a time.
    public static readonly int[] DiggableMountainGraphics = { 0x23B, 0x23A };

    public static bool IsDiggableMountainGraphic(Map map, Point3D loc)
    {
        if (map == null)
        {
            return false;
        }

        var lt = map.Tiles.GetLandTile(loc.X, loc.Y);
        if (Array.IndexOf(DiggableMountainGraphics, lt.ID & TileData.MaxLandValue) >= 0)
        {
            return true;
        }

        foreach (var item in map.GetItemsInRange<Static>(loc, 0))
        {
            if (item.Location == loc && Array.IndexOf(DiggableMountainGraphics, item.ItemID) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     A mine entrance is only allowed where the wall is genuinely straight — the same
    ///     diggable graphic has to continue at least 3 tiles in both directions along the
    ///     wall's own run (perpendicular to which way the digger was facing), not just at
    ///     the single tile that got dug. Stops a mine from spawning on some one-tile jag in
    ///     an otherwise irregular cliff edge.
    /// </summary>
    public static bool IsWallSegmentStraightEnough(Map map, Point3D loc, Direction facing)
    {
        var eastWest = (facing & Direction.Mask) is Direction.East or Direction.West;

        for (var offset = 1; offset <= 3; offset++)
        {
            foreach (var sign in Signs)
            {
                var checkLoc = eastWest
                    ? new Point3D(loc.X, loc.Y + sign * offset, loc.Z)
                    : new Point3D(loc.X + sign * offset, loc.Y, loc.Z);

                if (!IsDiggableMountainGraphic(map, checkLoc))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static readonly int[] Signs = { -1, 1 };

    public static bool HasEntranceAt(Point3D loc, Map map)
    {
        if (map == null)
        {
            return false;
        }

        foreach (var item in map.GetItemsInRange<Teleporter>(loc, 0))
        {
            if (item.Location == loc)
            {
                return true;
            }
        }

        return false;
    }

    // -- Graphics: 0x8E8 real (impassable) wall. 0x544 is the "pseudo-wall" — walkable,
    // looks ambiguous on purpose, but is still a real MineRockWall you can dig to grow the
    // cave further. 0x53B is real open floor, 0x548 the corner variant of it. Real walls
    // only ever go on the north/west side of anything; south/east always gets a pseudo-wall
    // instead — that's what keeps the cave open-feeling in those two directions while still
    // being expandable there.
    private const int RealWallGraphic = 0x8E8; // fallback only — shouldn't normally show up
    private const int WestWallGraphic = 0x260;
    private const int NorthWallGraphic = 0x261;
    private const int CornerWallGraphic = 0x262;
    private const int PseudoWallGraphic = 0x544; // east / other pseudo-wall sides
    private const int SouthPseudoWallGraphic = 0x541;

    /// <summary>Picks the right wall graphic from which absolute compass side(s) this
    /// particular wall tile is actually on, relative to whatever it's bordering.</summary>
    private static int GetWallGraphic(bool walkable, bool isNorth, bool isWest, bool isSouth)
    {
        if (!walkable)
        {
            if (isNorth && isWest)
            {
                return CornerWallGraphic;
            }

            if (isNorth)
            {
                return NorthWallGraphic;
            }

            if (isWest)
            {
                return WestWallGraphic;
            }

            return RealWallGraphic;
        }

        return isSouth ? SouthPseudoWallGraphic : PseudoWallGraphic;
    }
    private const int FloorPlain = 0x53B;

    /// <summary>
    ///     Builds the mine's entry shaft. Entrance sits right where the digger was
    ///     standing; the shaft runs 4 tiles into the mountain in whichever direction they
    ///     were facing, with the exit at the far end. 3 tiles wide.
    /// </summary>
    public static void CreateMine(Mobile from, Point3D outsideLoc, Direction facing, Map outsideMap)
    {
        facing &= Direction.Mask;
        var eastWest = facing is Direction.East or Direction.West;
        var forwardSign = facing is Direction.East or Direction.South ? 1 : -1;

        // The entrance sits directly under the digger's feet — same X/Y as outsideLoc, just
        // buried in the mountain instead of teleported elsewhere. FindBuriedSpot nudges it
        // a little if the shaft's actual footprint (not just a generic symmetric ring)
        // isn't solidly enough surrounded by rock.
        var entrancePoint = FindBuriedSpot(outsideMap, outsideLoc, eastWest, forwardSign);
        entrancePoint = new Point3D(entrancePoint.X, entrancePoint.Y, outsideLoc.Z);

        var founderSkill = Math.Min(from.Skills[SkillName.Mining].Value, SkillCap);

        var floorMinX = int.MaxValue;
        var floorMaxX = int.MinValue;
        var floorMinY = int.MaxValue;
        var floorMaxY = int.MinValue;

        // Shaft runs from along=0 (entrance) to along=4 (exit), 3 tiles wide.
        for (var along = 0; along <= 4; along++)
        {
            for (var across = -1; across <= 1; across++)
            {
                var loc = OffsetAlong(entrancePoint, eastWest, forwardSign, along, across);
                PlaceFloorAngled(loc, outsideMap);

                floorMinX = Math.Min(floorMinX, loc.X);
                floorMaxX = Math.Max(floorMaxX, loc.X);
                floorMinY = Math.Min(floorMinY, loc.Y);
                floorMaxY = Math.Max(floorMaxY, loc.Y);
            }
        }

        // Real walls on absolute north/west (Y below the floor's min, or X below the
        // floor's min); pseudo-walls on absolute south/east — using the real bounding box
        // of the floor we just placed, not "near/far cap relative to facing", which is what
        // was flipping north and south depending on which way the digger was looking.
        var forwardVector = eastWest ? (forwardSign, 0) : (0, forwardSign);
        var outwardVector = eastWest ? (0, 1) : (1, 0);

        foreach (var (wallLoc, along, across) in ShaftPerimeter(entrancePoint, eastWest, forwardSign))
        {
            var isNorth = wallLoc.Y < floorMinY;
            var isWest = wallLoc.X < floorMinX;
            var isSouth = wallLoc.Y > floorMaxY;
            var walkable = !isNorth && !isWest;

            var (growthDx, growthDy) = along == 5 ? forwardVector : outwardVector;

            PlaceWall(wallLoc, outsideMap, entrancePoint, 0, founderSkill, walkable, isNorth, isWest, isSouth, growthDx, growthDy);
        }

        var exitLoc = OffsetAlong(entrancePoint, eastWest, forwardSign, 4, 0);
        var exit = new Teleporter(outsideLoc, outsideMap) { Name = "выход из шахты" };
        exit.MoveToWorld(exitLoc, outsideMap);

        var entrance = new Teleporter(entrancePoint, outsideMap) { Name = "вход в шахту" };
        entrance.MoveToWorld(outsideLoc, outsideMap);

        MarkEntrance(from, outsideLoc, outsideMap, eastWest, forwardSign);

        from.SendMessage(0x59, "Ты выкапываешь вход и укрепляешь небольшую камеру в толще горы.");
    }

    /// <summary>
    ///     Обозначает вход снаружи.
    ///
    ///     Сам вход — это Teleporter, а он невидим: до сих пор выкопанная шахта ничем не
    ///     отличалась от обычного склона горы, и найти её можно было только помня, где
    ///     копал. Ставим три вещи, все видимые издалека:
    ///
    ///     — две жаровни по бокам от проёма. Горят вечно (Duration = Zero) и светят, так
    ///       что ночью вход виден с расстояния;
    ///     — деревянный столб с табличкой прямо перед входом, с именем хозяина: по щелчку
    ///       видно, чья это шахта, а не просто «дыра в горе».
    ///
    ///     Каждая вещь ставится только если тайл её принимает — жаровня, упавшая внутрь
    ///     скалы, никому не поможет.
    /// </summary>
    private static void MarkEntrance(Mobile from, Point3D outsideLoc, Map map, bool eastWest, int forwardSign)
    {
        // Столб развёрнут по оси прохода: 0xB98 и 0xB99 — те же деревянные указатели,
        // что стоят в ванильных пещерах (см. Data/Decoration/Britannia/_orccave.cfg).
        var sign = new Sign(eastWest ? 0xB98 : 0xB99)
        {
            Name = $"шахта {from.Name ?? "неизвестного"}",
            Movable = false
        };

        PlaceMarker(sign, OffsetAlong(outsideLoc, eastWest, forwardSign, -1, 1), map);

        PlaceMarker(new Brazier(), OffsetAlong(outsideLoc, eastWest, forwardSign, 0, -1), map);
        PlaceMarker(new Brazier(), OffsetAlong(outsideLoc, eastWest, forwardSign, 0, 1), map);
    }

    private static void PlaceMarker(Item item, Point3D loc, Map map)
    {
        if (map == null || !map.CanFitItem(loc, 16))
        {
            item.Delete(); // тайл занят или это сплошная скала — метку туда не поставить
            return;
        }

        item.MoveToWorld(loc, map);
    }

    private static Point3D OffsetAlong(Point3D entrance, bool eastWest, int forwardSign, int along, int across) =>
        eastWest
            ? new Point3D(entrance.X + along * forwardSign, entrance.Y + across, entrance.Z)
            : new Point3D(entrance.X + across, entrance.Y + along * forwardSign, entrance.Z);

    /// <summary>The ring of tiles just outside the 3x5 entry shaft.</summary>
    private static IEnumerable<(Point3D loc, int along, int across)> ShaftPerimeter(Point3D entrance, bool eastWest, int forwardSign)
    {
        for (var along = -1; along <= 5; along++)
        {
            for (var across = -2; across <= 2; across++)
            {
                var insideShaft = along is >= 0 and <= 4 && across is >= -1 and <= 1;
                if (insideShaft)
                {
                    continue;
                }

                var onImmediateRing = along is >= -1 and <= 5 && across is >= -2 and <= 2 &&
                                       (along is -1 or 5 || across is -2 or 2);
                if (!onImmediateRing)
                {
                    continue;
                }

                yield return (OffsetAlong(entrance, eastWest, forwardSign, along, across), along, across);
            }
        }
    }

    private static void PlaceFloorAngled(Point3D loc, Map map)
    {
        var floor = new MineFloorTile();
        floor.ItemID = FloorPlain;
        floor.MoveToWorld(loc, map);
    }

    /// <summary>
    ///     Looks outward from the dig point for a spot where the whole actual shaft
    ///     footprint (5 long in the facing direction, 3 wide, plus a 1-tile buffer ring
    ///     around that) is solidly impassable rock — not just a generic symmetric ring
    ///     around the center, which let the far end of the shaft poke out past the
    ///     mountain's real edge. Falls back to the dig point itself if nothing better turns
    ///     up nearby.
    /// </summary>
    private static Point3D FindBuriedSpot(Map map, Point3D near, bool eastWest, int forwardSign)
    {
        for (var radius = 0; radius <= 12; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius)
                    {
                        continue;
                    }

                    var candidate = new Point3D(near.X + dx, near.Y + dy, near.Z);

                    if (IsShaftFootprintBuried(map, candidate, eastWest, forwardSign))
                    {
                        return candidate;
                    }
                }
            }
        }

        return near;
    }

    private static bool IsShaftFootprintBuried(Map map, Point3D entrance, bool eastWest, int forwardSign)
    {
        // Same footprint as ShaftPerimeter's own range, plus one extra tile of buffer all
        // the way around so nothing ends up right on the mountain's actual edge.
        for (var along = -2; along <= 6; along++)
        {
            for (var across = -3; across <= 3; across++)
            {
                var loc = OffsetAlong(entrance, eastWest, forwardSign, along, across);

                // Not just real terrain — also has to be clear of any mine structure we
                // already built earlier (a previous test dig nearby), or the new shaft's
                // entrance point can land right inside old walls/floor and the player ends
                // up stuck in overlapping geometry.
                if (HasAnyStructureAt(loc, map))
                {
                    return false;
                }

                var lt = map.Tiles.GetLandTile(loc.X, loc.Y);
                var flags = TileData.LandTable[lt.ID & TileData.MaxLandValue].Flags;

                if ((flags & TileFlag.Impassable) == 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    ///     A single swing against a rock wall — real wall or walkable pseudo-wall, both are
    ///     MineRockWall. One hit, one small yield, one point off the reserve. Once the
    ///     reserve is exhausted: a real wall breaks into open floor and grows a normal
    ///     4-direction frontier (north/west neighbors become real walls, south/east
    ///     neighbors become pseudo-walls, same rule as the original shaft). A pseudo-wall
    ///     just becomes real floor and shifts one tile further out in the same direction —
    ///     it doesn't fan out, it advances.
    /// </summary>
    public static bool TryDigWall(Mobile from, MineRockWall wall, Map map, out Item ore)
    {
        ore = null;

        var center = wall.OriginCenter;
        var distance = Math.Max(Math.Abs(wall.X - center.X), Math.Abs(wall.Y - center.Y));

        if (distance > MaxOreDistance)
        {
            if (distance > 1000)
            {
                // Absurdly large — almost certainly a wall left over from before
                // OriginCenter was tracked properly, not a real safety boundary. Re-anchor
                // it to itself instead of leaving it permanently undiggable.
                wall.OriginCenter = wall.Location;
                center = wall.OriginCenter;
                distance = 0;
            }
            else
            {
                from.SendMessage("Эта стена находится за пределами безопасного расширения шахты.");
                return false;
            }
        }

        var miningSkill = Math.Min(from.Skills[SkillName.Mining].Value, SkillCap);
        var successChance = Math.Clamp(
            0.9 - distance / (double)MaxOreDistance * 0.5 + miningSkill / SkillCap * 0.3,
            0.05,
            0.95
        );

        if (Utility.RandomDouble() > successChance)
        {
            return false;
        }

        wall.OreReserve--;

        if (wall.IsVein)
        {
            var richAmount = Utility.RandomMinMax(3, 6 + distance / 3);
            ore = CreateVeinResource(wall.VeinResourceName, richAmount);
        }
        else
        {
            var metal = Systems.MahaonMetals.MahaonOreGenerator.PickMetal(miningSkill);
            var amount = Math.Max(1, Systems.MahaonMetals.MahaonOreGenerator.YieldCount(miningSkill) / 3);
            ore = new MahaonOre(metal, amount);
        }

        if (wall.OreReserve > 0)
        {
            return true; // wall keeps standing, still has more to give
        }

        var wallLoc = wall.Location;
        var wasWalkable = wall.Walkable;
        var growthDx = wall.GrowthDx;
        var growthDy = wall.GrowthDy;
        wall.Delete();

        var floor = new MineFloorTile();
        floor.ItemID = FloorPlain;
        floor.MoveToWorld(wallLoc, map);
        ScheduleCollapseCheck(floor, map);

        void ExpandFrontier()
        {
            if (wasWalkable)
            {
                // Advance straight in this wall's own stored direction — not recomputed
                // from "away from center", which went diagonal the moment a wall wasn't
                // perfectly axis-aligned with the origin.
                var dx = growthDx;
                var dy = growthDy;

                if (dx == 0 && dy == 0)
                {
                    dx = 1; // shouldn't happen — direction should always be set — but never stall
                }

                var nextLoc = new Point3D(wallLoc.X + dx, wallLoc.Y + dy, wallLoc.Z);
                var nextDistance = Math.Max(Math.Abs(nextLoc.X - center.X), Math.Abs(nextLoc.Y - center.Y));

                if (nextDistance <= MaxOreDistance && !HasAnyStructureAt(nextLoc, map))
                {
                    var nextIsSouth = nextLoc.Y > wallLoc.Y;
                    PlaceWall(nextLoc, map, center, nextDistance, miningSkill, true, false, false, nextIsSouth, dx, dy);
                }

                // The tunnel needs sides as it advances — one flank gets a real wall, the
                // other a pseudo-wall, same absolute-compass rule as everywhere else.
                var (perpDx, perpDy) = dx != 0 ? (0, 1) : (1, 0);

                var flankA = new Point3D(wallLoc.X + perpDx, wallLoc.Y + perpDy, wallLoc.Z);
                var flankB = new Point3D(wallLoc.X - perpDx, wallLoc.Y - perpDy, wallLoc.Z);

                var flankADistance = Math.Max(Math.Abs(flankA.X - center.X), Math.Abs(flankA.Y - center.Y));
                var flankBDistance = Math.Max(Math.Abs(flankB.X - center.X), Math.Abs(flankB.Y - center.Y));

                if (flankADistance <= MaxOreDistance && !HasAnyStructureAt(flankA, map))
                {
                    var aIsNorth = flankA.Y < wallLoc.Y;
                    var aIsWest = flankA.X < wallLoc.X;
                    var aIsSouth = flankA.Y > wallLoc.Y;
                    PlaceWall(flankA, map, center, flankADistance, miningSkill, !aIsNorth && !aIsWest, aIsNorth, aIsWest, aIsSouth, dx, dy);
                }

                if (flankBDistance <= MaxOreDistance && !HasAnyStructureAt(flankB, map))
                {
                    var bIsNorth = flankB.Y < wallLoc.Y;
                    var bIsWest = flankB.X < wallLoc.X;
                    var bIsSouth = flankB.Y > wallLoc.Y;
                    PlaceWall(flankB, map, center, flankBDistance, miningSkill, !bIsNorth && !bIsWest, bIsNorth, bIsWest, bIsSouth, dx, dy);
                }

                return;
            }

            // A real wall broke — normal 4-direction frontier, same absolute-compass rule
            // as the original shaft: north/west neighbors get real walls, south/east get
            // pseudo-walls, so the "always open to the south/east" feel continues out here.
            foreach (var neighbor in Neighbors(wallLoc))
            {
                var neighborDistance = Math.Max(Math.Abs(neighbor.X - center.X), Math.Abs(neighbor.Y - center.Y));
                if (neighborDistance > MaxOreDistance || HasAnyStructureAt(neighbor, map))
                {
                    continue;
                }

                var isNorth = neighbor.Y < wallLoc.Y;
                var isWest = neighbor.X < wallLoc.X;
                var isSouth = neighbor.Y > wallLoc.Y;
                var ndx = neighbor.X - wallLoc.X;
                var ndy = neighbor.Y - wallLoc.Y;
                PlaceWall(neighbor, map, center, distance, miningSkill, !isNorth && !isWest, isNorth, isWest, isSouth, ndx, ndy);
            }
        }

        // A chisel in the digger's pack means "offer to go deeper instead" — same idea as
        // the surface's "start a mine?" gump. Declining (or having no chisel) just does the
        // normal expansion immediately, same as before this existed.
        if (from.Backpack?.FindItemByType<MahaonChisel>() != null)
        {
            from.SendGump(new LadderSuggestionGump(from, wallLoc, from.Direction, center, map, ExpandFrontier));
        }
        else
        {
            ExpandFrontier();
        }

        return true;
    }

    private static int StartingReserve(int distance, double miningSkill)
    {
        var depthFactor = Math.Min(distance / (double)MaxOreDistance, 1.0);
        var skillFactor = miningSkill / SkillCap;

        // Skill is the dominant factor here — a 120-skill miner pulls a lot more out of the
        // same vein than a fresh 0-skill one, on top of the usual depth bonus.
        return Utility.RandomMinMax(2, 4) + (int)(skillFactor * 14 + depthFactor * 4);
    }

    /// <summary>
    ///     Places a ladder down 5 Z from the exhausted tile, using whichever graphic
    ///     matches the direction being dug, and starts a fresh pseudo-wall right past it at
    ///     the new (deeper) level so the same dig-to-expand mechanic continues down there.
    /// </summary>
    public static void BuildLadder(Mobile from, Point3D loc, Direction facing, Point3D center, Map map)
    {
        facing &= Direction.Mask;

        // The wall-break already dropped a plain floor tile here before the ladder gump
        // was even answered — remove it now, the ladder takes its place instead of sitting
        // underneath it (which made it un-clickable).
        foreach (var item in map.GetItemsInRange<MineFloorTile>(loc, 0))
        {
            if (item.Location == loc)
            {
                item.Delete();
                break;
            }
        }

        var graphic = facing switch
        {
            Direction.South => 0x79E,
            Direction.East => 0x7B1,
            Direction.West => 0x7B8,
            Direction.North => 0x7B1,
            _ => 0x7B1
        };

        var (dx, dy) = facing switch
        {
            Direction.South => (0, 1),
            Direction.North => (0, -1),
            Direction.East => (1, 0),
            Direction.West => (-1, 0),
            _ => (1, 0)
        };

        var deeperZ = loc.Z - 5;
        var ladderLoc = new Point3D(loc.X, loc.Y, deeperZ);

        var ladder = new MineLadder();
        ladder.ItemID = graphic;
        ladder.MoveToWorld(ladderLoc, map);

        var miningSkill = Math.Min(from.Skills[SkillName.Mining].Value, SkillCap);
        var pseudoLoc = new Point3D(ladderLoc.X + dx, ladderLoc.Y + dy, deeperZ);

        if (!HasAnyStructureAt(pseudoLoc, map))
        {
            var pseudoIsSouth = pseudoLoc.Y > ladderLoc.Y;
            PlaceWall(pseudoLoc, map, center, 0, miningSkill, true, false, false, pseudoIsSouth, dx, dy);
        }

        from.SendMessage(0x59, "Ты вырубаешь ступени вниз, в толщу горы.");
    }

    public static void PlaceSupportBeam(Mobile from, MineSupportBeam beam, Point3D loc, Map map)
    {
        beam.MoveToWorld(loc, map);
        from.SendMessage(0x59, "Ты вклиниваешь опорную балку на место.");
    }

    /// <summary>Отодвигает того, кто стоит на обваливающемся тайле, на соседний свободный.
    /// Не нашлось ни одного — оставляем как есть: лучше неудобно, чем выбросить человека
    /// неизвестно куда.</summary>
    private static void ShoveOut(Mobile m, Point3D loc, Map map)
    {
        foreach (var neighbor in Neighbors(loc))
        {
            if (!HasAnyStructureAt(neighbor, map) && map.CanFit(neighbor, 16, false, false))
            {
                m.MoveToWorld(neighbor, map);
                return;
            }
        }
    }

    private static bool HasAnyStructureAt(Point3D loc, Map map)
    {
        foreach (var item in map.GetItemsInRange<Item>(loc, 0))
        {
            if (item.Location == loc && item is MineFloorTile or MineRockWall)
            {
                return true;
            }
        }

        return false;
    }

    private static void PlaceWall(
        Point3D loc, Map map, Point3D originCenter, int distance, double miningSkill,
        bool walkable, bool isNorth, bool isWest, bool isSouth, int growthDx = 0, int growthDy = 0
    )
    {
        var wall = new MineRockWall();
        wall.OriginCenter = originCenter;
        wall.OreReserve = StartingReserve(distance, miningSkill);
        wall.Walkable = walkable;
        wall.GrowthDx = growthDx;
        wall.GrowthDy = growthDy;
        wall.ItemID = GetWallGraphic(walkable, isNorth, isWest, isSouth);
        wall.MoveToWorld(loc, map);

        var depthFactor = Math.Min(distance / (double)MaxOreDistance, 1.0);
        var skillFactor = miningSkill / SkillCap;
        var veinChance = BaseVeinChance + depthFactor * 0.07 + skillFactor * 0.20;

        if (Utility.RandomDouble() >= veinChance)
        {
            return;
        }

        string resourceName;
        string resourceNameRu;
        int hue;

        if (Utility.RandomDouble() < 0.3)
        {
            var gemIndex = PickWeightedIndex(GemTiers.Length, distance, miningSkill);
            (var gemType, resourceNameRu, hue) = GemTiers[gemIndex];
            resourceName = gemType.Name;
        }
        else
        {
            var metal = Systems.MahaonMetals.MahaonOreGenerator.PickMetal(miningSkill);
            resourceName = $"MahaonMetal:{metal}";
            resourceNameRu = Systems.MahaonMetals.MahaonMetalTable.Get(metal).RuName;
            hue = 0; // цвета для новых металлов ещё не определены пользователем
        }

        wall.MarkAsVein(resourceName, resourceNameRu, hue);
    }

    private static int PickWeightedIndex(int tierCount, int distance, double miningSkill)
    {
        var distanceFactor = Math.Min(distance / (double)MaxOreDistance, 1.0);
        var skillFactor = miningSkill / SkillCap;
        var bias = distanceFactor * 0.7 + skillFactor * 0.3;

        var roll = (Utility.RandomDouble() + Utility.RandomDouble()) / 2.0;
        return Math.Clamp((int)(roll * bias * tierCount), 0, tierCount - 1);
    }

    private static Item CreateVeinResource(string resourceName, int amount)
    {
        if (resourceName.StartsWith("MahaonMetal:", StringComparison.Ordinal))
        {
            var metalName = resourceName["MahaonMetal:".Length..];

            if (Enum.TryParse<Systems.MahaonMetals.MahaonMetal>(metalName, out var metal))
            {
                return new MahaonOre(metal, amount);
            }
        }

        foreach (var oreType in OreTiers)
        {
            if (oreType.Name == resourceName)
            {
                return MahaonResourceTiers.CreateOre(oreType, amount);
            }
        }

        foreach (var (gemType, _, _) in GemTiers)
        {
            if (gemType.Name == resourceName)
            {
                return (Item)Activator.CreateInstance(gemType, amount);
            }
        }

        return new MahaonOre(Systems.MahaonMetals.MahaonMetal.Iron, amount);
    }

    private static Point3D[] Neighbors(Point3D p) =>
    [
        new(p.X + 1, p.Y, p.Z),
        new(p.X - 1, p.Y, p.Z),
        new(p.X, p.Y + 1, p.Z),
        new(p.X, p.Y - 1, p.Z)
    ];

    private static void ScheduleCollapseCheck(Item floor, Map map)
    {
        var loc = floor.Location;

        Timer.DelayCall(CollapseCheckDelay, () =>
        {
            if (floor.Deleted)
            {
                return;
            }

            var supported = false;

            foreach (var item in map.GetItemsInRange<MineSupportBeam>(loc, SupportRadius))
            {
                supported = true;
                break;
            }

            if (!supported && Utility.RandomDouble() < CollapseChanceIfUnsupported)
            {
                // Сначала вытолкнуть тех, кто стоит ровно на обваливающемся тайле: сюда
                // сейчас встанет непроходимая стена, и предупреждение в чат от этого никого
                // не спасало — человек просто оказывался замурован.
                var trapped = new List<Mobile>();

                foreach (var mobile in map.GetMobilesInRange(loc, 1))
                {
                    mobile.SendMessage(0x22, "Шахта стонет и обрушивается вокруг тебя!");

                    if (mobile.Location == loc)
                    {
                        trapped.Add(mobile);
                    }
                }

                foreach (var mobile in trapped)
                {
                    ShoveOut(mobile, loc, map);
                }

                floor.Delete();

                // Обвал ставил стену конструктором по умолчанию: запас руды ноль, центр
                // (0,0,0), графика — та самая «запасная, в норме не должна появляться».
                // Первая же попытка её копнуть уводила запас в минус, стена исчезала и
                // разрасталась во все стороны от несуществующего центра. Теперь завал —
                // обычная стена, привязанная сама к себе.
                PlaceWall(loc, map, loc, 0, SkillCap / 2, false, false, false, false);
            }
        });
    }
}
