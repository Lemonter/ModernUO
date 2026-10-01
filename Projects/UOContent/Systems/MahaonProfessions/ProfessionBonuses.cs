namespace Server.Systems.MahaonProfessions;

/// <summary>
///     Все боевые числа профессий в одном месте.
///
///     Раздача идёт по схеме «первичка — весь набор, вторичка — сигнатурный перк в полную
///     силу». Из четырёх чисел ниже сигнатурный ровно один — «Меткий глаз» рейнджера,
///     поэтому только он достаётся и вторичке. Остальные три — обычные перки своих
///     категорий и работают лишь у того, кому категория первична.
///
///     До этого всё раздавалось через TouchesCategory, который не отличает первичную
///     категорию от вторичной: вторичка «Воин» давала полный двойной урон, вторичка
///     «Магия» — полный полуторный урон заклинаний и вдвое быстрый каст. Паладин и Боевой
///     маг получались механически одинаковыми, а вторички Барда и Ремесла — ловушкой.
///
///     Сами числа тоже урезаны. +100 к percentageBonus в BaseWeapon.ComputeDamage — это
///     ровно удвоение урона, и оно съедало треть всего бюджета бонусов (там
///     Math.Min(.., 300), в котором живут слееры, Enemy of One, Honor и прочее).
/// </summary>
public static class ProfessionBonuses
{
    /// <summary>Прибавка к percentageBonus в ближнем бою за категорию «Воин».
    /// Было 100 (удвоение) без учёта вторички.</summary>
    public const int WarriorMeleeDamageBonus = 40;

    /// <summary>Прибавка к percentageBonus из лука за категорию «Рейнджер».</summary>
    public const int RangerRangedDamageBonus = 30;

    /// <summary>Множитель урона заклинаний за «Магию». Было ×1.5 без учёта вторички.</summary>
    public const double MagicSpellDamageBonus = 0.25;

    /// <summary>Сокращение времени каста за «Магию». Было ровно вдвое (−50%).</summary>
    public const double MagicCastSpeedBonus = 0.25;

    /// <summary>«Честный бой» — сигнатурный перк Бусидо: прибавка, пока противник один.</summary>
    public const int BushidoDuelDamageBonus = 30;

    /// <summary>«Тяжёлая рука» — обычный перк Воина, только первичная категория.</summary>
    public static int MeleeDamageBonus(Mobile attacker) =>
        (ProfessionSystem.HasFullKit(attacker, ProfessionCategory.Warrior) ? WarriorMeleeDamageBonus : 0) +
        DuelDamageBonus(attacker);

    /// <summary>
    ///     «Честный бой». Считаем, сколько существ прямо сейчас дерутся против него: если
    ///     ровно одно — это поединок, и прибавка работает. Штрафа за свалку нет, просто
    ///     нет и бонуса.
    /// </summary>
    private static int DuelDamageBonus(Mobile attacker)
    {
        if (attacker == null || !ProfessionSystem.HasSignature(attacker, ProfessionCategory.Bushido))
        {
            return 0;
        }

        var foes = 0;

        foreach (var m in attacker.GetMobilesInRange(12))
        {
            if (m != attacker && m.Combatant == attacker && m.Alive && !m.Deleted && ++foes > 1)
            {
                return 0;
            }
        }

        return BushidoDuelDamageBonus;
    }

    /// <summary>Насколько дешевле школьные заклинания у своей категории — «Тёмный
    /// резерв» (Некромантия), «Свет не гаснет» (Вера), «Средоточие» (Мистицизм), «Нить
    /// не рвётся» (Плетение) и «Дух воина» (Бусидо) — это все одно и то же число.</summary>
    public const double SchoolManaDiscount = 0.25;

    /// <summary>
    ///     Скидка на ману школьных заклинаний той категории, которой навык принадлежит.
    ///     Один список вместо пяти одинаковых проверок, разбросанных по школам.
    /// </summary>
    public static double SchoolManaScalar(Mobile caster, SkillName castSkill)
    {
        var category = castSkill switch
        {
            SkillName.Necromancy   => ProfessionCategory.Necromancy,
            SkillName.Chivalry     => ProfessionCategory.Faith,
            SkillName.Mysticism    => ProfessionCategory.Mysticism,
            SkillName.Spellweaving => ProfessionCategory.Weaving,
            _                      => (ProfessionCategory?)null
        };

        return category != null && ProfessionSystem.HasFullKit(caster, category.Value)
            ? 1.0 - SchoolManaDiscount
            : 1.0;
    }

    /// <summary>«Стальная воля» — обычный перк Бусидо: во столько раз чаще парирует.</summary>
    public static double ParryScalar(Mobile defender) =>
        ProfessionSystem.HasFullKit(defender, ProfessionCategory.Bushido) ? 1.25 : 1.0;

    /// <summary>«Длань света» — сигнатурный перк Веры: насколько сильнее лечит.</summary>
    public const double FaithHealingBonus = 0.30;

    /// <summary>
    ///     «Длань света». Работает и на бинты, и на лечебные заклинания — на всё, чем один
    ///     поднимает другого.
    /// </summary>
    public static double HealingScalar(Mobile healer) =>
        healer != null && ProfessionSystem.HasSignature(healer, ProfessionCategory.Faith)
            ? 1.0 + FaithHealingBonus
            : 1.0;

    /// <summary>«Меткий глаз» — сигнатурный перк Рейнджера, достаётся и вторичке.</summary>
    public static int RangedDamageBonus(Mobile attacker) =>
        ProfessionSystem.HasSignature(attacker, ProfessionCategory.Ranger) ? RangerRangedDamageBonus : 0;

    /// <summary>«Точное плетение» — обычный перк Магии, только первичная категория.</summary>
    public static double SpellDamageScalar(Mobile caster) =>
        ProfessionSystem.HasFullKit(caster, ProfessionCategory.Magic) ? 1.0 + MagicSpellDamageBonus : 1.0;

    /// <summary>«Скорость чар» — обычный перк Магии, только первичная категория.</summary>
    public static double CastDelayScalar(Mobile caster) =>
        ProfessionSystem.HasFullKit(caster, ProfessionCategory.Magic) ? 1.0 - MagicCastSpeedBonus : 1.0;
}
