using Server.Systems.MahaonMapEdits;
using Xunit;

namespace Server.Tests.Items;

/// <summary>
///     Тесты хранилища правок карты.
///
///     Хранится ровно два состояния: исходный мир и изменённый. Проверяется то, на чём это
///     держится, — что изменённый мир накрывает исходный, что сброс возвращает ровно
///     исходный, и что закрепление делает изменённый новым исходным.
///
///     Прежняя версия хранила append-only журнал и умела откатывать отдельную правку. Её
///     тесты удалены вместе с ней: одна копка на глубине d писала в журнал порядка (2d+1)²
///     записей, и тонкий откат не окупал ни сохранения, ни прохода по журналу на каждый
///     взмах.
///
///     Работаем на Фелукке в заведомо пустом углу карты, каждому тесту — свой блок 8x8.
///     Это не аккуратность ради аккуратности: слепки «как было» хранятся по блокам и
///     переживают сброс, а закрепление перезаписывает их НАВСЕГДА. Дели тесты один блок —
///     закреплённое в одном протекало бы в остальные, и падали бы не те, кто виноват.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class MahaonMapEditsTests
{
    private static int _block;

    private readonly int X;
    private readonly int Y;

    public MahaonMapEditsTests()
    {
        X = 16 + System.Threading.Interlocked.Increment(ref _block) * 16;
        Y = 16;
    }

    private static Map Facet => Map.Felucca;

    private static void Reset() => MahaonMapEdits.ResetAll();

    [Fact]
    public void AddedStatic_IsVisibleAtThatTile()
    {
        Reset();

        MahaonMapEdits.AddStatic(Facet, X, Y, 5, 0x1797, 0);

        Assert.Single(MahaonMapEdits.StaticsAt(Facet, X, Y));
        Assert.True(MahaonMapEdits.IsChanged(Facet, X, Y));

        Reset();
    }

    [Fact]
    public void SetLand_IsVisibleAtThatTile()
    {
        Reset();

        var before = MahaonMapEdits.LandAt(Facet, X, Y);

        MahaonMapEdits.SetLand(Facet, X, Y, 0x0009, (sbyte)(before.Z - 3));

        var now = MahaonMapEdits.LandAt(Facet, X, Y);

        Assert.Equal(0x0009, now.Graphic);
        Assert.Equal(before.Z - 3, now.Z);

        Reset();
    }

    /// <summary>
    ///     Десятая правка одного тайла не добавляет десятой записи — она заменяет
    ///     девятую. Ровно ради этого журнал и убран.
    /// </summary>
    [Fact]
    public void RepeatedEdits_DoNotAccumulate()
    {
        Reset();

        var z = MahaonMapEdits.LandAt(Facet, X, Y).Z;

        for (var i = 1; i <= 20; i++)
        {
            MahaonMapEdits.SetLand(Facet, X, Y, 0x0009, z - i);
        }

        Assert.Equal(1, MahaonMapEdits.ChangedBlocks);
        Assert.Equal(z - 20, MahaonMapEdits.LandAt(Facet, X, Y).Z);

        Reset();
    }

    [Fact]
    public void Reset_BringsBackTheOriginalWorld()
    {
        Reset();

        var land = MahaonMapEdits.LandAt(Facet, X, Y);
        var statics = MahaonMapEdits.StaticsAt(Facet, X, Y).Count;

        MahaonMapEdits.SetLand(Facet, X, Y, 0x0009, (sbyte)(land.Z - 5));
        MahaonMapEdits.AddStatic(Facet, X, Y, 0, 0x1797, 0);

        Assert.Equal(1, MahaonMapEdits.ChangedBlocks);

        MahaonMapEdits.ResetAll();

        Assert.Equal(0, MahaonMapEdits.ChangedBlocks);
        Assert.Equal(land, MahaonMapEdits.LandAt(Facet, X, Y));
        Assert.Equal(statics, MahaonMapEdits.StaticsAt(Facet, X, Y).Count);
    }

    [Fact]
    public void ResetBlock_TouchesOnlyThatBlock()
    {
        Reset();

        MahaonMapEdits.AddStatic(Facet, X, Y, 0, 0x1797, 0);
        MahaonMapEdits.AddStatic(Facet, X + 8, Y, 0, 0x1797, 0);

        Assert.Equal(2, MahaonMapEdits.ChangedBlocks);

        Assert.True(MahaonMapEdits.ResetBlock(Facet, X, Y));

        Assert.Equal(1, MahaonMapEdits.ChangedBlocks);
        Assert.Empty(MahaonMapEdits.StaticsAt(Facet, X, Y));
        Assert.Single(MahaonMapEdits.StaticsAt(Facet, X + 8, Y));

        Reset();
    }

    /// <summary>
    ///     Закрепление необратимо по замыслу: изменённый мир становится исходным, и
    ///     сбрасывать уже не к чему.
    /// </summary>
    [Fact]
    public void Commit_MakesTheChangedWorldTheOriginalOne()
    {
        Reset();

        MahaonMapEdits.AddStatic(Facet, X, Y, 7, 0x1797, 0);

        Assert.Equal(1, MahaonMapEdits.Commit());
        Assert.Equal(0, MahaonMapEdits.ChangedBlocks);

        // Статик на месте, хотя изменённых блоков больше нет: он теперь часть исходного.
        Assert.Single(MahaonMapEdits.StaticsAt(Facet, X, Y));

        MahaonMapEdits.ResetAll();

        Assert.Single(MahaonMapEdits.StaticsAt(Facet, X, Y));
    }

    /// <summary>
    ///     Блок, которого никто не трогал, обязан показывать то, что лежит в файлах карты,
    ///     а не пустоту. На этом ошибались: «не трогали» молча означало «тут ничего нет», и
    ///     вся прибрежная вода шарда — а она статикой — переставала существовать для
    ///     сервера.
    /// </summary>
    [Fact]
    public void UntouchedBlock_ReadsThroughToTheMapFile()
    {
        Reset();

        Assert.False(MahaonMapEdits.IsChanged(Facet, X, Y));

        var fromFile = 0;

        foreach (var _ in Facet.Tiles.GetStaticTiles(X, Y))
        {
            fromFile++;
        }

        Assert.Equal(fromFile, MahaonMapEdits.StaticsAt(Facet, X, Y).Count);
    }
}
