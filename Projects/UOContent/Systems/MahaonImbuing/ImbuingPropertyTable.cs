using System;
using Server.Items;

namespace Server.Systems.MahaonImbuing;

public enum ImbuingItemType
{
    Weapon,
    Armor,
    Jewelry
}

/// <summary>One imbuable property, 3 intensity tiers (0=низкий, 1=средний, 2=высокий).
/// Apply sets the REAL engine property (AosAttributes/AosWeaponAttributes indexer for
/// weapon/jewelry properties, direct field for armor resist bonuses — different
/// mechanisms, hence the delegate rather than one universal setter) — no parallel
/// "fake" property system, this modifies exactly what vanilla item properties already
/// read from.</summary>
public sealed class ImbuingProperty
{
    public string Id;
    public string RuName;
    public ImbuingItemType[] AppliesTo;
    public int[] MinSkillPerTier;      // 3 entries
    public int[] MagnitudePerTier;     // 3 entries — the actual value applied
    public int[] GoldPerTier;          // 3 entries
    public (Type material, int amount)[][] MaterialsPerTier; // 3 entries, each a list
    public Action<Item, int> Apply;    // (item, magnitude for the chosen tier)

    /// <summary>
    ///     Свойство, которое умеет вкладывать только своя профессия. Пусто у всех, кроме
    ///     двух «магических печатей» на оружии — это сигнатурный перк Ремесла «Печать
    ///     мастера»: чары, позволяющие магу не расставаться с оружием, ставит только
    ///     ремесленник, и любой маг за ними идёт к нему.
    /// </summary>
    public Server.Systems.MahaonProfessions.ProfessionCategory? ExclusiveTo;

    public bool AppliesToItemType(ImbuingItemType type) => Array.IndexOf(AppliesTo, type) >= 0;
}

public static class ImbuingPropertyTable
{
    private static readonly ImbuingItemType[] WeaponOnly = { ImbuingItemType.Weapon };
    private static readonly ImbuingItemType[] ArmorOnly = { ImbuingItemType.Armor };
    private static readonly ImbuingItemType[] JewelryAndArmor = { ImbuingItemType.Jewelry, ImbuingItemType.Armor };
    private static readonly ImbuingItemType[] JewelryAndWeapon = { ImbuingItemType.Jewelry, ImbuingItemType.Weapon };

    // Материалы по ярусам — растущая стоимость: низкий ярус только на магическом
    // остатке, средний подмешивает осколки реликвии, высокий требует зачарованную
    // эссенцию (которая берётся только через расколдовывание — верхний ярус нельзя
    // просто нафармить с рядовых монстров).
    private static (Type, int)[] LowTierMats => new (Type, int)[] { (typeof(MagicalResidue), 5) };
    private static (Type, int)[] MidTierMats => new (Type, int)[] { (typeof(MagicalResidue), 5), (typeof(RelicFragment), 3) };
    private static (Type, int)[] HighTierMats => new (Type, int)[]
    {
        (typeof(RelicFragment), 4), (typeof(EnchantedEssence), 2)
    };

    private static (Type, int)[][] StandardTierMats => new[] { LowTierMats, MidTierMats, HighTierMats };

