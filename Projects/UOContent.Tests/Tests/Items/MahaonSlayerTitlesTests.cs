using System;
using Server.Items;
using Server.Systems.MahaonSlayer;
using Server.Mobiles;
using Xunit;

namespace Server.Tests.Items;

/// <summary>
///     Тесты званий охотника — ступеней, названий и полноты справочника пород.
///
///     Само начисление убийств и прибавку к урону проверить здесь нечем: для этого нужны
///     живые монстры и бой. Зато проверяется всё, что считается арифметикой и таблицами, —
///     то есть то, что ломается молча при правке порогов или при появлении новой породы.
/// </summary>
public class MahaonSlayerTitlesTests
{
    [Fact]
    public void TierFor_StepsExactlyAtThresholds()
    {
        var t = MahaonSlayerTitles.Thresholds;

        Assert.Equal(0, MahaonSlayerTitles.TierFor(0));
        Assert.Equal(0, MahaonSlayerTitles.TierFor(t[0] - 1));
        Assert.Equal(1, MahaonSlayerTitles.TierFor(t[0]));
        Assert.Equal(1, MahaonSlayerTitles.TierFor(t[1] - 1));
        Assert.Equal(2, MahaonSlayerTitles.TierFor(t[1]));
        Assert.Equal(3, MahaonSlayerTitles.TierFor(t[2]));
        Assert.Equal(3, MahaonSlayerTitles.TierFor(t[2] * 100));
    }

    /// <summary>
    ///     Пороги обязаны идти по возрастанию: TierFor берёт последний пройденный, и на
    ///     перепутанном порядке звание молча застряло бы на первой ступени.
    /// </summary>
    [Fact]
    public void Thresholds_AreAscending()
    {
        var t = MahaonSlayerTitles.Thresholds;

        for (var i = 1; i < t.Length; i++)
        {
            Assert.True(t[i] > t[i - 1], $"порог {i} ({t[i]}) не больше предыдущего ({t[i - 1]})");
        }
    }

    /// <summary>
    ///     Максимальная прибавка не должна подкрадываться к слееровому оружию (+100%):
    ///     звание — признание заслуг, а не замена клинку.
    /// </summary>
    [Fact]
    public void MaxBonus_StaysModest()
    {
        var max = MahaonSlayerTitles.Thresholds.Length * MahaonSlayerTitles.BonusPerTier;

        Assert.InRange(max, 1, 25);
    }

    /// <summary>
    ///     Каждая порода из таксономии ультимы обязана иметь русское имя. Иначе игрок
    ///     однажды получит звание «убийца DragonSlaying» — и узнает об этом раньше меня.
    /// </summary>
    [Fact]
    public void EverySlayerNameHasARussianName()
    {
        foreach (var name in Enum.GetValues<SlayerName>())
        {
            if (name == SlayerName.None)
            {
                continue;
            }

            Assert.True(SlayerRu.HasName(name), $"нет русского названия для {name}");
        }
    }

    /// <summary>Названия в родительном падеже — они подставляются в «убийца …».</summary>
    [Fact]
    public void RussianNames_ReadAsGenitive()
    {
        Assert.Equal("орков", SlayerRu.GenitiveFor(SlayerName.OrcSlaying));
        Assert.Equal("драконов", SlayerRu.GenitiveFor(SlayerName.DragonSlaying));
        Assert.Equal("нежити", SlayerRu.GenitiveFor(SlayerName.Silver));
    }

    /// <summary>
    ///     Орк должен засчитываться и в свою породу, и в надгруппу человекоподобных — на
    ///     этом стоит замысел: широкое звание растёт параллельно узкому.
    /// </summary>
    [Fact]
    public void EntriesFor_CreditsBothSpeciesAndSuperGroup()
    {
        var matched = SlayerRu.EntriesFor(typeof(Orc));

        Assert.Contains(SlayerName.OrcSlaying, matched);
        Assert.Contains(SlayerName.Repond, matched);
    }

    /// <summary>
    ///     Запомненный ответ обязан совпадать с честным перебором. Кэш висит на горячем пути
    ///     боя, и разойдясь с правдой он молча раздавал бы не те прибавки к урону.
    /// </summary>
    [Fact]
    public void CachedEntries_MatchDirectLookup()
    {
        foreach (var type in new[] { typeof(Orc), typeof(Dragon), typeof(Lich), typeof(Zombie) })
        {
            var direct = SlayerRu.EntriesFor(type);
            var cached = SlayerRu.CachedEntriesFor(type);
            var again = SlayerRu.CachedEntriesFor(type); // второй раз — уже из кэша

            Assert.Equal(direct, cached);
            Assert.Equal(direct, again);
        }
    }

    /// <summary>Следующий порог — тот, до которого ещё не дотянули; на вершине порогов нет.</summary>
    [Fact]
    public void NextThreshold_PointsAtTheOneNotYetReached()
    {
        var t = MahaonSlayerTitles.Thresholds;

        Assert.Equal(t[0], MahaonSlayerTitles.NextThreshold(0));
        Assert.Equal(t[0], MahaonSlayerTitles.NextThreshold(t[0] - 1));
        Assert.Equal(t[1], MahaonSlayerTitles.NextThreshold(t[0]));
        Assert.Equal(t[2], MahaonSlayerTitles.NextThreshold(t[1]));
        Assert.Equal(0, MahaonSlayerTitles.NextThreshold(t[2]));
        Assert.Equal(0, MahaonSlayerTitles.NextThreshold(t[2] * 10));
    }

    /// <summary>Свиток чужого игрока и пустого моба не должен ронять показ профиля.</summary>
    [Fact]
    public void TitlesAndProgress_AreEmptyForUnknownMobile()
    {
        Assert.Empty(MahaonSlayerTitles.TitlesOf(null));
        Assert.Empty(MahaonSlayerTitles.ProgressOf(null));
    }
}
