using System;
using System.Collections.Generic;
using System.Linq;
using Server.Commands;

namespace Server.Systems.MahaonDiagnostics;

/// <summary>
///     Temporary diagnostic — [CountEntities reports how many of each Item/Mobile type
///     exist right now, sorted by count descending. Meant to answer "why does the world
///     have ~2 million entities when it looks empty" by showing exactly which type is
///     ballooning, rather than guessing (blood decals? orphaned raid mobs? something
///     else?). Remove once the underlying leak is actually found and fixed.
/// </summary>
public static class MahaonEntityCounter
{
    public static void Initialize()
    {
        CommandSystem.Register("CountEntities", AccessLevel.GameMaster, CountEntities_OnCommand);
    }

    [Usage("CountEntities")]
    [Description("Считает все Item/Mobile в мире по типам, выводит топ-30 по количеству.")]
    private static void CountEntities_OnCommand(CommandEventArgs e)
    {
        var counts = new Dictionary<string, int>();

        foreach (var item in World.Items.Values)
        {
            var name = item.GetType().Name;
            counts.TryGetValue(name, out var c);
            counts[name] = c + 1;
        }

        foreach (var mobile in World.Mobiles.Values)
        {
            var name = mobile.GetType().Name;
            counts.TryGetValue(name, out var c);
            counts[name] = c + 1;
        }

        var totalItems = World.Items.Count;
        var totalMobiles = World.Mobiles.Count;

        e.Mobile.SendMessage(0x59, $"Всего: {totalItems} предметов, {totalMobiles} мобилов, {totalItems + totalMobiles} суммарно.");
        e.Mobile.SendMessage(0x59, "Топ-30 по количеству:");

        foreach (var (name, count) in counts.OrderByDescending(kv => kv.Value).Take(30))
        {
            e.Mobile.SendMessage(0x22, $"{name}: {count}");
            Console.WriteLine($"[MahaonEntityCounter] {name}: {count}");
        }

        Console.WriteLine($"[MahaonEntityCounter] Всего: {totalItems} предметов, {totalMobiles} мобилов.");
    }
}
