using System.Linq;
using Server.Systems.MahaonMapEdits;
using Xunit;

namespace Server.Tests.Items;

/// <summary>
///     Тесты отката правок карты.
///
///     Откат — не удобство, а условие, на котором эту систему вообще можно включать: она
///     меняет мир необратимо для всех сразу. Проверяется именно то, ради чего хранилище
///     переписано с «текущего состояния» на журнал: что отменить можно отдельную правку, а
///     не только весь блок 8x8, в который заодно попало чужое.
///
///     Работаем на Фелуке в заведомо пустом углу карты: снимок блока там пустой, и
///     выведенное состояние получается предсказуемым без поднятия мира.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class MahaonMapEditUndoTests
{
    // Дальний угол, где статики заведомо нет.
    //
    // Каждому тесту — свой блок 8x8, и это не аккуратность ради аккуратности. Слепки «как
    // было» хранятся по блокам и переживают UndoAll: закрепление (Commit) перезаписывает
    // их НАВСЕГДА, по замыслу. Делили бы тесты один блок — закреплённое в одном протекало
    // бы в остальные, и падали бы не те, кто виноват.
    private static int _block;

    private readonly int X;
    private readonly int Y;

    public MahaonMapEditUndoTests()
    {
        X = 16 + System.Threading.Interlocked.Increment(ref _block) * 16;
        Y = 16;
    }

    private static Map Facet => Map.Felucca;

    private void Reset() => MahaonMapEdits.UndoAll();

    [Fact]
    public void AddedStatic_AppearsInDerivedState()
    {
        Reset();
        MahaonMapEdits.AddStatic(Facet, X, Y, 5, 0x1797, 0, "мост");

        var tiles = MahaonMapEdits.StaticsAt(Facet, X, Y);

        Assert.Single(tiles);
        Assert.Equal(0x1797, tiles[0].Graphic);
        Assert.Equal(5, tiles[0].Z);
    }

    [Fact]
    public void Undo_RemovesOnlyThatEdit()
    {
        Reset();
        MahaonMapEdits.AddStatic(Facet, X, Y, 1, 0x0001, 0, "первый");
        MahaonMapEdits.AddStatic(Facet, X, Y, 2, 0x0002, 0, "второй");

        var firstId = MahaonMapEdits.Entries[0].Id;

        Assert.True(MahaonMapEdits.Undo(firstId));

        var tiles = MahaonMapEdits.StaticsAt(Facet, X, Y);

        Assert.Single(tiles);
        Assert.Equal(0x0002, tiles[0].Graphic);
    }

    /// <summary>
    ///     Главное, ради чего переписано хранилище: две правки в ОДНОМ блоке 8x8, откат
    ///     одной не должен трогать другую. Прежняя версия умела откатывать только блок
    ///     целиком и снесла бы обе.
    /// </summary>
    [Fact]
    public void Undo_KeepsOtherEditsInTheSameBlock()
    {
        Reset();
        MahaonMapEdits.AddStatic(Facet, X, Y, 0, 0x00AA, 0, "чужой мост");
        MahaonMapEdits.AddStatic(Facet, X + 1, Y + 1, 0, 0x00BB, 0, "осада");

        Assert.Equal(
            MahaonMapEdits.BlockIdFor(Facet, X, Y),
            MahaonMapEdits.BlockIdFor(Facet, X + 1, Y + 1)
        );

        Assert.Equal(1, MahaonMapEdits.UndoByReason("осада"));

        Assert.Single(MahaonMapEdits.StaticsAt(Facet, X, Y));
        Assert.Empty(MahaonMapEdits.StaticsAt(Facet, X + 1, Y + 1));
    }

    [Fact]
    public void UndoByReason_RemovesWholeEvent()
    {
        Reset();
        MahaonMapEdits.AddStatic(Facet, X, Y, 0, 0x0001, 0, "осада Британии");
        MahaonMapEdits.AddStatic(Facet, X + 2, Y, 0, 0x0002, 0, "осада Британии");
        MahaonMapEdits.AddStatic(Facet, X + 4, Y, 0, 0x0003, 0, "мост игрока");

        Assert.Equal(2, MahaonMapEdits.UndoByReason("осада Британии"));
        Assert.Single(MahaonMapEdits.Entries);
        Assert.Equal("мост игрока", MahaonMapEdits.Entries[0].Reason);
    }

    [Fact]
    public void UndoLast_PeelsFromTheEnd()
    {
        Reset();

        for (var i = 0; i < 5; i++)
        {
            MahaonMapEdits.AddStatic(Facet, X + i, Y, 0, 0x0100 + i, 0, "серия");
        }

        Assert.Equal(2, MahaonMapEdits.UndoLast(2));
        Assert.Equal(3, MahaonMapEdits.Entries.Count);
        Assert.Equal(0x0102, MahaonMapEdits.Entries[^1].Graphic);
    }

    /// <summary>Откат всего обязан вернуть мир к исходному: ни одного задетого блока.</summary>
    [Fact]
    public void UndoAll_LeavesNothingDerived()
    {
        Reset();
        MahaonMapEdits.AddStatic(Facet, X, Y, 0, 0x0001, 0, "а");
        MahaonMapEdits.SetLand(Facet, X, Y, 0x00A8, 0, "б");

        Assert.NotEmpty(MahaonMapEdits.Derived);

        MahaonMapEdits.UndoAll();

        Assert.Empty(MahaonMapEdits.Entries);
        Assert.Empty(MahaonMapEdits.Derived);
        Assert.Empty(MahaonMapEdits.StaticsAt(Facet, X, Y));
    }

    /// <summary>
    ///     Повторное чтение выведенного состояния должно давать то же самое: оно
    ///     кэшируется и сбрасывается при правках, и ошибка в сбросе проявилась бы именно так.
    /// </summary>
    [Fact]
    public void Derived_IsStableBetweenReads()
    {
        Reset();
        MahaonMapEdits.AddStatic(Facet, X, Y, 3, 0x0777, 0, "проверка");

        var first = MahaonMapEdits.StaticsAt(Facet, X, Y).ToList();
        var second = MahaonMapEdits.StaticsAt(Facet, X, Y).ToList();

        Assert.Equal(first, second);

        MahaonMapEdits.AddStatic(Facet, X, Y, 4, 0x0778, 0, "проверка");

        Assert.Equal(2, MahaonMapEdits.StaticsAt(Facet, X, Y).Count);

        Reset();
    }

    /// <summary>
    ///     Закрепление превращает правки в новое «как было»: журнал пустеет, живой слой
    ///     перестаёт их рассылать, а состояние остаётся тем же. Это недостающее звено всего
    ///     замысла — без него список растёт вечно.
    /// </summary>
    [Fact]
    public void Commit_FoldsEditsIntoTheBaseline()
    {
        Reset();
        MahaonMapEdits.AddStatic(Facet, X, Y, 7, 0x0AAA, 0, "мост");

        var before = MahaonMapEdits.StaticsAt(Facet, X, Y);

        Assert.Single(before);
        Assert.Equal(1, MahaonMapEdits.Commit());

        Assert.Empty(MahaonMapEdits.Entries);
        Assert.Empty(MahaonMapEdits.Derived);

        // Состояние сохранилось — но уже как исходное, а не как правка.
        var after = MahaonMapEdits.StaticsAt(Facet, X, Y);

        Assert.Single(after);
        Assert.Equal(0x0AAA, after[0].Graphic);

        Reset();
    }

    /// <summary>
    ///     После закрепления откат этих правок невозможен — снимок перезаписан. Это цена
    ///     необратимости, и она должна быть именно такой, иначе закрепление ничего не даёт.
    /// </summary>
    [Fact]
    public void Commit_MakesEditsUnundoable()
    {
        Reset();
        MahaonMapEdits.AddStatic(Facet, X, Y, 0, 0x0BBB, 0, "стена");
        MahaonMapEdits.Commit();

        Assert.Equal(0, MahaonMapEdits.UndoAll());
        Assert.Single(MahaonMapEdits.StaticsAt(Facet, X, Y));

        Reset();
    }

    /// <summary>
    ///     Наращивание обязано давать то же, что честное проигрывание журнала с нуля.
    ///
    ///     Добавление правки не пересчитывает состояние заново, а накладывается на уже
    ///     посчитанное — иначе каждый удар киркой проигрывал бы весь журнал, и чем дольше
    ///     живёт сервер, тем медленнее. Плата за скорость — два пути вместо одного, и
    ///     ломаются такие вещи именно расхождением путей, причём молча.
    /// </summary>
    [Fact]
    public void IncrementalState_MatchesFullReplay()
    {
        Reset();

        MahaonMapEdits.AddStatic(Facet, X, Y, 1, 0x00C1, 0, "смесь");
        MahaonMapEdits.AddStatic(Facet, X + 1, Y, 2, 0x00C2, 0, "смесь");
        MahaonMapEdits.SetLand(Facet, X, Y, 0x00A8, -3, "смесь");
        MahaonMapEdits.RemoveStatics(Facet, X, Y, 0x00C1, "смесь");
        MahaonMapEdits.AddStatic(Facet, X, Y, 4, 0x00C3, 0, "смесь");

        var key = (Facet.MapID, MahaonMapEdits.BlockIdFor(Facet, X, Y));

        var incremental = MahaonMapEdits.Derived[key];
        var incrementalStatics = incremental.Statics.ToList();
        var incrementalLand = incremental.Land?.ToList();

        MahaonMapEdits.Rebuild();

        var replayed = MahaonMapEdits.Derived[key];

        Assert.Equal(incrementalStatics, replayed.Statics);
        Assert.Equal(incrementalLand, replayed.Land?.ToList());
        Assert.Equal(incremental.StaticsTouched, replayed.StaticsTouched);

        Reset();
    }

    [Fact]
    public void SetLand_IsRecordedAndUndoable()
    {
        Reset();
        MahaonMapEdits.SetLand(Facet, X, Y, 0x00A8, -5, "ров");

        var key = (Facet.MapID, MahaonMapEdits.BlockIdFor(Facet, X, Y));

        Assert.True(MahaonMapEdits.Derived[key].LandTouched);
        Assert.Equal(0x00A8, MahaonMapEdits.Derived[key].Land[(Y & 7) * 8 + (X & 7)].Graphic);

        MahaonMapEdits.UndoAll();

        Assert.Empty(MahaonMapEdits.Derived);
    }
}
