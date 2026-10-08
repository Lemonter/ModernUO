using System.Collections.Generic;
using Server.Systems.MahaonFarming;
using Server.Systems.MahaonWorld;
using Xunit;

namespace Server.Tests.Items;

/// <summary>
///     Тесты разбиения полей на делянки.
///
///     Проверяется то, ради чего разбиение и появилось: на одной делянке должна расти одна
///     культура. Раньше грядка ставилась по графике своего тайла, и на поле выходила
///     мешанина — декоратор расставлял статику как попало, а засев честно это повторял.
///
///     Отдельно закреплено, что культура при отсутствии статики выбирается ОТ КООРДИНАТ, а
///     не случайно: засев идемпотентен, и повторный запуск обязан дать тот же результат.
///     Со случайным выбором каждый прогон перепахивал бы игрокам весь мир.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class MahaonFieldPlotsTests
{
    private static Map Facet => Map.Felucca;

    private static Dictionary<(int X, int Y), (int Z, MahaonCropKind Kind)> Tiles(
        params (int X, int Y, MahaonCropKind Kind)[] tiles
    )
    {
        var map = new Dictionary<(int X, int Y), (int Z, MahaonCropKind Kind)>();

        foreach (var (x, y, kind) in tiles)
        {
            map[(x, y)] = (0, kind);
        }

        return map;
    }

    /// <summary>Смежные тайлы — одно поле, даже если культуры на них разные.</summary>
    [Fact]
    public void AdjacentTiles_FormOnePlot()
    {
        var plots = MahaonFieldPlots.Build(
            Facet,
            Tiles(
                (1000, 1000, MahaonCropKind.Wheat),
                (1001, 1000, MahaonCropKind.Cabbage),
                (1002, 1000, MahaonCropKind.Wheat)
            )
        );

        Assert.Single(plots);
        Assert.Equal(3, plots[0].Tiles.Count);
    }

    /// <summary>Главное: у поля одна культура, и это большинство нарисованного.</summary>
    [Fact]
    public void Plot_TakesTheMajorityCrop()
    {
        var plots = MahaonFieldPlots.Build(
            Facet,
            Tiles(
                (1100, 1100, MahaonCropKind.Wheat),
                (1101, 1100, MahaonCropKind.Wheat),
                (1102, 1100, MahaonCropKind.Wheat),
                (1103, 1100, MahaonCropKind.Cabbage)
            )
        );

        Assert.Single(plots);
        Assert.Equal(MahaonCropType.Wheat, plots[0].Crop);
    }

    /// <summary>Диагональ считается соседством — иначе делянка распалась бы на полосы.</summary>
    [Fact]
    public void DiagonalNeighbours_StayInTheSamePlot()
    {
        var plots = MahaonFieldPlots.Build(
            Facet,
            Tiles(
                (1200, 1200, MahaonCropKind.Onion),
                (1201, 1201, MahaonCropKind.Onion)
            )
        );

        Assert.Single(plots);
        Assert.Equal(2, plots[0].Tiles.Count);
    }

    /// <summary>Разнесённые далеко тайлы — разные поля, и у каждого своя культура.</summary>
    [Fact]
    public void DistantTiles_FormSeparatePlots()
    {
        var plots = MahaonFieldPlots.Build(
            Facet,
            Tiles(
                (1300, 1300, MahaonCropKind.Wheat),
                (1400, 1400, MahaonCropKind.Cabbage)
            )
        );

        Assert.Equal(2, plots.Count);
        Assert.Contains(plots, p => p.Crop == MahaonCropType.Wheat);
        Assert.Contains(plots, p => p.Crop == MahaonCropType.Cabbage);
    }

    /// <summary>
    ///     Выбор культуры для делянки без статики обязан быть устойчивым: тот же тайл —
    ///     та же культура, сколько бы раз ни запускали засев.
    /// </summary>
    [Fact]
    public void CropChoice_IsStableAcrossRuns()
    {
        var first = MahaonFieldPlots.Build(Facet, Tiles((1500, 1500, MahaonCropKind.Wheat)));
        var second = MahaonFieldPlots.Build(Facet, Tiles((1500, 1500, MahaonCropKind.Wheat)));

        Assert.Equal(first[0].Crop, second[0].Crop);
    }

    /// <summary>
    ///     Периметр делянки не засевается: спрайты грядок крупнее тайла, и посев вплотную
    ///     до края вылезает за пашню на траву.
    /// </summary>
    [Fact]
    public void Plot_LeavesAOneTileBorderUnplanted()
    {
        var tiles = new List<(int, int, MahaonCropKind)>();

        // Квадрат 5x5 — внутри должен остаться ровно 3x3.
        for (var x = 0; x < 5; x++)
        {
            for (var y = 0; y < 5; y++)
            {
                tiles.Add((2000 + x, 2000 + y, MahaonCropKind.Wheat));
            }
        }

        var plots = MahaonFieldPlots.Build(Facet, Tiles(tiles.ToArray()));

        Assert.Single(plots);
        Assert.Equal(9, plots[0].Tiles.Count);

        foreach (var (x, y, _) in plots[0].Tiles)
        {
            Assert.InRange(x, 2001, 2003);
            Assert.InRange(y, 2001, 2003);
        }
    }

    /// <summary>
    ///     Узкую делянку размывание съело бы подчистую — лучше засеять её как есть, чем
    ///     оставить игроку голый прямоугольник без единого ростка.
    /// </summary>
    [Fact]
    public void NarrowPlot_IsPlantedAnyway()
    {
        var plots = MahaonFieldPlots.Build(
            Facet,
            Tiles(
                (2100, 2100, MahaonCropKind.Onion),
                (2101, 2100, MahaonCropKind.Onion),
                (2102, 2100, MahaonCropKind.Onion)
            )
        );

        Assert.Single(plots);
        Assert.Equal(3, plots[0].Tiles.Count);
    }

    /// <summary>Вспаханная земля узнаётся по диапазонам тайлов из клиента.</summary>
    [Theory]
    [InlineData(0x0009, true)]
    [InlineData(0x0015, true)]
    [InlineData(0x0150, true)]
    [InlineData(0x015C, true)]
    [InlineData(0x0008, false)]
    [InlineData(0x0016, false)]
    [InlineData(0x00A8, false)]
    public void PlotGround_IsRecognisedByRange(int landId, bool expected)
    {
        Assert.Equal(expected, MahaonFieldPlots.IsPlotGround(landId));
    }
}
