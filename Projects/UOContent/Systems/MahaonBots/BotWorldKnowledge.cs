using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonBots;

/// <summary>
///     Куда ходить не надо.
///
///     До сих пор бот выбирал цель похода, ничего о ней не зная: BlindScoutPoint брал
///     случайный угол и случайное расстояние и объявлял получившуюся точку местом
///     назначения — не проверяя, суша ли это вообще, тот ли материк, не середина ли озера
///     и не внутренность ли горы. Дальше он честно шёл туда четверть часа, признавал цель
///     недостижимой, вставал — и рулетка занятий могла выдать ему ровно ту же точку снова.
///     Проблема выглядела как «плохой поиск пути», но искать путь было некуда: беда была
///     в цели, а не в дороге.
///
///     Здесь копится то, чего боту хватило бы, чтобы больше туда не ходить: места, до
///     которых дойти не удалось. Память общая на всех, как и склад известных жил и
///     деревьев (BotMobile.SharedMineSpots и соседи), — один бот убился о недостижимый
///     островок, остальные туда уже не пойдут.
///
///     Забывается со временем: мир меняется, мост могут построить, дверь открыть. Держать
///     запрет вечным значило бы постепенно выжечь карту.
/// </summary>
public static class BotWorldKnowledge
{
    /// <summary>Насколько близко к прежней неудаче должна лежать новая цель, чтобы
    /// считаться той же самой. Тайлов.</summary>
    private const int SameSpotRadius = 6;

    /// <summary>Через сколько запрет снимается.</summary>
    private static readonly TimeSpan Forgets = TimeSpan.FromMinutes(30);

    /// <summary>Больше этого не храним — список обходится линейно, а мир не бесконечен.</summary>
    private const int MaxRemembered = 512;

    private static readonly Dictionary<Map, List<(Point3D loc, DateTime until)>> Unreachable = new();

    /// <summary>Сюда дойти не удалось — какое-то время не предлагать это место снова.</summary>
    public static void MarkUnreachable(Map map, Point3D loc)
    {
        if (map == null || map == Map.Internal)
        {
            return;
        }

        if (!Unreachable.TryGetValue(map, out var list))
        {
            Unreachable[map] = list = new List<(Point3D, DateTime)>();
        }

        Prune(list);

        for (var i = 0; i < list.Count; i++)
        {
            if (IsNear(list[i].loc, loc))
            {
                list[i] = (list[i].loc, Core.Now + Forgets); // уже знали — продлеваем
                return;
            }
        }

        if (list.Count >= MaxRemembered)
        {
            list.RemoveAt(0); // самое старое уступает место свежей неудаче
        }

        list.Add((loc, Core.Now + Forgets));
    }

    /// <summary>Ходили ли туда недавно безуспешно.</summary>
    public static bool IsKnownUnreachable(Map map, Point3D loc)
    {
        if (map == null || !Unreachable.TryGetValue(map, out var list))
        {
            return false;
        }

        Prune(list);

        foreach (var (known, _) in list)
        {
            if (IsNear(known, loc))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Годится ли точка в цель похода вообще: внутри карты, на неё можно встать, и
    ///     недавно об неё никто не убился. Это не проверка проходимости — полноценный
    ///     путь считать дорого, — но она отсекает то, что и путём не является: воду,
    ///     скалу, край карты.
    /// </summary>
    public static bool IsPlausibleDestination(Map map, Point3D loc)
    {
        if (map == null || map == Map.Internal)
        {
            return false;
        }

        if (loc.X < 0 || loc.Y < 0 || loc.X >= map.Width || loc.Y >= map.Height)
        {
            return false;
        }

        return map.CanSpawnMobile(loc) && !IsKnownUnreachable(map, loc);
    }

    private static bool IsNear(Point3D a, Point3D b) =>
        Math.Abs(a.X - b.X) <= SameSpotRadius && Math.Abs(a.Y - b.Y) <= SameSpotRadius;

    private static void Prune(List<(Point3D loc, DateTime until)> list)
    {
        var now = Core.Now;

        for (var i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].until <= now)
            {
                list.RemoveAt(i);
            }
        }
    }
}
