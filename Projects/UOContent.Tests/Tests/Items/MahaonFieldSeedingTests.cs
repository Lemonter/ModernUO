using System;
using System.Collections.Generic;
using Server.Systems.MahaonFarming;
using Xunit;

namespace Server.Tests.Items;

/// <summary>
///     Тесты разбора карты для команды [MahaonFields.
///
///     Проверяют ровно то, что ошибается тихо. Команда читает staidxN.mul/staticsN.mul
///     напрямую (через TileMatrix нельзя — он кэширует каждый прочитанный блок навсегда, а
///     тут обход всего мира), и вся её правильность держится на арифметике смещений.
///     Перепутать в ней порядок блоков или байт внутри записи — значит засеять мир
///     грядками со сдвигом: правдоподобно выглядящим и потому незамеченным до тех пор,
///     пока кто-нибудь не найдёт пшеницу посреди океана.
///
///     Файлов карты в тестовом хосте нет, поэтому блоки собираются здесь руками — что даже
///     лучше: известен точный ожидаемый ответ.
/// </summary>
public class MahaonFieldSeedingTests
{
    private const int BlockRecord = 12;
    private const int StaticRecord = 7;
    private const int Wheat = 0x0C57;
    private const int NotACrop = 0x0EED; // мешок золота — заведомо не поле

    /// <summary>Собирает индекс и статику из описания «в блоке B лежат такие-то тайлы».</summary>
    private static (byte[] Index, byte[] Statics) Build(
        int blockCount, params (int Block, (int Id, int X, int Y, int Z)[] Tiles)[] blocks
    )
    {
        var index = new byte[blockCount * BlockRecord];

        // Блоки без статики помечаем lookup = -1 — так это и лежит в настоящем staidx.
        for (var b = 0; b < blockCount; b++)
        {
            BitConverter.GetBytes(-1).CopyTo(index, b * BlockRecord);
        }

        var statics = new List<byte>();

        foreach (var (block, tiles) in blocks)
        {
            var lookup = statics.Count;

            foreach (var (id, x, y, z) in tiles)
            {
                statics.AddRange(BitConverter.GetBytes((ushort)id));
                statics.Add((byte)x);
                statics.Add((byte)y);
                statics.Add(unchecked((byte)(sbyte)z));
                statics.AddRange(BitConverter.GetBytes((short)0)); // hue
            }

            BitConverter.GetBytes(lookup).CopyTo(index, block * BlockRecord);
            BitConverter.GetBytes(tiles.Length * StaticRecord).CopyTo(index, block * BlockRecord + 4);
        }

        return (index, statics.ToArray());
    }

    private static bool[] WantWheat()
    {
        var wanted = new bool[0x10000];
        wanted[Wheat] = true;
        return wanted;
    }

    /// <summary>
    ///     Блок в индексе адресуется как (bx * blockHeight + by), а x/y внутри записи —
    ///     смещение в клетке 8x8. Значит блок 3 при blockHeight 512 это bx=0, by=3, и тайл
    ///     со смещением (5, 2) обязан оказаться в мировых (5, 26).
    /// </summary>
    [Fact]
    public void ScanBlock_ResolvesWorldCoordinates()
    {
        var (index, statics) = Build(1024, (3, new[] { (Wheat, 5, 2, 7) }));
        var found = new List<MahaonFieldSeeding.FieldTile>();

        MahaonFieldSeeding.ScanBlock(index, statics, 3, 512, WantWheat(), found);

        var tile = Assert.Single(found);
        Assert.Equal(5, tile.X);
        Assert.Equal(3 * 8 + 2, tile.Y);
        Assert.Equal(7, tile.Z);
        Assert.Equal(Wheat, tile.Graphic);
    }

    /// <summary>
    ///     Тот же тайл в блоке 512 (bx=1, by=0) — проверяет, что делится именно на
    ///     blockHeight, а не на ширину. Перепутать эти две величины и есть главный способ
    ///     засеять мир со сдвигом: карта Фелуки 896x512 блоков, и обе цифры правдоподобны.
    /// </summary>
    [Fact]
    public void ScanBlock_DividesByBlockHeightNotWidth()
    {
        var (index, statics) = Build(1024, (512, new[] { (Wheat, 1, 1, 0) }));
        var found = new List<MahaonFieldSeeding.FieldTile>();

        MahaonFieldSeeding.ScanBlock(index, statics, 512, 512, WantWheat(), found);

        var tile = Assert.Single(found);
        Assert.Equal(8 + 1, tile.X);
        Assert.Equal(1, tile.Y);
    }

    /// <summary>Отрицательная высота — обычное дело в подземельях и на берегу.</summary>
    [Fact]
    public void ScanBlock_ReadsNegativeZ()
    {
        var (index, statics) = Build(64, (0, new[] { (Wheat, 0, 0, -35) }));
        var found = new List<MahaonFieldSeeding.FieldTile>();

        MahaonFieldSeeding.ScanBlock(index, statics, 0, 8, WantWheat(), found);

        Assert.Equal(-35, Assert.Single(found).Z);
    }

    [Fact]
    public void ScanBlock_SkipsTilesThatAreNotCrops()
    {
        var (index, statics) = Build(
            64,
            (0, new[] { (NotACrop, 0, 0, 0), (Wheat, 4, 4, 0), (NotACrop, 7, 7, 0) })
        );
        var found = new List<MahaonFieldSeeding.FieldTile>();

        MahaonFieldSeeding.ScanBlock(index, statics, 0, 8, WantWheat(), found);

        Assert.Equal(Wheat, Assert.Single(found).Graphic);
    }

    /// <summary>
    ///     Разбор имени фасета не должен падать на постороннем слове.
    ///
    ///     Map.Parse бросает FormatException на всём, что не фасет, и команда [MahaonFields
    ///     clear роняла в консоль стек вместо того, чтобы сказать человеку, что он
    ///     опечатался. Ловим саму привычку: к разбору пользовательского ввода ходим только
    ///     через TryParse.
    /// </summary>
    [Theory]
    [InlineData("clear")]
    [InlineData("всё")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Felucca")]
    public void MapTryParse_NeverThrowsOnUserInput(string input)
    {
        var exception = Record.Exception(() => Map.TryParse(input, null, out _));

        Assert.Null(exception);
    }

    /// <summary>Пустой блок (lookup = -1) — это норма, а не повод упасть.</summary>
    [Fact]
    public void ScanBlock_IgnoresEmptyBlocks()
    {
        var (index, statics) = Build(64, (0, new[] { (Wheat, 0, 0, 0) }));
        var found = new List<MahaonFieldSeeding.FieldTile>();

        MahaonFieldSeeding.ScanBlock(index, statics, 5, 8, WantWheat(), found);

        Assert.Empty(found);
    }

    /// <summary>
    ///     Битый индекс не должен ронять команду посреди мира: длина, уводящая за конец
    ///     файла, встречается в самодельных картах.
    /// </summary>
    [Fact]
    public void ScanBlock_SurvivesCorruptIndex()
    {
        var (index, statics) = Build(64, (0, new[] { (Wheat, 0, 0, 0) }));
        BitConverter.GetBytes(9999).CopyTo(index, 4); // length за пределами файла

        var found = new List<MahaonFieldSeeding.FieldTile>();

        MahaonFieldSeeding.ScanBlock(index, statics, 0, 8, WantWheat(), found);
        MahaonFieldSeeding.ScanBlock(index, statics, 999, 8, WantWheat(), found); // блок за концом индекса

        Assert.Empty(found);
    }
}
