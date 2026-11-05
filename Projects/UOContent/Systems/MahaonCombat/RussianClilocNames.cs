using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Русские названия предметов для серверных текстов — журнала боя и прочего, что сервер
///     пишет сам, а не номером cliloc.
///
///     ---- Откуда ----
///
///     Клиент показывает предметы по номеру cliloc, а перевод лежит в Cliloc_ru.csv в папке
///     клиента («номер;текст;флаг»). Сервер его раньше не читал, поэтому в журнале выходило
///     «по RingmailChest» — имя класса, потому что у стандартной брони Name пустой. Читаем тот
///     же CSV лениво при первом обращении; нет файла — откатываемся на английский cliloc.
/// </summary>
public static class RussianClilocNames
{
    private static Dictionary<int, string> _table;

    /// <summary>Имя предмета для текста: своё имя, иначе русский cliloc, иначе английский.</summary>
    public static string NameOf(Item item)
    {
        if (item == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(item.Name))
        {
            return item.Name;
        }

        return Get(item.LabelNumber) ?? Localization.GetText(item.LabelNumber)?.ToLowerInvariant() ?? item.GetType().Name;
    }

    public static string Get(int number)
    {
        _table ??= Load();
        return _table.TryGetValue(number, out var text) ? text : null;
    }

    private static Dictionary<int, string> Load()
    {
        var table = new Dictionary<int, string>();
        var path = Core.FindDataFile("Cliloc_ru.csv", false);

        if (path == null || !File.Exists(path))
        {
            return table;
        }

        try
        {
            foreach (var line in File.ReadLines(path))
            {
                var first = line.IndexOf(';');

                if (first <= 0 || !int.TryParse(line.AsSpan(0, first), out var number))
                {
                    continue; // заголовок или битая строка
                }

                var last = line.LastIndexOf(';');
                var text = last > first ? line.Substring(first + 1, last - first - 1) : line[(first + 1)..];

                if (text.Length > 0 && !text.Contains('~'))
                {
                    table[number] = text; // шаблоны с ~аргументами~ для имён не годятся
                }
            }
        }
        catch (IOException)
        {
            // файл занят или повреждён — журнал просто останется на английском
        }

        return table;
    }
}
