using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonCombat;

public enum WeaponCategory : byte
{
    Wrestling = 0,
    Swords = 1,
    Macing = 2,
    Fencing = 3,
    Archery = 4,
    Throwing = 5
}

/// <summary>
///     Generalized replacement for what used to be martial-arts-only — every weapon
///     category (unarmed + the four real weapon skills) now gets the same "5 styles,
///     pick one active, train them all" treatment. Style index (0-4) is per-category —
///     "0" means something different in Swords vs Macing — so anything that touches a
///     style choice needs BOTH the category and the index, never the index alone.
///
///     Breadth bonus — the actual answer to "why train more than one style": every 20
///     points banked in a NON-active style of the same category adds +1% damage to
///     whichever style IS active, capped at +10% total. Training styles you rarely use
///     still pays off passively — no need to constantly switch mid-fight to get value
///     out of them.
/// </summary>
public static class WeaponStyleSystem
{
    private static readonly ProgressTable<WeaponCategory, byte> StyleValue = new("branch:style");

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static WeaponCategory? GetCategory(SkillName skill) => skill switch
    {
        SkillName.Wrestling => WeaponCategory.Wrestling,
        SkillName.Swords    => WeaponCategory.Swords,
        SkillName.Macing    => WeaponCategory.Macing,
        SkillName.Fencing   => WeaponCategory.Fencing,
        SkillName.Archery   => WeaponCategory.Archery,
        SkillName.Throwing  => WeaponCategory.Throwing,
        _                   => null
    };

    /// <summary>Сколько стилей у каждого вида оружия: 0 — классический, 1–6 — именные.</summary>
    public const int StyleCount = 7;

    private static string ChoiceTrack(WeaponCategory category) => $"choice:style/{category}";

    /// <summary>
    ///     Активный стиль — тот, что выбрал игрок, отдельно от места удара.
    ///
    ///     ---- Как было ----
    ///
    ///     По прежней договорённости («стиль теперь идёт от места удара») стиль выводился из
    ///     выбранного места: целишься в шею — у тебя стиль, которому шея любимое место.
    ///     Теперь решено иначе: стиль и место — два независимых выбора, стиль даёт свою плюшку
    ///     и платит за неё сам (COMBAT-PROGRESSION.md, раздел 7.1).
    ///
    ///     ---- Переезд ----
    ///
    ///     Выбор хранится в общем хранилище (MahaonProgress, дорожка «choice:style/вид»).
    ///     У того, кто ещё ничего не выбирал, при первом обращении записывается ровно тот стиль,
    ///     который у него и так получался от места удара. Так обновление ничего не меняет у
    ///     игрока молча: дальше стиль просто перестаёт ходить за местом.
    /// </summary>
    public static byte GetActiveStyle(Mobile m, WeaponCategory category)
    {
        if (m == null)
        {
            return 0;
        }

        var track = ChoiceTrack(category);

        if (MahaonProgress.TryGet(m, track, out var chosen))
        {
            return chosen >= 0 && chosen < StyleCount ? (byte)chosen : (byte)0;
        }

        var legacy = LegacyStyleFromLocation(m, category);
        MahaonProgress.Set(m, track, legacy);

        return legacy;
    }

    /// <summary>Выбрать стиль. Номер вне 0..6 — отказ.</summary>
    public static bool SetActiveStyle(Mobile m, WeaponCategory category, byte style)
    {
        if (m == null || style >= StyleCount)
        {
            return false;
        }

        MahaonProgress.Set(m, ChoiceTrack(category), style);
        MahaonSkillTree.NotifyChanged(m);

        return true;
    }

    /// <summary>Каким был бы стиль по старому правилу — нужен только для переезда.</summary>
    private static byte LegacyStyleFromLocation(Mobile m, WeaponCategory category)
    {
        var location = HitLocationSystem.GetChosenLocation(m);

        if (location != null && HitLocationSystem.CanUseLocation(m, location.Value) &&
            StyleByFavoredLocation.TryGetValue((category, location.Value), out var style))
        {
            return style;
        }

        return 0;
    }

