using Xunit;

namespace Server.Tests.Pathfinding;

/// <summary>
///     Тест на то, из-за чего боты ходили кругами.
///
///     В журнале это выглядело так:
///
///         отрезок #8:  [2500, 466] → [2520, 466]  длина 20, до цели 12
///         отрезок #9:  [2519, 466] → [2499, 466]  длина 20, до цели 7
///         отрезок #10: [2500, 466] → [2520, 466]  длина 20, до цели 12
///
///     Бот строил отрезок фиксированной длины и проезжал цель насквозь: до цели было
///     двенадцать тайлов, а отрезок — двадцать. С той стороны цель оказывалась позади,
///     направление разворачивалось, и он ехал обратно, снова мимо. Ни разу при этом не
///     остановившись — то есть проверка застревания по неподвижности его не ловила.
///
///     Здесь закреплена сама арифметика: длина отрезка никогда не больше расстояния до
///     цели. Полноценный ход бота тут не воспроизвести (нужен мир, карта и таймеры), но
///     обрезание длины — это то место, где ошибка и была.
/// </summary>
public class BotLegOvershootTests
{
    /// <summary>Как считает длину отрезка BotMovement.TryFindReachableWaypoint.</summary>
    private static int LegLength(int configured, int distanceToGoal) =>
        System.Math.Min(configured, System.Math.Max(1, distanceToGoal));

    /// <summary>Тот самый случай из журнала: цель в двенадцати, отрезок настроен на двадцать.</summary>
    [Fact]
    public void LegNeverOvershootsTheGoal()
    {
        Assert.Equal(12, LegLength(20, 12));
        Assert.Equal(7, LegLength(20, 7));
    }

    /// <summary>Цель дальше отрезка — длина остаётся штатной, резать нечего.</summary>
    [Fact]
    public void DistantGoalKeepsTheConfiguredLength()
    {
        Assert.Equal(20, LegLength(20, 50));
        Assert.Equal(20, LegLength(20, 20));
    }

    /// <summary>
    ///     Вплотную к цели длина не должна обнулиться: отрезок нулевой длины — это точка
    ///     под ногами, маршрут до неё тривиален, и бот встал бы навсегда.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ZeroDistanceStillProducesAStep(int distance)
    {
        Assert.Equal(1, LegLength(20, distance));
    }

    /// <summary>
    ///     Главное свойство: после шага бот не оказывается дальше от цели, чем был. Именно
    ///     это нарушалось и давало маятник.
    /// </summary>
    [Theory]
    [InlineData(30)]
    [InlineData(20)]
    [InlineData(12)]
    [InlineData(7)]
    [InlineData(2)]
    public void StepNeverIncreasesDistance(int distanceToGoal)
    {
        var step = LegLength(20, distanceToGoal);
        var remaining = distanceToGoal - step;

        Assert.True(
            remaining >= 0 || distanceToGoal <= 1,
            $"с расстояния {distanceToGoal} шаг {step} проносит мимо цели на {-remaining}"
        );
    }
}
