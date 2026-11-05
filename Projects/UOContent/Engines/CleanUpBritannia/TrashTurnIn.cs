using System.Collections.Generic;
using Server.Items;

namespace Server.Engines.CleanUpBritannia;

/// <summary>
///     Сдача мусора: в ServUO очки начислялись из <c>BaseTrash</c>, которого в этом репозитории нет —
///     бочка и сундук для мусора существуют отдельно. Здесь собрана общая для них логика: посчитать
///     стоимость предмета (и всего, что лежит внутри контейнера) и выдать очки с сообщением 1151280.
/// </summary>
public static class TrashTurnIn
{
    /// <summary>Стоимость предмета вместе с содержимым, если это контейнер.</summary>
    public static double Collect(Item item)
    {
        if (item == null || item.Deleted)
        {
            return 0;
        }

        var points = CleanUpBritanniaData.GetPoints(item);

        if (item is Container container)
        {
            foreach (var child in container.Items)
            {
                points += Collect(child);
            }
        }

        return points;
    }

    /// <summary>Начисляет очки и сообщает игроку, сколько предметов и очков он сдал.</summary>
    public static void Award(Mobile from, double points, int count)
    {
        if (!CleanUpBritanniaData.Enabled || from == null || points <= 0)
        {
            return;
        }

        // You have received approximately ~1_VALUE~points for turning in ~2_COUNT~items for Clean Up Britannia.
        from.SendLocalizedMessage(1151280, $"{points}\t{count}");
        CleanUpBritanniaData.AwardPoints(from, points, false);
    }

    /// <summary>
    ///     Суммирует отложенные очки по игрокам и выдаёт их. Предметы, которые успели вынуть из
    ///     контейнера, не учитываются.
    /// </summary>
    public static int AwardPending(Container trash, List<(Mobile From, Item Item, double Points)> pending)
    {
        if (pending.Count == 0)
        {
            return 0;
        }

        var totals = new Dictionary<Mobile, (double Points, int Count)>();

        foreach (var (from, item, points) in pending)
        {
            if (from == null || item == null || item.Deleted || item.RootParent != trash)
            {
                continue;
            }

            totals.TryGetValue(from, out var sum);
            totals[from] = (sum.Points + points, sum.Count + 1);
        }

        pending.Clear();

        foreach (var (from, sum) in totals)
        {
            Award(from, sum.Points, sum.Count);
        }

        return totals.Count;
    }
}