    /// <summary>Ванильный навык, на котором стоит этот вид оружия.</summary>
    public static SkillName SkillOf(WeaponCategory category) => category switch
    {
        WeaponCategory.Wrestling => SkillName.Wrestling,
        WeaponCategory.Swords    => SkillName.Swords,
        WeaponCategory.Macing    => SkillName.Macing,
        WeaponCategory.Fencing   => SkillName.Fencing,
        WeaponCategory.Archery   => SkillName.Archery,
        WeaponCategory.Throwing  => SkillName.Throwing,
        _                        => SkillName.Wrestling
    };

    public static double GetValue(Mobile m, WeaponCategory category, byte style)
    {
        // Style 0 ("Классический"/baseline) rides on the REAL underlying vanilla skill
        // for that category — same convention Wrestling already used (style 0 there
        // tracked the true SkillName.Wrestling value, not a separate counter).
        if (style == 0)
        {
            return m.Skills[SkillOf(category)].Value;
        }

        return StyleValue.GetValueOrDefault((m, category, style), 0.0);
    }

    /// <summary>
    ///     После удачного удара (зовётся из BaseWeapon.OnHit): шанс прироста ВЫБРАННОГО стиля,
    ///     тем выше, чем больнее был удар. Раньше качался стиль, чьё любимое место попалось под
    ///     удар, — пока стиль жил от места удара; теперь стиль выбирают отдельно, и качается он.
    /// </summary>
    private const double GainChancePerDamage = 0.01;
    private const double MaxExtraGainChance = 0.5;

    public static void OnSuccessfulHit(Mobile attacker, Item weapon, int damage)
    {
        var category = CategoryOf(weapon);

        if (category == null)
        {
            return;
        }

        // Качается выбранный стиль. Раньше — тот, чьё любимое место попалось под удар: стиль
        // жил от места удара, и тренировка шла за ним же.
        var style = GetActiveStyle(attacker, category.Value);

        // Классический стиль — это сам навык оружия, его тренирует ванильная проверка навыка.
        if (style == 0)
        {
            return;
        }

        var key = (attacker, category.Value, style);
        var current = StyleValue.GetValueOrDefault(key, 0.0);

        // Чем больше урона нанесено этим ударом, тем выше шанс — на GainChance наслаивается
        // дополнительный шанс, растущий с уроном, а не заменяет собой базовый.
        var chance = GainChance + System.Math.Min(MaxExtraGainChance, System.Math.Max(0, damage) * GainChancePerDamage);

        var cap = MahaonMasteryCapSystem.GetCap(attacker, StyleName(category.Value, style));
        if (current >= cap || Utility.RandomDouble() > chance)
        {
            return;
        }

        var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
        var newValue = System.Math.Min(cap, current + gain);
        StyleValue[key] = newValue;
        MahaonSkillTree.NotifyChanged(attacker);
        MahaonSkillTree.AnnounceGain(attacker, StyleName(category.Value, style), newValue - current, newValue);
    }

    // 1% max HP per 1.0 Wrestling skill, plus 1% per 10.0 in each trained Wrestling style
    // (summed across all non-baseline styles, same "train the ones you don't actively use
    // too" idea as the breadth bonus above) — see PlayerMobile.HitsMax for where this gets
    // applied.
    public static double GetWrestlingHitsBonusPercent(Mobile m)
    {
        var percent = m.Skills[SkillName.Wrestling].Value;

        for (byte s = 1; s <= 6; s++)
        {
            percent += GetValue(m, WeaponCategory.Wrestling, s) * 0.1;
        }

        return percent;
    }

    public static WeaponCategory? CategoryOf(Item weapon) =>
        weapon is BaseWeapon w ? GetCategory(w.DefSkill) : null;

