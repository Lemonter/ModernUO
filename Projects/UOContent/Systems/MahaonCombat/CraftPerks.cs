using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;
using Server.Systems.MahaonMetals;

namespace Server.Systems.MahaonCombat;

public enum CraftPerkKind : byte
{
    MaterialSave, // N% шанс, что удачное изделие не потратит материалы
    Exceptional,  // +N к шансу исключительного качества (если навык и так его позволяет)
    Durability,   // изделие прочнее ещё на N%
    ExtraYield,   // N% шанс лишнего предмета или ресурса
    DoubleHaul    // N% шанс удвоить добычу целиком
}

public readonly record struct CraftPerk(int Threshold, CraftPerkKind Kind, int Value);

/// <summary>
///     Этап 8: перки ремесленных веток на 25 / 50 / 75 / 100 — как у школ магии.
///
///     ---- Что было ----
///
///     Ветки ремёсел давали плавную прибавку при успехе: прочность изделий (кузня, портной,
///     плотник, ювелир), лишнее зелье/свиток (алхимия, начертание), сытость (кулинария),
///     лишний ресурс (добыча). Это остаётся, ступени — сверху.
///
///     ---- Правило ----
///
///     Шанс изготовить — дело навыка. Перк «исключительное качество» прибавляет только там, где
///     навык и так позволяет исключительное (базовый шанс выше нуля): мастерство ветки не
///     открывает того, чего не умеет навык.
///
///     ---- Где работают ----
///
///     Только общие точки: CraftItem (качество при проверке, списание материалов, после
///     изготовления) и обработчики добычи руды, леса и рыбы.
/// </summary>
public static class CraftPerks
{
    private static CraftPerk P(int t, CraftPerkKind k, int v) => new(t, k, v);

    private static readonly CraftPerk[] Smith =
        [P(25, CraftPerkKind.MaterialSave, 10), P(50, CraftPerkKind.Exceptional, 5), P(75, CraftPerkKind.Durability, 10), P(100, CraftPerkKind.Exceptional, 10)];

    private static readonly CraftPerk[] Tailor =
        [P(25, CraftPerkKind.MaterialSave, 10), P(50, CraftPerkKind.Exceptional, 5), P(75, CraftPerkKind.Durability, 10), P(100, CraftPerkKind.MaterialSave, 15)];

    private static readonly CraftPerk[] Carpenter =
        [P(25, CraftPerkKind.MaterialSave, 15), P(50, CraftPerkKind.Exceptional, 5), P(75, CraftPerkKind.Durability, 10), P(100, CraftPerkKind.MaterialSave, 15)];

    private static readonly CraftPerk[] Tinker =
        [P(25, CraftPerkKind.MaterialSave, 10), P(50, CraftPerkKind.Exceptional, 5), P(75, CraftPerkKind.Durability, 15), P(100, CraftPerkKind.Exceptional, 10)];

    private static readonly CraftPerk[] Brewer =
        [P(25, CraftPerkKind.MaterialSave, 10), P(50, CraftPerkKind.ExtraYield, 10), P(75, CraftPerkKind.MaterialSave, 15), P(100, CraftPerkKind.ExtraYield, 15)];

    private static readonly CraftPerk[] Gatherer =
        [P(25, CraftPerkKind.ExtraYield, 10), P(50, CraftPerkKind.DoubleHaul, 5), P(75, CraftPerkKind.ExtraYield, 10), P(100, CraftPerkKind.DoubleHaul, 10)];

    /// <summary>Перки линии: «craft:blacksmith/Armor», «gather:mining/Обычный» и т. п.</summary>
    public static IReadOnlyList<CraftPerk> PerksOf(string line)
    {
        if (line == null)
        {
            return Array.Empty<CraftPerk>();
        }

        if (line.StartsWith("gather:", StringComparison.Ordinal))
        {
            return Gatherer;
        }

        return line switch
        {
            _ when line.StartsWith("craft:blacksmith/", StringComparison.Ordinal) => Smith,
            _ when line.StartsWith("craft:tailoring/", StringComparison.Ordinal)  => Tailor,
            "craft:carpentry"                                                    => Carpenter,
            "craft:tinkering"                                                    => Tinker,
            "craft:cooking" or "craft:alchemy" or "craft:inscription"            => Brewer,
            _                                                                    => Array.Empty<CraftPerk>()
        };
    }

    private static int Sum(string line, double value, CraftPerkKind kind)
    {
        var total = 0;

        foreach (var perk in PerksOf(line))
        {
            if (perk.Kind == kind && value >= perk.Threshold)
            {
                total += perk.Value;
            }
        }

        return total;
    }

