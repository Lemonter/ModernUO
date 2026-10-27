using System;
using Server.Engines.Harvest;
using Server.Targeting;

namespace Server.Systems.Bots;

public enum ResourceKind : byte
{
    Ore,
    Wood,
    Fish
}

/// <summary>
/// Finds the thing to swing at from where a bot stands — what a player would click. Ore is the
/// impassable mountainside, wood is a tree drawn in the map statics, fish is water in the land or
/// the statics. The same targets the real swing systems take, so a bot gathers exactly as a
/// player clicking that tile would.
/// </summary>
public static class ResourceProbe
{
    public const int Reach = 2;

    public static SkillName SkillFor(ResourceKind kind) => kind switch
    {
        ResourceKind.Ore  => SkillName.Mining,
        ResourceKind.Wood => SkillName.Lumberjacking,
        _                 => SkillName.Fishing
    };

    public static bool HasTargetNear(Map map, Point3D from, ResourceKind kind) => TryFind(map, from, kind, out _, out _);

    /// <summary>The nearest target within <see cref="Reach"/>: its location and, for statics,
    /// the tile graphic (0 for land).</summary>
    public static bool TryFind(Map map, Point3D from, ResourceKind kind, out Point3D target, out int graphic) =>
        TryFind(map, from, kind, Reach, out target, out graphic);

    /// <summary>
    ///     То же, но на заданном радиусе — чтобы можно было посмотреть ДАЛЬШЕ вытянутой руки.
    ///
    ///     Понадобилось из-за леса. Reach равен двум, то есть «до чего дотянусь»; срубив
    ///     соседнее дерево, бот в этом радиусе не находил больше ничего и заканчивал работу,
    ///     хотя стоял в лесу. Широкий радиус нужен не для замаха, а чтобы знать, куда сделать
    ///     несколько шагов (см. GatherAction).
    ///
    ///     Цена честная: радиус 12 — это 625 клеток с перебором статики на каждой. Поэтому
    ///     широкий поиск запускается только когда вблизи пусто, а не каждый тик.
    /// </summary>
    public static bool TryFind(Map map, Point3D from, ResourceKind kind, int radius, out Point3D target, out int graphic)
    {
        target = Point3D.Zero;
        graphic = 0;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        var treeTiles = kind == ResourceKind.Wood ? Lumberjacking.System.GetDefinition()?.StaticTiles : null;
        var bestDist = int.MaxValue;

        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                if (dx == 0 && dy == 0 && kind != ResourceKind.Fish)
                {
                    continue;
                }

                var x = from.X + dx;
                var y = from.Y + dy;
                if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                {
                    continue;
                }

                var dist = dx * dx + dy * dy;
                if (dist >= bestDist)
                {
                    continue;
                }

                switch (kind)
                {
                    case ResourceKind.Ore:
                        {
                            var land = map.Tiles.GetLandTile(x, y);
                            if ((TileData.LandTable[land.ID & TileData.MaxLandValue].Flags & TileFlag.Impassable) != 0)
                            {
                                bestDist = dist;
                                target = new Point3D(x, y, land.Z);
                                graphic = 0;
                            }

                            break;
                        }
                    case ResourceKind.Wood:
                        {
                            if (treeTiles == null)
                            {
                                break;
                            }

                            foreach (var tile in map.Tiles.GetStaticTiles(x, y))
                            {
                                if (Array.IndexOf(treeTiles, tile.ID) < 0)
                                {
                                    continue;
                                }

                                // Corrected the way a client click would be, so the depletion key
                                // the swing system keeps matches.
                                var candidate = new StaticTarget(new Point3D(x, y, tile.Z), tile.ID);
                                if (MahaonMining.MahaonLumberjackingSwings.IsDepleted(map, candidate.Location))
                                {
                                    continue;
                                }

                                bestDist = dist;
                                target = candidate.Location;
                                graphic = tile.ID;
                                break;
                            }

                            break;
                        }
                    default:
                        {
                            var land = map.Tiles.GetLandTile(x, y);
                            if ((TileData.LandTable[land.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0)
                            {
                                bestDist = dist;
                                target = new Point3D(x, y, land.Z);
                                graphic = 0;
                                break;
                            }

                            foreach (var tile in map.Tiles.GetStaticTiles(x, y))
                            {
                                if ((TileData.ItemTable[tile.ID & TileData.MaxItemValue].Flags & TileFlag.Wet) != 0)
                                {
                                    bestDist = dist;
                                    target = new Point3D(x, y, tile.Z);
                                    graphic = tile.ID;
                                    break;
                                }
                            }

                            break;
                        }
                }
            }
        }

        return bestDist != int.MaxValue;
    }
}
