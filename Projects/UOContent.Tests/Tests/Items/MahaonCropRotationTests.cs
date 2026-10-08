using System;
using System.Collections.Generic;
using Server.Systems.MahaonFarming;
using Server.Systems.MahaonWorld;
using Xunit;

namespace Server.Tests.Items;

/// <summary>
///     Тесты севооборота.
///
///     Проверяется одно требование, но оно с подвохом: культура должна быть случайной и
///     обязательно ДРУГОЙ. «Случайная» ломается незаметно — перекос вылезет через месяц
///     игры, когда окажется, что на всех полях вечно одна и та же репа. «Другая» ломается
///     заметнее, но реже: раз в двадцать смен поле остаётся прежним, и это списывают на
///     совпадение.
/// </summary>
public class MahaonCropRotationTests
{
    /// <summary>Главное: новая культура никогда не совпадает с прежней.</summary>
    [Fact]
    public void NextCrop_IsNeverTheSame()
    {
        foreach (var crop in Enum.GetValues<MahaonCropType>())
        {
            for (var i = 0; i < 500; i++)
            {
                Assert.NotEqual(crop, MahaonCropRotation.NextCrop(crop));
            }
        }
    }

    /// <summary>
    ///     И второе: выбор должен покрывать все альтернативы, а не топтаться на нескольких.
    ///
    ///     Способ «тяни, пока не совпадёт» дал бы то же «никогда не та же», но при кривой
    ///     реализации легко получить перекос — например, если запасной вариант берётся
    ///     всегда одним и тем же. Здесь проверяем, что за много бросков выпали ВСЕ
    ///     двадцать остальных культур.
    /// </summary>
    [Fact]
    public void NextCrop_ReachesEveryOtherCrop()
    {
        const MahaonCropType from = MahaonCropType.Onion;

        var seen = new HashSet<MahaonCropType>();

        for (var i = 0; i < 5000; i++)
        {
            seen.Add(MahaonCropRotation.NextCrop(from));
        }

        var expected = Enum.GetValues<MahaonCropType>().Length - 1;

        Assert.Equal(expected, seen.Count);
        Assert.DoesNotContain(from, seen);
    }

    /// <summary>
    ///     Отдельно — последняя культура в перечислении. Она служит запасным вариантом
    ///     внутри выбора, и именно на ней ошибка «всегда возвращаем запасную» была бы не
    ///     видна остальным тестам.
    /// </summary>
    [Fact]
    public void NextCrop_HandlesTheLastCropFairly()
    {
        var all = Enum.GetValues<MahaonCropType>();
        var last = all[^1];

        var seen = new HashSet<MahaonCropType>();

        for (var i = 0; i < 5000; i++)
        {
            var next = MahaonCropRotation.NextCrop(last);

            Assert.NotEqual(last, next);
            seen.Add(next);
        }

        Assert.Equal(all.Length - 1, seen.Count);
    }

    /// <summary>Распределение не обязано быть идеальным, но и вырожденным быть не должно.</summary>
    [Fact]
    public void NextCrop_IsNotHeavilySkewed()
    {
        const MahaonCropType from = MahaonCropType.Wheat;

        var counts = new Dictionary<MahaonCropType, int>();
        const int rolls = 20000;

        for (var i = 0; i < rolls; i++)
        {
            var next = MahaonCropRotation.NextCrop(from);

            counts.TryGetValue(next, out var n);
            counts[next] = n + 1;
        }

        var alternatives = Enum.GetValues<MahaonCropType>().Length - 1;
        var fair = rolls / (double)alternatives;

        foreach (var (crop, count) in counts)
        {
            // Втрое от ожидаемого в любую сторону — запас огромный, но вырожденный выбор
            // (одна культура на треть всех бросков) в него уже не влезет.
            Assert.InRange(count, fair / 3.0, fair * 3.0);
        }
    }
}