    // ---- Какая ветка отвечает за это изделие ---------------------------------------------

    /// <summary>Линия и её значение у мастера для изделия этого типа в этом ремесле.</summary>
    public static (string Line, double Value) BranchFor(Mobile from, CraftSystem system, Type itemType)
    {
        if (from == null || system == null)
        {
            return (null, 0);
        }

        switch (system)
        {
            case DefBlacksmithy:
            {
                var spec = itemType == null ? (BlacksmithSpecialization?)null
                    : typeof(BaseShield).IsAssignableFrom(itemType) ? BlacksmithSpecialization.Shield
                    : typeof(BaseArmor).IsAssignableFrom(itemType) ? BlacksmithSpecialization.Armor
                    : typeof(BaseBashing).IsAssignableFrom(itemType) || typeof(BaseStaff).IsAssignableFrom(itemType)
                        ? BlacksmithSpecialization.Blunt
                    : typeof(BaseWeapon).IsAssignableFrom(itemType) ? BlacksmithSpecialization.Blade
                    : null;

                if (spec == null)
                {
                    return (null, 0); // гвозди, инструменты — не дело веток брони и оружия
                }

                return ($"craft:blacksmith/{spec.Value}", BlacksmithSpecializationSystem.GetValue(from, spec.Value));
            }

            case DefTailoring:
            {
                var spec = itemType == null ? (TailoringSpecialization?)null
                    : typeof(BaseArmor).IsAssignableFrom(itemType) ? TailoringSpecialization.LeatherArmorer
                    : typeof(BaseClothing).IsAssignableFrom(itemType) ? TailoringSpecialization.Clothier
                    : null;

                return spec == null ? (null, 0) : ($"craft:tailoring/{spec.Value}", TailoringSpecializationSystem.GetValue(from, spec.Value));
            }

            case DefCarpentry:   return ("craft:carpentry", CarpentrySpecializationSystem.GetValue(from));
            case DefTinkering:   return ("craft:tinkering", TinkeringSpecializationSystem.GetValue(from));
            case DefCooking:     return ("craft:cooking", CookingSpecializationSystem.GetValue(from));
            case DefAlchemy:     return ("craft:alchemy", AlchemySpecializationSystem.GetValue(from));
            case DefInscription: return ("craft:inscription", InscriptionSpecializationSystem.GetValue(from));
        }

        return (null, 0);
    }

    // ---- Общие точки крафта --------------------------------------------------------------

    /// <summary>Прибавка к шансу исключительного качества (0–1). Только если базовый шанс уже выше нуля.</summary>
    public static double ExceptionalBonus(Mobile from, CraftSystem system, Type itemType, double baseChance)
    {
        if (baseChance <= 0)
        {
            return 0;
        }

        var (line, value) = BranchFor(from, system, itemType);
        return Sum(line, value, CraftPerkKind.Exceptional) / 100.0;
    }

    /// <summary>Удачное изделие не тратит материалы — бросок перка «экономия».</summary>
    public static bool SavesMaterials(Mobile from, CraftSystem system, Type itemType)
    {
        var (line, value) = BranchFor(from, system, itemType);
        var chance = Sum(line, value, CraftPerkKind.MaterialSave);

        if (chance <= 0 || Utility.Random(100) >= chance)
        {
            return false;
        }

        from.SendMessage(0x59, "Мастерство сберегло материалы.");
        return true;
    }

    /// <summary>После изготовления: прочность и лишний предмет по перкам.</summary>
    public static void OnItemCrafted(Mobile from, Item item, CraftSystem system)
    {
        if (from == null || item == null)
        {
            return;
        }

        var (line, value) = BranchFor(from, system, item.GetType());

        var durability = Sum(line, value, CraftPerkKind.Durability);

        if (durability > 0)
        {
            var k = 1.0 + durability / 100.0;

            switch (item)
            {
                case BaseWeapon w when w.MaxHitPoints > 0:
                    w.MaxHitPoints = (int)(w.MaxHitPoints * k);
                    w.HitPoints = w.MaxHitPoints;
                    break;
                case BaseArmor a when a.MaxHitPoints > 0:
                    a.MaxHitPoints = (int)(a.MaxHitPoints * k);
                    a.HitPoints = a.MaxHitPoints;
                    break;
                case BaseClothing c when c.MaxHitPoints > 0:
                    c.MaxHitPoints = (int)(c.MaxHitPoints * k);
                    c.HitPoints = c.MaxHitPoints;
                    break;
                case BaseJewel j when j.MaxHitPoints > 0:
                    j.MaxHitPoints = (int)(j.MaxHitPoints * k);
                    j.HitPoints = j.MaxHitPoints;
                    break;
            }
        }

        var extra = Sum(line, value, CraftPerkKind.ExtraYield);

        if (extra > 0 && item.Stackable && Utility.Random(100) < extra)
        {
            item.Amount += 1;
            from.SendMessage(0x59, "Мастерство даёт ещё одну штуку.");
        }
    }

