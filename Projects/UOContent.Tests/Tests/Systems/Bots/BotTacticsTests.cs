using Server.Systems.Bots;
using Xunit;

namespace Server.Tests.Systems.Bots;

/// <summary>
///     Арифметика перевеса. Проверяется без мира нарочно: она чистая, а именно от неё зависит,
///     уйдёт бот из драки один против пяти или останется умирать, как было раньше.
/// </summary>
public class BotTacticsTests
{
    private static BotSituation Sides(int foes, int allies) => new(foes, allies, 0, null, false);

    [Fact]
    public void OneOnOne_IsEven()
    {
        var even = Sides(1, 0);

        Assert.Equal(1.0, even.Odds, 3);
        Assert.False(even.Outnumbered);
        Assert.False(even.Outnumbering);
    }

    /// <summary>Один против пяти: это и есть та расстановка, в которой бот раньше стоял до конца.</summary>
    [Fact]
    public void OneAgainstFive_IsOutnumbered()
    {
        var bad = Sides(5, 0);

        Assert.Equal(0.2, bad.Odds, 3);
        Assert.True(bad.Outnumbered);
        Assert.False(bad.Outnumbering);
    }

    /// <summary>Пятеро против двоих: перевес наш, отступать незачем — товарищи останутся одни.</summary>
    [Fact]
    public void FiveAgainstTwo_IsOutnumbering()
    {
        var good = Sides(2, 4);

        Assert.Equal(2.5, good.Odds, 3);
        Assert.True(good.Outnumbering);
        Assert.False(good.Outnumbered);
    }

    /// <summary>Два на два и три на два — ещё не повод ни бежать, ни наглеть.</summary>
    [Theory]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    public void NarrowOdds_AreNeither(int foes, int allies)
    {
        var близко = Sides(foes, allies);

        Assert.False(близко.Outnumbered);
        Assert.False(близко.Outnumbering);
    }

    /// <summary>Враги кончились — делить не на что, и порог не должен обращаться в ноль.</summary>
    [Fact]
    public void NoFoes_IsNotOutnumbered()
    {
        var clear = Sides(0, 0);

        Assert.False(clear.Outnumbered);
        Assert.True(clear.Outnumbering);
    }
}