    /// <summary>Ширина: +1% урона за каждые 20 очков в других стилях этого оружия, до 10%.</summary>
    public static int GetDamageBonus(Mobile attacker, Item weapon)
    {
        var category = CategoryOf(weapon);

        if (category == null)
        {
            return 0;
        }

        // Плюшка и цена самого стиля — вкладом в профиль удара (WeaponStylePerks). Здесь
        // осталась только «ширина»: очки в других стилях того же оружия.
        return GetBreadthBonus(attacker, category.Value, GetActiveStyle(attacker, category.Value));
    }

    private static int GetBreadthBonus(Mobile m, WeaponCategory category, byte activeStyle)
    {
        var total = 0.0;

        for (byte s = 0; s < 7; s++)
        {
            if (s == activeStyle)
            {
                continue;
            }

            total += GetValue(m, category, s);
        }

        return (int)System.Math.Min(BreadthCap, total / BreadthUnitsPerPercent);
    }

    /// <summary>После удачного удара — особые механики стилей (контрудар, выбить оружие,
    /// задеть соседа, кровь). Зовётся из BaseWeapon.OnHit.</summary>
    public static void OnAfterHit(Mobile attacker, Mobile defender, Item weapon, int damage)
    {
        if (attacker == null || defender == null || weapon is not BaseWeapon w)
        {
            return;
        }

        WeaponStylePerks.OnAfterHit(attacker, defender, w, damage);
    }

    // Любимые места стилей — остались только для переезда: у того, кто ещё не выбирал
    // стиль, он выводится из выбранного места по старому правилу (LegacyStyleFromLocation).
    // Бонусов они больше не дают: стиль и место — два независимых выбора (раздел 7.1).
    private static readonly Dictionary<(WeaponCategory, byte), HitLocation> FavoredLocation = new()
    {
        // Рукопашный бой
        [(WeaponCategory.Wrestling, 1)] = HitLocation.Chest, // Бокс — body punches
        [(WeaponCategory.Wrestling, 2)] = HitLocation.Legs,  // Муай-тай — famous leg kicks
        [(WeaponCategory.Wrestling, 3)] = HitLocation.Hands, // Карате — hand strikes/blocks
        [(WeaponCategory.Wrestling, 4)] = HitLocation.Arms,  // Айкидо — joint locks, arm throws
        [(WeaponCategory.Wrestling, 5)] = HitLocation.Back,  // Самбо — takedowns, control from behind
        [(WeaponCategory.Wrestling, 6)] = HitLocation.Neck,  // Греко-римская борьба — choke holds

        // Мечи
        [(WeaponCategory.Swords, 1)] = HitLocation.Neck,  // Кендзюцу — decisive precision strikes
        [(WeaponCategory.Swords, 2)] = HitLocation.Hands, // Дуэльный — target the sword-hand
        [(WeaponCategory.Swords, 3)] = HitLocation.Chest, // Берсерк — wild heavy cleaves
        [(WeaponCategory.Swords, 4)] = HitLocation.Legs,  // Клинок тени — hamstring from ambush
        [(WeaponCategory.Swords, 5)] = HitLocation.Arms,  // Двуручный размах — wide two-handed arcs
        [(WeaponCategory.Swords, 6)] = HitLocation.Back,  // Стиль возмездия — flanking counter-strikes

        // Дробящее оружие
        [(WeaponCategory.Macing, 1)] = HitLocation.Chest, // Молот войны — crushing torso blows
        [(WeaponCategory.Macing, 2)] = HitLocation.Arms,  // Цепной — flail wraps around limbs
        [(WeaponCategory.Macing, 3)] = HitLocation.Hands, // Оглушающий удар — strike the weapon hand
        [(WeaponCategory.Macing, 4)] = HitLocation.Legs,  // Рунный кузнец — sweeping low blows
        [(WeaponCategory.Macing, 5)] = HitLocation.Neck,  // Палач — executioner's decapitating blow
        [(WeaponCategory.Macing, 6)] = HitLocation.Back,  // Штурмовой таран — charging flank strike

        // Колющее оружие
        [(WeaponCategory.Fencing, 1)] = HitLocation.Legs,  // Копейщик — reach weapon, leg pokes
        [(WeaponCategory.Fencing, 2)] = HitLocation.Neck,  // Ассасин — precise assassination strike
        [(WeaponCategory.Fencing, 3)] = HitLocation.Chest, // Пронзающий — armor-piercing thrust
        [(WeaponCategory.Fencing, 4)] = HitLocation.Arms,  // Вихрь клинков — spinning arm slashes
        [(WeaponCategory.Fencing, 5)] = HitLocation.Hands, // Дуэлянт-фехтовальщик — classic en garde precision
        [(WeaponCategory.Fencing, 6)] = HitLocation.Back,  // Ночной клинок — dagger backstab

        // Стрельба из лука
        [(WeaponCategory.Archery, 1)] = HitLocation.Neck,  // Снайпер — precision headshot-equivalent
        [(WeaponCategory.Archery, 2)] = HitLocation.Chest, // Шквальный огонь — wide easy target
        [(WeaponCategory.Archery, 3)] = HitLocation.Legs,  // Следопыт — hobbling shots
        [(WeaponCategory.Archery, 4)] = HitLocation.Hands, // Эльфийская стрельба — disarming precision
        [(WeaponCategory.Archery, 5)] = HitLocation.Arms,  // Охотник за головами — disabling shots
        [(WeaponCategory.Archery, 6)] = HitLocation.Back,  // Тёмный лучник — sniping from the shadows

        // Метание
        [(WeaponCategory.Throwing, 1)] = HitLocation.Neck,  // Меткий бросок — precision throw
        [(WeaponCategory.Throwing, 2)] = HitLocation.Hands, // Отравленные лезвия — coated blades to the hand
        [(WeaponCategory.Throwing, 3)] = HitLocation.Chest, // Шквал клинков — wide spread of blades
        [(WeaponCategory.Throwing, 4)] = HitLocation.Arms,  // Смертельный вихрь — spinning multi-throw
        [(WeaponCategory.Throwing, 5)] = HitLocation.Legs,  // Дальнобойный бросок — long-range hobbling throw
        [(WeaponCategory.Throwing, 6)] = HitLocation.Back   // Теневой метатель — thrown from ambush
    };

