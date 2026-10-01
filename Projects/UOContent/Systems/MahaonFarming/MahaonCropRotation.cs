using System;
using System.Collections.Generic;
using Server.Items;
using Server.Systems.MahaonSeasons;
using Server.Systems.MahaonWorld;

namespace Server.Systems.MahaonFarming;

/// <summary>
///     Севооборот: каждый новый год роста поле засевается другой культурой.
///
///     Было поле кабачков — стало льна, потом кукурузы, в другой раз моркови. Мир перестаёт
///     выглядеть застывшим: одни и те же делянки год за годом выдают разное, и запоминать
///     «за пшеницей мне сюда» больше не выходит.
///
///     ---- Когда именно ----
///
///     На весне, то есть в начале нового цикла роста, а не на каждой смене сезона. Цикл у
///     грядки один и тот же: росток весной, наливается летом, собирают осенью, спит зимой.
///     Меняй мы культуру посреди цикла — кабачок превращался бы в лён между ростком и
///     урожаем, и это выглядело бы поломкой, а не севооборотом.
///
///     ---- Полями, а не тайлами ----
///
///     Культура выбирается на ПОЛЕ целиком. Ради этого поля и заводились: на делянке должна
///     расти одна культура. Меняй мы каждую грядку по отдельности, вернулась бы ровно та
///     мешанина, от которой уходили.
///
///     Границы поля не хранятся — они пересобираются заливкой по соседству прямо перед
///     сменой. Это и дешевле (нет нового поля в сохранении у пяти тысяч грядок), и надёжнее:
///     если делянку расширили руками или часть грядок снесли, севооборот увидит её такой,
///     какая она сейчас, а не какой была при засеве.
///
///     ---- Что НЕ вращается ----
///
///     Грядки, посеянные игроком семенами на своей земле. Человек выбрал культуру
///     осознанно, и менять её за него нельзя — иначе посаженный женьшень однажды окажется
///     репой. Отличаем по тому же признаку, что и уборка: поле лежит на пашне или на тайле
///     карты, огород игрока — на обычной земле.
/// </summary>
public static class MahaonCropRotation
{
    /// <summary>Включён ли севооборот. Выключается настройкой, если надоест.</summary>
    public static bool Enabled { get; set; } = true;

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("farming.cropRotation", true);
        SeasonSystem.OnSeasonChanged += OnSeasonChanged;
    }

    private static void OnSeasonChanged(MahaonSeason season)
    {
        if (!Enabled || season != MahaonSeason.Spring)
        {
            return;
        }

        Rotate();
    }

    /// <summary>Меняет культуру на всех системных полях. Возвращает, сколько полей сменилось.</summary>
    public static int Rotate()
    {
        var beds = CollectRotatable();

        if (beds.Count == 0)
        {
            return 0;
        }

        var visited = new HashSet<(int, int, int)>();
        var fields = 0;

        foreach (var (key, bed) in beds)
        {
            if (!visited.Add(key))
            {
                continue;
            }

            var field = new List<MahaonCropTile>();

            Flood(beds, key, visited, field);

            if (field.Count == 0)
            {
                continue;
            }

            var next = NextCrop(bed.CropType);

            foreach (var tile in field)
            {
                tile.SetCrop(next);
            }

            fields++;
        }

        return fields;
    }

    /// <summary>
    ///     Грядки, которые вообще участвуют: только системные поля, без игроковых огородов.
    ///     Ключ — карта и координаты, по ним же идёт заливка.
    /// </summary>
    private static Dictionary<(int MapId, int X, int Y), MahaonCropTile> CollectRotatable()
    {
        var beds = new Dictionary<(int, int, int), MahaonCropTile>();

        foreach (var tile in MahaonCropTile.All)
        {
            if (tile.Deleted || tile.Map == null || tile.Map == Map.Internal)
            {
                continue;
            }

            if (!MahaonFieldSeeding.IsSystemField(tile))
            {
                continue;
            }

            beds[(tile.Map.MapID, tile.X, tile.Y)] = tile;
        }

        return beds;
    }

    /// <summary>Собирает одно поле — все грядки, соединённые по соседству.</summary>
    private static void Flood(
        Dictionary<(int MapId, int X, int Y), MahaonCropTile> beds,
        (int MapId, int X, int Y) start,
        HashSet<(int, int, int)> visited,
        List<MahaonCropTile> field
    )
    {
        var queue = new Queue<(int MapId, int X, int Y)>();

        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var key = queue.Dequeue();

            if (!beds.TryGetValue(key, out var tile))
            {
                continue;
            }

            field.Add(tile);

            // Восьмистороннее соседство — то же, что при разбиении на делянки. Плюс
            // перешагивание через один тайл: у поля снят периметр, и между двумя частями
            // одной делянки может быть пустая полоса.
            for (var dx = -2; dx <= 2; dx++)
            {
                for (var dy = -2; dy <= 2; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    var next = (key.MapId, key.X + dx, key.Y + dy);

                    if (beds.ContainsKey(next) && visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }
        }
    }

    /// <summary>Случайная культура, но обязательно не та же самая.</summary>
    public static MahaonCropType NextCrop(MahaonCropType current)
    {
        var all = Enum.GetValues<MahaonCropType>();

        if (all.Length <= 1)
        {
            return current;
        }

        // Выбираем из всех, кроме текущей, а не «тянем пока не совпадёт»: второе при
        // единственной альтернативе крутилось бы вхолостую, а при невезении — долго.
        var index = Utility.Random(all.Length - 1);

        return all[index] == current ? all[^1] : all[index];
    }
}
