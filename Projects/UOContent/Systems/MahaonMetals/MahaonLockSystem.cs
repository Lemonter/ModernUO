using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonMetals;

/// <summary>Металл замка — переиспользует тот же MahaonMetalTracker, что и оружие/броня
/// (это просто Item→MahaonMetal, ему всё равно, сундук это или меч). Замок физически
/// нельзя увидеть глазами — узнаётся только через реальную проверку навыка Mining (см.
/// LockableContainer.OnSingleClick), не через тултип (тултип общий на всех наблюдателей
/// разом, не может показывать разным игрокам разное).</summary>
public static class MahaonLockSystem
{
    public static void AssignRandomLock(Item container, double difficultyBias)
    {
        // difficultyBias 0..1 — насколько "продвинутый" тир должен получиться (для
        // сундуков кладов логично завязать на уровень карты, для рядовых сундуков — 0).
        var metal = WeightedPick(difficultyBias);
        MahaonMetalTracker.SetMetal(container, metal);
    }

    private static MahaonMetal WeightedPick(double difficultyBias)
    {
        var maxRoll = 30 + difficultyBias * 90; // 30..120

        List<(MahaonMetal metal, double weight)> candidates = null;
        var total = 0.0;

        foreach (var (metal, info) in MahaonMetalTable.Data)
        {
            if (info.MiningSkillRequired > maxRoll)
            {
                continue;
            }

            // Вес растёт с требуемым навыком металла, масштаб зависит от difficultyBias —
            // на уровне 0 (bias=0) все кандидаты равны (там и диапазон-то всего 4 штуки,
            // разница не критична). На уровне 5 (bias=1) топовый металл диапазона в
            // разы вероятнее нижнего — тот же принцип "выше требование — реже, но не
            // строго линейно", что и у генератора руды, только тут по возрастанию
            // редкости, а не по убыванию (сундук выше уровнем должен смещаться к
            // старшим тирам, а не просто открывать доступ к ним наравне с младшими).
            var weight = 1.0 + difficultyBias * (info.MiningSkillRequired / 10.0);
            (candidates ??= new List<(MahaonMetal, double)>()).Add((metal, weight));
            total += weight;
        }

        if (candidates == null)
        {
            return MahaonMetal.Iron;
        }

        var roll = Utility.RandomDouble() * total;

        foreach (var (metal, weight) in candidates)
        {
            if (roll < weight)
            {
                return metal;
            }

            roll -= weight;
        }

        return candidates[^1].metal;
    }

    /// <summary>Правило доступа: отмычка должна быть ровно на 1 тир ниже замка, либо на
    /// любое количество тиров выше (включая точное совпадение) — отмычка на 2+ тира ниже
    /// не подходит вообще. Явная проверка через int, не через вычитание прямо на enum —
    /// у Iron (значение 0) "минус 1" переполнило бы байт-подложку enum.</summary>
    public static bool CanAttempt(MahaonMetal lockMetal, MahaonMetal pickMetal)
    {
        var lockValue = (int)lockMetal;
        var pickValue = (int)pickMetal;

        return pickValue >= lockValue || pickValue == lockValue - 1;
    }

    /// <summary>Точное совпадение металла — "угадали металл" — даёт быстрый взлом.</summary>
    public static bool IsExactMatch(MahaonMetal lockMetal, MahaonMetal pickMetal) => lockMetal == pickMetal;
}
