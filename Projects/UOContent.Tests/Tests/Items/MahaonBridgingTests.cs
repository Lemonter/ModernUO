using Server.Systems.MahaonMapEdits;
using Xunit;

namespace Server.Tests.Items;

/// <summary>
///     Тесты наведения мостов.
///
///     Главное здесь — не «кладётся ли доска», а правило, которое не даёт застелить мир
///     досками: настил ложится только над провалом. Без него любой смог бы заасфальтировать
///     площадь просто потому, что может, и мосты перестали бы быть мостами.
///
///     Каждому тесту свой блок — слепки «как было» хранятся по блокам и переживают откат.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class MahaonBridgingTests
{
    private static int _block;

    private readonly int X;
    private readonly int Y;

    public MahaonBridgingTests()
    {
        X = 512 + System.Threading.Interlocked.Increment(ref _block) * 16;
        Y = 16;
    }

    private static Map Facet => Map.Felucca;

    private void Reset() => MahaonMapEdits.UndoAll();

    /// <summary>
    ///     Ставит флаги тайлу земли и возвращает как было.
    ///
    ///     Через restore, а не просто присваиванием, по неочевидной причине: в тестовом
    ///     хосте TileData не загружен и MaxLandValue равен нулю, так что маска схлопывает
    ///     ЛЮБОЙ индекс в нулевой. Поставив здесь Wet и не убрав, я пометил бы водой всю
    ///     землю разом — и соседние тесты начали бы строить мосты на лужайке. Ровно это и
    ///     случилось при первом прогоне.
    /// </summary>
    private static TileFlag SetLandFlags(int graphic, TileFlag flags)
    {
        var index = graphic & TileData.MaxLandValue;
        var previous = TileData.LandTable[index].Flags;

        TileData.LandTable[index].Flags = flags;

        return previous;
    }

    /// <summary>Опускает землю под точкой, чтобы получился провал нужной глубины.</summary>
    private void SinkGround(int depth)
    {
        var z = MahaonMapEdits.LandAt(Facet, X, Y).Z;

        MahaonMapEdits.SetLand(Facet, X, Y, 0x0009, z - depth, "подготовка");
    }

    [Fact]
    public void Build_RefusesOnFlatGround()
    {
        Reset();
        SetLandFlags(0, TileFlag.None);

        var z = MahaonMapEdits.LandAt(Facet, X, Y).Z;

        Assert.Equal(MahaonBridging.BuildResult.NoGap, MahaonBridging.Build(Facet, X, Y, z, "мост"));

        Reset();
    }

    /// <summary>Провал мельче порога — всё ещё не мост, а ступенька.</summary>
    [Fact]
    public void Build_RefusesOnShallowDip()
    {
        Reset();
        SetLandFlags(0, TileFlag.None);
        SinkGround(MahaonBridging.MinGap - 1);

        var standing = MahaonMapEdits.LandAt(Facet, X, Y).Z + MahaonBridging.MinGap - 1;

        Assert.Equal(MahaonBridging.BuildResult.NoGap, MahaonBridging.Build(Facet, X, Y, standing, "мост"));

        Reset();
    }

    [Fact]
    public void Build_WorksOverARealGap()
    {
        Reset();

        var original = MahaonMapEdits.LandAt(Facet, X, Y).Z;

        SinkGround(MahaonBridging.MinGap + 2);

        Assert.Equal(MahaonBridging.BuildResult.Built, MahaonBridging.Build(Facet, X, Y, original, "мост"));

        var planks = MahaonMapEdits.StaticsAt(Facet, X, Y);

        Assert.Single(planks);
        Assert.True(MahaonBridging.IsPlank(planks[0].Graphic));
        Assert.Equal(original, planks[0].Z);

        Reset();
    }

    /// <summary>
    ///     Над водой настилают независимо от глубины — на то она и вода. Флаг Wet ставим
    ///     руками: TileData в тестовом хосте пуст, и без этого вода неотличима от лужайки.
    /// </summary>
    [Fact]
    public void Build_WorksOverWaterAtAnyDepth()
    {
        Reset();

        const int water = 0x00A8;

        var previous = SetLandFlags(water, TileFlag.Wet);

        try
        {
            var z = MahaonMapEdits.LandAt(Facet, X, Y).Z;

            MahaonMapEdits.SetLand(Facet, X, Y, water, z, "пруд");

            Assert.Equal(MahaonBridging.BuildResult.Built, MahaonBridging.Build(Facet, X, Y, z, "мост"));
        }
        finally
        {
            SetLandFlags(water, previous);
            Reset();
        }
    }

    [Fact]
    public void Build_RefusesSecondLayer()
    {
        Reset();

        var original = MahaonMapEdits.LandAt(Facet, X, Y).Z;

        SinkGround(MahaonBridging.MinGap + 2);
        MahaonBridging.Build(Facet, X, Y, original, "мост");

        Assert.Equal(MahaonBridging.BuildResult.Occupied, MahaonBridging.Build(Facet, X, Y, original, "мост"));

        Reset();
    }

    [Fact]
    public void Dismantle_RemovesOnlyPlanks()
    {
        Reset();

        var original = MahaonMapEdits.LandAt(Facet, X, Y).Z;

        SinkGround(MahaonBridging.MinGap + 2);
        MahaonBridging.Build(Facet, X, Y, original, "мост");
        MahaonMapEdits.AddStatic(Facet, X, Y, original, 0x0006, 0, "чужое");

        Assert.Equal(1, MahaonBridging.Dismantle(Facet, X, Y, "разбор"));

        var left = MahaonMapEdits.StaticsAt(Facet, X, Y);

        Assert.Single(left);
        Assert.Equal(0x0006, left[0].Graphic);

        Reset();
    }

    /// <summary>
    ///     Настил обязан взрываться. Он не помечен ни стеной, ни крышей, и по общему
    ///     правилу оказался бы прочнее горы — мост, который нельзя разрушить, это нелепость.
    /// </summary>
    [Fact]
    public void Planks_AreDestructible()
    {
        foreach (var graphic in MahaonBridging.PlankGraphics)
        {
            Assert.True(MahaonDemolition.IsDestructible(graphic), $"настил {graphic:X4} не рушится");
        }
    }
}
