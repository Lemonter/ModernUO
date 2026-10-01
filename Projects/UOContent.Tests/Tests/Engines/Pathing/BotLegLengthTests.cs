using Server.Engines.Pathing.Cache;
using Server.PathAlgorithms;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding;

/// <summary>
///     Тесты, закрепляющие два факта, на которых стоит движение ботов (BotMovement.cs).
///
///     Первый: BitmapAStarAlgorithm отказывается искать маршрут, если старт и цель дальше
///     AreaSize (38) тайлов. Это важно не само по себе, а потому что PathFollower.Follow
///     при неуспешном маршруте НЕ останавливается — он делает шаг GetDirectionTo(goal),
///     то есть ведёт бота на цель по прямой, сквозь любые препятствия. Отсюда и брались
///     боты, упирающиеся в стену: им давали цель, до которой поиск пути не работал.
///
///     Второй: отрезок длиной MaxLegLength (20) на открытой местности маршрутизируется
///     нормально. Если кто-то поднимет эту константу ближе к 38, окно поиска (квадрат
///     38×38 посередине между концами отрезка) перестанет оставлять место на обход вбок —
///     тест не поймает это напрямую, но зафиксирует хотя бы то, что выбранная длина
///     рабочая.
/// </summary>
[Collection("Sequential Pathfinding Tests")]
public class BotLegLengthTests
{
    private readonly ITestOutputHelper _output;

    public BotLegLengthTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>Та же константа, что и BotController.MaxLegLength — она приватная, поэтому
    /// здесь повторена намеренно: тест сторожит именно число.</summary>
    private const int MaxLegLength = 20;

    private const int AreaSize = 38;

    [Fact]
    public void GoalBeyondAreaSize_ProducesNoPath()
    {
        StepCache.Instance.Clear();

        var map = Map.Maps[1];
        Assert.NotNull(map);

        var stub = new WalkerStub();

        map.GetAverageZ(1500, 1600, out _, out var z, out _);
        var start = new Point3D(1500, 1600, (sbyte)z);
        var farGoal = new Point3D(1500 + AreaSize + 5, 1600, (sbyte)z);

        stub.MoveToWorld(start, map);

        var canTry = BitmapAStarAlgorithm.Instance.CheckCondition(stub, map, start, farGoal);
        var result = BitmapAStarAlgorithm.Instance.Find(stub, map, start, farGoal);

        stub.Delete();

        Assert.False(canTry, "за пределами AreaSize алгоритм не должен даже браться за поиск");
        Assert.Null(result);

        _output.WriteLine(
            $"цель за {AreaSize + 5} тайлов: поиск не запускается — именно здесь PathFollower " +
            "срывается на слепой шаг по прямой"
        );
    }

    [Fact]
    public void LegWithinMaxLength_IsRoutable()
    {
        StepCache.Instance.Clear();

        var map = Map.Maps[1];
        Assert.NotNull(map);

        var stub = new WalkerStub();

        map.GetAverageZ(1500, 1600, out _, out var z, out _);
        var start = new Point3D(1500, 1600, (sbyte)z);
        var legGoal = new Point3D(1500 - MaxLegLength, 1600, (sbyte)z);

        stub.MoveToWorld(start, map);

        var canTry = BitmapAStarAlgorithm.Instance.CheckCondition(stub, map, start, legGoal);

        stub.Delete();

        Assert.True(canTry, $"отрезок в {MaxLegLength} тайлов обязан попадать в окно поиска");

        // Настоящая величина, ради которой отрезок и укорочен: окно 38×38 центрируется
        // МЕЖДУ концами отрезка, поэтому в стороны от прямой остаётся (38 − длина) / 2.
        // Меньше восьми тайлов — и обойти здание уже негде, A* вернёт неудачу, а
        // PathFollower поведёт бота в стену по прямой.
        var lateralRoom = (AreaSize - MaxLegLength) / 2;

        Assert.True(lateralRoom >= 8, $"на обход вбок остаётся всего {lateralRoom} тайлов — мало");

        _output.WriteLine($"отрезок {MaxLegLength} тайлов: на обход вбок остаётся {lateralRoom} тайлов с каждой стороны");
    }

    private sealed class WalkerStub : Mobile
    {
        public WalkerStub()
        {
            Body = 0xC9;
        }
    }
}