    // Reverse of FavoredLocation — "which style trains when this location gets hit", used
    // by OnSuccessfulHit. Built once from the table above instead of hand-duplicated, so
    // the two can never drift out of sync with each other.
    private static readonly Dictionary<(WeaponCategory, HitLocation), byte> StyleByFavoredLocation =
        BuildStyleByFavoredLocation();

    private static Dictionary<(WeaponCategory, HitLocation), byte> BuildStyleByFavoredLocation()
    {
        var map = new Dictionary<(WeaponCategory, HitLocation), byte>();

        foreach (var ((category, style), location) in FavoredLocation)
        {
            map[(category, location)] = style;
        }

        return map;
    }

    public static string RuLocationName(HitLocation location) => location switch
    {
        HitLocation.Chest => "грудь",
        HitLocation.Arms  => "руки",
        HitLocation.Legs  => "ноги",
        HitLocation.Hands => "кисти",
        HitLocation.Back  => "спину",
        HitLocation.Neck  => "шею",
        HitLocation.Head  => "голову",
        _                 => location.ToString()
    };

    public static string RuCategoryName(WeaponCategory category) => category switch
    {
        WeaponCategory.Wrestling => "Рукопашный бой",
        WeaponCategory.Swords    => "Мечи",
        WeaponCategory.Macing    => "Дробящее оружие",
        WeaponCategory.Fencing   => "Колющее оружие",
        WeaponCategory.Archery   => "Стрельба из лука",
        WeaponCategory.Throwing  => "Метание",
        _                        => category.ToString()
    };

