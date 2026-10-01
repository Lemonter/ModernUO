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
    private static readonly Dictionary<(Mobile, WeaponCategory, byte), double> StyleValue = new();

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

    /// <summary>Style is no longer picked directly — it's derived from whichever
    /// HitLocationSystem stance the player is currently holding ("стиль теперь идёт от
    /// места удара"). No favored-location match for the current stance (nothing chosen
    /// yet, aiming Head, or Tactics dropped below the zone's requirement) falls back to 0,
    /// the baseline style.</summary>
    public static byte GetActiveStyle(Mobile m, WeaponCategory category)
    {
        var location = HitLocationSystem.GetChosenLocation(m);

        if (location != null && HitLocationSystem.CanUseLocation(m, location.Value) &&
            StyleByFavoredLocation.TryGetValue((category, location.Value), out var style))
        {
            return style;
        }

        return 0;
    }

    public static double GetValue(Mobile m, WeaponCategory category, byte style)
    {
        // Style 0 ("Классический"/baseline) rides on the REAL underlying vanilla skill
        // for that category — same convention Wrestling already used (style 0 there
        // tracked the true SkillName.Wrestling value, not a separate counter).
        if (style == 0)
        {
            var skillName = category switch
            {
                WeaponCategory.Wrestling => SkillName.Wrestling,
                WeaponCategory.Swords    => SkillName.Swords,
                WeaponCategory.Macing    => SkillName.Macing,
                WeaponCategory.Fencing   => SkillName.Fencing,
                WeaponCategory.Archery   => SkillName.Archery,
                WeaponCategory.Throwing  => SkillName.Throwing,
                _                        => SkillName.Wrestling
            };

            return m.Skills[skillName].Value;
        }

        return StyleValue.GetValueOrDefault((m, category, style), 0.0);
    }

    /// <summary>Call right after a successful hit — see BaseWeapon.OnHit's call site.
    /// Trains whichever style favors the location the attacker actually aimed for this
    /// swing (via HitLocationSystem's called-shot, already resolved by ConsumeHitBonus
    /// earlier in OnHit) — NOT whichever style happens to be "active". Deliberately
    /// decoupled: "active" style only controls the flat BaseBonus/GetDamageBonus for now;
    /// training comes from where you actually chose to hit, per the shard owner's own
    /// design ("от выбора места удара качается стиль, связанный с ним"). An unaimed swing
    /// (no called shot) trains nothing here — the baseline style trains through the
    /// normal vanilla skill-check path instead.</summary>
    private const double GainChancePerDamage = 0.01;
    private const double MaxExtraGainChance = 0.5;

    public static void OnSuccessfulHit(Mobile attacker, Item weapon, int damage)
    {
        var category = CategoryOf(weapon);

        if (category == null)
        {
            return;
        }

        var location = HitLocationSystem.GetLastConsumedLocation(attacker);

        if (location == null || !StyleByFavoredLocation.TryGetValue((category.Value, location.Value), out var style))
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

    private static WeaponCategory? CategoryOf(Item weapon) =>
        weapon is BaseWeapon w ? GetCategory(w.DefSkill) : null;

    /// <summary>Added to BaseWeapon.OnHit's percentageBonus chain — covers every weapon
    /// category now, not just unarmed.</summary>
    public static int GetDamageBonus(Mobile attacker, Item weapon)
    {
        var category = CategoryOf(weapon);

        if (category == null)
        {
            return 0;
        }

        var style = GetActiveStyle(attacker, category.Value);
        var bonus = BaseBonus(category.Value, style);
        bonus += GetBreadthBonus(attacker, category.Value, style);

        return bonus;
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

    private static int BaseBonus(WeaponCategory category, byte style) => category switch
    {
        WeaponCategory.Wrestling => style switch
        {
            1 => 10,  // Бокс
            2 => 20,  // Муай-тай
            3 => 5,   // Карате
            4 => -5,  // Айкидо — makes it up via counter-attack
            5 => 14,  // Самбо
            6 => 17,  // Греко-римская борьба
            _ => 0    // Борьба (baseline)
        },
        WeaponCategory.Swords => style switch
        {
            1 => 15,  // Кендзюцу
            2 => 8,   // Дуэльный
            3 => 18,  // Берсерк
            4 => 12,  // Клинок тени
            5 => 16,  // Двуручный размах
            6 => 13,  // Стиль возмездия
            _ => 0    // Классический
        },
        WeaponCategory.Macing => style switch
        {
            1 => 16,  // Молот войны
            2 => 9,   // Цепной
            3 => 6,   // Оглушающий удар — value is in the disarm proc
            4 => 14,  // Рунный кузнец
            5 => 19,  // Палач
            6 => 15,  // Штурмовой таран
            _ => 0    // Классический
        },
        WeaponCategory.Fencing => style switch
        {
            1 => 13,  // Копейщик
            2 => 8,   // Ассасин — value is in the ambush bonus
            3 => 17,  // Пронзающий
            4 => 11,  // Вихрь клинков
            5 => 10,  // Дуэлянт-фехтовальщик
            6 => 14,  // Ночной клинок
            _ => 0    // Классический
        },
        WeaponCategory.Archery => style switch
        {
            1 => 6,   // Снайпер — value is in the distance bonus
            2 => 10,  // Шквальный огонь
            3 => 14,  // Следопыт
            4 => 12,  // Эльфийская стрельба
            5 => 15,  // Охотник за головами
            6 => 13,  // Тёмный лучник
            _ => 0    // Классический
        },
        WeaponCategory.Throwing => style switch
        {
            1 => 14,  // Меткий бросок
            2 => 9,   // Отравленные лезвия
            3 => 17,  // Шквал клинков
            4 => 19,  // Смертельный вихрь
            5 => 7,   // Дальнобойный бросок — value is in the distance bonus, like Archery's Sniper
            6 => 12,  // Теневой метатель
            _ => 0    // Классический
        },
        _ => 0
    };

    // Signature mechanics — one per category (matches Aikido's counter-attack from the
    // original martial-arts-only version). Checked from BaseWeapon.OnHit right after
    // damage is dealt — see the call site there.

    public static void OnAfterHit(Mobile attacker, Mobile defender, Item weapon)
    {
        var category = CategoryOf(weapon);

        if (category == null)
        {
            return;
        }

        switch (category.Value)
        {
            case WeaponCategory.Wrestling:
                TryWrestlingCounter(attacker, defender);
                break;
            case WeaponCategory.Macing:
                TryMacingDisarm(attacker, defender);
                break;
        }
    }

    private const double AikidoCounterChance = 0.20;

    private static void TryWrestlingCounter(Mobile attacker, Mobile defender)
    {
        if (defender.Weapon is not Fists || GetActiveStyle(defender, WeaponCategory.Wrestling) != 4)
        {
            return;
        }

        if (!defender.Alive || Utility.RandomDouble() > AikidoCounterChance)
        {
            return;
        }

        var damage = Utility.RandomMinMax(4, 10);
        attacker.Damage(damage, defender);
        defender.FixedParticles(0x3735, 1, 30, 0x26B8, EffectLayer.Waist);
        defender.PlaySound(0x23A);
        defender.PublicOverheadMessage(MessageType.Regular, 0x59, false, "Контрудар!");
    }

    private const double DisarmChance = 0.12;

    private static void TryMacingDisarm(Mobile attacker, Mobile defender)
    {
        if (attacker.Weapon is not BaseWeapon aw || GetCategory(aw.DefSkill) != WeaponCategory.Macing)
        {
            return;
        }

        if (GetActiveStyle(attacker, WeaponCategory.Macing) != 3) // Оглушающий удар
        {
            return;
        }

        if (defender.Weapon is not BaseWeapon || Utility.RandomDouble() > DisarmChance)
        {
            return;
        }

        var weapon = defender.FindItemOnLayer(Layer.OneHanded) ?? defender.FindItemOnLayer(Layer.TwoHanded);

        if (weapon == null || defender.Backpack == null || !defender.Backpack.TryDropItem(defender, weapon, false))
        {
            return;
        }

        attacker.PublicOverheadMessage(MessageType.Regular, 0x59, false, "Оглушающий удар выбивает оружие!");
        defender.PlaySound(0x3B9);
    }

    // Ассасин (Fencing style 2) — bonus vs a target this attacker hasn't hit before this
    // fight. Checked via BaseWeapon.GetDamageBonus chain isn't enough (needs per-pair
    // state), so this is a separate hook — see the call site in BaseWeapon.OnHit.
    //
    // "Before this fight" is approximated as a time window rather than a real combat-
    // start/end hook (none exists in this codebase to tie into) — a pair that hasn't
    // traded a hit in AmbushWindow is treated as a fresh engagement again. This used to be
    // a HashSet that only ever grew (Add, never Remove) — once any pair landed one hit,
    // the bonus was gone forever for that pair, not per fight, for the rest of the
    // server's uptime, and every pair that ever fought (including long-deleted monsters)
    // stayed in the set permanently.
    private const int AmbushWindowSeconds = 30;
    private static readonly Dictionary<(Mobile, Mobile), DateTime> LastAmbushHit = new();

    public static int GetAmbushBonus(Mobile attacker, Mobile defender, Item weapon)
    {
        if (weapon is not BaseWeapon w || GetCategory(w.DefSkill) != WeaponCategory.Fencing)
        {
            return 0;
        }

        if (GetActiveStyle(attacker, WeaponCategory.Fencing) != 2) // Ассасин
        {
            return 0;
        }

        var key = (attacker, defender);

        if (LastAmbushHit.TryGetValue(key, out var last) &&
            Core.Now - last < TimeSpan.FromSeconds(AmbushWindowSeconds))
        {
            return 0;
        }

        LastAmbushHit[key] = Core.Now;
        return 25; // first hit on a fresh target hits noticeably harder
    }

    // Снайпер (Archery style 1) — the further away the target was when the shot landed,
    // the bigger the bonus. Capped so point-blank archery isn't heavily penalized, just
    // not specially rewarded.
    public static int GetDistanceBonus(Mobile attacker, Mobile defender, Item weapon)
    {
        if (weapon is not BaseWeapon w)
        {
            return 0;
        }

        var category = GetCategory(w.DefSkill);

        if (category != WeaponCategory.Archery && category != WeaponCategory.Throwing)
        {
            return 0;
        }

        var sniperStyle = category == WeaponCategory.Archery ? 1 : 5; // Снайпер / Дальнобойный бросок

        if (GetActiveStyle(attacker, category.Value) != sniperStyle)
        {
            return 0;
        }

        var distance = attacker.GetDistanceToSqrt(defender);
        return (int)System.Math.Min(20, distance * 2.5);
    }

    // Location affinity — ties into the EXISTING called-shot system (HitLocationSystem)
    // rather than inventing a parallel mechanic. Each non-baseline style favors one body
    // slot thematically (Кендзюцу = decisive Neck strikes, Муай-тай = Leg kicks, etc).
    // Two effects, both keyed off HitLocationSystem.GetLastConsumedLocation (only ever
    // set when the attacker actually called a shot this swing — passive/unaimed hits get
    // neither bonus nor penalty from this):
    //   1. Attacking your OWN favored location = bonus (you're playing to your strength).
    //   2. Hitting a DEFENDER whose active style favors a DIFFERENT location than where
    //      you just hit them = extra bonus (their specialization left this zone weaker).
    // Baseline ("Классический"/style 0) styles have no favored location — balanced,
    // no bonus either way, matching their role as the safe all-rounder pick.
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

    private const int ExploitWeakZoneBonus = 10;

    // The old attacker-side "hit your own favored zone" bonus is gone — now that style IS
    // derived from the chosen location (see GetActiveStyle), the attacker's active style
    // trivially always favors wherever they're aiming, by construction. Keeping it would
    // just be a second copy of BaseBonus stacked on top of itself for free. The defender-
    // side check below is still real: it's about the DEFENDER's own stance, independent of
    // what the attacker chose.
    public static int GetLocationSynergyBonus(Mobile attacker, Mobile defender, Item weapon)
    {
        var location = HitLocationSystem.GetLastConsumedLocation(attacker);

        if (location == null)
        {
            return 0; // unaimed swing — no location synergy either way
        }

        var bonus = 0;

        var defenderCategory = defender.Weapon is Item defenderWeaponItem ? CategoryOf(defenderWeaponItem) : null;

        if (defenderCategory != null)
        {
            var defenderStyle = GetActiveStyle(defender, defenderCategory.Value);

            if (FavoredLocation.TryGetValue((defenderCategory.Value, defenderStyle), out var defenderFavored) &&
                defenderFavored != location)
            {
                bonus += ExploitWeakZoneBonus; // defender's specialization doesn't cover this zone
            }
        }

        return bonus;
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

        // Surfacing the favored zone right in the name — the location-synergy bonus above
        // is invisible/unplannable otherwise. Baseline styles (no entry in the map) show
        // with no suffix, matching their "balanced, no specialty" role.
        return FavoredLocation.TryGetValue((category, style), out var loc)
            ? $"{baseName} ({RuLocationName(loc)})"
            : baseName;
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
