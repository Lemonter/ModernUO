using Server.Systems.MahaonMapEdits;
using Xunit;

namespace Server.Tests.Items;

/// <summary>
///     Тесты земляных работ.
///
///     Земля — единственная часть системы правок карты, которую предметами не подменить, и
///     до сих пор она нигде не была задействована: файлов mapdif у клиента нет, пакет с
///     землёй ни разу не уходил. Поэтому здесь проверяется сама арифметика глубины, на
///     которой всё держится: где яма ещё яма, где она уже пруд, и куда возвращает засыпка.
///
///     Живую карту тесты не трогают — работают через журнал правок, который и есть источник
///     истины.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class MahaonExcavationTests
{
    // Дальний угол карты: там ни города, ни домов, ни камня.
    private const int X = 24;
    private const int Y = 24;

    private static Map Facet => Map.Felucca;

    private static void Reset() => MahaonMapEdits.UndoAll();

    /// <summary>Сколько правок земли записано на этом тайле.</summary>
    private static int LandEdits()
    {
        var n = 0;

        foreach (var e in MahaonMapEdits.Entries)
        {
            if (e.Kind == MapEditKind.SetLand && e.X == X && e.Y == Y)
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>Высота, записанная последней правкой этого тайла.</summary>
    private static int LastZ()
    {
        var z = int.MinValue;

        foreach (var e in MahaonMapEdits.Entries)
        {
            if (e.Kind == MapEditKind.SetLand && e.X == X && e.Y == Y)
            {
                z = e.Z;
            }
        }

        return z;
    }

    [Fact]
    public void Dig_LowersGroundByOne()
    {
        Reset();

        var before = MahaonExcavation.CurrentZ(Facet, X, Y);
        var result = MahaonExcavation.Dig(Facet, X, Y, "копка");

        Assert.Equal(MahaonExcavation.DigResult.Dug, result);
        Assert.Equal(1, LandEdits());
        Assert.Equal(before - 1, LastZ());

        Reset();
    }

    /// <summary>
    ///     Сама копка воду не наливает — никогда, на любой глубине. Вода приходит только
    ///     по рельефу из настоящего источника (MahaonWaterFlow), и это главное отличие от
    ///     первых двух версий: там пруд ПОЯВЛЯЛСЯ на дне, хоть посреди сухого луга.
    /// </summary>
    [Fact]
    public void Dig_NeverMakesWaterByItself()
    {
        Reset();

        for (var i = 0; i < MahaonExcavation.MaxDepth; i++)
        {
            Assert.Equal(MahaonExcavation.DigResult.Dug, MahaonExcavation.Dig(Facet, X, Y, "копка"));
            Assert.False(MahaonExcavation.IsWater(Facet, X, Y));
        }

        Reset();
    }

    /// <summary>Бездонных ям не бывает: дальше воды лопата не идёт.</summary>
    [Fact]
    public void Dig_StopsOnceFlooded()
    {
        Reset();

        for (var i = 0; i < MahaonExcavation.MaxDepth; i++)
        {
            MahaonExcavation.Dig(Facet, X, Y, "копка");
        }

        var edits = LandEdits();

        Assert.Equal(MahaonExcavation.DigResult.TooDeep, MahaonExcavation.Dig(Facet, X, Y, "копка"));
        Assert.Equal(edits, LandEdits());

        Reset();
    }

    [Fact]
    public void Fill_RaisesBackTowardOriginal()
    {
        Reset();

        var original = MahaonExcavation.CurrentZ(Facet, X, Y);

        MahaonExcavation.Dig(Facet, X, Y, "копка");
        MahaonExcavation.Dig(Facet, X, Y, "копка");

        Assert.True(MahaonExcavation.Fill(Facet, X, Y, "засыпка"));
        Assert.Equal(original - 1, LastZ());

        Assert.True(MahaonExcavation.Fill(Facet, X, Y, "засыпка"));
        Assert.Equal(original, LastZ());

        Reset();
    }

    /// <summary>Выше исходной земли не насыпаем — это была бы стройка, а не засыпка.</summary>
    [Fact]
    public void Fill_WillNotRaiseAboveOriginalGround()
    {
        Reset();

        Assert.False(MahaonExcavation.Fill(Facet, X, Y, "засыпка"));
        Assert.Equal(0, LandEdits());

        Reset();
    }

    /// <summary>
    ///     Вся яма подписана одной пометкой, значит откатывается одним движением — ровно
    ///     как подрыв.
    /// </summary>
    [Fact]
    public void WholePit_UndoesAsOneEvent()
    {
        Reset();

        for (var i = 0; i < 3; i++)
        {
            MahaonExcavation.Dig(Facet, X + i, Y, "копка Васи");
        }

        Assert.Equal(3, MahaonMapEdits.Entries.Count);
        Assert.Equal(3, MahaonMapEdits.UndoByReason("копка Васи"));
        Assert.Empty(MahaonMapEdits.Entries);

        Reset();
    }

    // Защиту городов здесь не проверить: регионы в тестовом хосте не загружены, и
    // Region.Find не находит охраняемую зону даже в центре Британии. Тест на это проходил
    // бы по неверной причине, а такой хуже отсутствующего — проверяется вживую, лопатой у
    // банка. Сама же ветка общая с подрывом (MahaonDemolition.IsDestructibleArea), и её
    // логика закреплена в MahaonDemolitionTests.

    /// <summary>
    ///     Поворот лопаты. Режимов у неё нет: направление берётся из журнала, и потому
    ///     важно, что цикл замкнут — копаем до воды, бьём ещё раз и начинаем засыпать, а
    ///     засыпав до исходной высоты, снова копаем.
    /// </summary>
    [Fact]
    public void FillsNext_TurnsAroundAtWaterAndBackAtOriginalHeight()
    {
        Reset();

        Assert.False(MahaonExcavation.FillsNext(Facet, X, Y)); // целина — копаем

        for (var i = 1; i < MahaonExcavation.MaxDepth; i++)
        {
            MahaonExcavation.Dig(Facet, X, Y, "копка: тест");
            Assert.False(MahaonExcavation.FillsNext(Facet, X, Y)); // яма ещё не полна
        }

        MahaonExcavation.Dig(Facet, X, Y, "копка: тест"); // набрала воду
        Assert.True(MahaonExcavation.FillsNext(Facet, X, Y));

        for (var i = 1; i < MahaonExcavation.MaxDepth; i++)
        {
            Assert.True(MahaonExcavation.Fill(Facet, X, Y, "засыпка: тест"));
            Assert.True(MahaonExcavation.FillsNext(Facet, X, Y)); // начал засыпать — засыпай
        }

        Assert.True(MahaonExcavation.Fill(Facet, X, Y, "засыпка: тест")); // вернулись на исходную
        Assert.False(MahaonExcavation.FillsNext(Facet, X, Y));           // и снова копаем
    }
}
