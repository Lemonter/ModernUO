using System.Collections.Generic;
using System.Linq;
using Server.Systems.MahaonMapEdits;
using Xunit;

namespace Server.Tests.Items;

/// <summary>
///     Тесты двоичного формата разностных файлов карты.
///
///     Это ровно тот код, где ошибка обходится дороже всего. Запекание переписывает файлы в
///     каталоге клиента; ошибка на один байт или на один блок не падает, а тихо портит
///     карту — и портит её сразу у всех, кто получит обновление. Заметят это не по
///     исключению в логе, а по дому, уехавшему на восемь тайлов.
///
///     Поэтому проверка построена как round-trip: собрали заплату — разобрали тем же
///     способом, каким её читает движок (TileMatrixPatch), — сверили с исходными данными.
/// </summary>
public class MahaonMapBakerTests
{
    private static List<MapStatic> Wall() => new()
    {
        new MapStatic(0x0006, 1, 2, 3, 0),
        new MapStatic(0x1797, 7, 7, -5, 0x482),
        new MapStatic(0xA8CA, 0, 0, 127, 0)
    };

    [Fact]
    public void StaticPatch_SurvivesRoundTrip()
    {
        var blocks = new List<(int, List<MapStatic>)>
        {
            (1234, Wall()),
            (5678, new List<MapStatic> { new(0x0C57, 4, 4, 0, 0) })
        };

        var (index, lookup, data) = MahaonMapBaker.BuildStaticPatch(blocks);
        var parsed = MahaonMapBaker.ParseStaticPatch(index, lookup, data);

        Assert.Equal(2, parsed.Count);
        Assert.Equal(Wall(), parsed[1234]);
        Assert.Single(parsed[5678]);
        Assert.Equal(new MapStatic(0x0C57, 4, 4, 0, 0), parsed[5678][0]);
    }

    /// <summary>
    ///     Пустой блок обязан записаться как offset = -1, а не как запись нулевой длины.
    ///     Именно по этому признаку движок понимает «блок пуст» — на этом стоит снос зданий.
    /// </summary>
    [Fact]
    public void EmptyBlock_IsWrittenAsDemolition()
    {
        var blocks = new List<(int, List<MapStatic>)> { (42, new List<MapStatic>()) };

        var (index, lookup, data) = MahaonMapBaker.BuildStaticPatch(blocks);

        Assert.Equal(4, index.Length);
        Assert.Equal(12, lookup.Length);
        Assert.Empty(data);
        Assert.Equal(-1, System.BitConverter.ToInt32(lookup, 0));

        var parsed = MahaonMapBaker.ParseStaticPatch(index, lookup, data);

        Assert.Empty(Assert.Contains(42, parsed));
    }

    /// <summary>Отрицательная высота и hue выше 0x7FFF не должны переполниться при записи.</summary>
    [Fact]
    public void StaticPatch_KeepsNegativeZAndHighHue()
    {
        var tiles = new List<MapStatic> { new(0xFFFF, 7, 7, -128, unchecked((short)0xF000)) };
        var (index, lookup, data) = MahaonMapBaker.BuildStaticPatch(new List<(int, List<MapStatic>)> { (7, tiles) });

        var parsed = MahaonMapBaker.ParseStaticPatch(index, lookup, data);

        Assert.Equal(tiles[0], parsed[7][0]);
    }

    [Fact]
    public void LandPatch_SurvivesRoundTrip()
    {
        var land = new MapLand[64];

        for (var i = 0; i < 64; i++)
        {
            land[i] = new MapLand((ushort)(0x00A8 + i), (sbyte)(i - 32));
        }

        var (index, data) = MahaonMapBaker.BuildLandPatch(new List<(int, MapLand[])> { (99, land) });
        var parsed = MahaonMapBaker.ParseLandPatch(index, data);

        Assert.Equal(land, Assert.Contains(99, parsed));
    }

    /// <summary>
    ///     У земли нет справочника: записи сопоставляются с указателем по порядку. Если
    ///     шаг записи посчитан неверно, второй блок прочитается со сдвигом — проверяем
    ///     именно на нескольких блоках, на одном такая ошибка не видна.
    /// </summary>
    [Fact]
    public void LandPatch_KeepsBlocksAlignedAcrossSeveralEntries()
    {
        var blocks = new List<(int, MapLand[])>();

        for (var b = 0; b < 4; b++)
        {
            var land = new MapLand[64];

            for (var i = 0; i < 64; i++)
            {
                land[i] = new MapLand((ushort)(b * 1000 + i), (sbyte)b);
            }

            blocks.Add((b * 777, land));
        }

        var (index, data) = MahaonMapBaker.BuildLandPatch(blocks);
        var parsed = MahaonMapBaker.ParseLandPatch(index, data);

        Assert.Equal(4, parsed.Count);

        foreach (var (blockId, land) in blocks)
        {
            Assert.Equal(land, parsed[blockId]);
        }
    }

    /// <summary>
    ///     Перенос чужих заплат: блоки, которые мы правим, должны выпасть, остальные —
    ///     сохраниться. Без этого запекание стирало бы правки карты от EA.
    /// </summary>
    [Fact]
    public void ParseStaticPatch_SkipsBlocksWeAreAboutToRewrite()
    {
        var blocks = new List<(int, List<MapStatic>)>
        {
            (10, Wall()),
            (20, Wall()),
            (30, Wall())
        };

        var (index, lookup, data) = MahaonMapBaker.BuildStaticPatch(blocks);
        var parsed = MahaonMapBaker.ParseStaticPatch(index, lookup, data, new[] { 20 });

        Assert.Equal(new[] { 10, 30 }, parsed.Keys.OrderBy(k => k).ToArray());
    }

    /// <summary>
    ///     Пересборка уже собранной заплаты обязана давать те же байты — иначе повторное
    ///     запекание тихо расходилось бы с предыдущим.
    /// </summary>
    [Fact]
    public void Rebaking_IsStable()
    {
        var blocks = new List<(int, List<MapStatic>)> { (1, Wall()), (2, new List<MapStatic>()) };

        var first = MahaonMapBaker.BuildStaticPatch(blocks);
        var parsed = MahaonMapBaker.ParseStaticPatch(first.Index, first.Lookup, first.Data);

        var again = MahaonMapBaker.BuildStaticPatch(
            parsed.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)).ToList()
        );

        Assert.Equal(first.Index, again.Index);
        Assert.Equal(first.Lookup, again.Lookup);
        Assert.Equal(first.Data, again.Data);
    }
}
