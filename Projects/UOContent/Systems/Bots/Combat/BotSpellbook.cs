using System;
using Server.Mobiles;
using Server.Spells;

namespace Server.Systems.Bots;

/// <summary>
///     Что бот может наложить ПРЯМО СЕЙЧАС и стоит ли начинать.
///
///     Зачем отдельный слой. Выбор заклинания раньше стоял на угаданных порогах навыка:
///     «магия 30 — можно Lightning, 50 — Fireball, 80 — EnergyBolt». Числа выглядели
///     разумно, но взяты они были из головы, а не из правил игры, и расходились с ними
///     грубо. Lightning — заклинание ЧЕТВЁРТОГО круга, и при магии 30 у него по таблице
///     движка (MagerySpell._requiredSkill, ветка Core.ML) диапазон 24..64, то есть шанс
///     успеха ровно (30−24)/40 = 15%. Пять попыток из шести — провал, с потерей маны и
///     времени. Именно это и видно на кладбище: толпа магов, беспрерывно фитилящая.
///
///     Поэтому здесь ничего не угадывается. Шанс берётся из тех же двух чисел, которыми
///     пользуется сам движок (Spell.GetCastSkills), по той же формуле, что в
///     SkillCheck.CheckLocation: chance = (навык − min) / (max − min), с обрезкой по
///     краям. Совпадение с настоящим броском не приблизительное, а точное.
/// </summary>
public static class BotSpellbook
{
    /// <summary>
    ///     Ниже этого шанса заклинание не рассматривается вовсе, каким бы сильным ни было.
    ///
    ///     Половина — не произвольное число, а точка, где ставка перестаёт себя оправдывать:
    ///     при меньшем шансе средний расход маны на одно попадание больше, чем у заклинания
    ///     кругом ниже, которое почти не фитилит. Побочно это даёт ровно то поведение,
    ///     которого ждёшь от игрока: при магии 30 верхняя доступная планка — третий круг
    ///     (10..50, то есть те же 50%), а не четвёртый.
    /// </summary>
    public const double MinCastChance = 0.5;

    /// <summary>
    ///     Шанс, что заклинание не сорвётся. Считается так же, как считает движок.
    ///
    ///     GetCastSkills отдаёт границы с учётом круга И свитка (у свитка требования на два
    ///     круга мягче), поэтому спрашивать надо именно его, а не выводить из круга самому.
    /// </summary>
    public static double CastChance(Mobile caster, Spell spell)
    {
        if (caster == null || spell == null)
        {
            return 0.0;
        }

        spell.GetCastSkills(out var min, out var max);

        var value = caster.Skills[spell.CastSkill].Value;

        if (value < min)
        {
            return 0.0;
        }

        if (value >= max || min >= max)
        {
            return 1.0;
        }

        return (value - min) / (max - min);
    }

    /// <summary>
    ///     Хватит ли реагентов — БЕЗ их расходования.
    ///
    ///     Своя проверка, а не Spell.ConsumeReagents, по двум причинам. Первая очевидна: тот
    ///     метод тратит, а нам надо только посмотреть. Вторая важнее — там стоит явное
    ///     исключение для ботов (Caster is BotMobile), из-за которого заклинания им ничего
    ///     не стоят. При этом снабжение (SupplyGoal) реагенты им покупает и держит запас,
    ///     то есть половина системы живёт так, будто они расходуются, а другая — будто нет.
    ///     Здесь считается честно, по рюкзаку.
    /// </summary>
    public static bool HasReagents(Mobile caster, Spell spell)
    {
        // Свиток и палочка несут собственную силу, реагенты им не нужны.
        if (spell?.Scroll != null)
        {
            return true;
        }

        var info = spell?.Info;
        var regs = info?.Reagents;

        if (regs == null || regs.Length == 0)
        {
            return true;
        }

        var pack = caster?.Backpack;

        if (pack == null)
        {
            return false;
        }

        var amounts = info.Amounts;

        for (var i = 0; i < regs.Length; i++)
        {
            var need = amounts != null && i < amounts.Length ? amounts[i] : 1;

            if (pack.GetAmount(regs[i], true) < need)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Можно ли начинать это заклинание: хватает маны с запасом, есть реагенты и шанс
    ///     не ниже порога.
    ///
    ///     Запас маны (reserve) — не перестраховка. Маг, вылив последнюю ману в атаку,
    ///     остаётся без лечения, и следующий удар для него последний. Именно так и выглядит
    ///     «пришёл, постоял, умер».
    /// </summary>
    public static bool Usable(Mobile caster, Spell spell, int manaReserve, double minChance = MinCastChance) =>
        caster != null && spell != null &&
        caster.Mana >= spell.GetMana() + manaReserve &&
        CastChance(caster, spell) >= minChance &&
        HasReagents(caster, spell);

    /// <summary>
    ///     Ожидаемая польза от попытки: сила, помноженная на шанс и поделённая на ману.
    ///
    ///     Сравнивать заклинания по одной силе нельзя — так всегда выигрывает старший круг,
    ///     даже когда он фитилит через раз. И по одному шансу нельзя — так всегда выигрывает
    ///     первый круг, который почти не бьёт. Правильная величина — средний урон на единицу
    ///     потраченной маны, и именно её выбирает живой игрок, не проговаривая вслух.
    /// </summary>
    public static double Value(Mobile caster, Spell spell, double power)
    {
        var mana = Math.Max(1, spell.GetMana());

        return power * CastChance(caster, spell) / mana;
    }
}
