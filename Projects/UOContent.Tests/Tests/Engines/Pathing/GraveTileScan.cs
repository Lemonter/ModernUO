using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding;

/// <summary>
///     Разведка по tiledata: какие статики клиент сам называет могилами и надгробиями.
///
///     Нужна, чтобы список графики для копки кладбищ был взят из настоящих данных
///     клиента, а не выписан по памяти. Тест ничего не проверяет — он печатает найденное
///     в вывод, а список потом переносится в определение добычи.
/// </summary>
[Collection("Sequential Pathfinding Tests")]
public class GraveTileScan
{
    private readonly ITestOutputHelper _output;

    public GraveTileScan(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ListGraveTiles()
    {
        var words = new[] { "grave", "tomb", "coffin", "casket", "bone", "skull" };
        var found = new SortedDictionary<int, string>();

        for (var id = 0; id < TileData.ItemTable.Length; id++)
        {
            var name = TileData.ItemTable[id].Name;

            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            foreach (var word in words)
            {
                if (name.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    found[id] = name;
                    break;
                }
            }
        }

        _output.WriteLine($"размер таблицы: {TileData.ItemTable.Length}");

        for (var probe = 0x1165; probe <= 0x117B; probe++)
        {
            _output.WriteLine($"проба 0x{probe:X4}: «{TileData.ItemTable[probe].Name}» flags={TileData.ItemTable[probe].Flags}");
        }

        foreach (var (id, name) in found)
        {
            _output.WriteLine($"0x{id:X4}  {name}");
        }

        _output.WriteLine($"— всего {found.Count}");
    }
}
