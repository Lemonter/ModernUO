using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonWorld;

/// <summary>
///     Где висит табличка городского дома. Как у классических домов ModernUO (BaseHouse.SetSign:
///     у маленького дома дверь в (0, 3, 7), табличка в (2, 4, 5)): снаружи, на клетку от стены
///     и на две клетки вбок от двери, на кронштейне, торчащем из стены. Табличка и кронштейн
///     смотрят поперёк стены: у стены вдоль X — «/» (табличка 0xBD2, кронштейн 0xB98), вдоль
///     Y — «\» (0xBD1 и 0xB97).
///
///     Если у двери на карте уже есть кронштейн для вывески (wooden/metal signpost
///     0xB97–0xBA2) — табличка вешается на него.
///
///     Раньше табличка ставилась на землю клеткой перед дверью и торчала посреди улицы.
/// </summary>
public static class MahaonCityHouseSignPlacement
{
    public const string PostName = "кронштейн таблички";

    private const int FirstSignpost = 0x0B97;
    private const int LastSignpost = 0x0BA2;

    // roomDx/roomDy — шаг от двери внутрь дома.
    public static void Place(MahaonCityHouseSign sign, BaseDoor door, int roomDx, int roomDy)
    {
        var map = door.Map;
        if (map == null)
        {
            return;
        }

        // Наружу — против шага внутрь; вдоль стены — поперёк ему.
        var ox = -roomDx;
        var oy = -roomDy;
        var px = roomDy != 0 ? 1 : 0;
        var py = roomDx != 0 ? 1 : 0;
        var wallAlongX = px != 0;

        // Готовый кронштейн у двери: на стене или на клетку снаружи от неё.
        for (var k = -2; k <= 2; k++)
        {
            foreach (var (cx, cy) in new[] { (door.X + k * px, door.Y + k * py), (door.X + ox + k * px, door.Y + oy + k * py) })
            {
                if (FindSignpost(map, cx, cy, door.Z, out var postGraphic, out var postZ))
                {
                    sign.ItemID = (postGraphic & 1) == 1 ? 0xBD1 : 0xBD2;
                    sign.MoveToWorld(new Point3D(cx, cy, postZ), map);
                    return;
                }
            }
        }

        // Своё место: вбок от двери, где стена продолжается (сначала две клетки, как у
        // классического дома, потом одна).
        var side = 2;
        foreach (var k in new[] { 2, -2, 1, -1 })
        {
            if (HasWall(map, door.X + k * px, door.Y + k * py, door.Z))
            {
                side = k;
                break;
            }
        }

        var location = new Point3D(door.X + ox + side * px, door.Y + oy + side * py, door.Z - 2);

        sign.ItemID = wallAlongX ? 0xBD2 : 0xBD1;
        sign.MoveToWorld(location, map);

        var post = new Static(wallAlongX ? 0xB98 : 0xB97) { Name = PostName };
        post.MoveToWorld(location, map);
    }

    // Висит ли табличка уже по-новому: на её клетке есть кронштейн (свой или карты).
    public static bool IsHung(MahaonCityHouseSign sign) =>
        sign.Map != null && FindSignpost(sign.Map, sign.X, sign.Y, sign.Z, out _, out _);

    // Дверь, перед которой табличку поставил прежний генератор: на соседней клетке по
    // одной из осей, и за ней (ещё шаг в ту же сторону) — пол этого дома. По высоте табличке
    // не верим: генератор ставил её на землю (GetAverageZ), а дверь квартиры на втором
    // этаже висит на двадцать выше — над землёй у балкона.
    public static bool TryFindDoor(MahaonCityHouseSign sign, out BaseDoor door, out int roomDx, out int roomDy)
    {
        door = null;
        roomDx = roomDy = 0;
        var bestDz = int.MaxValue;

        foreach (var (dx, dy) in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
        {
            foreach (var item in sign.Map.GetItemsAt(sign.X + dx, sign.Y + dy))
            {
                if (item is not BaseDoor found)
                {
                    continue;
                }

                var dz = System.Math.Abs(found.Z - sign.Z);
                var leadsHome = sign.House?.HasTileNear(found.X + dx, found.Y + dy, found.Z, 20) ?? dz <= 16;
                if (leadsHome && dz < bestDz)
                {
                    door = found;
                    roomDx = dx;
                    roomDy = dy;
                    bestDz = dz;
                }
            }
        }

        return door != null;
    }

    // Табличка, стоящая на земле перед дверью, — перевесить (один раз, при загрузке мира).
    // Возвращает, сколько перевешено и сколько осталось без двери рядом.
    public static (int Moved, int Orphaned) MigrateAll()
    {
        var signs = new List<MahaonCityHouseSign>();

        foreach (var item in World.Items.Values)
        {
            if (item is MahaonCityHouseSign { Deleted: false, Map: not null } sign && sign.Map != Map.Internal &&
                !IsHung(sign))
            {
                signs.Add(sign);
            }
        }

        var moved = 0;
        foreach (var sign in signs)
        {
            if (TryFindDoor(sign, out var door, out var dx, out var dy))
            {
                Place(sign, door, dx, dy);
                moved++;
            }
        }

        return (moved, signs.Count - moved);
    }

    // Убрать свой кронштейн вместе с табличкой.
    public static void RemovePost(Map map, Point3D location)
    {
        if (map == null || map == Map.Internal)
        {
            return;
        }

        var posts = new List<Item>();
        foreach (var item in map.GetItemsAt(location.X, location.Y))
        {
            if (item is Static { Name: PostName } && item.Z == location.Z)
            {
                posts.Add(item);
            }
        }

        foreach (var post in posts)
        {
            post.Delete();
        }
    }

    private static bool FindSignpost(Map map, int x, int y, int z, out int graphic, out int postZ)
    {
        foreach (var tile in map.Tiles.GetStaticTiles(x, y))
        {
            if (tile.ID is >= FirstSignpost and <= LastSignpost && System.Math.Abs(tile.Z - z) <= 30)
            {
                graphic = tile.ID;
                postZ = tile.Z;
                return true;
            }
        }

        foreach (var item in map.GetItemsAt(x, y))
        {
            if (item.ItemID is >= FirstSignpost and <= LastSignpost && System.Math.Abs(item.Z - z) <= 30)
            {
                graphic = item.ItemID;
                postZ = item.Z;
                return true;
            }
        }

        graphic = 0;
        postZ = 0;
        return false;
    }

    // Стена здания у двери: непроходимая статика-стена на высоте двери.
    private static bool HasWall(Map map, int x, int y, int z)
    {
        foreach (var tile in map.Tiles.GetStaticTiles(x, y))
        {
            var data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
            if (data.Wall && tile.Z <= z + 10 && tile.Z + data.CalcHeight > z)
            {
                return true;
            }
        }

        return false;
    }
}