    public static readonly ImbuingProperty[] All =
    {
        new ImbuingProperty
        {
            Id = "weapon_damage",
            RuName = "Урон оружия",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 30, 60, 90 },
            MagnitudePerTier = new[] { 10, 20, 35 },
            GoldPerTier = new[] { 250, 600, 1400 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.Attributes[AosAttribute.WeaponDamage] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_leech_hits",
            RuName = "Похищение жизни при ударе",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 30, 60, 90 },
            MagnitudePerTier = new[] { 15, 30, 50 },
            GoldPerTier = new[] { 250, 600, 1400 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.HitLeechHits] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_leech_mana",
            RuName = "Похищение маны при ударе",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 40, 70, 100 },
            MagnitudePerTier = new[] { 15, 30, 50 },
            GoldPerTier = new[] { 300, 700, 1600 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.HitLeechMana] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_lightning",
            RuName = "Молния при ударе",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 40, 70, 100 },
            MagnitudePerTier = new[] { 15, 30, 50 },
            GoldPerTier = new[] { 300, 700, 1600 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.HitLightning] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_fireball",
            RuName = "Огненный шар при ударе",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 40, 70, 100 },
            MagnitudePerTier = new[] { 15, 30, 50 },
            GoldPerTier = new[] { 300, 700, 1600 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.HitFireball] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "resist_physical",
            RuName = "Сопротивление физическому",
            AppliesTo = ArmorOnly,
            MinSkillPerTier = new[] { 20, 50, 80 },
            MagnitudePerTier = new[] { 3, 6, 10 },
            GoldPerTier = new[] { 200, 500, 1200 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseArmor a)
                {
                    a.PhysicalBonus = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "resist_fire",
            RuName = "Сопротивление огню",
            AppliesTo = ArmorOnly,
            MinSkillPerTier = new[] { 20, 50, 80 },
            MagnitudePerTier = new[] { 3, 6, 10 },
            GoldPerTier = new[] { 200, 500, 1200 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseArmor a)
                {
                    a.FireBonus = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "regen_hits",
            RuName = "Восполнение здоровья",
            AppliesTo = ArmorOnly,
            MinSkillPerTier = new[] { 50, 80, 110 },
            MagnitudePerTier = new[] { 1, 2, 3 },
            GoldPerTier = new[] { 350, 800, 1800 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseArmor a)
                {
                    a.Attributes[AosAttribute.RegenHits] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "regen_mana",
            RuName = "Восполнение маны",
            AppliesTo = JewelryAndArmor,
            MinSkillPerTier = new[] { 50, 80, 110 },
            MagnitudePerTier = new[] { 1, 2, 3 },
            GoldPerTier = new[] { 350, 800, 1800 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.RegenMana] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "luck",
            RuName = "Удача",
            AppliesTo = JewelryAndArmor,
            MinSkillPerTier = new[] { 30, 60, 90 },
            MagnitudePerTier = new[] { 40, 70, 100 },
            GoldPerTier = new[] { 250, 600, 1400 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.Luck] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "cast_speed",
            RuName = "Ускоренное чтение заклинаний",
            AppliesTo = JewelryAndWeapon,
            MinSkillPerTier = new[] { 60, 90, 120 },
            MagnitudePerTier = new[] { 1, 2, 3 },
            GoldPerTier = new[] { 500, 1100, 2400 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.CastSpeed] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "cast_recovery",
            RuName = "Ускоренное восстановление после каста",
            AppliesTo = JewelryAndWeapon,
            MinSkillPerTier = new[] { 50, 80, 110 },
            MagnitudePerTier = new[] { 1, 2, 3 },
            GoldPerTier = new[] { 350, 800, 1800 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.CastRecovery] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "lower_stat_req_weapon",
            RuName = "Снижение требований к силе (оружие)",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 20, 45, 70 },
            MagnitudePerTier = new[] { 10, 20, 40 },
            GoldPerTier = new[] { 180, 400, 900 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.LowerStatReq] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "self_repair_weapon",
            RuName = "Самовосстановление (оружие)",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 45, 75, 105 },
            MagnitudePerTier = new[] { 1, 2, 3 },
            GoldPerTier = new[] { 300, 700, 1600 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.SelfRepair] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_lower_defend",
            RuName = "Снижение защиты при ударе",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 35, 65, 95 },
            MagnitudePerTier = new[] { 15, 30, 50 },
            GoldPerTier = new[] { 250, 600, 1400 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.HitLowerDefend] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "spell_damage",
            RuName = "Урон заклинаний",
            AppliesTo = JewelryAndWeapon,
            MinSkillPerTier = new[] { 55, 85, 115 },
            MagnitudePerTier = new[] { 8, 15, 25 },
            GoldPerTier = new[] { 400, 900, 2000 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.SpellDamage] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "resist_cold",
            RuName = "Сопротивление холоду",
            AppliesTo = ArmorOnly,
            MinSkillPerTier = new[] { 20, 50, 80 },
            MagnitudePerTier = new[] { 3, 6, 10 },
            GoldPerTier = new[] { 200, 500, 1200 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseArmor a)
                {
                    a.ColdBonus = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "resist_poison",
            RuName = "Сопротивление яду",
            AppliesTo = ArmorOnly,
            MinSkillPerTier = new[] { 20, 50, 80 },
            MagnitudePerTier = new[] { 3, 6, 10 },
            GoldPerTier = new[] { 200, 500, 1200 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseArmor a)
                {
                    a.PoisonBonus = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "resist_energy",
            RuName = "Сопротивление энергии",
            AppliesTo = ArmorOnly,
            MinSkillPerTier = new[] { 20, 50, 80 },
            MagnitudePerTier = new[] { 3, 6, 10 },
            GoldPerTier = new[] { 200, 500, 1200 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseArmor a)
                {
                    a.EnergyBonus = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "self_repair_armor",
            RuName = "Самовосстановление (броня)",
            AppliesTo = ArmorOnly,
            MinSkillPerTier = new[] { 45, 75, 105 },
            MagnitudePerTier = new[] { 1, 2, 3 },
            GoldPerTier = new[] { 300, 700, 1600 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseArmor a)
                {
                    a.ArmorAttributes[AosArmorAttribute.SelfRepair] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "regen_stam",
            RuName = "Восполнение выносливости",
            AppliesTo = JewelryAndArmor,
            MinSkillPerTier = new[] { 50, 80, 110 },
            MagnitudePerTier = new[] { 1, 2, 3 },
            GoldPerTier = new[] { 350, 800, 1800 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.RegenStam] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "lower_reg_cost",
            RuName = "Снижение расхода реагентов",
            AppliesTo = JewelryAndWeapon,
            MinSkillPerTier = new[] { 25, 55, 85 },
            MagnitudePerTier = new[] { 10, 20, 40 },
            GoldPerTier = new[] { 200, 500, 1200 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.LowerRegCost] = magnitude;
                }
            }
        },

        // -- Added later: rounding the table out toward real vanilla Imbuing's ~60-70
        // properties (was 22) — every Apply below writes into an AosAttribute/
        // AosWeaponAttribute/AosArmorAttribute member confirmed to actually exist and be
        // read by the engine (Projects/UOContent/Misc/AOS.cs, BaseWeapon.cs, BaseArmor.cs)
        // — not a parallel/fake system, same rule as everything above.

        new ImbuingProperty
        {
            Id = "defend_chance",
            RuName = "Увеличение шанса защиты",
            AppliesTo = JewelryAndArmor,
            MinSkillPerTier = new[] { 55, 85, 115 },
            MagnitudePerTier = new[] { 5, 10, 15 },
            GoldPerTier = new[] { 450, 1000, 2200 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.DefendChance] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_chance",
            RuName = "Увеличение шанса попадания",
            AppliesTo = JewelryAndWeapon,
            MinSkillPerTier = new[] { 55, 85, 115 },
            MagnitudePerTier = new[] { 5, 10, 15 },
            GoldPerTier = new[] { 450, 1000, 2200 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.AttackChance] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "bonus_str",
            RuName = "Бонус силы",
            AppliesTo = JewelryAndArmor,
            MinSkillPerTier = new[] { 25, 50, 80 },
            MagnitudePerTier = new[] { 2, 4, 7 },
            GoldPerTier = new[] { 200, 450, 1000 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.BonusStr] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "bonus_dex",
            RuName = "Бонус ловкости",
            AppliesTo = JewelryAndArmor,
            MinSkillPerTier = new[] { 25, 50, 80 },
            MagnitudePerTier = new[] { 2, 4, 7 },
            GoldPerTier = new[] { 200, 450, 1000 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.BonusDex] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "bonus_int",
            RuName = "Бонус интеллекта",
            AppliesTo = JewelryAndArmor,
            MinSkillPerTier = new[] { 25, 50, 80 },
            MagnitudePerTier = new[] { 2, 4, 7 },
            GoldPerTier = new[] { 200, 450, 1000 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.BonusInt] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "bonus_hits",
            RuName = "Увеличение здоровья",
            AppliesTo = JewelryAndArmor,
            MinSkillPerTier = new[] { 35, 65, 95 },
            MagnitudePerTier = new[] { 2, 4, 6 },
            GoldPerTier = new[] { 280, 600, 1300 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.BonusHits] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "bonus_stam",
            RuName = "Увеличение выносливости",
            AppliesTo = JewelryAndArmor,
            MinSkillPerTier = new[] { 35, 65, 95 },
            MagnitudePerTier = new[] { 2, 4, 6 },
            GoldPerTier = new[] { 280, 600, 1300 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.BonusStam] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "bonus_mana",
            RuName = "Увеличение маны",
            AppliesTo = JewelryAndArmor,
            MinSkillPerTier = new[] { 35, 65, 95 },
            MagnitudePerTier = new[] { 2, 4, 6 },
            GoldPerTier = new[] { 280, 600, 1300 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.BonusMana] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "weapon_speed",
            RuName = "Ускорение атаки",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 45, 75, 105 },
            MagnitudePerTier = new[] { 5, 10, 18 },
            GoldPerTier = new[] { 350, 800, 1800 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.Attributes[AosAttribute.WeaponSpeed] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "lower_mana_cost",
            RuName = "Снижение затрат маны",
            AppliesTo = JewelryAndWeapon,
            MinSkillPerTier = new[] { 50, 80, 110 },
            MagnitudePerTier = new[] { 3, 6, 10 },
            GoldPerTier = new[] { 400, 900, 2000 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.LowerManaCost] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "reflect_physical",
            RuName = "Отражение физического урона",
            AppliesTo = ArmorOnly,
            MinSkillPerTier = new[] { 40, 70, 100 },
            MagnitudePerTier = new[] { 3, 6, 10 },
            GoldPerTier = new[] { 300, 650, 1450 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseArmor a)
                {
                    a.Attributes[AosAttribute.ReflectPhysical] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "enhance_potions",
            RuName = "Улучшение зелий",
            AppliesTo = JewelryAndArmor,
            MinSkillPerTier = new[] { 25, 55, 85 },
            MagnitudePerTier = new[] { 8, 15, 25 },
            GoldPerTier = new[] { 220, 500, 1150 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.EnhancePotions] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "spell_channeling",
            ExclusiveTo = Server.Systems.MahaonProfessions.ProfessionCategory.Craft,
            RuName = "Чтение заклинаний без снятия предмета",
            AppliesTo = JewelryAndWeapon,
            MinSkillPerTier = new[] { 20, 40, 60 },
            MagnitudePerTier = new[] { 1, 1, 1 }, // toggle, not a scaling magnitude
            GoldPerTier = new[] { 150, 300, 600 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.SpellChanneling] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "night_sight",
            RuName = "Ночное зрение",
            AppliesTo = JewelryAndArmor,
            MinSkillPerTier = new[] { 20, 40, 60 },
            MagnitudePerTier = new[] { 1, 1, 1 }, // toggle, not a scaling magnitude
            GoldPerTier = new[] { 150, 300, 600 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is IAosItem aosItem)
                {
                    aosItem.Attributes[AosAttribute.NightSight] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_leech_stam",
            RuName = "Похищение выносливости при ударе",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 35, 65, 95 },
            MagnitudePerTier = new[] { 15, 30, 50 },
            GoldPerTier = new[] { 280, 650, 1500 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.HitLeechStam] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_dispel",
            RuName = "Развеивание при ударе",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 45, 75, 105 },
            MagnitudePerTier = new[] { 15, 30, 50 },
            GoldPerTier = new[] { 320, 750, 1700 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.HitDispel] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_harm",
            RuName = "Ранение (Harm) при ударе",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 25, 50, 80 },
            MagnitudePerTier = new[] { 20, 35, 55 },
            GoldPerTier = new[] { 180, 400, 900 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.HitHarm] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_magic_arrow",
            RuName = "Магическая стрела при ударе",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 25, 50, 80 },
            MagnitudePerTier = new[] { 20, 35, 55 },
            GoldPerTier = new[] { 180, 400, 900 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.HitMagicArrow] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_cold_area",
            RuName = "Ледяная волна при ударе",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 50, 80, 110 },
            MagnitudePerTier = new[] { 15, 25, 40 },
            GoldPerTier = new[] { 400, 900, 2000 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.HitColdArea] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_fire_area",
            RuName = "Огненная волна при ударе",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 50, 80, 110 },
            MagnitudePerTier = new[] { 15, 25, 40 },
            GoldPerTier = new[] { 400, 900, 2000 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.HitFireArea] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_poison_area",
            RuName = "Ядовитая волна при ударе",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 50, 80, 110 },
            MagnitudePerTier = new[] { 15, 25, 40 },
            GoldPerTier = new[] { 400, 900, 2000 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.HitPoisonArea] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "hit_energy_area",
            RuName = "Энергетическая волна при ударе",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 50, 80, 110 },
            MagnitudePerTier = new[] { 15, 25, 40 },
            GoldPerTier = new[] { 400, 900, 2000 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.HitEnergyArea] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "use_best_skill",
            RuName = "Бой лучшим боевым навыком",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 20, 40, 60 },
            MagnitudePerTier = new[] { 1, 1, 1 }, // toggle, not a scaling magnitude
            GoldPerTier = new[] { 150, 300, 600 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.UseBestSkill] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "mage_weapon",
            ExclusiveTo = Server.Systems.MahaonProfessions.ProfessionCategory.Craft,
            RuName = "Оружие мага",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 40, 75, 110 },
            MagnitudePerTier = new[] { 10, 20, 30 }, // 30 = no Magery penalty at all, see BaseWeapon.OnAdded
            GoldPerTier = new[] { 350, 800, 1800 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.MageWeapon] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "weapon_durability",
            RuName = "Прочность оружия",
            AppliesTo = WeaponOnly,
            MinSkillPerTier = new[] { 30, 60, 90 },
            MagnitudePerTier = new[] { 20, 35, 50 },
            GoldPerTier = new[] { 200, 450, 1000 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseWeapon w)
                {
                    w.WeaponAttributes[AosWeaponAttribute.DurabilityBonus] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "mage_armor",
            RuName = "Броня мага",
            AppliesTo = ArmorOnly,
            MinSkillPerTier = new[] { 20, 40, 60 },
            MagnitudePerTier = new[] { 1, 1, 1 }, // toggle, not a scaling magnitude
            GoldPerTier = new[] { 150, 300, 600 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseArmor a)
                {
                    a.ArmorAttributes[AosArmorAttribute.MageArmor] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "armor_durability",
            RuName = "Прочность брони",
            AppliesTo = ArmorOnly,
            MinSkillPerTier = new[] { 30, 60, 90 },
            MagnitudePerTier = new[] { 20, 35, 50 },
            GoldPerTier = new[] { 200, 450, 1000 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseArmor a)
                {
                    a.ArmorAttributes[AosArmorAttribute.DurabilityBonus] = magnitude;
                }
            }
        },
        new ImbuingProperty
        {
            Id = "lower_stat_req_armor",
            RuName = "Снижение требований к силе (броня)",
            AppliesTo = ArmorOnly,
            MinSkillPerTier = new[] { 20, 45, 70 },
            MagnitudePerTier = new[] { 10, 20, 40 },
            GoldPerTier = new[] { 180, 400, 900 },
            MaterialsPerTier = StandardTierMats,
            Apply = (item, magnitude) =>
            {
                if (item is BaseArmor a)
                {
                    a.ArmorAttributes[AosArmorAttribute.LowerStatReq] = magnitude;
                }
            }
        }
    };
}
