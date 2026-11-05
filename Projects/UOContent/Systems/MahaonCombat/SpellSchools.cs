using System;
using System.Collections.Generic;
using Server.Spells;

namespace Server.Systems.MahaonCombat;

/// <summary>Школа заклинания у конкретного мага: устойчивый ключ линейки, имя и уровень.</summary>
public readonly record struct SpellSchoolInfo(string Line, string Name, double Value);

/// <summary>
///     Одна точка вопроса «к какой школе относится это заклинание» поверх пяти школьных систем.
///
///     ---- Зачем ----
///
///     Каждая школьная система знает свои заклинания (у каждой свой SchoolOf), но этап 7
///     спрашивает про школу из трёх мест: цена стихии (её сбивает школа заклинания), линейка
///     мастерства (соседи — та же школа) и доска (линейки магических навыков — школы). Чтобы
///     эти три места не завели по своей копии классификации, они спрашивают здесь.
///
///     Справочник по типам (каталог) строится один раз: заклинания распознаются сопоставлением
///     экземпляра, поэтому для каталога каждое зарегистрированное заклинание создаётся по разу.
/// </summary>
public static class SpellSchools
{
    public static bool TryDescribe(Mobile m, ISpell spell, out SpellSchoolInfo info) =>
        MagerySchoolSystem.TryDescribe(m, spell, out info) ||
        NecromancySchoolSystem.TryDescribe(m, spell, out info) ||
        ChivalrySchoolSystem.TryDescribe(m, spell, out info) ||
        SpellweavingSchoolSystem.TryDescribe(m, spell, out info) ||
        MysticismSchoolSystem.TryDescribe(m, spell, out info);

    /// <summary>Заклинание в каталоге: номер в реестре, тип, навык каста и школа.</summary>
    public sealed record Entry(int Id, Type Type, SkillName CastSkill, string Line, string SchoolName);

    private static List<Entry> _catalog;
    private static Dictionary<Type, Entry> _byType;

    public static IReadOnlyList<Entry> Catalog
    {
        get
        {
            EnsureCatalog();
            return _catalog;
        }
    }

    public static Entry ByType(Type type)
    {
        EnsureCatalog();
        return type != null && _byType.TryGetValue(type, out var entry) ? entry : null;
    }

    public static Entry ById(int id)
    {
        var types = SpellRegistry.Types;
        return id >= 0 && id < types.Length ? ByType(types[id]) : null;
    }

    private static void EnsureCatalog()
    {
        if (_catalog != null)
        {
            return;
        }

        var catalog = new List<Entry>();
        var byType = new Dictionary<Type, Entry>();
        var types = SpellRegistry.Types;

        for (var id = 0; id < types.Length; id++)
        {
            if (types[id] == null)
            {
                continue;
            }

            Spell spell;

            try
            {
                spell = SpellRegistry.NewSpell(id, null, null);
            }
            catch
            {
                continue; // заклинание, которое без мага не создаётся, в каталог не попадает
            }

            if (spell == null || !TryDescribe(null, spell, out var info))
            {
                continue;
            }

            var entry = new Entry(id, types[id], spell.CastSkill, info.Line, info.Name);
            catalog.Add(entry);
            byType[types[id]] = entry;
        }

        _byType = byType;
        _catalog = catalog;
    }

    // ---- Имена и иконки -----------------------------------------------------------------

    /// <summary>
    ///     Русское имя заклинания из Cliloc_ru.csv, иначе английское. Номера cliloc имён идут
    ///     блоками по видам магии; у Магии первый круг в клиенте переставлен (Реактивная броня
    ///     стоит первой), поэтому для него своя раскладка.
    /// </summary>
    public static string RuName(int id, string fallback)
    {
        var cliloc = id switch
        {
            6                => 1044381,
            >= 0 and <= 5    => 1044382 + id,
            >= 7 and <= 63   => 1044381 + id,
            >= 100 and <= 116 => 1060509 + id - 100,
            >= 200 and <= 209 => 1060493 + id - 200,
            >= 600 and <= 615 => 1071026 + id - 600,
            >= 677 and <= 692 => 1031678 + id - 677,
            _                => 0
        };

        return (cliloc != 0 ? RussianClilocNames.Get(cliloc) : null) ?? fallback;
    }

    public static string RuName(Spell spell)
    {
        if (spell == null)
        {
            return "";
        }

        return RuName(SpellRegistry.GetRegistryNumber(spell.GetType()), spell.Name);
    }

    /// <summary>Иконка заклинания в атласе гампов — та же, что в книге заклинаний.</summary>
    public static int IconOf(int id) => id switch
    {
        >= 0 and <= 63    => 0x08C0 + id,
        >= 100 and <= 116 => 0x5000 + id - 100,
        >= 200 and <= 209 => 0x5100 + id - 200,
        >= 600 and <= 615 => 0x59D8 + id - 600,
        >= 677 and <= 692 => 0x5DC0 + id - 677,
        _                 => 0
    };
}