    // ---- Добыча --------------------------------------------------------------------------

    public static string MiningLine(MahaonMetalTier tier) => $"gather:mining/{tier}";

    public static string LumberLine(LumberjackingSpecialization spec) => $"gather:lumber/{spec}";

    public static string FishingLine(FishingSpecialization spec) => $"gather:fishing/{spec}";

    /// <summary>После добычи (руда, бревно, рыба): лишний ресурс и шанс удвоить добычу.</summary>
    public static void OnHarvested(Mobile from, string line, double value, Item resource)
    {
        if (from == null || resource == null || !resource.Stackable)
        {
            return;
        }

        var extra = Sum(line, value, CraftPerkKind.ExtraYield);

        if (extra > 0 && Utility.Random(100) < extra)
        {
            resource.Amount += 1;
        }

        var doubled = Sum(line, value, CraftPerkKind.DoubleHaul);

        if (doubled > 0 && Utility.Random(100) < doubled)
        {
            resource.Amount *= 2;
            from.SendMessage(0x59, "Удача добытчика: добыча удвоилась.");
        }
    }

    // ---- Описания, объявления, доска -----------------------------------------------------

    public static string Describe(CraftPerk perk) => perk.Kind switch
    {
        CraftPerkKind.MaterialSave => $"{perk.Value}% шанс не потратить материалы",
        CraftPerkKind.Exceptional  => $"+{perk.Value}% к шансу исключительного качества",
        CraftPerkKind.Durability   => $"изделия прочнее ещё на {perk.Value}%",
        CraftPerkKind.ExtraYield   => $"{perk.Value}% шанс лишней штуки",
        CraftPerkKind.DoubleHaul   => $"{perk.Value}% шанс удвоить добычу",
        _                          => ""
    };

    public static void AnnounceUnlock(Mobile m, string line, double before, double after)
    {
        if (m is not Mobiles.PlayerMobile)
        {
            return;
        }

        foreach (var perk in PerksOf(line))
        {
            if (before < perk.Threshold && after >= perk.Threshold)
            {
                m.SendMessage(WeaponMasterySystem.GainHue, $"Открыт перк ремесла ({perk.Threshold}): {Describe(perk)}.");
            }
        }
    }

    /// <summary>Линия ремесла по навыку и русскому имени ветки — для доски.</summary>
    public static string LineOf(int skillId, string branchName)
    {
        switch ((SkillName)skillId)
        {
            case SkillName.Blacksmith:
                foreach (var s in Enum.GetValues<BlacksmithSpecialization>())
                {
                    if (BlacksmithSpecializationSystem.RuSpecializationName(s) == branchName) return $"craft:blacksmith/{s}";
                }

                break;
            case SkillName.Tailoring:
                foreach (var s in Enum.GetValues<TailoringSpecialization>())
                {
                    if (TailoringSpecializationSystem.RuSpecializationName(s) == branchName) return $"craft:tailoring/{s}";
                }

                break;
            case SkillName.Carpentry when branchName == CarpentrySpecializationSystem.RuSpecializationName: return "craft:carpentry";
            case SkillName.Tinkering when branchName == TinkeringSpecializationSystem.RuSpecializationName: return "craft:tinkering";
            case SkillName.Cooking when branchName == CookingSpecializationSystem.RuSpecializationName:     return "craft:cooking";
            case SkillName.Alchemy when branchName == AlchemySpecializationSystem.RuSpecializationName:     return "craft:alchemy";
            case SkillName.Inscribe when branchName == InscriptionSpecializationSystem.RuSpecializationName: return "craft:inscription";
            case SkillName.Mining:
                foreach (var t in Enum.GetValues<MahaonMetalTier>())
                {
                    if (GatheringSpecializationSystem.RuMiningName(t) == branchName) return MiningLine(t);
                }

                break;
            case SkillName.Lumberjacking:
                foreach (var s in Enum.GetValues<LumberjackingSpecialization>())
                {
                    if (GatheringSpecializationSystem.RuLumberName(s) == branchName) return LumberLine(s);
                }

                break;
            case SkillName.Fishing:
                foreach (var s in Enum.GetValues<FishingSpecialization>())
                {
                    if (FishingSpecializationSystem.RuSpecializationName(s) == branchName) return FishingLine(s);
                }

                break;
        }

        return null;
    }
}
