using System;
using System.Collections.Generic;
using Server.Regions;

namespace Server.Systems.MahaonMapEdits;

/// <summary>
///     Правила разрушения карты и само разрушение.
///
///     Отдельно от MahaonMapEdits намеренно: там механика («как записать правку»), здесь
///     политика («что вообще позволено сносить»). Политику придётся править куда чаще, и
///     держать её в одном месте важнее, чем сэкономить файл.
///
///     ---- Что считается постройкой ----
///
///     Стены и крыши помечены в tiledata флагами Wall и Roof, а скалы, горы и пол пещер —
///     нет. Проверено по самому tiledata, а не на глаз: из 1217 тайлов с «wall» в названии
///     флаг Wall стоит у всех, а из 138 «rock/mountain» — ни у одного. Значит одно это
///     условие отделяет дом от ландшафта, и выдумывать список графики не нужно.
///
///     Двери исключены отдельно. По флагам они проходят как стены (секретные двери — Wall +
///     Door), но в подземельях это загадка, а не препятствие, и сносить её взрывом значит
///     ломать чужой замысел.
///
///     ---- Где нельзя ----
///
///     В охраняемых городах. Иначе первый же шутник сносит банк Британии, и никакой откат
///     не вернёт людям потерянное за те полчаса, пока никто не заметил.
/// </summary>
public static class MahaonDemolition
{
    /// <summary>Можно ли снести этот тайл в принципе — вопрос только к графике.</summary>
    public static bool IsDestructible(int graphic)
    {
        // Построенное игроками рушится всегда. Настил не помечен ни стеной, ни крышей, и
        // по общему правилу оказался бы прочнее горы — мост, который нельзя взорвать, это
        // нелепость, да и половина смысла осады в том, чтобы отрезать переправу.
        if (MahaonBridging.IsPlank(graphic))
        {
            return true;
        }

        var data = TileData.ItemTable[graphic & TileData.MaxItemValue];

        if ((data.Flags & TileFlag.Door) != 0)
        {
            return false;
        }

        return (data.Flags & (TileFlag.Wall | TileFlag.Roof)) != 0;
    }

    /// <summary>Можно ли что-то сносить здесь — вопрос к месту.</summary>
    public static bool IsDestructibleArea(Map map, Point3D location)
    {
        if (map == null || map == Map.Internal)
        {
            return false;
        }

        var region = Region.Find(location, map);

        return region?.GetRegion<GuardedRegion>() == null;
    }

    /// <summary>
    ///     Сносит постройку в радиусе. Возвращает, сколько тайлов снесено.
    ///
    ///     Пометка (reason) обязательна и должна быть общей для всего события: по ней всё
    ///     разрушенное откатывается одним движением, если событие вышло криво. Поэтому
    ///     «подрыв Васи в 14:32», а не просто «взрыв» — иначе откат снесёт и чужие подрывы.
    /// </summary>
    public static int Demolish(Map map, Point3D center, int radius, string reason)
    {
        if (!IsDestructibleArea(map, center))
        {
            return 0;
        }

        var destroyed = 0;

        for (var x = center.X - radius; x <= center.X + radius; x++)
        {
            for (var y = center.Y - radius; y <= center.Y + radius; y++)
            {
                if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                {
                    continue;
                }

                // Круг, а не квадрат: квадратная дыра в стене читается как баг.
                var dx = x - center.X;
                var dy = y - center.Y;

                if (dx * dx + dy * dy > radius * radius)
                {
                    continue;
                }

                destroyed += DemolishTile(map, x, y, reason);
            }
        }

        return destroyed;
    }

    private static int DemolishTile(Map map, int x, int y, string reason)
    {
        var destroyed = 0;

        // Берём снимок: RemoveStatics меняет выведенное состояние под нами.
        var present = new List<MapStatic>(MahaonMapEdits.StaticsAt(map, x, y));

        if (present.Count == 0)
        {
            // Блока ещё не касались — значит смотрим прямо в карту.
            foreach (var tile in map.Tiles.GetStaticTiles(x, y))
            {
                if (IsDestructible(tile.ID))
                {
                    destroyed += MahaonMapEdits.RemoveStatics(map, x, y, tile.ID, reason);
                }
            }

            return destroyed;
        }

        foreach (var s in present)
        {
            if (IsDestructible(s.Graphic))
            {
                destroyed += MahaonMapEdits.RemoveStatics(map, x, y, s.Graphic, reason);
            }
        }

        return destroyed;
    }

    /// <summary>Общая пометка для одного события — чтобы откат брал его целиком.</summary>
    public static string ReasonFor(Mobile author, string what) =>
        $"{what}: {author?.RawName ?? "неизвестно"} в {Core.Now:HH:mm dd.MM}";
}
