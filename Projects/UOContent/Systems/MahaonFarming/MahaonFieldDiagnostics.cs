using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Systems.MahaonSeasons;
using Server.Systems.MahaonWorld;

namespace Server.Systems.MahaonFarming;

/// <summary>
///     Диагностика полей: где они, сколько их и почему в них ничего не происходит.
///
///     Написано после того, как «поля глухо» оказалось проблемой координат, а не кода:
///     проверка шла в месте, где полей нет ни одного тайла, и отличить это от поломки было
///     нечем. Половина замешательства с полями — вопрос «а тут вообще должно что-то расти?»,
///     и отвечать на него угадыванием дорого.
///
///     Отдельно показывает сезон: грядка видна не всегда. Зимой она прячется, после сбора
///     тоже, и оба раза выглядит это одинаково — как будто поля исчезли.
/// </summary>
public static class MahaonFieldDiagnostics
{
    /// <summary>Размер участка, по которому группируем скопления.</summary>
    private const int ClusterSize = 32;

    public static void Configure()
    {
        CommandSystem.Register("Fields", AccessLevel.GameMaster, Fields_OnCommand);
    }

    [Usage("Fields [where]")]
    [Description("Что с полями: сезон, сколько грядок, что под ногами. 'where' — где они вообще есть.")]
    private static void Fields_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Length >= 1 && e.GetString(0).InsensitiveEquals("where"))
        {
            ReportClusters(from);
            return;
        }

        if (e.Length >= 1 && e.GetString(0).InsensitiveEquals("rotate"))
        {
            // Севооборот сам срабатывает раз в игровой год — это два часа реального
            // времени. Ждать их, чтобы посмотреть, работает ли он, невозможно.
            var fields = MahaonCropRotation.Rotate();

            from.SendMessage(
                fields > 0 ? 0x59 : 0x22,
                fields > 0
                    ? $"Севооборот прошёл: полей сменило культуру — {fields}."
                    : "Менять нечего: системных полей не найдено."
            );

            return;
        }

        if (e.Length >= 1 && e.GetString(0).InsensitiveEquals("plant"))
        {
            // Без культуры — берём наугад. Засевать делянки пачками удобнее, когда не надо
            // каждый раз придумывать, что тут будет расти; а список культур подсказываем
            // только когда человек ошибся в названии.
            MahaonCropType? crop = null;

            if (e.Length >= 2)
            {
                if (!Enum.TryParse<MahaonCropType>(e.GetString(1), true, out var parsed))
                {
                    from.SendMessage(0x22, $"Не знаю культуру «{e.GetString(1)}».");
                    from.SendMessage(0x3B2, string.Join(", ", Enum.GetNames<MahaonCropType>()));

                    return;
                }

                crop = parsed;
            }

            from.SendMessage(
                0x3B2,
                crop == null
                    ? "Укажи делянку — засею её целиком случайной культурой."
                    : $"Укажи делянку — засею её целиком: {crop}."
            );

            from.Target = new PlantTarget(crop);

            return;
        }

        ReportState(from);
    }

    /// <summary>
    ///     Ручная посадка делянки.
    ///
    ///     Нужна там, где вспаханная земля нарисована, а статики на ней нет вовсе:
    ///     автоматический засев идёт от статики и такое поле просто не видит. Игрок же
    ///     видит прямоугольник пашни и ждёт, что на нём растёт.
    /// </summary>
    private class PlantTarget : Targeting.Target
    {
        /// <summary>null — культуру выбрать наугад на месте.</summary>
        private readonly MahaonCropType? _crop;

        public PlantTarget(MahaonCropType? crop) : base(18, true, Targeting.TargetFlags.None) => _crop = crop;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D p || from.Map == null)
            {
                return;
            }

            var map = from.Map;
            var tiles = MahaonFieldPlots.FloodBarePlot(map, p.X, p.Y);

            if (tiles.Count == 0)
            {
                from.SendMessage(0x22, "Это не вспаханная земля — сажать не во что.");
                return;
            }

            // Культура выбирается ОДИН раз на делянку, а не на тайл: иначе вышла бы та же
            // мешанина, ради устранения которой всё разбиение на поля и затевалось.
            var all = Enum.GetValues<MahaonCropType>();
            var crop = _crop ?? all[Utility.Random(all.Length)];

            var planted = 0;

            foreach (var (x, y, z) in tiles)
            {
                var occupied = false;

                foreach (var existing in map.GetItemsInRange<MahaonCropTile>(new Point3D(x, y, z), 0))
                {
                    if (!existing.Deleted && existing.X == x && existing.Y == y)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (occupied)
                {
                    continue;
                }

                var tile = new MahaonCropTile(crop);
                tile.MoveToWorld(new Point3D(x, y, z), map);
                planted++;
            }

            from.SendMessage(
                0x59,
                $"Делянка {tiles.Count} тайлов, засеяно новых: {planted} — {crop}."
            );
        }
    }

    private static void ReportState(Mobile from)
    {
        var season = SeasonSystem.CurrentSeason;

        from.SendMessage(0x59, $"Сезон: {SeasonRu(season)}.");

        if (season != MahaonSeason.Autumn)
        {
            from.SendMessage(
                0x35,
                season == MahaonSeason.Winter
                    ? "Зимой грядки НЕВИДИМЫ и не собираются — это не поломка."
                    : "Собрать можно только осенью. Сейчас грядки видны, но не готовы."
            );
        }

        // Сколько грядок вообще в мире и на этом фасете.
        var total = 0;
        var onMap = 0;
        var invisible = 0;

        foreach (var item in World.Items.Values)
        {
            if (item is not MahaonCropTile { Deleted: false } tile)
            {
                continue;
            }

            total++;

            if (tile.Map == from.Map)
            {
                onMap++;

                if (!tile.Visible)
                {
                    invisible++;
                }
            }
        }

        from.SendMessage(
            0x59,
            total == 0
                ? "Грядок в мире НЕТ ВООБЩЕ — похоже, [MahaonFields ни разу не запускался."
                : $"Грядок в мире: {total}, на этом фасете: {onMap} (невидимых сейчас: {invisible})."
        );

        // Что под ногами и рядом.
        var map = from.Map;

        if (map == null)
        {
            return;
        }

        var near = new List<MahaonCropTile>();

        foreach (var tile in map.GetItemsInRange<MahaonCropTile>(from.Location, 12))
        {
            if (!tile.Deleted)
            {
                near.Add(tile);
            }
        }

        if (near.Count == 0)
        {
            from.SendMessage(0x22, "Рядом (12 тайлов) грядок нет.");
            from.SendMessage(0x3B2, "Где они есть: [Fields where");

            // Самое частое недоразумение: проверять поля там, где их не рисовали.
            var statics = CountFieldStaticsNear(map, from.Location, 12);

            from.SendMessage(
                statics > 0 ? 0x35 : 0x3B2,
                statics > 0
                    ? $"Но тайлов полей в карте рядом: {statics}. Значит засев их пропустил — стоит перезапустить [MahaonFields."
                    : "И в самой карте тут полей тоже нет — место просто не полевое."
            );

            return;
        }

        from.SendMessage(0x59, $"Грядок рядом: {near.Count}. Ближайшие:");

        var shown = 0;

        foreach (var tile in near)
        {
            if (shown++ >= 5)
            {
                break;
            }

            from.SendMessage(
                0x3B2,
                $"  {tile.CropType} в ({tile.X}, {tile.Y}, {tile.Z}) — графика {tile.ItemID:X4}, " +
                $"{(tile.Visible ? "видима" : "НЕВИДИМА")}"
            );
        }
    }

    /// <summary>Сколько тайлов полей нарисовано в самой карте рядом — независимо от грядок.</summary>
    private static int CountFieldStaticsNear(Map map, Point3D location, int radius)
    {
        var count = 0;

        for (var x = location.X - radius; x <= location.X + radius; x++)
        {
            for (var y = location.Y - radius; y <= location.Y + radius; y++)
            {
                if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                {
                    continue;
                }

                foreach (var tile in map.Tiles.GetStaticTiles(x, y))
                {
                    if (MahaonCrops.FromTile(tile.ID) != null)
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    /// <summary>
    ///     Где на этом фасете вообще есть поля. Считается по уже расставленным грядкам —
    ///     то есть показывает не «что нарисовано в карте», а «что реально работает».
    /// </summary>
    private static void ReportClusters(Mobile from)
    {
        var map = from.Map;

        if (map == null)
        {
            return;
        }

        var clusters = new Dictionary<(int, int), int>();

        foreach (var item in World.Items.Values)
        {
            if (item is not MahaonCropTile { Deleted: false } tile || tile.Map != map)
            {
                continue;
            }

            var key = (tile.X / ClusterSize, tile.Y / ClusterSize);

            clusters.TryGetValue(key, out var n);
            clusters[key] = n + 1;
        }

        if (clusters.Count == 0)
        {
            from.SendMessage(0x22, $"На {map.Name} грядок нет. Запусти [MahaonFields.");
            return;
        }

        var best = new List<((int X, int Y) Cell, int Count)>();

        foreach (var (cell, count) in clusters)
        {
            best.Add((cell, count));
        }

        best.Sort((a, b) => b.Count.CompareTo(a.Count));

        from.SendMessage(0x59, $"Скопления полей на {map.Name} (всего участков {clusters.Count}):");

        for (var i = 0; i < best.Count && i < 10; i++)
        {
            var (cell, count) = best[i];
            var x = cell.X * ClusterSize + ClusterSize / 2;
            var y = cell.Y * ClusterSize + ClusterSize / 2;

            from.SendMessage(0x3B2, $"  [go {x} {y}  —  грядок {count}");
        }
    }

    private static string SeasonRu(MahaonSeason season) => season switch
    {
        MahaonSeason.Spring => "весна (росток)",
        MahaonSeason.Summer => "лето (почти созрел)",
        MahaonSeason.Autumn => "осень — СБОР",
        _                   => "зима (грядки спрятаны)"
    };
}