    public static string StyleName(WeaponCategory category, byte style)
    {
        var baseName = category switch
        {
            WeaponCategory.Wrestling => style switch
            {
                1 => "Бокс", 2 => "Муай-тай", 3 => "Карате", 4 => "Айкидо",
                5 => "Самбо", 6 => "Греко-римская борьба", _ => "Борьба"
            },
            WeaponCategory.Swords => style switch
            {
                1 => "Кендзюцу", 2 => "Дуэльный", 3 => "Берсерк", 4 => "Клинок тени",
                5 => "Двуручный размах", 6 => "Стиль возмездия", _ => "Классический"
            },
            WeaponCategory.Macing => style switch
            {
                1 => "Молот войны", 2 => "Цепной", 3 => "Оглушающий удар", 4 => "Рунный кузнец",
                5 => "Палач", 6 => "Штурмовой таран", _ => "Классический"
            },
            WeaponCategory.Fencing => style switch
            {
                1 => "Копейщик", 2 => "Ассасин", 3 => "Пронзающий", 4 => "Вихрь клинков",
                5 => "Дуэлянт-фехтовальщик", 6 => "Ночной клинок", _ => "Классический"
            },
            WeaponCategory.Archery => style switch
            {
                1 => "Снайпер", 2 => "Шквальный огонь", 3 => "Следопыт", 4 => "Эльфийская стрельба",
                5 => "Охотник за головами", 6 => "Тёмный лучник", _ => "Классический"
            },
            WeaponCategory.Throwing => style switch
            {
                1 => "Меткий бросок", 2 => "Отравленные лезвия", 3 => "Шквал клинков", 4 => "Смертельный вихрь",
                5 => "Дальнобойный бросок", 6 => "Теневой метатель", _ => "Классический"
            },
            _ => style.ToString()
        };

        return baseName;
    }

    /// <summary>
    ///     Прежнее имя — с любимым местом в скобках («Кендзюцу (шею)»). Под ним лежат уже
    ///     поднятые свитками потолки; при запуске они переезжают на новое имя (MigrateCaps).
    /// </summary>
    private static string LegacyName(WeaponCategory category, byte style) =>
        FavoredLocation.TryGetValue((category, style), out var loc)
            ? $"{StyleName(category, style)} ({RuLocationName(loc)})"
            : StyleName(category, style);

    /// <summary>Потолки стилей, поднятые под старыми именами, — на новые имена.</summary>
    public static void Initialize()
    {
        var moved = 0;

        foreach (var m in World.Mobiles.Values)
        {
            if (m is not Mobiles.PlayerMobile)
            {
                continue;
            }

            for (var c = WeaponCategory.Wrestling; c <= WeaponCategory.Throwing; c++)
            {
                for (byte s = 1; s < StyleCount; s++)
                {
                    var legacy = LegacyName(c, s);
                    var current = StyleName(c, s);

                    if (legacy == current || !MahaonProgress.TryGet(m, "cap/" + legacy, out var cap))
                    {
                        continue;
                    }

                    if (MahaonMasteryCapSystem.RaiseCap(m, current, cap))
                    {
                        moved++;
                    }

                    MahaonProgress.Set(m, "cap/" + legacy, 0); // переехало — старую запись гасим
                }
            }
        }

        if (moved > 0)
        {
            Console.WriteLine($"Стили: потолков переехало на новые имена — {moved}.");
        }
    }

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonWeaponStyleValue", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            MahaonSpecializationPersistenceHelper.Write3(
                writer, StyleValue, (w, category) => w.Write((byte)category), (w, style) => w.Write(style)
            );
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version
            MahaonSpecializationPersistenceHelper.Read3(
                reader, StyleValue, r => (WeaponCategory)r.ReadByte(), r => r.ReadByte()
            );
        }
    }
}
