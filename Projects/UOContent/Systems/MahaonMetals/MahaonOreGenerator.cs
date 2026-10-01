using System.Collections.Generic;

namespace Server.Systems.MahaonMetals;

/// <summary>Взвешенный выбор металла при копании — тот же принцип, что уже был у
/// MahaonResourceTiers (выше навык открывает более высокие тиры и сдвигает шансы в их
/// сторону, но никогда не гарантирует топ-тир), просто на новой таблице из 24 металлов
/// вместо ванильных 9.</summary>
public static class MahaonOreGenerator
{
    // Чем выше требуемый навык металла, тем меньше базовый вес — тот же принцип
    // убывающей редкости, что был в старой системе.
    private static double WeightFor(int miningSkillRequired) => System.Math.Max(1.0, 50.0 - miningSkillRequired * 0.45);

    public static MahaonMetal PickMetal(double miningSkill)
    {
        List<(MahaonMetal metal, double weight)> eligible = null;
        var total = 0.0;

        foreach (var (metal, info) in MahaonMetalTable.Data)
        {
            if (miningSkill < info.MiningSkillRequired)
            {
                continue;
            }

            var weight = WeightFor(info.MiningSkillRequired);
            (eligible ??= new List<(MahaonMetal, double)>()).Add((metal, weight));
            total += weight;
        }

        if (eligible == null)
        {
            return MahaonMetal.Iron; // все навыки Mining проходят порог 0 (Iron)
        }

        var roll = Server.Utility.RandomDouble() * total;

        foreach (var (metal, weight) in eligible)
        {
            if (roll < weight)
            {
                return metal;
            }

            roll -= weight;
        }

        return eligible[^1].metal;
    }

    /// <summary>Сколько единиц даёт один удар киркой — масштабируется от навыка, не
    /// плоское число.</summary>
    public static int YieldCount(double skill) => System.Math.Max(1, (int)(skill / 10.0));
}
