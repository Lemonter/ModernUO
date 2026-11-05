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

    private static void Reset() => MahaonMapEdits.ResetAll();

    /// <summary>
    ///     Высота тайла сейчас.
    ///
    ///     Раньше тесты считали записи в журнале правок и брали высоту из последней. Журнала
    ///     больше нет: хранится исходный мир и изменённый, и спрашивать надо у изменённого.
    /// </summary>
    private static int LastZ() => MahaonMapEdits.LandAt(Facet, X, Y).Z;

    [Fact]
    public void Dig_LowersGroundByOne()
    {
        Reset();

        var before = MahaonExcavation.CurrentZ(Facet, X, Y);
        var result = MahaonExcavation.Dig(Facet, X, Y);

        Assert.Equal(MahaonExcavation.DigResult.Dug, result);
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

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(MahaonExcavation.DigResult.Dug, MahaonExcavation.Dig(Facet, X, Y));
            Assert.False(MahaonExcavation.IsWater(Facet, X, Y));
        }

        Reset();
    }

    /// <summary>
    ///     Игрового предела глубины больше нет — остался предел самой карты: высота тайла
    ///     хранится байтом со знаком. Проверяем, что лопата доходит ровно до него и там
    ///     встаёт, не уводя высоту за границу типа.
    /// </summary>
    [Fact]
    public void Dig_StopsOnlyAtTheBottomOfTheWorld()
    {
        Reset();

        var start = MahaonExcavation.CurrentZ(Facet, X, Y);

        // С запасом: от любой разумной целины до дна карты не больше двух с лишним сотен.
        for (var i = 0; i < 400; i++)
        {
            if (MahaonExcavation.Dig(Facet, X, Y) != MahaonExcavation.DigResult.Dug)
            {
                break;
            }
        }

        Assert.Equal(MahaonExcavation.MinZ, LastZ());
        Assert.True(start - MahaonExcavation.MinZ > 5, "прежний предел в пять единиц должен быть позади");

        var bottom = LastZ();

        Assert.Equal(MahaonExcavation.DigResult.TooDeep, MahaonExcavation.Dig(Facet, X, Y));
        Assert.Equal(bottom, LastZ());

        Reset();
    }

    [Fact]
    public void Fill_RaisesBackTowardOriginal()
    {
        Reset();

        var original = MahaonExcavation.CurrentZ(Facet, X, Y);

        MahaonExcavation.Dig(Facet, X, Y);
        MahaonExcavation.Dig(Facet, X, Y);

        Assert.True(MahaonExcavation.Fill(Facet, X, Y));
        Assert.Equal(original - 1, LastZ());

        Assert.True(MahaonExcavation.Fill(Facet, X, Y));
        Assert.Equal(original, LastZ());

        Reset();
    }

    /// <summary>Выше исходной земли не насыпаем — это была бы стройка, а не засыпка.</summary>
    [Fact]
    public void Fill_WillNotRaiseAboveOriginalGround()
    {
        Reset();

        var untouched = LastZ();

        Assert.False(MahaonExcavation.Fill(Facet, X, Y));
        Assert.Equal(untouched, LastZ());

        Reset();
    }

    /// <summary>
    ///     Яма сбрасывается вся разом — вместе со всем, что изменено. Отката по событию
    ///     больше нет: хранится не история правок, а два состояния мира.
    /// </summary>
    [Fact]
    public void WholePit_ResetsBackToTheOriginalWorld()
    {
        Reset();

        var before = new int[3];

        for (var i = 0; i < 3; i++)
        {
            before[i] = MahaonExcavation.CurrentZ(Facet, X + i, Y);
            MahaonExcavation.Dig(Facet, X + i, Y);
        }

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(before[i] - 1, MahaonExcavation.CurrentZ(Facet, X + i, Y));
        }

        Assert.True(MahaonMapEdits.ResetAll() > 0);

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(before[i], MahaonExcavation.CurrentZ(Facet, X + i, Y));
        }
    }

    // Защиту городов здесь не проверить: регионы в тестовом хосте не загружены, и
    // Region.Find не находит охраняемую зону даже в центре Британии. Тест на это проходил
    // бы по неверной причине, а такой хуже отсутствующего — проверяется вживую, лопатой у
    // банка. Сама же ветка общая с подрывом (MahaonDemolition.IsDestructibleArea), и её
    // логика закреплена в MahaonDemolitionTests.

    /// <summary>
    ///     Направление задаёт игрок, а не история тайла. Раньше лопата выводила его из
    ///     журнала правок и разворачивалась сама, упёршись в дно; теперь дна нет, и
    ///     разворачивать стало нечем — режим спрашивают у игрока.
    /// </summary>
    [Fact]
    public void DigMode_DefaultsToDiggingAndRemembersTheChoice()
    {
        var player = new Mobile();

        try
        {
            Assert.Equal(DigMode.Dig, MahaonDigMode.Of(player));

            MahaonDigMode.Set(player, DigMode.Fill);
            Assert.Equal(DigMode.Fill, MahaonDigMode.Of(player));

            MahaonDigMode.Set(player, DigMode.Harvest);
            Assert.Equal(DigMode.Harvest, MahaonDigMode.Of(player));

            MahaonDigMode.Set(player, DigMode.Dig);
            Assert.Equal(DigMode.Dig, MahaonDigMode.Of(player));
        }
        finally
        {
            MahaonDigMode.Set(player, DigMode.Dig);
            player.Delete();
        }
    }
}
