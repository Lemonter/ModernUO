using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonProfessions;

/// <summary>Один перк: как называется, что делает и достаётся ли он вторичной категории.</summary>
public readonly record struct ProfessionPerk(string Name, string Description, bool Signature);

/// <summary>
///     Витрина перков — то, что игрок читает в камне профессий, прежде чем выбрать путь.
///
///     Сами перки живут не здесь: каждый вкручен в ту систему, на которую влияет
///     (парирование — в BaseWeapon.CheckParry, скидка на ману — в Spell.ScaleMana,
///     лечение — в SpellHelper.Heal и в бинтах, и так далее). Здесь только их описания,
///     иначе выбор профессии остаётся тем же, чем был: списком навыков без единого слова
///     о том, ради чего этот путь вообще выбирают.
///
///     Правило раздачи одно на все двенадцать категорий: сигнатурный перк работает и у
///     того, кому категория вторична, остальные два — только у первичной. Поэтому
///     сигнатурный ровно один в каждом списке и стоит первым.
/// </summary>
public static class ProfessionPerks
{
    private static readonly Dictionary<ProfessionCategory, ProfessionPerk[]> _perks = new()
    {
        [ProfessionCategory.Magic] = new[]
        {
            new ProfessionPerk(
                "Свободные руки",
                "Заклинание не вышибает оружие и щит из рук — можно колдовать, не расставаясь с ними.",
                true
            ),
            new ProfessionPerk("Точное плетение", "Заклинания бьют на четверть сильнее.", false),
            new ProfessionPerk("Скорость чар", "Заклинания читаются на четверть быстрее.", false)
        },

        [ProfessionCategory.Craft] = new[]
        {
            new ProfessionPerk(
                "Печать мастера",
                "Только его рука вкладывает в оружие чтение заклинаний и «оружие мага» — за этими печатями все идут к нему.",
                true
            ),
            new ProfessionPerk("Крепкая спина", "Уносит на 100 камней больше.", false),
            new ProfessionPerk("Щедрая жила", "Каждая пятая добыча из жилы, дерева или воды выходит двойной.", false)
        },

        [ProfessionCategory.Warrior] = new[]
        {
            new ProfessionPerk(
                "Стойкость",
                "Пока здоровья меньше трети, весь входящий урон режется вдвое. Без отката, пока держится на ногах.",
                true
            ),
            new ProfessionPerk("Тяжёлая рука", "Ближний бой бьёт на 40% сильнее.", false),
            new ProfessionPerk("Второе дыхание", "Выносливость восстанавливается вдвое быстрее.", false)
        },

        [ProfessionCategory.Thief] = new[]
        {
            new ProfessionPerk("Тень в бою", "Прячется прямо посреди схватки, когда все прочие уже не могут.", true),
            new ProfessionPerk("Уворот", "Каждый третий удар по нему уходит в пустоту.", false),
            new ProfessionPerk("Лёгкая рука", "Вытащит из чужого рюкзака вчетверо более тяжёлую вещь.", false)
        },

        [ProfessionCategory.Bard] = new[]
        {
            new ProfessionPerk(
                "Слово громче стали",
                "Инструмент ему не нужен: голос сам сойдёт за лютню, и умение сработает с пустыми руками.",
                true
            ),
            new ProfessionPerk("Дальний зов", "Песню слышно на восемь клеток дальше.", false),
            new ProfessionPerk("Верный слух", "Разлад, провокация и миротворчество удаются заметно чаще.", false)
        },

        [ProfessionCategory.Ranger] = new[]
        {
            new ProfessionPerk("Меткий глаз", "Стрельба бьёт на 30% сильнее.", true),
            new ProfessionPerk("Бережливый колчан", "Половина выстрелов не тратит стрелу.", false),
            new ProfessionPerk("Чутьё следопыта", "Выслеживание достаёт вдвое дальше.", false)
        },

        [ProfessionCategory.Necromancy] = new[]
        {
            new ProfessionPerk("Власть над мёртвыми", "Поднятая нежить не рассыпается вдвое дольше.", true),
            new ProfessionPerk("Тёмный резерв", "Заклинания некромантии стоят на четверть меньше маны.", false),
            new ProfessionPerk("Костяной доспех", "Поднятая нежить выходит в полтора раза крепче.", false)
        },

        [ProfessionCategory.Faith] = new[]
        {
            new ProfessionPerk("Длань света", "Лечит на 30% сильнее — и бинтами, и заклинаниями.", true),
            new ProfessionPerk("Свет не гаснет", "Рыцарские заклинания стоят на четверть меньше маны.", false),
            new ProfessionPerk("Скорая помощь", "Бинты ложатся на треть быстрее.", false)
        },

        [ProfessionCategory.Mysticism] = new[]
        {
            new ProfessionPerk(
                "Стихийный круг",
                "Сила его чар идёт от самой Мистики — второй навык (Вкладывание или Сосредоточение) ему не нужен.",
                true
            ),
            new ProfessionPerk("Средоточие", "Заклинания мистицизма стоят на четверть меньше маны.", false),
            new ProfessionPerk("Неодолимые чары", "От его заклинаний отмахнуться на четверть труднее.", false)
        },

        [ProfessionCategory.Weaving] = new[]
        {
            new ProfessionPerk(
                "Тонкая нить",
                "Аркановая сфера ему не нужна: и без неё плетение держит средний уровень силы.",
                true
            ),
            new ProfessionPerk("Сила круга", "Уровень плетения выше на две ступени.", false),
            new ProfessionPerk("Нить не рвётся", "Плетение стоит на четверть меньше маны.", false)
        },

        [ProfessionCategory.Bushido] = new[]
        {
            new ProfessionPerk("Честный бой", "Пока противник один на один — урон выше на 30%.", true),
            new ProfessionPerk("Стальная воля", "Парирует на четверть чаще.", false),
            new ProfessionPerk("Дух воина", "Приёмы боевых школ стоят на четверть меньше маны.", false)
        },

        [ProfessionCategory.Ninjitsu] = new[]
        {
            new ProfessionPerk(
                "Теневые клоны",
                "Двойник выходит не один, а два, и слотов питомцев они не занимают.",
                true
            ),
            new ProfessionPerk("Бесшумный шаг", "Пять лишних шагов в тени сверх того, что даёт навык.", false),
            new ProfessionPerk("Ядовитый клинок", "Яд сходит с его клинка заметно охотнее.", false)
        }
    };

    public static IReadOnlyList<ProfessionPerk> For(ProfessionCategory category) =>
        _perks.GetValueOrDefault(category, Array.Empty<ProfessionPerk>());

    /// <summary>Сигнатурный перк категории — тот единственный, что достаётся и вторичке.</summary>
    public static ProfessionPerk? Signature(ProfessionCategory category)
    {
        foreach (var perk in For(category))
        {
            if (perk.Signature)
            {
                return perk;
            }
        }

        return null;
    }
}
