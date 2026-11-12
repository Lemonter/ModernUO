using System;
using Server.Commands;
using Server.Spells;
using Server.Targeting;
using Server.Spells.Shamanism;
using Server.Systems.MahaonCombat;

namespace Server.Systems.MahaonShamanism;

/// <summary>
///     Специализации шаманства — по одной на раздел книги.
///
///     Навык один (Шаманство), а разделов четыре, и пользователь хотел, чтобы выбор
///     традиции что-то значил, не запрещая учить остальные. Поэтому устроено так же, как
///     школы остальных видов магии (MysticismSchoolSystem и соседи): у каждого раздела своя
///     дорожка 0–100, она растёт от кастов этого раздела и усиливает только его. Учить можно
///     всё, но сильным станешь в том, что колдуешь чаще.
///
///     У шаманства специализация решает больше, чем у школ магии: вершины раздела (два
///     последних заклинания) без неё не открываются вовсе (VertexRequirement). Иначе
///     шаман, качавший один огонь, бил бы метеоритом и землетрясением одинаково.
///
///     Значения лежат в общем хранилище прокачки (MahaonProgress) под дорожками
///     «branch:shamanism/Orc» и т.д., отдельного сохранения не нужно. Члены ShamanTradition
///     поэтому переименовывать нельзя — это ключи сохранения.
/// </summary>
public static class ShamanismSchoolSystem
{
    private static readonly ProgressTable<ShamanTradition> SchoolValue = new("branch:shamanism");

    public const double MaxValue = 100.0;

    /// <summary>Сколько специализации раздела нужно для его вершин.</summary>
    public const double VertexRequirement = 60.0;

    private const double GainChance = 0.2;
    private const double BreadthUnitsPerPercent = 30.0;
    private const int BreadthCap = 10;

    public static void Configure()
    {
        CommandSystem.Register("ShamanSpec", AccessLevel.GameMaster, ShamanSpec_OnCommand);
        CommandSystem.Register("ShamanReset", AccessLevel.GameMaster, e =>
        {
            ShamanUltimates.Reset();
            e.Mobile.SendMessage(0x59, "Откаты вершин шаманства сброшены.");
        });
    }

    [Usage("ShamanSpec <orc|goblin|ogre|ancient|all> <0-100>")]
    [Description("Выставляет специализацию раздела шаманства выбранному персонажу.")]
    private static void ShamanSpec_OnCommand(CommandEventArgs e)
    {
        if (e.Length < 2 || !double.TryParse(e.GetString(1), out var value))
        {
            e.Mobile.SendMessage(0x22, "Формат: [ShamanSpec <orc|goblin|ogre|ancient|all> <0-100>");
            return;
        }

        var name = e.GetString(0).ToLowerInvariant();
        ShamanTradition? only = name switch
        {
            "orc"     => ShamanTradition.Orc,
            "goblin"  => ShamanTradition.Goblin,
            "ogre"    => ShamanTradition.Ogre,
            "ancient" => ShamanTradition.Ancient,
            _         => null
        };

        if (only == null && name != "all")
        {
            e.Mobile.SendMessage(0x22, "Раздел: orc, goblin, ogre, ancient или all.");
            return;
        }

        e.Mobile.SendMessage(0x3B2, "Кому?");
        e.Mobile.BeginTarget(
            16, false, TargetFlags.None,
            (from, targeted) =>
            {
                if (targeted is not Mobile m)
                {
                    return;
                }

                for (var t = ShamanTradition.Orc; t <= ShamanTradition.Ancient; t++)
                {
                    if (only == null || only == t)
                    {
                        SetValue(m, t, value);
                    }
                }

                from.SendMessage(0x59, $"{m.Name}: специализация {name} = {value:0.0}.");
            }
        );
    }

    public static double GetValue(Mobile m, ShamanTradition tradition) =>
        m == null ? 0.0 : SchoolValue.GetValueOrDefault((m, tradition), 0.0);

    public static void SetValue(Mobile m, ShamanTradition tradition, double value) =>
        SchoolValue[(m, tradition)] = Math.Clamp(value, 0.0, MaxValue);

    public static void OnSpellCast(Mobile caster, ISpell spell)
    {
        // Призрак не учится: посмертие — это трата накопленного, а не тренировка.
        if (spell is ShamanSpell shaman && caster is { Alive: true })
        {
            Train(caster, shaman.Tradition);
        }
    }

    private static void Train(Mobile m, ShamanTradition tradition)
    {
        var key = (m, tradition);
        var current = SchoolValue.GetValueOrDefault(key, 0.0);
        var cap = MahaonMasteryCapSystem.GetCap(m, RuName(tradition));

        if (current >= cap || Utility.RandomDouble() > GainChance)
        {
            return;
        }

        var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
        var newValue = Math.Min(cap, current + gain);
        SchoolValue[key] = newValue;

        MahaonSkillTree.NotifyChanged(m);
        MahaonSkillTree.AnnounceGain(m, RuName(tradition), newValue - current, newValue);

        if (current < VertexRequirement && newValue >= VertexRequirement)
        {
            m.SendMessage(0x59, $"{RuName(tradition)}: тебе открылись вершины раздела.");
        }
    }

    /// <summary>Бонус от остальных разделов: широкий шаман чуть сильнее и в своём.</summary>
    private static double BreadthPercent(Mobile m, ShamanTradition active)
    {
        var total = 0.0;

        for (var t = ShamanTradition.Orc; t <= ShamanTradition.Ancient; t++)
        {
            if (t != active)
            {
                total += GetValue(m, t);
            }
        }

        return Math.Min(BreadthCap, total / BreadthUnitsPerPercent);
    }

    /// <summary>Множитель силы заклинаний раздела: до +30% от своей ветки и до +10% от широты.</summary>
    public static double PowerScalar(Mobile m, ShamanTradition tradition) =>
        1.0 + (GetValue(m, tradition) / MaxValue * 30.0 + BreadthPercent(m, tradition)) / 100.0;

    /// <summary>Множитель цены в мане: до −25%.</summary>
    public static double ManaScalar(Mobile m, ShamanTradition tradition) =>
        1.0 - Math.Min(25.0, GetValue(m, tradition) / MaxValue * 20.0 + BreadthPercent(m, tradition) / 2.0) / 100.0;

    public static bool TryDescribe(Mobile m, ISpell spell, out SpellSchoolInfo info)
    {
        if (spell is not ShamanSpell shaman)
        {
            info = default;
            return false;
        }

        info = new SpellSchoolInfo(
            $"shamanism/{shaman.Tradition}",
            RuName(shaman.Tradition),
            GetValue(m, shaman.Tradition)
        );
        return true;
    }

    public static string RuName(ShamanTradition tradition) => tradition switch
    {
        ShamanTradition.Orc     => "Орочье шаманство",
        ShamanTradition.Goblin  => "Гоблинское шаманство",
        ShamanTradition.Ogre    => "Огрское шаманство",
        ShamanTradition.Ancient => "Древнее шаманство",
        _                       => tradition.ToString()
    };
}
