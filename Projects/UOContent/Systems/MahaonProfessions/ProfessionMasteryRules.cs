using System.Collections.Generic;
using Server.Systems.MahaonCombat;

namespace Server.Systems.MahaonProfessions;

/// <summary>
///     Профессиональная мастерка — пятый пункт разбора профессий.
///
///     Мастерки (школы магии, воинские стили, ремесленные и воровские специализации) до
///     сих пор были одинаковы для всех: любой мог поднять свитком потолок любой из них, и
///     профессия на это не влияла никак. Здесь появляется потолок потолка: свиток
///     мастерства работает только на те мастерки, что лежат на твоём пути.
///
///     — своя первичная категория: до 120, как и обычные свитки силы на навыки;
///     — вторичная категория: до 110;
///     — третий путь, выбранный игроком: до 105;
///     — чужая мастерка: 100, то есть свиток на неё не подействует вовсе.
///
///     Ничего уже достигнутого это не отбирает: базовый потолок остаётся 100 у всех, и
///     качается любая мастерка по-прежнему кем угодно. Ограничение касается только того,
///     насколько далеко её можно увести за общую сотню.
///
///     К какой категории относится мастерка, здесь не перечисляется руками: дерево
///     навыков уже разложено по подкатегориям, названным по самому навыку («Кузнечное
///     дело», «Мечи», «Некромантия»), а ProfessionData.CategorySkills знает, какой
///     категории принадлежит каждый из 58 навыков. Второй список тех же имён рядом с
///     существующим только разъезжался бы с ним.
/// </summary>
public static class ProfessionMasteryRules
{
    public const double ForeignCeiling = MahaonMasteryCapSystem.DefaultCap;   // 100
    public const double SecondaryCeiling = 110.0;
    public const double ThirdCeiling = 105.0;
    public const double PrimaryCeiling = MahaonMasteryCapSystem.AbsoluteCap;  // 120

    private static Dictionary<string, ProfessionCategory> _subCategoryToProfession;

    /// <summary>
    ///     Имя подкатегории дерева навыков → категория профессии. Строится один раз:
    ///     состав навыков в категориях задан в коде и в игре не меняется.
    /// </summary>
    private static Dictionary<string, ProfessionCategory> SubCategoryMap
    {
        get
        {
            if (_subCategoryToProfession != null)
            {
                return _subCategoryToProfession;
            }

            var map = new Dictionary<string, ProfessionCategory>();

            foreach (var (category, skills) in ProfessionData.CategorySkills)
            {
                foreach (var skill in skills)
                {
                    // Первая категория, которая заявила навык своим, и остаётся его
                    // хозяйкой — навык может встречаться в нескольких списках (Тактика
                    // нужна и Воину, и Бусидо), и мастерка должна принадлежать кому-то
                    // одному.
                    map.TryAdd(MahaonSkillTree.RuSkillName(skill), category);

                    // Боевые стили лежат в дереве не под именем навыка, а под именем
                    // категории оружия («Мечи», «Дробящее оружие»), поэтому их имена
                    // добавляются отдельно.
                    var weaponCategory = skill switch
                    {
                        SkillName.Wrestling => WeaponCategory.Wrestling,
                        SkillName.Swords    => WeaponCategory.Swords,
                        SkillName.Macing    => WeaponCategory.Macing,
                        SkillName.Fencing   => WeaponCategory.Fencing,
                        SkillName.Archery   => WeaponCategory.Archery,
                        SkillName.Throwing  => WeaponCategory.Throwing,
                        _                   => (WeaponCategory?)null
                    };

                    if (weaponCategory != null)
                    {
                        map.TryAdd(WeaponStyleSystem.RuCategoryName(weaponCategory.Value), category);
                    }
                }
            }

            return _subCategoryToProfession = map;
        }
    }

    /// <summary>
    ///     Какой категории принадлежит мастерка с таким именем. Ищем через снимок дерева:
    ///     мастерка лежит в подкатегории, названной по своему навыку, а имя навыка мы уже
    ///     умеем переводить в категорию.
    /// </summary>
    public static ProfessionCategory? CategoryOf(Mobile m, string masteryName)
    {
        if (m == null || masteryName == null)
        {
            return null;
        }

        foreach (var treeCategory in MahaonSkillTree.GetTreeSnapshot(m))
        {
            foreach (var sub in treeCategory.SubCategories)
            {
                if (sub.IsUsableSkillList || !SubCategoryMap.TryGetValue(sub.Name, out var category))
                {
                    continue;
                }

                foreach (var entry in sub.Entries)
                {
                    if (entry.Name == masteryName)
                    {
                        return category;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    ///     Категория подкатегории дерева навыков — дешёвая проверка по имени, без снимка
    ///     всего дерева. Тому, кто и так обходит дерево, звать GetCeiling на каждую
    ///     мастерку нельзя: снимок строится целиком на каждый вызов.
    /// </summary>
    public static ProfessionCategory? CategoryOfSubCategory(string subCategoryName) =>
        subCategoryName != null && SubCategoryMap.TryGetValue(subCategoryName, out var category)
            ? category
            : null;

    /// <summary>Выше этого потолок мастерки такой категории у этого игрока не поднять.</summary>
    public static double CeilingFor(Mobile m, ProfessionCategory? category)
    {
        if (category == null)
        {
            // Мастерку не удалось соотнести ни с одной категорией — не запрещаем, ведём
            // себя как раньше. Иначе новая мастерка, забытая в таблице навыков, молча
            // переставала бы принимать свитки.
            return PrimaryCeiling;
        }

        // Профессии ещё нет — правило не применяется вовсе, иначе свиток мастерства
        // оказывался бы бесполезен у всякого, кто до камня профессий пока не дошёл.
        if (ProfessionSystem.GetProfession(m) == null)
        {
            return PrimaryCeiling;
        }

        if (ProfessionSystem.HasFullKit(m, category.Value))
        {
            return PrimaryCeiling;
        }

        if (ProfessionSystem.GetThirdCategory(m) == category.Value)
        {
            return ThirdCeiling;
        }

        return ProfessionSystem.TouchesCategory(m, category.Value) ? SecondaryCeiling : ForeignCeiling;
    }

    /// <summary>Выше этого потолок этой мастерки у этого игрока не поднять ничем.</summary>
    public static double GetCeiling(Mobile m, string masteryName) => CeilingFor(m, CategoryOf(m, masteryName));
}
